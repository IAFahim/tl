using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tl.Gen.CSharp.Analysis;

internal sealed record NewStyleColumn(string Name, string TypeName, bool Writable);

internal sealed class NewStyleAnalysis
{
    public NewStyleAnalysis(CSharpCompilation compilation, string autoName, IReadOnlyList<NewStyleColumn> autoColumns, string autoSource, IReadOnlyDictionary<string, int> inline, ISet<string> tlFrameFeeders, IReadOnlyList<INamedTypeSymbol> states)
    {
        Compilation = compilation;
        AutoName = autoName;
        AutoColumns = autoColumns;
        AutoSource = autoSource;
        Inline = inline;
        TlFrameFeeders = tlFrameFeeders;
        States = states;
    }

    public readonly CSharpCompilation Compilation;
    public readonly string AutoName;
    public readonly IReadOnlyList<NewStyleColumn> AutoColumns;
    public readonly string AutoSource;
    public readonly IReadOnlyDictionary<string, int> Inline;
    public readonly ISet<string> TlFrameFeeders;
    public readonly IReadOnlyList<INamedTypeSymbol> States;
    public IReadOnlyList<NewStyleColumn> AutoEvents { get; init; } = [];

    public bool HasAuto => AutoSource.Length != 0;
}

internal static class NewStyle
{
    internal const string AutoNamespace = "global::Tl.Generated";
    static readonly SymbolDisplayFormat Full = SymbolDisplayFormat.FullyQualifiedFormat;

    internal static string FeederKey(string consumer, string method) => consumer + "." + method;

    internal static string AutoNameOf(string id) => "TlAuto_" + id;

    internal static int FeedKey(string assembly)
    {
        var hash = 2166136261u;
        foreach (var c in assembly)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        var key = unchecked((int)(hash & 0x7fffffff));
        return key == 0 ? 1 : key;
    }

    internal static bool IsKindAttribute(INamedTypeSymbol? attribute)
        => attribute is { Name: "TlKindAttribute" or "TimelineKindAttribute", ContainingNamespace: { Name: "Tl", ContainingNamespace.IsGlobalNamespace: true } };

    internal static bool IsStateType(ITypeSymbol type, INamedTypeSymbol? stateAttribute)
    {
        if (stateAttribute is null || type.TypeKind != TypeKind.Struct)
            return false;
        foreach (var attribute in type.GetAttributes())
            if (Same(attribute.AttributeClass, stateAttribute))
                return true;
        return false;
    }

    internal static NewStyleAnalysis Prepare(CSharpCompilation compilation, string id, Func<ITypeSymbol, bool> isState)
    {
        var inline = new Dictionary<string, int>(StringComparer.Ordinal);
        var feeders = new HashSet<string>(StringComparer.Ordinal);
        var states = new List<INamedTypeSymbol>();
        var tlKind = compilation.GetTypeByMetadataName("Tl.TlKindAttribute");
        var tlFrame = compilation.GetTypeByMetadataName("Tl.TlFrame`1");
        var tlState = compilation.GetTypeByMetadataName("Tl.TlStateAttribute");
        var system = compilation.GetTypeByMetadataName("Tl.TimelineSystemAttribute");
        var feeds = compilation.GetTypeByMetadataName("Tl.FeedsAttribute");
        var pulse = compilation.GetTypeByMetadataName("Tl.PulseAttribute");
        var autoName = AutoNameOf(id);
        var empty = new NewStyleAnalysis(compilation, autoName, [], "", inline, feeders, states);
        if (system is null || feeds is null || pulse is null)
            return empty;
        var kinds = new List<INamedTypeSymbol>();
        var consumers = new List<INamedTypeSymbol>();
        var systemColumns = new HashSet<string>(StringComparer.Ordinal);
        var systemEvents = new HashSet<string>(StringComparer.Ordinal);
        var eventTracks = new List<INamedTypeSymbol>();
        var tlTrack = compilation.GetTypeByMetadataName("Tl.ITlTrack`1");
        var events = compilation.GetTypeByMetadataName("Tl.Events`1");
        var member = compilation.GetTypeByMetadataName("Tl.TimelineSystemMemberAttribute");
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(node) is not INamedTypeSymbol type)
                    continue;
                if (tlState is not null && IsStateType(type, tlState) && !states.Contains(type, SymbolEqualityComparer.Default))
                    states.Add(type);
                foreach (var attribute in type.GetAttributes())
                {
                    if (IsKindAttribute(attribute.AttributeClass))
                    {
                        kinds.Add(type);
                        foreach (var pair in attribute.NamedArguments)
                            if (pair.Key == "Inline" && pair.Value.Value is int value && value > 0)
                                inline[type.ToDisplayString(Full)] = value;
                    }
                    if (Same(attribute.AttributeClass, system))
                        foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
                            if (field.Type is INamedTypeSymbol { IsGenericType: true } span && (span.MetadataName == "Span`1" || span.MetadataName == "ReadOnlySpan`1"))
                                systemColumns.Add(span.TypeArguments[0].ToDisplayString(Full));
                            else if (field.Type is INamedTypeSymbol { IsGenericType: true } evented && Same(evented.OriginalDefinition, events))
                                systemEvents.Add(evented.TypeArguments[0].ToDisplayString(Full));
                }
                if (tlTrack is not null && events is not null && EventElements(type, events).Count != 0 && !eventTracks.Contains(type, SymbolEqualityComparer.Default))
                    foreach (var face in type.AllInterfaces)
                        if (Same(face.OriginalDefinition, tlTrack))
                        {
                            eventTracks.Add(type);
                            break;
                        }
                foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (tlFrame is not null && method.IsStatic && method.Name is "OnClipStart" or "OnClipEnd" && method.Parameters.Length > 0 && method.Parameters[0].Type is INamedTypeSymbol clipFrame && Same(clipFrame.OriginalDefinition, tlFrame))
                        feeders.Add(FeederKey(type.ToDisplayString(Full), method.Name));
                    foreach (var attribute in method.GetAttributes())
                        if (Same(attribute.AttributeClass, feeds) || Same(attribute.AttributeClass, pulse))
                        {
                            if (!consumers.Contains(type, SymbolEqualityComparer.Default))
                                consumers.Add(type);
                            if (tlFrame is not null && method.Parameters.Length > 0 && method.Parameters[0].Type is INamedTypeSymbol first && Same(first.OriginalDefinition, tlFrame))
                                feeders.Add(FeederKey(type.ToDisplayString(Full), method.Name));
                        }
                }
            }
        }
        var autoKinds = new HashSet<string>(StringComparer.Ordinal);
        var columns = new List<(string type, bool writable)>();
        foreach (var kind in kinds)
        {
            if (!DirectApply(kind, isState, systemColumns, out var apply))
                continue;
            autoKinds.Add(kind.ToDisplayString(Full));
            for (var i = 1; i < apply!.Parameters.Length; i++)
                AddColumn(columns, apply.Parameters[i].Type.ToDisplayString(Full), apply.Parameters[i].RefKind == RefKind.Ref);
        }
        var members = new HashSet<string>(StringComparer.Ordinal);
        var feedsForeign = false;
        foreach (var consumer in consumers)
            foreach (var method in consumer.GetMembers().OfType<IMethodSymbol>())
                foreach (var attribute in method.GetAttributes())
                {
                    if (!Same(attribute.AttributeClass, feeds) && !Same(attribute.AttributeClass, pulse))
                        continue;
                    if (attribute.ConstructorArguments.Length != 1 || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol fed)
                        continue;
                    var foreign = IsForeignKind(fed, compilation.Assembly);
                    if (!foreign && !autoKinds.Contains(fed.ToDisplayString(Full)))
                        continue;
                    feedsForeign |= foreign;
                    members.Add(consumer.ToDisplayString(Full));
                    foreach (var parameter in method.Parameters)
                        if (isState(parameter.Type))
                            AddColumn(columns, parameter.Type.ToDisplayString(Full), parameter.RefKind == RefKind.Ref);
                }
        var autoEvents = new List<string>();
        if (events is not null)
        {
            foreach (var track in eventTracks)
            {
                var elements = EventElements(track, events);
                if (members.Contains(track.ToDisplayString(Full)))
                {
                    foreach (var element in elements)
                        if (!autoEvents.Contains(element))
                            autoEvents.Add(element);
                    continue;
                }
                if (HasMember(track, member) || elements.Any(systemEvents.Contains))
                    continue;
                members.Add(track.ToDisplayString(Full));
                foreach (var element in elements)
                    if (!autoEvents.Contains(element))
                        autoEvents.Add(element);
            }
        }
        var autoFull = AutoNamespace + "." + autoName;
        var result = compilation;
        if (feeders.Count != 0 || members.Count != 0 || HasAttribute(kinds, tlKind))
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var rewriter = new Rewriter(model, tlKind, tlFrame, members, autoFull);
                var rewritten = rewriter.Visit(tree.GetRoot());
                if (rewriter.Changed)
                    result = result.ReplaceSyntaxTree(tree, tree.WithRootAndOptions(rewritten, tree.Options));
            }
        }
        if (columns.Count == 0 && !feedsForeign && autoEvents.Count == 0)
            return new NewStyleAnalysis(result, autoName, [], "", inline, feeders, states);
        var text = new StringBuilder();
        text.Append("namespace Tl.Generated\n{\n[global::Tl.TimelineSystem]\npublic ref partial struct ").Append(autoName).Append("\n{\n");
        var autoColumns = new List<NewStyleColumn>();
        for (var i = 0; i < columns.Count; i++)
        {
            var name = "C" + i + "_" + Simple(columns[i].type);
            text.Append("public global::System.").Append(columns[i].writable ? "Span<" : "ReadOnlySpan<").Append(columns[i].type).Append("> ").Append(name).Append(";\n");
            autoColumns.Add(new NewStyleColumn(name, columns[i].type, columns[i].writable));
        }
        var eventFields = new List<NewStyleColumn>();
        for (var i = 0; i < autoEvents.Count; i++)
        {
            var name = "E" + i + "_" + Simple(autoEvents[i]);
            text.Append("public global::Tl.Events<").Append(autoEvents[i]).Append("> ").Append(name).Append(";\n");
            eventFields.Add(new NewStyleColumn(name, autoEvents[i], true));
        }
        text.Append("}\n}\n");
        var source = text.ToString();
        var options = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions ?? CSharpParseOptions.Default;
        result = result.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, options, "TlAuto.g.cs"));
        return new NewStyleAnalysis(result, autoName, autoColumns, source, inline, feeders, states) { AutoEvents = eventFields };
    }

    static List<string> EventElements(INamedTypeSymbol type, INamedTypeSymbol events)
    {
        var elements = new List<string>();
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
        {
            if (!method.IsStatic || method.Name is not ("OnClipStart" or "OnClipEnd"))
                continue;
            for (var i = 1; i < method.Parameters.Length; i++)
                if (method.Parameters[i].Type is INamedTypeSymbol { IsGenericType: true } evented && Same(evented.OriginalDefinition, events))
                {
                    var element = evented.TypeArguments[0].ToDisplayString(Full);
                    if (!elements.Contains(element))
                        elements.Add(element);
                }
        }
        return elements;
    }

    static bool HasMember(INamedTypeSymbol type, INamedTypeSymbol? member)
    {
        if (member is null)
            return false;
        foreach (var attribute in type.GetAttributes())
            if (Same(attribute.AttributeClass, member))
                return true;
        return false;
    }

    internal static bool IsForeignKind(INamedTypeSymbol type, IAssemblySymbol assembly)
    {
        if (Same(type.ContainingAssembly, assembly))
            return false;
        foreach (var attribute in type.GetAttributes())
            if (IsKindAttribute(attribute.AttributeClass))
                return true;
        return false;
    }

    static bool DirectApply(INamedTypeSymbol kind, Func<ITypeSymbol, bool> isState, ISet<string> systemColumns, out IMethodSymbol? apply)
    {
        apply = kind.GetMembers("Apply").OfType<IMethodSymbol>().FirstOrDefault(static m => m.IsStatic && m.ReturnsVoid && m.Arity == 0);
        if (apply is null || apply.Parameters.Length < 2)
            return false;
        for (var i = 1; i < apply.Parameters.Length; i++)
        {
            var parameter = apply.Parameters[i];
            if (!isState(parameter.Type) || systemColumns.Contains(parameter.Type.ToDisplayString(Full)))
                return false;
        }
        return true;
    }

    static void AddColumn(List<(string type, bool writable)> columns, string type, bool writable)
    {
        for (var i = 0; i < columns.Count; i++)
            if (columns[i].type == type)
            {
                if (writable && !columns[i].writable)
                    columns[i] = (type, true);
                return;
            }
        columns.Add((type, writable));
    }

    static string Simple(string type)
    {
        var bare = type.StartsWith("global::", StringComparison.Ordinal) ? type.Substring(8) : type;
        var angle = bare.IndexOf('<');
        if (angle >= 0)
            bare = bare.Substring(0, angle);
        var dot = bare.LastIndexOf('.');
        var name = dot < 0 ? bare : bare.Substring(dot + 1);
        var builder = new StringBuilder();
        foreach (var c in name)
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        return builder.ToString();
    }

    static bool HasAttribute(List<INamedTypeSymbol> kinds, INamedTypeSymbol? attribute)
    {
        if (attribute is null)
            return false;
        foreach (var kind in kinds)
            foreach (var data in kind.GetAttributes())
                if (Same(data.AttributeClass, attribute))
                    return true;
        return false;
    }

    static bool Same(ISymbol? a, ISymbol? b) => a is not null && b is not null && SymbolEqualityComparer.Default.Equals(a, b);

    sealed class Rewriter : CSharpSyntaxRewriter
    {
        readonly SemanticModel _model;
        readonly INamedTypeSymbol? _tlKind;
        readonly INamedTypeSymbol? _tlFrame;
        readonly ISet<string> _members;
        readonly string _auto;

        public Rewriter(SemanticModel model, INamedTypeSymbol? tlKind, INamedTypeSymbol? tlFrame, ISet<string> members, string auto)
        {
            _model = model;
            _tlKind = tlKind;
            _tlFrame = tlFrame;
            _members = members;
            _auto = auto;
        }

        public bool Changed { get; private set; }

        public override SyntaxNode? VisitAttribute(AttributeSyntax node)
        {
            if (_tlKind is not null && _model.GetTypeInfo(node).Type is INamedTypeSymbol type && Same(type, _tlKind)
                || _tlKind is not null && _model.GetSymbolInfo(node).Symbol is IMethodSymbol ctor && Same(ctor.ContainingType, _tlKind))
            {
                Changed = true;
                var kept = new List<AttributeArgumentSyntax>();
                if (node.ArgumentList is not null)
                    foreach (var argument in node.ArgumentList.Arguments)
                        if (argument.NameEquals?.Name.Identifier.ValueText is not ("Inline" or "GamePulses"))
                            kept.Add(argument);
                var renamed = SyntaxFactory.Attribute(SyntaxFactory.ParseName("global::Tl.TimelineKind"));
                return kept.Count == 0 ? renamed : renamed.WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(kept)));
            }
            return base.VisitAttribute(node);
        }

        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            if (TlFrameClip(node) is { } clip)
            {
                Changed = true;
                return SyntaxFactory.ParseTypeName("global::Tl.Frame<global::Tl.TlNoSettings<" + clip + ">, " + clip + ">").WithTriviaFrom(node);
            }
            return base.VisitGenericName(node);
        }

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (node.Right is GenericNameSyntax && TlFrameClip(node) is { } clip)
            {
                Changed = true;
                return SyntaxFactory.ParseTypeName("global::Tl.Frame<global::Tl.TlNoSettings<" + clip + ">, " + clip + ">").WithTriviaFrom(node);
            }
            return base.VisitQualifiedName(node);
        }

        string? TlFrameClip(SyntaxNode node)
        {
            if (_tlFrame is not null && _model.GetSymbolInfo(node).Symbol is INamedTypeSymbol type && Same(type.OriginalDefinition, _tlFrame) && type.TypeArguments.Length == 1)
                return type.TypeArguments[0].ToDisplayString(Full);
            return null;
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var visited = (StructDeclarationSyntax)base.VisitStructDeclaration(node)!;
            if (_model.GetDeclaredSymbol(node) is INamedTypeSymbol type && _members.Contains(type.ToDisplayString(Full)))
            {
                Changed = true;
                var member = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("global::Tl.TimelineSystemMember"),
                    SyntaxFactory.ParseAttributeArgumentList("(typeof(" + _auto + "))"))));
                return visited.AddAttributeLists(member);
            }
            return visited;
        }
    }
}
