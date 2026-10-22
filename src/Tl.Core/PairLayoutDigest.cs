using System.Reflection;
using System.Runtime.CompilerServices;
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

    static string Fold(Type type)
    {
        var layout = type.StructLayoutAttribute;
        var kind = layout?.Value ?? LayoutKind.Auto;
        var builder = new StringBuilder();
        builder.Append(layout is null ? "auto" : $"{kind}:{layout.Pack}:{layout.Size}");
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Array.Sort(fields, static (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));
        foreach (var field in fields)
        {
            builder.Append('\n');
            var buffer = (FixedBufferAttribute?)Attribute.GetCustomAttribute(field, typeof(FixedBufferAttribute), false);
            if (buffer is not null)
            {
                builder.Append("fixed:").Append(buffer.ElementType.FullName).Append(':').Append(buffer.Length);
                continue;
            }
            builder.Append(kind == LayoutKind.Auto ? "-" : Marshal.OffsetOf(type, field.Name).ToInt64().ToString());
            var fieldType = field.FieldType;
            if (fieldType.IsEnum) builder.Append(':').Append(Enum.GetUnderlyingType(fieldType).FullName);
            else if (!fieldType.IsPrimitive) builder.Append(':').Append(Fold(fieldType));
        }
        return builder.ToString();
    }
}
