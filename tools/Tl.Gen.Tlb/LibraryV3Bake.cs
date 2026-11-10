using System.Buffers.Binary;

namespace Tl.Gen.Tlb;

public static class LibraryV3Bake
{
    public static byte[] AssembleV3(IReadOnlyList<LibraryPart> parts)
        => Tl.LibraryWriter.Write(Specs(parts));

    public static IReadOnlyList<Tl.TimelineSpec> Specs(IReadOnlyList<LibraryPart> parts)
    {
        if (parts == null)
            throw new BakeDiagnosticException("parts are null.");
        var specs = new List<Tl.TimelineSpec>(parts.Count);
        for (var i = 0; i < parts.Count; i++)
            specs.Add(FromTlb(parts[i]));
        return specs;
    }

    static Tl.TimelineSpec FromTlb(LibraryPart part)
    {
        var tlb = part.Tlb;
        var name = part.Name ?? "";
        if (tlb == null || tlb.Length == 0)
            throw new BakeDiagnosticException($"timeline '{name}' has no TLB bytes.");
        try
        {
            Tl.TimelineRef.Validate(tlb);
        }
        catch (ArgumentException error)
        {
            throw new BakeDiagnosticException($"timeline '{name}': {error.Message}");
        }
        var duration = U32(tlb, 12);
        var loops = U32(tlb, 8) != 0;
        var stageCount = (int)U32(tlb, 20);
        var pairCount = (int)U32(tlb, 24);
        var pairOffset = (int)U32(tlb, 28);
        var stageOffset = (int)U32(tlb, 32);
        var pairKeys = new ulong[pairCount];
        for (var p = 0; p < pairCount; p++)
            pairKeys[p] = BinaryPrimitives.ReadUInt64LittleEndian(tlb.AsSpan(pairOffset + 48 * p));
        var steps = new List<Tl.StepSpec>();
        for (var s = 0; s < stageCount; s++)
        {
            var stage = stageOffset + 16 * s;
            var program = (int)U32(tlb, stage + 8);
            var count = (int)U32(tlb, stage + 12);
            for (var k = 0; k < count; k++)
            {
                var slot = (int)U32(tlb, program + 8 * k);
                var pair = (int)U32(tlb, program + 8 * k + 4);
                if (pair < 0 || pair >= pairCount)
                    throw new BakeDiagnosticException($"timeline '{name}': step pair {pair} is out of bounds.");
                var start = U32(tlb, slot + 8);
                var end = U32(tlb, slot + 12);
                if (start >= end || end > duration)
                    throw new BakeDiagnosticException($"timeline '{name}': window [{start}, {end}) is outside its duration {duration}.");
                var window = (int)(end - start);
                steps.Add(new Tl.StepSpec(start, end, 0u, pairKeys[pair], new float[window], new float[window]));
            }
        }
        return new Tl.TimelineSpec(duration, loops, steps.ToArray());
    }

    static uint U32(ReadOnlySpan<byte> bytes, int at) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(at));
}
