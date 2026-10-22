using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Tl;

internal static class PairLayoutDigest
{
    internal static ulong Of<TTrack, TClip>() => Of(typeof(TTrack), typeof(TClip));

    internal static ulong Of(Type track, Type clip) => Hash(Fold(track) + "\0" + Fold(clip));

    static ulong Hash(string text)
    {
        var hash = 14695981039346656037ul;
        foreach (var c in text) hash = (hash ^ c) * 1099511628211ul;
        return hash | 1;
    }

    [UnconditionalSuppressMessage("TrimAnalysis", "IL2070", Justification = "Generated installers root the pair types' fields with DynamicDependency before calling VerifyLayout.")]
    static string Fold(Type type)
    {
        var layout = type.StructLayoutAttribute;
        var builder = new StringBuilder();
        builder.Append(layout is null ? "auto" : $"{layout.Value}:{layout.Pack}:{layout.Size}");
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Array.Sort(fields, static (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));
        foreach (var field in fields)
        {
            builder.Append('\n');
            var fieldType = field.FieldType;
            if (fieldType.IsEnum) builder.Append(Enum.GetUnderlyingType(fieldType).FullName);
            else if (fieldType.IsPrimitive) builder.Append(Width(fieldType));
            else builder.Append(Fold(fieldType));
        }
        return builder.ToString();
    }

    static int Width(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.Boolean or TypeCode.Byte or TypeCode.SByte => 1,
        TypeCode.Char or TypeCode.Int16 or TypeCode.UInt16 => 2,
        TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Single => 4,
        _ => 8
    };
}
