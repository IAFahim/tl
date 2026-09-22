using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Tl.Fuzz;

public readonly record struct FuzzClipSpec(uint Start, uint End, float Value);

public static class FuzzBake
{
    public static byte[] Bake(ushort duration, bool looping, float scale, IReadOnlyList<FuzzClipSpec> clips)
    {
        var cuts = new SortedSet<uint>();
        if (duration != 0)
        {
            cuts.Add(0u);
            cuts.Add(duration);
            foreach (var clip in clips)
            {
                cuts.Add(clip.Start);
                cuts.Add(clip.End);
            }
        }
        var edges = cuts.ToArray();
        var stageCount = edges.Length - 1;
        if (stageCount < 0) stageCount = 0;

        var values = clips.Select(clip => clip.Value).Distinct().Order().ToArray();
        var valueIndex = new Dictionary<float, ushort>();
        for (var i = 0; i < values.Length; i++) valueIndex[values[i]] = (ushort)i;

        var activeByStage = new List<FuzzClipSpec>[stageCount];
        var stepCount = 0;
        for (var stage = 0; stage < stageCount; stage++)
        {
            var edge = edges[stage];
            var active = new List<FuzzClipSpec>();
            foreach (var clip in clips)
                if (clip.Start <= edge && edge < clip.End)
                    active.Add(clip);
            activeByStage[stage] = active;
            if (active.Count > 0) stepCount++;
        }

        var pairOffset = 64u;
        var stageOffset = pairOffset + 48u;
        var programBase = stageOffset + 16u * (uint)stageCount;
        var poolOffset = (programBase + 8u * (uint)stepCount + 15u) & ~15u;
        var trackPoolAddress = poolOffset;
        var clipPoolAddress = (trackPoolAddress + 4u + 15u) & ~15u;
        var frameOffset = (clipPoolAddress + 4u * (uint)Math.Max(values.Length, 1) + 15u) & ~15u;
        var bytesLength = frameOffset + 24u * (uint)stepCount;

        var bytes = new byte[bytesLength];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0x31424C54u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 3u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), looping ? 1u : 0u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), duration);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)stageCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), pairOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), stageOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), poolOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(40), frameOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), bytesLength);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), bytesLength);

        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan((int)pairOffset), PairRuntime<FuzzTrack, FuzzClip>.Key);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 8), 24u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 12), trackPoolAddress - pairOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 16), 1u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 20), 4u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 24), clipPoolAddress - pairOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 28), (uint)Math.Max(values.Length, 1));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 32), 4u);

        var program = programBase;
        var slot = frameOffset;
        for (var stage = 0; stage < stageCount; stage++)
        {
            var at = (int)stageOffset + 16 * stage;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), edges[stage]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 4), edges[stage + 1]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 8), program);
            var active = activeByStage[stage];
            if (active.Count > 0)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 12), 1u);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)program), slot);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)program + 4), 0u);
                program += 8u;
                slot += 24u;
            }
        }

        MemoryMarshal.Write(bytes.AsSpan((int)trackPoolAddress), new FuzzTrack(scale));
        for (var i = 0; i < values.Length; i++)
            MemoryMarshal.Write(bytes.AsSpan((int)clipPoolAddress + 4 * i), new FuzzClip(values[i]));

        slot = frameOffset;
        for (var stage = 0; stage < stageCount; stage++)
        {
            var active = activeByStage[stage];
            if (active.Count == 0) continue;
            var ordered = active.OrderBy(clip => clip.Start).ToArray();
            var windowStart = ordered.Min(clip => clip.Start);
            var windowEnd = ordered.Max(clip => clip.End);
            var factorStart = 0u;
            var factorSpan = 0u;
            ushort second = SlotRowNoClip;
            if (ordered.Length >= 2)
            {
                factorStart = Math.Max(ordered[0].Start, ordered[1].Start);
                factorSpan = Math.Min(ordered[0].End, ordered[1].End) - factorStart;
                second = valueIndex[ordered[1].Value];
            }
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)slot, 2), 0);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)slot + 2, 2), valueIndex[ordered[0].Value]);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)slot + 4, 2), second);
            bytes[slot + 6] = 0;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)slot + 8, 4), windowStart);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)slot + 12, 4), windowEnd);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)slot + 16, 4), factorStart);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)slot + 20, 4), factorSpan);
            slot += 24u;
        }
        return bytes;
    }

    private const ushort SlotRowNoClip = 0xFFFF;
}

public sealed class FuzzModel(ushort duration, bool looping, float scale, IReadOnlyList<FuzzClipSpec> clips)
{
    public ushort Duration { get; } = duration;
    public bool Looping { get; } = looping;
    public float Scale { get; } = scale;
    public IReadOnlyList<FuzzClipSpec> Clips { get; } = clips;

    public float ClipSum(ushort tick)
    {
        var active = Clips.Where(clip => clip.Start <= tick && tick < clip.End).OrderBy(clip => clip.Start).ToArray();
        if (active.Length == 0) return 0f;
        var first = active[0];
        if (active.Length == 1) return first.Value;
        var second = active[1];
        var factorStart = Math.Max(first.Start, second.Start);
        var factorSpan = Math.Min(first.End, second.End) - factorStart;
        var factor = factorSpan <= 1 ? 0.5f : (tick - factorStart) / (float)(factorSpan - 1);
        return first.Value + (second.Value - first.Value) * factor;
    }

    public float Forward(ushort tick) => tick >= Duration ? 0f : Scale * ClipSum(tick);

    public float Backward(ushort tick)
    {
        if (tick >= Duration) return 0f;
        return tick + 1u == Duration ? Scale * ClipSum((ushort)(Duration - 1)) : Scale * ClipSum(tick);
    }

    public float BackwardMoveFrom(ushort position)
    {
        if (Duration == 0 || position > Duration) return 0f;
        if (position == Duration) return Looping ? 0f : Scale * ClipSum((ushort)(Duration - 1));
        if (position == 0) return Looping ? Scale * ClipSum((ushort)(Duration - 1)) : 0f;
        return Scale * ClipSum((ushort)(position - 1));
    }
}
