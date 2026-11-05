using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.Intrinsics;
#endif
using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Tl;

public static class Timeline
{

    public static void SealConsumers() => PairTable.Seal();

    public static FrameQuery<TTrack, TClip> Query<TTrack, TClip>(in TimelineComponent component) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged => new(component);

    [MethodImpl(AggressiveInlining | AggressiveOptimization)]
    public static void Advance(ReadOnlySpan<ushort> indices, Span<ushort> positions, bool forward)
        => Advance(indices, positions, positions, forward);

    [MethodImpl(AggressiveOptimization)]
    public static unsafe void Advance(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        Guard.Same(positions, next);
        Guard.Same(indices, positions);
        Checked.Columns(indices, positions, next);
        Checked.Domain(indices, positions);
        var count = positions.Length;
        var motion = TimelineTable.Motion;
        var i = 0;
#if NET
        var bound = count - (count & 15);
        while (i < bound)
        {
            var block = i + 16;
            if (Vector256.EqualsAll(Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(indices), (nuint)i), Vector256.Create(indices[i])))
            {
                var m = motion[indices[i]];
                if (forward) LaneOps.AdvanceForward((ushort)(m & 0xFFFF), (m & 0x80000000u) != 0, positions, next, i, block);
                else LaneOps.AdvanceBackward((ushort)(m & 0xFFFF), (m & 0x80000000u) != 0, positions, next, i, block);
            }
            else LaneOps.AdvanceRows(motion, indices, positions, next, forward, i, block);
            i = block;
        }
#endif
        for (; i < count; i++)
        {
            var m = motion[indices[i]];
            var duration = (ushort)(m & 0xFFFF);
            var looping = (m & 0x80000000u) != 0;
            var pos = positions[i];
            if (forward)
                next[i] = pos < duration ? (looping && pos + 1 == duration ? (ushort)0 : (ushort)(pos + 1)) : pos;
            else
                next[i] = looping
                    ? pos < duration ? (pos == 0 ? (ushort)(duration - 1) : (ushort)(pos - 1)) : pos
                    : pos > 0 && pos <= duration ? (ushort)(pos - 1) : pos;
        }
    }

    [MethodImpl(AggressiveInlining | AggressiveOptimization)]
    public static void Advance(TimelineAsset asset, Span<ushort> positions, bool forward)
        => Advance(asset.Index, positions, positions, forward);

    [MethodImpl(AggressiveInlining | AggressiveOptimization)]
    public static void Advance(TimelineAsset asset, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        if (asset is null) throw new ArgumentNullException(nameof(asset));
        Advance(asset.Index, positions, next, forward);
    }

    [MethodImpl(AggressiveInlining | AggressiveOptimization)]
    public static unsafe void Advance(ushort index, Span<ushort> positions, bool forward)
    {
        Checked.Domain(index, positions);
        if (positions.Length <= LaneOps.SmallSpan)
        {
            AdvanceRecords(TimelineTable.Motion[index], positions, positions, forward);
            return;
        }
        Advance(index, positions, positions, forward);
    }

    [MethodImpl(AggressiveInlining | AggressiveOptimization)]
    static void AdvanceRecords(uint motion, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        var duration = (ushort)(motion & 0xFFFF);
        var looping = (motion & 0x80000000u) != 0;
        for (var i = 0; i < positions.Length; i++)
        {
            var p = positions[i];
            if (forward)
            {
                if (p < duration) { p++; if (looping && p == duration) p = 0; }
                next[i] = p;
            }
            else
                next[i] = looping
                    ? p < duration ? (p == 0 ? (ushort)(duration - 1) : (ushort)(p - 1)) : p
                    : p > 0 && p <= duration ? (ushort)(p - 1) : p;
        }
    }

    [MethodImpl(AggressiveOptimization)]
    public static unsafe void Advance(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        Guard.Same(positions, next);
        Checked.Domain(index, positions);
        var m = TimelineTable.Motion[index];
        var duration = (ushort)(m & 0xFFFF);
        var looping = (m & 0x80000000u) != 0;
        var count = positions.Length;
        if (count <= LaneOps.SmallSpan)
        {
            AdvanceRecords(m, positions, next, forward);
            return;
        }
        if (duration > 0)
        {
            if (forward) LaneOps.AdvanceForward(duration, looping, positions, next, 0, count);
            else LaneOps.AdvanceBackward(duration, looping, positions, next, 0, count);
            return;
        }
        positions.CopyTo(next);
    }
}
