using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

internal static partial class SystemReader
{
    static SystemTypeModel Mk(INamedTypeSymbol type, SyntaxNode node, Compilation? compilation, SystemModelKind kind)
        => new(Symbols.Name(type), kind, compilation?.AssemblyName ?? "", "", node);
    static SystemTypeModel Stamp(SystemTypeModel model) => model with { Fingerprint = SystemDigest.Of(model) };

    internal static SystemTypeModel? KindOf(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (context.SemanticModel.Compilation is not CSharpCompilation compilation) return null;
        if (context.TargetSymbol is not INamedTypeSymbol type) return null;
        if (context.TargetNode is not TypeDeclarationSyntax node) return null;
        var contracts = Contracts(compilation);
        var kindContracts = KindContractsOf(compilation);
        if (contracts is null || kindContracts is null) return null;
        var diagnostics = new List<DeclarationDiagnostic>();
        var kind = ReadKind(type, node, kindContracts, contracts, diagnostics);
        var applyPlain = new List<bool>(); var idlePlain = new List<bool>();
        if (kind is not null)
        {
            var apply = ApplyLike(type, "Apply"); var idle = ApplyLike(type, "Idle");
            if (apply is not null) for (var i = 1; i < apply.Parameters.Length; i++) applyPlain.Add(SystemGeneration.IsPlainState(apply.Parameters[i].Type));
            if (idle is not null) foreach (var p in idle.Parameters) idlePlain.Add(SystemGeneration.IsPlainState(p.Type));
        }
        return Stamp(Mk(type, node, compilation, SystemModelKind.Kind)) with { KindDecl = kind, Diagnostics = diagnostics, ApplyPlain = applyPlain, IdlePlain = idlePlain };
    }

    internal static SystemTypeModel? SystemOf(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
        => TypeOf(context, cancellation, SystemModelKind.System, state: false);
    internal static SystemTypeModel? StateOf(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
        => TypeOf(context, cancellation, SystemModelKind.State, state: true);
    static SystemTypeModel? TypeOf(GeneratorAttributeSyntaxContext context, CancellationToken cancellation, SystemModelKind kind, bool state)
    {
        cancellation.ThrowIfCancellationRequested();
        if (context.TargetSymbol is not INamedTypeSymbol type) return null;
        if (context.TargetNode is not TypeDeclarationSyntax node) return null;
        var diagnostics = new List<DeclarationDiagnostic>();
        var system = state ? null : ReadSystem(type, node, diagnostics);
        if (state && type.TypeKind != TypeKind.Struct) return null;
        return Stamp(Mk(type, node, context.SemanticModel.Compilation, kind)) with { System = system, Diagnostics = diagnostics };
    }

    static IMethodSymbol? ApplyLike(INamedTypeSymbol type, string name) => type.GetMembers(name).OfType<IMethodSymbol>().FirstOrDefault(static m => m.IsStatic && m.ReturnsVoid && m.Arity == 0);

    internal static SystemTypeModel? FeederOf(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (context.SemanticModel.Compilation is not CSharpCompilation compilation) return null;
        if (context.TargetSymbol is not IMethodSymbol method) return null;
        if (context.TargetNode is not MethodDeclarationSyntax node) return null;
        var type = method.ContainingType;
        if (type is null) return null;
        var contracts = Contracts(compilation);
        var kindContracts = KindContractsOf(compilation);
        if (contracts is null || kindContracts is null) return null;
        ITypeSymbol? track = null, clip = null, marker = null;
        foreach (var face in type.AllInterfaces)
        {
            if (Symbols.Same(face.OriginalDefinition, contracts.Track)) { track = face.TypeArguments[0]; clip = face.TypeArguments[1]; }
            if (Symbols.Same(face.OriginalDefinition, contracts.Marker)) marker = face.TypeArguments[0];
        }
        if (track is null && marker is null) return null;
        var foldTypes = new List<string>();
        if (track is not null) _ = ReadFold(type, node, contracts, track, clip!, new List<DeclarationDiagnostic>(), foldTypes, new List<int>());
        var diagnostics = new List<DeclarationDiagnostic>();
        var foreign = new Dictionary<string, (INamedTypeSymbol Type, SyntaxNode Site)>(StringComparer.Ordinal);
        var feeders = ReadFeeders(type, node, contracts, kindContracts, track, clip, marker, foldTypes, diagnostics, compilation.Assembly, foreign);
        var foreignKinds = new List<KindDecl>();
        foreach (var entry in foreign)
            if (ReadForeignKind(entry.Value.Type, entry.Value.Site, diagnostics) is { } known) foreignKinds.Add(known);
        var facts = new List<string>();
        foreach (var p in method.Parameters)
            facts.Add(Symbols.Name(p.Type) + "\0" + (SystemGeneration.IsPlainState(p.Type) ? "1" : "0") + "\0" + (p.RefKind == RefKind.Ref ? "1" : "0"));
        var kindType = ""; var kindForeign = false;
        foreach (var attribute in method.GetAttributes())
            if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is INamedTypeSymbol fed)
            {
                kindType = Symbols.Name(fed);
                kindForeign = NewStyle.IsForeignKind(fed, compilation.Assembly);
            }
        facts.Insert(0, kindType + "\0" + (kindForeign ? "1" : "0"));
        return Stamp(Mk(type, node, compilation, SystemModelKind.Feeder)) with { Diagnostics = diagnostics, Feeders = feeders, Foreign = foreignKinds, Facts = facts };
    }

    internal static SystemTypeModel? TrackOf(GeneratorSyntaxContext context, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (context.SemanticModel.Compilation is not CSharpCompilation compilation) return null;
        if (context.Node is not TypeDeclarationSyntax node) return null;
        if (context.SemanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol type) return null;
        if (type.AllInterfaces.Any(static i => i.OriginalDefinition is { MetadataName: "ITlModule", ContainingNamespace: { Name: "Tl" } }))
            return Stamp(Mk(type, node, compilation, SystemModelKind.Module));
        var consumer = ReadConsumerFor(compilation, type, node);
        if (consumer is null) return null;
        var contracts = Contracts(compilation)!;
        var isTlTrack = type.AllInterfaces.Any(static i => i.OriginalDefinition is { MetadataName: "ITlTrack`1", ContainingNamespace: { Name: "Tl", ContainingNamespace.IsGlobalNamespace: true } });
        var events = isTlTrack ? NewStyle.EventElements(type, contracts.Events) : null;
        return Stamp(Mk(type, node, compilation, SystemModelKind.Consumer)) with { Consumer = consumer, Diagnostics = consumer.Diagnostics, Events = events };
    }

    static SystemConsumer? ReadConsumerFor(CSharpCompilation compilation, INamedTypeSymbol type, SyntaxNode node)
    {
        var contracts = Contracts(compilation);
        if (contracts is null || !TypeAnalyzer.IsNewStyle(type)) return null;
        var diagnostics = new List<DeclarationDiagnostic>();
        var foreign = new Dictionary<string, (INamedTypeSymbol Type, SyntaxNode Site)>(StringComparer.Ordinal);
        return ReadConsumer(type, node, contracts, KindContractsOf(compilation), compilation, diagnostics, foreign, collectFeeders: false);
    }

    internal static (IReadOnlyList<SystemAssignment> Assignments, IReadOnlyList<DeclarationDiagnostic> Diagnostics, string AutoSource) AssignModels(IReadOnlyList<SystemTypeModel> models, string id)
    {
        var diagnostics = new List<DeclarationDiagnostic>();
        foreach (var model in models) diagnostics.AddRange(model.Diagnostics ?? []);
        var (auto, members, source) = AutoOf(models, id);
        var systems = new List<SystemDecl>(); var consumers = new List<SystemConsumer>(); var kinds = new List<KindDecl>();
        var sites = new Dictionary<string, SyntaxNode>(StringComparer.Ordinal); var kindSites = new Dictionary<string, SyntaxNode>(StringComparer.Ordinal);
        var feeders = new Dictionary<string, List<FeederDecl>>(StringComparer.Ordinal);
        if (auto is not null) systems.Add(auto);
        foreach (var model in models)
            switch (model.Kind)
            {
                case SystemModelKind.System when model.System is not null: systems.Add(model.System); break;
                case SystemModelKind.Kind when model.KindDecl is not null:
                    kinds.Add(model.KindDecl); if (model.Site is not null) kindSites[model.KindDecl.TypeName] = model.Site; break;
                case SystemModelKind.Feeder when model.Feeders is not null:
                    if (!feeders.TryGetValue(model.TypeName, out var owned)) { owned = []; feeders[model.TypeName] = owned; }
                    owned.AddRange(model.Feeders);
                    if (model.Foreign is not null) foreach (var known in model.Foreign) { kinds.Add(known); if (model.Site is not null) kindSites[known.TypeName] = model.Site; }
                    break;
                case SystemModelKind.Consumer when model.Consumer is not null:
                    var consumer = model.Consumer;
                    if (auto is not null && members.Contains(consumer.ConsumerTypeName)) consumer = consumer with { MemberSystems = [auto.TypeName] };
                    consumers.Add(consumer); if (model.Site is not null) sites[consumer.ConsumerTypeName] = model.Site; break;
            }
        consumers.Sort(static (a, b) => string.CompareOrdinal(a.ConsumerTypeName, b.ConsumerTypeName));
        var withFeeders = consumers.Select(consumer => feeders.TryGetValue(consumer.ConsumerTypeName, out var owned) ? consumer with { Feeders = owned } : consumer).ToList();
        var assignments = Assign(systems, withFeeders, sites, diagnostics);
        var result = kinds.Count == 0 && !withFeeders.Any(static c => c.Feeders.Count > 0) ? assignments : AssignKinds(systems, assignments, kinds, kindSites, sites, diagnostics);
        return (result, diagnostics, source);
    }

    static (SystemDecl? AutoSystem, ISet<string> Members, string AutoSource) AutoOf(IReadOnlyList<SystemTypeModel> models, string id)
    {
        var systemColumns = new HashSet<string>(StringComparer.Ordinal); var systemEvents = new HashSet<string>(StringComparer.Ordinal);
        foreach (var model in models)
            if (model.Kind == SystemModelKind.System && model.System is not null)
                foreach (var field in model.System.Fields)
                {
                    if (field.Kind is SystemFieldKind.Column or SystemFieldKind.ColumnReadOnly) systemColumns.Add(field.ElementTypeName);
                    if (field.Kind == SystemFieldKind.Events) systemEvents.Add(field.ElementTypeName);
                }
        var autoKinds = new HashSet<string>(StringComparer.Ordinal); var columns = new List<(string type, bool writable)>();
        foreach (var model in models)
        {
            if (model.Kind != SystemModelKind.Kind || model.KindDecl is null) continue;
            var apply = model.KindDecl.ApplyParams;
            if (apply.Count == 0) continue;
            var ok = true;
            for (var i = 0; i < apply.Count; i++)
                if (model.ApplyPlain is null || i >= model.ApplyPlain.Count || !model.ApplyPlain[i] || systemColumns.Contains(apply[i].TypeName)) { ok = false; break; }
            if (!ok) continue;
            autoKinds.Add(model.KindDecl.TypeName);
            foreach (var p in apply) NewStyle.AddColumn(columns, p.TypeName, p.Modifier == "ref ");
            if (model.KindDecl.IdleParams is not null)
                for (var i = 0; i < model.KindDecl.IdleParams.Count; i++)
                    if (model.IdlePlain is not null && i < model.IdlePlain.Count && model.IdlePlain[i] && !systemColumns.Contains(model.KindDecl.IdleParams[i].TypeName))
                        NewStyle.AddColumn(columns, model.KindDecl.IdleParams[i].TypeName, model.KindDecl.IdleParams[i].Modifier == "ref ");
        }
        var feedsForeign = false; var members = new HashSet<string>(StringComparer.Ordinal);
        foreach (var model in models)
        {
            if (model.Kind != SystemModelKind.Feeder || model.Facts is not { Count: > 0 }) continue;
            var meta = model.Facts[0].Split('\0');
            if (meta[1] == "0" && !autoKinds.Contains(meta[0])) continue;
            feedsForeign |= meta[1] == "1";
            members.Add(model.TypeName);
            for (var i = 1; i < model.Facts.Count; i++)
            {
                var parts = model.Facts[i].Split('\0');
                if (parts.Length == 3 && parts[1] == "1") NewStyle.AddColumn(columns, parts[0], parts[2] == "1");
            }
        }
        var autoEvents = new List<string>(); var eventFields = new List<NewStyleColumn>();
        foreach (var model in models)
        {
            if (model.Kind != SystemModelKind.Consumer || model.Events is not { Count: > 0 }) continue;
            if (!members.Contains(model.TypeName) && model.Consumer is { MemberSystems.Count: > 0 } || model.Events.Any(systemEvents.Contains)) continue;
            members.Add(model.TypeName);
            foreach (var e in model.Events) if (!autoEvents.Contains(e)) autoEvents.Add(e);
        }
        if (columns.Count == 0 && !feedsForeign && autoEvents.Count == 0) return (null, members, "");
        var autoName = NewStyle.AutoNameOf(id);
        var text = new StringBuilder();
        text.Append("namespace Tl.Generated\n{\n[global::Tl.TimelineSystem]\npublic ref partial struct ").Append(autoName).Append("\n{\n");
        var autoColumns = new List<NewStyleColumn>();
        for (var i = 0; i < columns.Count; i++)
        {
            var name = "C" + i + "_" + NewStyle.Simple(columns[i].type);
            text.Append("public global::System.").Append(columns[i].writable ? "Span<" : "ReadOnlySpan<").Append(columns[i].type).Append("> ").Append(name).Append(";\n");
            autoColumns.Add(new(name, columns[i].type, columns[i].writable));
        }
        for (var i = 0; i < autoEvents.Count; i++)
        {
            var name = "E" + i + "_" + NewStyle.Simple(autoEvents[i]);
            text.Append("public global::Tl.Events<").Append(autoEvents[i]).Append("> ").Append(name).Append(";\n");
            eventFields.Add(new(name, autoEvents[i], true));
        }
        text.Append("}\n}\n");
        var fields = new List<SystemField>();
        foreach (var column in autoColumns)
            fields.Add(new(column.Name, column.Writable ? "global::System.Span<" + column.TypeName + ">" : "global::System.ReadOnlySpan<" + column.TypeName + ">",
                column.Writable ? SystemFieldKind.Column : SystemFieldKind.ColumnReadOnly, column.TypeName));
        foreach (var property in eventFields)
            fields.Add(new(property.Name, "global::Tl.Events<" + property.TypeName + ">", SystemFieldKind.Events, property.TypeName));
        return (new("global::Tl.Generated." + autoName, true, StableKey("Tl.Generated." + autoName), fields), members, text.ToString());
    }
}
