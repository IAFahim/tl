using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Tl.Gen.CSharp.Analysis;

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

    internal static List<string> EventElements(INamedTypeSymbol type, INamedTypeSymbol events)
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

    internal static bool IsForeignKind(INamedTypeSymbol type, IAssemblySymbol assembly)
    {
        if (Same(type.ContainingAssembly, assembly))
            return false;
        foreach (var attribute in type.GetAttributes())
            if (IsKindAttribute(attribute.AttributeClass))
                return true;
        return false;
    }

    internal static void AddColumn(List<(string type, bool writable)> columns, string type, bool writable)
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

    internal static string Simple(string type)
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
