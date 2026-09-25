using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Tl.TestSupport;

namespace Tl.Core.Tests;

internal static class BakeFingerprint
{
    [ModuleInitializer]
    internal static void Wire() => DomainBaker.FingerprintOf = Of;

    internal static ulong Of(Type track, Type clip) => Hash(Fold(track) + "\0" + Fold(clip));

    internal static ulong Hash(string text)
    {
        const ulong seed = 14695981039346656037, prime = 1099511628211;
        var hash = seed;
        foreach (var c in text) hash = (hash ^ c) * prime;
        return hash | 1;
    }

    static string Fold(Type type)
    {
        var builder = new StringBuilder(type.FullName);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Array.Sort(fields, static (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));
        foreach (var field in fields)
        {
            builder.Append('\n').Append(field.Name).Append(':').Append(field.FieldType.FullName);
            if (!field.FieldType.IsPrimitive) builder.Append(':').Append(Fold(field.FieldType));
        }
        return builder.ToString();
    }
}
