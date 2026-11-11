using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

internal sealed record NewStyleColumn(string Name, string TypeName, bool Writable);

internal sealed class NewStyleAnalysis
{
    public NewStyleAnalysis(CSharpCompilation compilation, string autoName, IReadOnlyList<NewStyleColumn> autoColumns, string autoSource, IReadOnlyDictionary<string, int> inline, IReadOnlyList<INamedTypeSymbol> states, ISet<string> members)
    {
        Compilation = compilation;
        AutoName = autoName;
        AutoColumns = autoColumns;
        AutoSource = autoSource;
        Inline = inline;
        Members = members;
        States = states;
    }

    public readonly CSharpCompilation Compilation;
    public readonly string AutoName;
    public readonly IReadOnlyList<NewStyleColumn> AutoColumns;
    public readonly string AutoSource;
    public readonly IReadOnlyDictionary<string, int> Inline;
    public readonly ISet<string> Members;
    public readonly IReadOnlyList<INamedTypeSymbol> States;
    public IReadOnlyList<NewStyleColumn> AutoEvents { get; init; } = [];

    public bool HasAuto => AutoSource.Length != 0;
}

internal static class NewStyle
{
    static readonly SymbolDisplayFormat Full = SymbolDisplayFormat.FullyQualifiedFormat;


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
        var states = new List<INamedTypeSymbol>();
        var tlState = compilation.GetTypeByMetadataName("Tl.TlStateAttribute");
        var system = compilation.GetTypeByMetadataName("Tl.TimelineSystemAttribute");
        var feeds = compilation.GetTypeByMetadataName("Tl.FeedsAttribute");
        var pulse = compilation.GetTypeByMetadataName("Tl.PulseAttribute");
        var autoName = AutoNameOf(id);
        var members = new HashSet<string>(StringComparer.Ordinal);
        var empty = new NewStyleAnalysis(compilation, autoName, [], "", inline, states, members);
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

                    foreach (var attribute in method.GetAttributes())
                        if (Same(attribute.AttributeClass, feeds) || Same(attribute.AttributeClass, pulse))
                        {
                            if (!consumers.Contains(type, SymbolEqualityComparer.Default))
                                consumers.Add(type);

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
            var idle = kind.GetMembers("Idle").OfType<IMethodSymbol>().FirstOrDefault(static m => m.IsStatic && m.ReturnsVoid && m.Arity == 0);
            if (idle is not null)
                foreach (var parameter in idle.Parameters)
                    if (isState(parameter.Type) && !systemColumns.Contains(parameter.Type.ToDisplayString(Full)))
                        AddColumn(columns, parameter.Type.ToDisplayString(Full), parameter.RefKind == RefKind.Ref);
        }
        var feedsForeign = false;
        foreach (var consumer in consumers)
            foreach (var method in consumer.GetMembers().OfType<IMethodSymbol>())
                foreach (var attribute in method.GetAttributes())
                {
                    if (!Same(attribute.AttributeClass, feeds) && !Same(attribute.AttributeClass, pulse))
                        continue;
                    if (attribute.ConstructorArguments.Length != 1 || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol fed)
                        continue;
                    if (DerivedKinds.TemplateOf(fed, out var derived) != DerivedKind.None)
                    {
                        if (derived is null || !isState(derived) || systemColumns.Contains(derived.ToDisplayString(Full)))
                            continue;
                        members.Add(consumer.ToDisplayString(Full));
                        AddColumn(columns, derived.ToDisplayString(Full), true);
                        foreach (var parameter in method.Parameters)
                            if (isState(parameter.Type))
                                AddColumn(columns, parameter.Type.ToDisplayString(Full), parameter.RefKind == RefKind.Ref);
                        continue;
                    }
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
        if (columns.Count == 0 && !feedsForeign && autoEvents.Count == 0)
            return new NewStyleAnalysis(compilation, autoName, [], "", inline, states, members);
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
        return new NewStyleAnalysis(compilation, autoName, autoColumns, source, inline, states, members) { AutoEvents = eventFields };
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

    static bool Same(ISymbol? a, ISymbol? b) => a is not null && b is not null && SymbolEqualityComparer.Default.Equals(a, b);

}
