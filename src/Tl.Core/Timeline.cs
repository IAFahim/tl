using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Tl;

public static class Timeline
{
    public static FrameQuery<TTrack, TClip> Query<TTrack, TClip>(in TimelineComponent component) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged => new(component);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance(ReadOnlySpan<ushort> indices, Span<ushort> positions, bool forward)
        => Advance(indices, positions, positions, forward);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe void Advance(ReadOnlySpan<ushort> indices, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        Checked.Columns(indices, positions, next);
        var count = positions.Length;
        var motion = TimelineTable.Motion;
        var i = 0;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance(TimelineAsset asset, Span<ushort> positions, bool forward)
        => Advance(asset.Index, positions, positions, forward);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance(TimelineAsset asset, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Advance(asset.Index, positions, next, forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe void Advance(ushort index, Span<ushort> positions, bool forward)
    {
        if (positions.Length <= LaneOps.SmallSpan)
        {
            AdvanceRecords(TimelineTable.Motion[index], positions, positions, forward);
            return;
        }
        Advance(index, positions, positions, forward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void AdvanceRecords(uint motion, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        var duration = (ushort)(motion & 0xFFFF);
        var looping = (motion & 0x80000000u) != 0;
        if (forward) AdvanceRecordsForward(duration, looping, positions, next);
        else AdvanceRecordsBackward(duration, looping, positions, next);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void AdvanceRecordsForward(ushort duration, bool looping, ReadOnlySpan<ushort> positions, Span<ushort> next)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            var p = positions[i];
            if (p < duration)
            {
                p++;
                if (looping && p == duration) p = 0;
            }
            next[i] = p;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void AdvanceRecordsBackward(ushort duration, bool looping, ReadOnlySpan<ushort> positions, Span<ushort> next)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            var p = positions[i];
            if (looping) next[i] = p < duration ? (p == 0 ? (ushort)(duration - 1) : (ushort)(p - 1)) : p;
            else next[i] = p > 0 && p <= duration ? (ushort)(p - 1) : p;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe void Advance(ushort index, ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
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

    public static void Bake(ushort timeline)
        => Dispatch(timeline, [], []);

    public static void Bake<T0>(ushort timeline, in T0 argument0)
        where T0 : allows ref struct
        => Dispatch(timeline, [TypeKey<T0>.Value], [Addr(argument0)]);

    public static void Bake<T0, T1>(ushort timeline, in T0 argument0, in T1 argument1)
        where T0 : allows ref struct
        where T1 : allows ref struct
        => Dispatch(timeline, [TypeKey<T0>.Value, TypeKey<T1>.Value], [Addr(argument0), Addr(argument1)]);

    public static void Bake<T0, T1, T2>(ushort timeline, in T0 argument0, in T1 argument1, in T2 argument2)
        where T0 : allows ref struct
        where T1 : allows ref struct
        where T2 : allows ref struct
        => Dispatch(timeline, [TypeKey<T0>.Value, TypeKey<T1>.Value, TypeKey<T2>.Value], [Addr(argument0), Addr(argument1), Addr(argument2)]);

    public static void Bake<T0, T1, T2, T3>(ushort timeline, in T0 argument0, in T1 argument1, in T2 argument2, in T3 argument3)
        where T0 : allows ref struct
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        => Dispatch(timeline, [TypeKey<T0>.Value, TypeKey<T1>.Value, TypeKey<T2>.Value, TypeKey<T3>.Value], [Addr(argument0), Addr(argument1), Addr(argument2), Addr(argument3)]);

    static unsafe nint Addr<T>(scoped in T argument) where T : allows ref struct
        => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in argument));

    static unsafe void Dispatch(ushort timeline, ReadOnlySpan<ulong> present, ReadOnlySpan<IntPtr> arguments)
    {
        var reference = TimelineTable.Reference(timeline);
        var pairs = reference.Pairs;
        Span<int> binding = stackalloc int[4];
        Span<IntPtr> invoke = stackalloc IntPtr[4];
        var count = (int)reference.PairCount;
        for (var pair = 0; pair < count; pair++)
        {
            var key = pairs[pair].Key;
            for (var entry = BakeTable.HeadOf(key); entry >= 0; entry = BakeTable.EntryAt[entry].Next)
            {
                var bake = BakeTable.EntryAt + entry;
                if (bake->ParamCount > arguments.Length) continue;
                if (!Satisfied(bake, present, binding)) continue;
                for (var slot = 0; slot < bake->ParamCount; slot++) invoke[slot] = arguments[binding[slot]];
                bake->Invoke((byte**)Unsafe.AsPointer(ref MemoryMarshal.GetReference(invoke)));
            }
        }
    }

    static unsafe bool Satisfied(BakeTable.Entry* bake, ReadOnlySpan<ulong> present, Span<int> binding)
    {
        for (var parameter = 0; parameter < bake->ParamCount; parameter++)
        {
            var bound = -1;
            for (var argument = 0; argument < present.Length; argument++)
                if (present[argument] == bake->ParamKeys[parameter])
                {
                    bound = argument;
                    break;
                }
            if (bound < 0) return false;
            binding[parameter] = bound;
        }
        return true;
    }
}
