using System.Text;
using Microsoft.CodeAnalysis;

namespace Tl.Gen.CSharp.Analysis;

internal static class PairLayout
{
    internal static ulong Of(ITypeSymbol track, ITypeSymbol clip) => Hash(Fold(track) + "\0" + Fold(clip));

    internal static IReadOnlyList<string> NestedTypes(ITypeSymbol track, ITypeSymbol clip)
    {
        var names = new List<string>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        if (track is INamedTypeSymbol trackNamed) seen.Add(trackNamed);
        if (clip is INamedTypeSymbol clipNamed) seen.Add(clipNamed);
        Fields(track);
        Fields(clip);
        return names;

        void Fields(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol { TypeKind: TypeKind.Struct } named) return;
            foreach (var member in named.GetMembers())
                if (member is IFieldSymbol { IsStatic: false } field)
                    Collect(field.Type);
        }

        void Collect(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol { TypeKind: TypeKind.Struct } named || Terminal(named) || !seen.Add(named)) return;
            names.Add(Symbols.Name(named));
            Fields(named);
        }
    }

    private static ulong Hash(string text)
    {
        var hash = 14695981039346656037ul;
        foreach (var c in text) hash = (hash ^ c) * 1099511628211ul;
        return hash | 1;
    }

    static string Fold(ITypeSymbol type)
    {
        var builder = new StringBuilder(Symbols.Name(type));
        foreach (var member in type.GetMembers())
            if (member is IFieldSymbol { IsStatic: false } field)
            {
                builder.Append('\n').Append(field.Name).Append(':').Append(Symbols.Name(field.Type));
                if (!Primitive(field.Type)) builder.Append(':').Append(Fold(field.Type));
            }
        return builder.ToString();
    }

    static bool Primitive(ITypeSymbol type) => type.SpecialType is
        SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Char or
        SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or
        SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or
        SpecialType.System_IntPtr or SpecialType.System_UIntPtr;

    static bool Terminal(INamedTypeSymbol type) => Primitive(type)
        || type.MetadataName.StartsWith("<", StringComparison.Ordinal) && type.MetadataName.EndsWith("e__FixedBuffer", StringComparison.Ordinal);
}
