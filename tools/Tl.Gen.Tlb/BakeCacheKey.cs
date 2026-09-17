using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Tl.Gen.Tlb;

public static class BakeCacheKey
{
    public const string ToolVersion = "tlbake-bake-v4";

    public static byte[] Compute(byte[] inputJson, IReadOnlyList<byte[]> assemblyFiles, bool strip) =>
        Compute(ToolVersion, inputJson, assemblyFiles, strip);

    public static byte[] Compute(string toolVersion, byte[] inputJson, IReadOnlyList<byte[]> assemblyFiles, bool strip)
    {
        ArgumentNullException.ThrowIfNull(toolVersion);
        ArgumentNullException.ThrowIfNull(inputJson);
        ArgumentNullException.ThrowIfNull(assemblyFiles);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Feed(hash, Encoding.UTF8.GetBytes(toolVersion));
        Feed(hash, inputJson);
        Span<byte> count = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(count, (uint)assemblyFiles.Count);
        hash.AppendData(count);
        foreach (var file in assemblyFiles)
        {
            ArgumentNullException.ThrowIfNull(file);
            hash.AppendData(SHA256.HashData(file));
        }
        hash.AppendData([strip ? (byte)1 : (byte)0]);
        return hash.GetHashAndReset();
    }

    public static string Prefix(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length < 6)
            throw new ArgumentException("Bake cache key must be a full SHA-256 digest.");
        return Convert.ToHexString(key, 0, 6).ToLowerInvariant();
    }

    private static void Feed(IncrementalHash hash, byte[] payload)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(length, (uint)payload.Length);
        hash.AppendData(length);
        hash.AppendData(payload);
    }
}
