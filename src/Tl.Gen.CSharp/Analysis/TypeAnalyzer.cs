using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

internal sealed record BakeCandidate(
    BakeDeclaration Declaration,
    string Signature,
    bool OpenConsumerTypeParameter,
    IReadOnlyList<DeclarationDiagnostic> Diagnostics,
    DeclarationDiagnostic Site);

internal sealed record TypeAnalysis(
    string TypeName,
    DeclarationDiagnostic? ContractSite,
    IReadOnlyList<JobConsumer> Consumers,
    IReadOnlyList<DeclarationDiagnostic> JobDiagnostics,
    IReadOnlyList<DeclarationDiagnostic> BakeDiagnostics,
    IReadOnlyList<BakeCandidate> Bakes)
{
    public bool Equals(TypeAnalysis? other) => other is not null && Fingerprint == other.Fingerprint;
    public override int GetHashCode() => Fingerprint.GetHashCode();

    private string? _fingerprint;
    internal string Fingerprint => _fingerprint ??= Build();

    private string Build()
    {
        var builder = new StringBuilder(TypeName);
        Append(builder, ContractSite is null ? "" : Site(ContractSite));
        foreach (var consumer in Consumers)
        {
            Append(builder, consumer.TrackTypeName);
            Append(builder, consumer.ClipTypeName);
            Append(builder, consumer.Identity);
            Append(builder, consumer.Layout.ToString());
            Append(builder, string.Join("\0", consumer.LayoutTypes ?? []));
            Append(builder, consumer.Job.TypeName);
            Append(builder, $"{consumer.Job.Dispatch}\0{consumer.Job.LiveFrame}\0{consumer.Job.MemoMethod}");
            Append(builder, string.Join("\0", consumer.Job.Slots.Select(Slot)));
            Append(builder, string.Join("\0", consumer.Job.LiveColumns.Select(Slot)));
        }
        foreach (var diagnostic in JobDiagnostics.Concat(BakeDiagnostics))
            Append(builder, Diagnostic(diagnostic));
        foreach (var candidate in Bakes)
        {
            Append(builder, candidate.Declaration.TypeName);
            Append(builder, candidate.Declaration.ConsumerTypeName);
            Append(builder, string.Join("\0", candidate.Declaration.Parameters.Select(static parameter => $"{parameter.TypeName}\0{(byte)parameter.Modifier}\0{parameter.IsConsumer}")));
            Append(builder, candidate.Signature);
            Append(builder, candidate.OpenConsumerTypeParameter.ToString());
            Append(builder, Site(candidate.Site));
            foreach (var diagnostic in candidate.Diagnostics)
                Append(builder, Diagnostic(diagnostic));
        }
        return builder.ToString();

        static void Append(StringBuilder builder, string value) => builder.Append('\0').Append(value);
        static string Slot(TimelineSlot slot) => $"{slot.Name}\0{slot.TypeName}\0{(byte)slot.Mode}\0{slot.Size}\0{slot.Integral}";
        static string Diagnostic(DeclarationDiagnostic diagnostic) => $"{diagnostic.File}\0{diagnostic.Line}\0{diagnostic.Column}\0{diagnostic.Code}\0{diagnostic.Message}\0{diagnostic.SpanStart}\0{diagnostic.SpanLength}\0{diagnostic.EndLine}\0{diagnostic.EndColumn}";
        static string Site(DeclarationDiagnostic site) => $"{site.File}\0{site.Line}\0{site.Column}\0{site.SpanStart}\0{site.SpanLength}\0{site.EndLine}\0{site.EndColumn}";
    }
}

internal static class TypeAnalyzer
{
    private const int ActiveParameters = 30;
    private const int MemoResults = 10;

    internal static bool IsCandidate(TypeDeclarationSyntax syntax)
        => syntax.BaseList is not null || syntax.Identifier.ValueText == "ITrack";

    internal static TypeAnalysis? Analyze(SemanticModel model, TypeDeclarationSyntax syntax, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (model.GetDeclaredSymbol(syntax) is not { } type)
            return null;
        var compilation = model.Compilation;
        var name = Symbols.Name(type);
        if (Contract(compilation) is not { } contracts)
            return new TypeAnalysis(name, ContractSite(syntax), [], [], [], []);
        if (type.TypeKind == TypeKind.Interface)
            return null;
        var job = Job(type, compilation, contracts, syntax, name);
        var bake = Bake(type, compilation, syntax, name);
        return job is null && bake is null
            ? null
            : new TypeAnalysis(name, null, job?.Consumers ?? [], job?.Diagnostics ?? [], bake?.Diagnostics ?? [], bake?.Candidates ?? []);
    }

    private static DeclarationDiagnostic? ContractSite(TypeDeclarationSyntax syntax)
    {
        foreach (var token in syntax.DescendantTokens())
            if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "ITrack")
                return Symbols.Site(syntax);
        return null;
    }

    private static (IReadOnlyList<JobConsumer> Consumers, IReadOnlyList<DeclarationDiagnostic> Diagnostics)? Job(
        INamedTypeSymbol type,
        Compilation compilation,
        (INamedTypeSymbol Job, INamedTypeSymbol Frame, INamedTypeSymbol Blend) contracts,
        SyntaxNode syntax,
        string name)
    {
        var markers = type.AllInterfaces.Where(item => Symbols.Same(item.OriginalDefinition, contracts.Job)).ToArray();
        if (markers.Length == 0)
            return null;
        var errors = new List<DeclarationDiagnostic>();
        void Err(string msg) => Symbols.Error(errors, syntax, "TLGEN65", $"Job '{name}' {msg}");
        if (type.IsAbstract) { Err("cannot be abstract."); return ([], errors); }
        if (type.Arity > 0 || type.TypeParameters.Length > 0) { Err("cannot be generic; open type parameters cannot be registered as timeline jobs."); return ([], errors); }
        var identity = compilation.AssemblyName + "\0" + name;
        var discovered = new List<JobConsumer>();
        var valid = true;
        foreach (var marker in markers)
        {
            var track = marker.TypeArguments[0];
            var clip = marker.TypeArguments[1];
            if (track.TypeKind == TypeKind.TypeParameter || clip.TypeKind == TypeKind.TypeParameter) { Err("cannot have open type parameters for its track or clip pairing."); valid = false; continue; }
            var ok = true;
            if (!track.IsUnmanagedType) { Err($"track type '{Symbols.Name(track)}' must be an unmanaged type."); ok = false; }
            if (!clip.IsUnmanagedType) { Err($"clip type '{Symbols.Name(clip)}' must be an unmanaged type."); ok = false; }
            if (ok && track is INamedTypeSymbol { IsReadOnly: false }) { Err($"track type '{Symbols.Name(track)}' must be a readonly struct; a mutating Blend would write interned asset bytes."); ok = false; }
            if (ok && !track.AllInterfaces.Any(item => Symbols.Same(item.OriginalDefinition, contracts.Blend) && item.TypeArguments.Length == 1 && Symbols.Same(item.TypeArguments[0], clip))) { Err($"track type '{Symbols.Name(track)}' must implement Tl.IBlend<{Symbols.Name(clip)}>."); ok = false; }
            if (!ok) { valid = false; continue; }
            var definition = Definition(compilation, type, contracts.Frame.Construct(track, clip), syntax, errors);
            if (definition is null) { valid = false; continue; }
            discovered.Add(new(Symbols.Name(track), Symbols.Name(clip), definition, identity, PairLayout.Of(track, clip), PairLayout.NestedTypes(track, clip)));
        }
        if (!valid)
            return ([], errors);
        return (discovered.OrderBy(static item => item.TrackTypeName + "\0" + item.ClipTypeName, StringComparer.Ordinal).ToArray(), errors);
    }

    private static (IReadOnlyList<BakeCandidate> Candidates, IReadOnlyList<DeclarationDiagnostic> Diagnostics)? Bake(
        INamedTypeSymbol type,
        Compilation compilation,
        SyntaxNode syntax,
        string name)
    {
        var marker = compilation.GetTypeByMetadataName("Tl.IBake`1");
        if (marker is null)
            return null;
        var declared = type.AllInterfaces.Where(item => Symbols.Same(item.OriginalDefinition, marker))
            .OrderBy(static item => Symbols.Name(item.TypeArguments[0]), StringComparer.Ordinal).ToArray();
        if (declared.Length == 0)
            return null;
        var errors = new List<DeclarationDiagnostic>();
        var candidates = new List<BakeCandidate>();
        var site = Symbols.Site(syntax);
        void Err(string message) => Symbols.Error(errors, syntax, "TLGEN70", $"Bake '{name}' {message}");
        void Add(string consumer, string signature, IReadOnlyList<BakeParameter> parameters, List<DeclarationDiagnostic> diagnostics, bool open = false)
            => candidates.Add(new(new(name, consumer, parameters, []), signature, open, diagnostics, site));
        if (type.IsAbstract) { Err("cannot be abstract."); return (candidates, errors); }
        if (type.Arity > 0 || type.TypeParameters.Length > 0) { Err("cannot be generic; open type parameters cannot be registered as bakes."); return (candidates, errors); }
        if (type.TypeKind != TypeKind.Struct) { Err("must be a struct; bakes are declared on structs."); return (candidates, errors); }
        foreach (var instance in declared)
        {
            var consumer = instance.TypeArguments[0];
            if (consumer.TypeKind == TypeKind.TypeParameter)
            {
                var open = new List<DeclarationDiagnostic>();
                Symbols.Error(open, syntax, "TLGEN70", $"Bake '{name}' cannot have an open type parameter as its consumer type.");
                Add("", "", [], open, open: true);
                continue;
            }
            var local = new List<DeclarationDiagnostic>();
            if (!TryReadMethod(type, consumer, compilation, syntax, local, out var method)) { Add(Symbols.Name(consumer), "", [], local); continue; }
            var parameters = new List<BakeParameter>();
            var accessible = true;
            foreach (var parameter in method.Parameters)
            {
                if (parameter.Type.TypeKind == TypeKind.Error)
                {
                    Symbols.Error(local, Symbols.Site(parameter, syntax), "TLGEN89", $"Bake '{name}' parameter type '{Symbols.Name(parameter.Type)}' does not resolve in this compilation; fix the type name or add the missing assembly reference; the generated binding references it from generated source.");
                    accessible = false;
                    continue;
                }
                if (!compilation.IsSymbolAccessibleWithin(parameter.Type, compilation.Assembly))
                {
                    Symbols.Error(local, Symbols.Site(parameter, syntax), "TLGEN73", $"Bake '{name}' parameter type '{Symbols.Name(parameter.Type)}' must be accessible from this compilation; the generated binding references it from generated source.");
                    accessible = false;
                    continue;
                }
                parameters.Add(new(Symbols.Name(parameter.Type), parameter.RefKind switch
                {
                    RefKind.In => BakeModifier.In,
                    RefKind.Ref => BakeModifier.Ref,
                    _ => BakeModifier.Value,
                }, Symbols.Same(parameter.Type, consumer)));
            }
            if (!accessible) { Add(Symbols.Name(consumer), "", parameters, local); continue; }
            if (parameters.Count(static parameter => !parameter.IsConsumer) > 4) { Symbols.Error(local, syntax, "TLGEN87", $"Bake '{name}' declares more than four state parameters; Timeline.Bake dispatches at most four state arguments per call."); Add(Symbols.Name(consumer), "", parameters, local); continue; }
            if (parameters.Count(static parameter => parameter is { IsConsumer: false, Modifier: BakeModifier.Ref }) > 1) { Symbols.Error(local, syntax, "TLGEN88", $"Bake '{name}' declares more than one ref state parameter; Timeline.BakeRef exposes exactly one writable ref slot per call, so the remaining ref parameters could never bind. Pass the other parameters by value or in."); Add(Symbols.Name(consumer), "", parameters, local); continue; }
            Add(Symbols.Name(consumer), Signature(method), parameters, local);
        }
        return (candidates, errors);
    }

    private static JobDefinition? Definition(Compilation compilation, INamedTypeSymbol type, ITypeSymbol frame, SyntaxNode site, List<DeclarationDiagnostic> errors)
    {
        JobDefinition? Err(SyntaxNode node, string code, string message)
        {
            Symbols.Error(errors, node, code, message);
            return null;
        }
        var name = Symbols.Name(type);
        var frameName = Symbols.Name(frame);
        var memos = Candidates(compilation, type, "Fold", frame);
        var actives = Candidates(compilation, type, "ExecuteActive", frame);
        if (memos.Length > 1 || actives.Length > 1)
            return Err(site, "TLGEN66", $"'{name}' declares multiple Fold or ExecuteActive.");
        var memo = memos.Length == 1 ? memos[0] : null;
        var active = actives.Length == 1 ? actives[0] : null;
        if (memo is null && active is null)
        {
            var hosted = compilation.GetTypeByMetadataName("Tl.IHostedConsumer");
            if (hosted is not null && type.AllInterfaces.Any(item => Symbols.Same(item, hosted)))
                return null;
            return Err(site, "TLGEN66", $"'{name}' needs one static void Fold(in {frameName}) or ExecuteActive; a family whose hook a host synthesizes must implement Tl.IHostedConsumer.");
        }

        var slots = new List<TimelineSlot>();
        if (memo is not null)
        {
            if (!Framed(memo, frame))
                return Err(Symbols.Site(memo, site), "TLGEN75", $"'{name}.Fold' must begin with in {frameName}.");
            foreach (var p in memo.Parameters.Skip(1))
            {
                if (p.IsOptional || p.IsParams || !p.Type.IsUnmanagedType || p.RefKind is not (RefKind.Ref or RefKind.Out))
                    return Err(Symbols.Site(p, site), "TLGEN76", $"'{name}.Fold' runs at fold; only unmanaged 'out'/'ref' results exist — 'in' has no caller.");
                var typeName = Symbols.Name(p.Type);
                var size = ResultSize(p.Type);
                if (size == 0 || slots.Count >= MemoResults)
                    return Err(Symbols.Site(p, site), "TLGEN79", $"'{name}.Fold' folds at most {MemoResults} unmanaged results of at most 8 bytes each ('{Symbols.Name(p.Type)} {p.Name}' is not one).");
                slots.Add(new(p.Name, typeName, p.RefKind == RefKind.Out ? SlotMode.Output : SlotMode.Reference, size, Integral(p.Type)));
            }
            if (slots.Count == 0)
                return Err(Symbols.Site(memo, site), "TLGEN76", $"'{name}.Fold' produces no result; dispatch-only is ExecuteActive.");
        }

        var dispatch = false;
        var liveFrame = false;
        List<TimelineSlot>? live = null;
        if (active is not null)
        {
            var framed = Framed(active, frame);
            var start = framed ? 1 : 0;
            var gameplay = active.Parameters.Length - start;
            var p1 = framed && active.Parameters.Length > 1 ? active.Parameters[1] : null;
            var sugar = p1 is { RefKind: RefKind.Ref, Type.IsUnmanagedType: true, IsOptional: false, IsParams: false }
                && memo is null && gameplay == 1;
            if (sugar)
            {
                var only = active.Parameters[1];
                if (!Symbols.Same(only.Type, compilation.GetSpecialType(SpecialType.System_Single)))
                    return Err(Symbols.Site(only, site), "TLGEN88", $"'{name}.ExecuteActive' sugar state must be 'ref float'; a keyless consumer binds one pooled float lane, so declare other state types as Fold results.");
                slots.Add(new(only.Name, Symbols.Name(only.Type), SlotMode.Reference));
            }
            else if (gameplay == 0)
            {
                dispatch = true;
                liveFrame = framed;
            }
            else
            {
                dispatch = true;
                liveFrame = framed;
                live = [];
                var outs = slots.Count(static slot => slot.Mode == SlotMode.Output);
                var feed = 0;
                var index = start;
                if (memo is not null)
                    while (index < active.Parameters.Length && feed < outs && active.Parameters[index].RefKind is RefKind.In
                        && active.Parameters[index].Type.IsUnmanagedType && !active.Parameters[index].IsOptional && !active.Parameters[index].IsParams
                        && Symbols.Name(active.Parameters[index].Type) == ResultName(feed))
                    {
                        var fed = active.Parameters[index++];
                        live.Add(new(fed.Name, Symbols.Name(fed.Type), SlotMode.MemoFeed, (byte)ResultSize(fed.Type)));
                        feed++;
                    }
                for (; index < active.Parameters.Length; index++)
                {
                    var p = active.Parameters[index];
                    if (p.IsOptional || p.IsParams || !p.Type.IsUnmanagedType || p.RefKind is not (RefKind.In or RefKind.Ref))
                        return Err(Symbols.Site(p, site), "TLGEN78", $"'{name}.ExecuteActive' columns must be required unmanaged 'in'/'ref'; '{Symbols.Name(p.Type)} {p.Name}' is not.");
                    live.Add(new(p.Name, Symbols.Name(p.Type), p.RefKind == RefKind.Ref ? SlotMode.Reference : SlotMode.Input));
                }
                if (live.Count > ActiveParameters)
                    return Err(Symbols.Site(active.Parameters[start + ActiveParameters], site), "TLGEN68", $"'{name}.ExecuteActive' declares {gameplay} gameplay parameters; the consumer ABI holds {ActiveParameters} gameplay columns per registered consumer (memo feeds included); declare at most {ActiveParameters}.");
                for (var i = 0; i < live.Count; i++)
                    for (var k = 0; k < i; k++)
                        if (live[i].Mode == live[k].Mode && live[i].Mode != SlotMode.MemoFeed && live[i].TypeName == live[k].TypeName)
                        {
                            var kind = live[i].Mode == SlotMode.Input ? "in" : "ref";
                            return Err(Symbols.Site(active.Parameters[start + i], site), "TLGEN81", $"'{name}.ExecuteActive' declares two live '{kind} {live[i].TypeName}' columns ('{live[k].Name}', '{live[i].Name}'); TypeKey binding cannot distinguish same-type columns — give them distinct types.");
                        }
            }
        }

        return new(name, slots, dispatch, liveFrame, memo is not null, live);

        string ResultName(int ordinal)
            => slots.Where(static slot => slot.Mode == SlotMode.Output).Select(static slot => slot.TypeName).ElementAtOrDefault(ordinal) ?? "";
    }

    private static byte ResultSize(ITypeSymbol t)
        => t switch
        {
            INamedTypeSymbol { EnumUnderlyingType: { } u } => ResultSize(u),
            { SpecialType: SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte } => 1,
            { SpecialType: SpecialType.System_Char or SpecialType.System_Int16 or SpecialType.System_UInt16 } => 2,
            { SpecialType: SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single } => 4,
            { SpecialType: SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Double } => 8,
            _ => 0,
        };

    private static bool Integral(ITypeSymbol t)
        => t.SpecialType is not (SpecialType.System_Single or SpecialType.System_Double);

    private static bool Framed(IMethodSymbol method, ITypeSymbol frame)
        => method.Parameters.Length > 0 && method.Parameters[0].RefKind == RefKind.In && Symbols.Same(method.Parameters[0].Type, frame);

    private static IMethodSymbol[] Candidates(Compilation compilation, INamedTypeSymbol type, string name, ITypeSymbol frame)
        => type.GetMembers(name).OfType<IMethodSymbol>().Where(m => m is { IsImplicitlyDeclared: false, IsStatic: true, ReturnsVoid: true, Arity: 0 }
            && compilation.IsSymbolAccessibleWithin(m, compilation.Assembly)
            && (m.Parameters.Length == 0
                || !Symbols.Same(m.Parameters[0].Type.OriginalDefinition, frame.OriginalDefinition)
                || Symbols.Same(m.Parameters[0].Type, frame))).ToArray();

    private static bool TryReadMethod(INamedTypeSymbol type, ITypeSymbol consumer, Compilation compilation, SyntaxNode site, List<DeclarationDiagnostic> errors, out IMethodSymbol method)
    {
        var candidates = type.GetMembers("Bake").OfType<IMethodSymbol>().Where(candidate => candidate is { IsImplicitlyDeclared: false, IsStatic: true, ReturnsVoid: true, Arity: 0, MethodKind: MethodKind.Ordinary }
            && compilation.IsSymbolAccessibleWithin(candidate, compilation.Assembly)
            && candidate.Parameters.All(static parameter => parameter.RefKind != RefKind.Out && parameter is { IsOptional: false, IsParams: false })).ToArray();
        if (candidates.Length == 1)
        {
            method = candidates[0];
            return true;
        }
        Symbols.Error(errors, site, "TLGEN71", $"Bake '{Symbols.Name(type)}' must declare exactly one accessible static void Bake; every parameter may be by value, in, or ref of any type (including Span<T> and ReadOnlySpan<T>), and a parameter whose type is exactly the consumer type '{Symbols.Name(consumer)}' is bound to default(TConsumer), because consumers are static and the parameter participates only in the per-pair bake signature match; out, optional, and params parameters are unsupported.");
        method = null!;
        return false;
    }

    private static string Signature(IMethodSymbol method)
        => $"{Symbols.Name(method.ContainingType)}.Bake({string.Join(", ", method.Parameters.Select(Format))})";

    private static string Format(IParameterSymbol parameter)
    {
        var modifier = parameter.RefKind == RefKind.In ? "in " : parameter.RefKind == RefKind.Ref ? "ref " : "";
        var name = string.IsNullOrEmpty(parameter.Name) ? "" : " " + parameter.Name;
        return modifier + Symbols.Name(parameter.Type) + name;
    }

    private static (INamedTypeSymbol Job, INamedTypeSymbol Frame, INamedTypeSymbol Blend)? Contract(Compilation compilation)
    {
        INamedTypeSymbol?[] types = [compilation.GetTypeByMetadataName("Tl.ITrack`2"), compilation.GetTypeByMetadataName("Tl.Frame`2"), compilation.GetTypeByMetadataName("Tl.IBlend`1")];
        return types.Any(static type => type is null) ? null : (types[0]!, types[1]!, types[2]!);
    }
}
