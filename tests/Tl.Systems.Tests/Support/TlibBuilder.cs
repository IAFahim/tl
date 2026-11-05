using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Tl.Systems.Tests;

internal static class TlibBuilder
{
    public static byte[] Pack(IList<(byte[] Tlb, string Name, uint Channel)> timelines)
    {
        var ordered = timelines.OrderBy(static entry => entry.Name, StringComparer.Ordinal).ToList();
        var names = new List<byte[]>();
        var nameBytes = 0;
        foreach (var entry in ordered)
        {
            var encoded = Encoding.UTF8.GetBytes(entry.Name);
            names.Add(encoded);
            nameBytes += encoded.Length;
        }
        var manifestOff = 64u;
        var nameBase = manifestOff + (uint)ordered.Count * 32u;
        var blobBase = (nameBase + (uint)nameBytes + 7u) & ~7u;
        var total = blobBase;
        foreach (var entry in ordered) total = (total + (uint)entry.Tlb.Length + 7u) & ~7u;
        total += 32u;
        var buffer = new byte[total];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0), 0x42494C54u);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(4), 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(8), (uint)ordered.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(12), manifestOff);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(16), blobBase);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(20), total);
        var nameAt = nameBase;
        var blobAt = blobBase;
        for (var i = 0; i < ordered.Count; i++)
        {
            var at = (int)(manifestOff + (uint)i * 32u);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(at), (uint)(i + 1));
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(at + 4), ordered[i].Channel);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(at + 8), nameAt);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(at + 12), (uint)names[i].Length);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(at + 16), blobAt);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(at + 20), (uint)ordered[i].Tlb.Length);
            names[i].CopyTo(buffer, (int)nameAt);
            nameAt += (uint)names[i].Length;
            ordered[i].Tlb.CopyTo(buffer, (int)blobAt);
            blobAt = (blobAt + (uint)ordered[i].Tlb.Length + 7u) & ~7u;
        }
        using var sha = SHA256.Create();
        var digest = sha.ComputeHash(buffer, 0, (int)total - 32);
        digest.CopyTo(buffer, (int)total - 32);
        return buffer;
    }

    public static byte[] PatchV6Meta(byte[] v6, ushort next, ushort channel, byte hold)
    {
        if (v6.Length < 80) throw new ArgumentException("TLB truncated.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(v6.AsSpan(0, 4)) != 0x31424C54u) throw new ArgumentException("TLB magic invalid.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(v6.AsSpan(4, 4)) != 6u) throw new ArgumentException("TLB is not v6.");
        var output = (byte[])v6.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(52, 4), next);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(56, 4), channel);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(60, 4), hold != 0 ? 1u : 0u);
        return output;
    }
}
