using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HandleInterning;

[StructLayout(LayoutKind.Sequential)]
public readonly struct ContentKey : IEquatable<ContentKey>
{
    public readonly ulong Low;
    public readonly ulong High;

    public ContentKey(ulong low, ulong high)
    {
        Low = low;
        High = high;
    }

    public static ContentKey Of(ReadOnlySpan<byte> bytes)
    {
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(bytes, digest);
        var low = BitConverter.ToUInt64(digest);
        var high = BitConverter.ToUInt64(digest.Slice(8));
        return new ContentKey(low | 1, high);
    }

    public bool Equals(ContentKey other) => Low == other.Low && High == other.High;
    public override bool Equals(object? obj) => obj is ContentKey other && Equals(other);
    public override int GetHashCode() => (int)(Low ^ (Low >> 32) ^ High ^ (High >> 32));
    public static bool operator ==(ContentKey left, ContentKey right) => left.Equals(right);
    public static bool operator !=(ContentKey left, ContentKey right) => !left.Equals(right);
}
