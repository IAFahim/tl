using System.Text;
using Microsoft.CodeAnalysis;

namespace Tl.Gen.CSharp.Analysis;

internal static class PairLayout
{
    internal static ulong Of(ITypeSymbol track, ITypeSymbol clip) => Hash(Fold(track) + "\0" + Fold(clip));

    internal static ulong Hash(string text)
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
}
