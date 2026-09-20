using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Tl;

public interface ITimelineLane<T>
    where T : unmanaged, ITimelineLane<T>
{
    static abstract ushort Duration { get; }
    static abstract bool Looping { get; }
    static abstract float Effect(ushort position);
    static abstract float InverseEffect(ushort position);
}

public static class Timeline<T>
    where T : unmanaged, ITimelineLane<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ReadOnlySpan<ushort> positions, bool forward, Span<float> effects)
        => new TimelineLane<T>(positions, forward).Apply(effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Apply(ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
        => TimelineLane<T>.Apply(positions, next, forward, effects);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance(Span<ushort> positions, bool forward)
        => TimelineLane<T>.Advance(positions, positions, forward);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Advance(ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
        => TimelineLane<T>.Advance(positions, next, forward);
}

internal ref struct TimelineLane<T>
    where T : unmanaged, ITimelineLane<T>
{
    const int Chunk = 4096;

    readonly ReadOnlySpan<ushort> _positions;
    readonly bool _forward;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal TimelineLane(ReadOnlySpan<ushort> positions, bool forward)
    {
        _positions = positions;
        _forward = forward;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static void Advance(ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward)
    {
        var duration = T.Duration;
        var count = positions.Length;
        if (count == 0) return;
        if (duration == 0) { positions.CopyTo(next); return; }
        var looping = T.Looping;
        var i = 0;
        if (Avx2.IsSupported)
        {
            var end = count - (count & 15);
            if (forward) LaneOps.AdvanceForward(duration, looping, positions, next, 0, end);
            else LaneOps.AdvanceBackward(duration, looping, positions, next, 0, end);
            i = end;
        }
        var last = (ushort)(duration - 1);
        for (; i < count; i++)
        {
            var pos = positions[i];
            if (forward)
            {
                next[i] = pos > last ? pos : looping && pos == last ? (ushort)0 : (ushort)(pos + 1);
            }
            else
            {
                next[i] = (looping ? pos > last : pos == 0 || pos > duration) ? pos : (ushort)(pos == 0 ? last : pos - 1);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void Apply(ReadOnlySpan<ushort> positions, Span<ushort> next, bool forward, Span<float> effects)
    {
        Checked.Columns(positions, next, effects);
        Checked.Length(positions, next);
        var duration = T.Duration;
        var count = positions.Length;
        if (count == 0) return;
        if (duration == 0) { positions.CopyTo(next); return; }
        var looping = T.Looping;
        var active = LaneAccelerator<T>.Active;
        var records = forward ? LaneAccelerator<T>.Forward : LaneAccelerator<T>.Backward;
        var reverse = !forward;
        var i = 0;
        if (Avx2.IsSupported && active && duration > 1)
        {
            var end = count - (count & 15);
            if (forward)
            {
                if (duration <= 8)
                    LaneOps.EffPermuteForward(LaneAccelerator<T>.ForwardEffects, duration, looping, positions, next, effects, 0, end);
                else
                    LaneOps.EffectForward(LaneAccelerator<T>.ForwardEffects, duration, looping, positions, next, effects, 0, end);
            }
            else
            {
                if (duration <= 8)
                    LaneOps.EffPermuteBackward(LaneAccelerator<T>.BackwardEffects, duration, looping, positions, next, effects, 0, end);
                else
                    LaneOps.EffectBackward(LaneAccelerator<T>.BackwardByPosition, duration, looping, positions, next, effects, 0, end);
            }
            i = end;
        }
        for (; i < count; i++)
        {
            var pos = positions[i];
            if (active && pos <= duration)
            {
                ref var r = ref records[pos];
                if (r.Next != LaneMovementRecord.Skipped)
                {
                    effects[i] += r.Effect;
                    next[i] = r.Next;
                    continue;
                }
            }
            if (TimelineMovement.Advance(duration, looping, reverse, pos, out var np, out var tick, out _))
            {
                effects[i] += forward ? T.Effect(pos) : T.InverseEffect(tick);
                next[i] = np;
            }
            else next[i] = pos;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe void Apply(Span<float> effects)
    {
        Checked.Columns(_positions, effects);
        var positions = _positions;
        var count = positions.Length;
        var duration = T.Duration;
        if (count == 0 || duration == 0) return;
        var looping = T.Looping;
        var forward = _forward;
        if (!LaneAccelerator<T>.Active || RunShaped(_positions))
        {
            ApplyRuns(effects);
            return;
        }
        ApplyAccelerated(effects);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    unsafe void ApplyRuns(Span<float> effects)
    {
        var positions = _positions;
        var count = positions.Length;
        var duration = T.Duration;
        var looping = T.Looping;
        var forward = _forward;
        var i = 0;
        while (i < count)
        {
                var position = positions[i];
                var end = LaneOps.RunEnd(positions, i, positions.Length);
                float delta;
                if (forward)
                {
                    if (position >= duration) { i = end; continue; }
                    delta = T.Effect(position);
                }
                else
                {
                    if (position == 0 && !looping || position > duration || looping && position == duration) { i = end; continue; }
                    var tick = position == 0 ? (ushort)(duration - 1) : (ushort)(position - 1);
                    delta = T.InverseEffect(tick);
                }
                if (end == i + 1)
                    effects[i] += delta;
                else
                    LaneOps.Add(effects, i, end, delta);
            i = end;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    unsafe void ApplyAccelerated(Span<float> effects)
    {
        var positions = _positions;
        var count = positions.Length;
        var duration = T.Duration;
        var looping = T.Looping;
        var forward = _forward;
        var i = 0;
        var gather = duration > 1 && Avx2.IsSupported;
        var permute = duration <= 8 && Avx2.IsSupported;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            var blockEnd = i + ((chunkEnd - i) >> 4 << 4);
            if (gather && blockEnd > i && LaneOps.StaggeredEnds(positions, i, blockEnd))
            {
                if (forward)
                {
                    if (permute)
                        LaneOps.EffPermuteForward(LaneAccelerator<T>.ForwardEffects, T.Duration, looping, positions, default, effects, i, blockEnd);
                    else
                        LaneOps.EffectForward(LaneAccelerator<T>.ForwardEffects, T.Duration, looping, positions, default, effects, i, blockEnd);
                }
                else
                {
                    if (permute)
                        LaneOps.EffPermuteBackward(LaneAccelerator<T>.BackwardEffects, T.Duration, looping, positions, default, effects, i, blockEnd);
                    else
                        LaneOps.EffectBackward(LaneAccelerator<T>.BackwardByPosition, T.Duration, looping, positions, default, effects, i, blockEnd);
                }
                i = blockEnd;
                continue;
            }
            while (i < chunkEnd)
            {
                var position = positions[i];
                var end = LaneOps.RunEnd(positions, i, positions.Length);
                if (forward)
                {
                    if (position >= duration) { i = end; continue; }
                    var delta = T.Effect(position);
                    if (end == i + 1) effects[i] += delta;
                    else LaneOps.Add(effects, i, end, delta);
                }
                else
                {
                    if (position == 0 && !looping || position > duration || looping && position == duration) { i = end; continue; }
                    var tick = position == 0 ? (ushort)(duration - 1) : (ushort)(position - 1);
                    var delta = T.InverseEffect(tick);
                    if (end == i + 1) effects[i] += delta;
                    else LaneOps.Add(effects, i, end, delta);
                }
                i = end;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static bool RunShaped(ReadOnlySpan<ushort> positions)
    {
        var sample = positions.Length;
        if (sample > 256) sample = 256;
        var pairs = 0;
        for (var i = 1; i < sample; i++)
            if (positions[i] == positions[i - 1]) pairs++;
        return pairs >= sample / 2;
    }

}

[StructLayout(LayoutKind.Sequential)]
public struct LaneMovementRecord
{
    public const ushort Skipped = 0xFFFF;
    public float Effect;
    public ushort Next;
}

internal static unsafe class LaneOps
{
    internal const int SmallSpan = 15;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static int RunEnd(ReadOnlySpan<ushort> values, int start, int limit)
    {
        var value = values[start];
        var end = start + 1;
        if (Vector512.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(values);
            var search = Vector512.Create(value);
            var bound = limit - 32;
            while (end <= bound)
            {
                var mask = (uint)Vector512.ExtractMostSignificantBits(Vector512.Equals(Vector512.LoadUnsafe(ref first, (nuint)end), search));
                if (mask != 0xFFFF_FFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 32;
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(values);
            var search = Vector256.Create(value);
            var bound = limit - 16;
            while (end <= bound)
            {
                var mask = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.LoadUnsafe(ref first, (nuint)end), search));
                if (mask != 0xFFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 16;
            }
        }
        while (end < limit && values[end] == value) end++;
        return end;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static bool SingletonChunk(ReadOnlySpan<ushort> positions, int start, int end)
    {
        var probe = start + 64;
        if (probe > end) probe = end;
        ref var origin = ref MemoryMarshal.GetReference(positions);
        var j = start;
        if (Vector.IsHardwareAccelerated)
        {
            var vectorLimit = probe - Vector<ushort>.Count - 1;
            while (j <= vectorLimit)
            {
                if (Vector.EqualsAny(Vector.LoadUnsafe(ref origin, (nuint)j), Vector.LoadUnsafe(ref origin, (nuint)(j + 1)))) return false;
                j += Vector<ushort>.Count;
            }
        }
        for (var k = j; k < probe - 1; k++)
            if (positions[k] == positions[k + 1]) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static bool StaggeredEnds(ReadOnlySpan<ushort> positions, int start, int end)
    {
        var headEnd = start + 32;
        if (headEnd > end) headEnd = end;
        if (!SingletonChunk(positions, start, headEnd)) return false;
        var tailStart = end - 32;
        return tailStart <= headEnd || SingletonChunk(positions, tailStart, end);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Add(Span<float> values, int start, int end, float delta)
    {
        var length = end - start;
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(values);
            var vector = new Vector<float>(delta);
            var limit = length & ~(Vector<float>.Count - 1);
            for (; i < limit; i += Vector<float>.Count)
                (Vector.LoadUnsafe(ref origin, (nuint)(start + i)) + vector).StoreUnsafe(ref origin, (nuint)(start + i));
        }
        for (; i < length; i++) values[start + i] += delta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Fill(Span<ushort> values, int start, int end, ushort value)
    {
        var length = end - start;
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            ref var origin = ref MemoryMarshal.GetReference(values);
            var vector = new Vector<ushort>(value);
            var limit = length & ~(Vector<ushort>.Count - 1);
            for (; i < limit; i += Vector<ushort>.Count) vector.StoreUnsafe(ref origin, (nuint)(start + i));
        }
        for (; i < length; i++) values[start + i] = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void EffPermuteForward(float* eff, ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, Span<float> effects, int i, int limit)
    {
        var table = Vector256.Load(eff);
        var last = (ushort)(duration - 1);
        var lastWide = Vector256.Create((uint)last);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        var hasNext = !nextColumn.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            if (hasNext)
            {
                var skipMask = Vector256.GreaterThan(pos, lastVector);
                var next = pos + one;
                if (wrap) next = Vector256.ConditionalSelect(Vector256.Equals(pos, lastVector), zero, next);
                next = Vector256.ConditionalSelect(skipMask, pos, next);
                next.StoreUnsafe(ref n, (nuint)i);
            }
            (var wideLo, var wideHi) = Vector256.Widen(pos);
            var gatherLo = Avx2.PermuteVar8x32(table, wideLo.AsInt32());
            var gatherHi = Avx2.PermuteVar8x32(table, wideHi.AsInt32());
            var skipLo = Vector256.GreaterThan(wideLo, lastWide).AsSingle();
            var skipHi = Vector256.GreaterThan(wideHi, lastWide).AsSingle();
            var effectLo = Vector256.LoadUnsafe(ref e, (nuint)i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, (nuint)i);
            var effectHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, (nuint)(i + 8));
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void EffPermuteBackward(float* backward, ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, Span<float> effects, int i, int limit)
    {
        var table = Vector256.Load(backward);
        var last = (ushort)(duration - 1);
        var lastVector = Vector256.Create(last);
        var lastWide = Vector256.Create((uint)last);
        var durationWide = Vector256.Create((uint)duration);
        var zeroUint = Vector256<uint>.Zero;
        var zero = Vector256<ushort>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        var hasNext = !nextColumn.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var index = Vector256.ConditionalSelect(Vector256.Equals(pos, zero), lastVector, pos + step);
            if (hasNext)
            {
                Vector256<ushort> moveMask;
                if (wrap) moveMask = Vector256.LessThanOrEqual(pos, lastVector);
                else moveMask = Vector256.GreaterThan(pos, zero) & Vector256.LessThanOrEqual(pos, Vector256.Create(duration));
                var next = Vector256.ConditionalSelect(moveMask, index, pos);
                next.StoreUnsafe(ref n, (nuint)i);
            }
            (var indexLo, var indexHi) = Vector256.Widen(index);
            var gatherLo = Avx2.PermuteVar8x32(table, indexLo.AsInt32());
            var gatherHi = Avx2.PermuteVar8x32(table, indexHi.AsInt32());
            (var posLo, var posHi) = Vector256.Widen(pos);
            Vector256<float> skipLo, skipHi;
            if (wrap)
            {
                skipLo = Vector256.GreaterThan(posLo, lastWide).AsSingle();
                skipHi = Vector256.GreaterThan(posHi, lastWide).AsSingle();
            }
            else
            {
                skipLo = (Vector256.Equals(posLo, zeroUint) | Vector256.GreaterThan(posLo, durationWide)).AsSingle();
                skipHi = (Vector256.Equals(posHi, zeroUint) | Vector256.GreaterThan(posHi, durationWide)).AsSingle();
            }
            var effectLo = Vector256.LoadUnsafe(ref e, (nuint)i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, (nuint)i);
            var effectHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, (nuint)(i + 8));
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void EffectForward(float* eff, ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, Span<float> effects, int i, int limit)
    {
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create((ushort)(duration - 1));
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        var hasNext = !nextColumn.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            if (hasNext)
            {
                var skipMask = Vector256.GreaterThan(pos, lastVector);
                var next = pos + one;
                if (wrap) next = Vector256.ConditionalSelect(Vector256.Equals(pos, lastVector), zero, next);
                next = Vector256.ConditionalSelect(skipMask, pos, next);
                next.StoreUnsafe(ref n, (nuint)i);
            }
            var clamped = Vector256.Min(pos, durationVector);
            (var wideLo, var wideHi) = Vector256.Widen(clamped);
            var gatherLo = Avx2.GatherVector256(eff, wideLo.AsInt32(), 4);
            var gatherHi = Avx2.GatherVector256(eff, wideHi.AsInt32(), 4);
            var skipLo = Vector256.Equals(wideLo, durationWide).AsSingle();
            var skipHi = Vector256.Equals(wideHi, durationWide).AsSingle();
            var effectLo = Vector256.LoadUnsafe(ref e, (nuint)i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, (nuint)i);
            var effectHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, (nuint)(i + 8));
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void EffectBackward(float* byp, ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, Span<float> effects, int i, int limit)
    {
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create((ushort)(duration - 1));
        var zeroUint = Vector256<uint>.Zero;
        var zero = Vector256<ushort>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        var hasNext = !nextColumn.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            if (hasNext)
            {
                var next = pos + step;
                Vector256<ushort> moveMask;
                if (wrap)
                {
                    moveMask = Vector256.LessThanOrEqual(pos, lastVector);
                    next = Vector256.ConditionalSelect(Vector256.Equals(pos, zero), lastVector, next);
                }
                else
                {
                    moveMask = Vector256.GreaterThan(pos, zero) & Vector256.LessThanOrEqual(pos, durationVector);
                }
                next = Vector256.ConditionalSelect(moveMask, next, pos);
                next.StoreUnsafe(ref n, (nuint)i);
            }
            var clamped = Vector256.Min(pos, durationVector);
            (var wideLo, var wideHi) = Vector256.Widen(clamped);
            var gatherLo = Avx2.GatherVector256(byp, wideLo.AsInt32(), 4);
            var gatherHi = Avx2.GatherVector256(byp, wideHi.AsInt32(), 4);
            Vector256<float> skipLo, skipHi;
            if (wrap)
            {
                skipLo = Vector256.Equals(wideLo, durationWide).AsSingle();
                skipHi = Vector256.Equals(wideHi, durationWide).AsSingle();
            }
            else
            {
                (var posLo, var posHi) = Vector256.Widen(pos);
                skipLo = (Vector256.Equals(posLo, zeroUint) | Vector256.GreaterThan(posLo, durationWide)).AsSingle();
                skipHi = (Vector256.Equals(posHi, zeroUint) | Vector256.GreaterThan(posHi, durationWide)).AsSingle();
            }
            var effectLo = Vector256.LoadUnsafe(ref e, (nuint)i);
            Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, (nuint)i);
            var effectHi = Vector256.LoadUnsafe(ref e, (nuint)(i + 8));
            Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, (nuint)(i + 8));
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void AdvanceForward(ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, int i, int limit)
    {
        var lastVector = Vector256.Create((ushort)(duration - 1));
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var next = pos + one;
            if (wrap) next = Vector256.ConditionalSelect(Vector256.Equals(pos, lastVector), zero, next);
            next = Vector256.ConditionalSelect(Vector256.LessThanOrEqual(pos, lastVector), next, pos);
            next.StoreUnsafe(ref n, (nuint)i);
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void AdvanceBackward(ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, int i, int limit)
    {
        var lastVector = Vector256.Create((ushort)(duration - 1));
        var durationVector = Vector256.Create(duration);
        var zero = Vector256<ushort>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var next = pos + step;
            Vector256<ushort> move;
            if (wrap)
            {
                move = Vector256.LessThanOrEqual(pos, lastVector);
                next = Vector256.ConditionalSelect(Vector256.Equals(pos, zero), lastVector, next);
            }
            else
            {
                move = Vector256.GreaterThan(pos, zero) & Vector256.LessThanOrEqual(pos, durationVector);
            }
            next = Vector256.ConditionalSelect(move, next, pos);
            next.StoreUnsafe(ref n, (nuint)i);
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void AdvanceRows(uint* motion, ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, bool forward, int i, int limit)
    {
        var one = Vector256.Create(1u);
        var zero = Vector256<uint>.Zero;
        var low = Vector256.Create(0xFFFFu);
        ref var idRef = ref MemoryMarshal.GetReference(ids);
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        while (i < limit)
        {
            var idv = Vector256.LoadUnsafe(ref idRef, (nuint)i);
            (var idLo, var idHi) = Vector256.Widen(idv);
            var mLo = Avx2.GatherVector256((int*)motion, idLo.AsInt32(), 4).AsUInt32();
            var mHi = Avx2.GatherVector256((int*)motion, idHi.AsInt32(), 4).AsUInt32();
            var durLo = mLo & low;
            var durHi = mHi & low;
            var loopLo = Vector256.LessThan(mLo.AsInt32(), Vector256<int>.Zero);
            var loopHi = Vector256.LessThan(mHi.AsInt32(), Vector256<int>.Zero);
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            (var posLo, var posHi) = Vector256.Widen(pos);
            Vector256<uint> nextLo, nextHi;
            if (forward)
            {
                nextLo = AdvanceForwardWide(posLo, durLo, loopLo, one, zero);
                nextHi = AdvanceForwardWide(posHi, durHi, loopHi, one, zero);
            }
            else
            {
                nextLo = AdvanceBackwardWide(posLo, durLo, loopLo, one, zero);
                nextHi = AdvanceBackwardWide(posHi, durHi, loopHi, one, zero);
            }
            Vector256.Narrow(nextLo, nextHi).StoreUnsafe(ref n, (nuint)i);
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static Vector256<uint> AdvanceForwardWide(Vector256<uint> pos, Vector256<uint> duration, Vector256<int> loop, Vector256<uint> one, Vector256<uint> zero)
    {
        var next = pos + one;
        next = Vector256.ConditionalSelect(Vector256.Equals(pos, duration - one) & loop.AsUInt32(), zero, next);
        return Vector256.ConditionalSelect(Vector256.LessThan(pos, duration), next, pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static Vector256<uint> AdvanceBackwardWide(Vector256<uint> pos, Vector256<uint> duration, Vector256<int> loop, Vector256<uint> one, Vector256<uint> zero)
    {
        var prev = pos - one;
        var loopNext = Vector256.ConditionalSelect(Vector256.Equals(pos, zero), duration - one, prev);
        var loopMove = Vector256.LessThan(pos, duration);
        var finiteMove = Vector256.GreaterThan(pos, zero) & Vector256.LessThanOrEqual(pos, duration);
        var next = Vector256.ConditionalSelect(loop.AsUInt32(), loopNext, prev);
        var move = Vector256.ConditionalSelect(loop.AsUInt32(), loopMove, finiteMove);
        return Vector256.ConditionalSelect(move, next, pos);
    }
}

internal static unsafe class LaneMovement
{
    internal const ushort Skipped = LaneMovementRecord.Skipped;

    internal static void Bake(float* forward, float* backward, ushort duration, bool looping, LaneMovementRecord* forwardRecords, LaneMovementRecord* backwardRecords, float* backwardByPosition)
    {
        var skipped = LaneMovementRecord.Skipped;
        for (var p = 0u; p <= duration; p++)
        {
            if (p < duration)
            {
                var wraps = looping && p + 1 == duration;
                forwardRecords[p] = new LaneMovementRecord { Effect = forward[p], Next = wraps ? (ushort)0 : (ushort)(p + 1) };
                if (p == 0 && looping)
                {
                    backwardRecords[p] = new LaneMovementRecord { Effect = backward[duration - 1], Next = (ushort)(duration - 1) };
                    backwardByPosition[p] = backward[duration - 1];
                }
                else if (p == 0)
                {
                    backwardRecords[p] = new LaneMovementRecord { Effect = 0f, Next = skipped };
                    backwardByPosition[p] = 0f;
                }
                else
                {
                    backwardRecords[p] = new LaneMovementRecord { Effect = backward[p - 1], Next = (ushort)(p - 1) };
                    backwardByPosition[p] = backward[p - 1];
                }
            }
            else
            {
                forwardRecords[p] = new LaneMovementRecord { Effect = 0f, Next = skipped };
                if (looping || duration == 0)
                {
                    backwardRecords[p] = new LaneMovementRecord { Effect = 0f, Next = skipped };
                    backwardByPosition[p] = 0f;
                }
                else
                {
                    backwardRecords[p] = new LaneMovementRecord { Effect = backward[duration - 1], Next = (ushort)(duration - 1) };
                    backwardByPosition[p] = backward[duration - 1];
                }
            }
        }
    }
}

internal static unsafe class LaneAccelerator<T>
    where T : unmanaged, ITimelineLane<T>
{
    public const ushort Skipped = LaneMovementRecord.Skipped;
    public static float* ForwardEffects;
    public static float* BackwardEffects;
    public static float* BackwardByPosition;
    public static LaneMovementRecord* Forward;
    public static LaneMovementRecord* Backward;
    public static bool Active;
}

internal readonly struct BakedLane<TTrack, TClip> : ITimelineLane<BakedLane<TTrack, TClip>>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    public static ushort Duration => LaneTable<TTrack, TClip>.Duration;
    public static bool Looping => LaneTable<TTrack, TClip>.Looping;
    public static float Effect(ushort position) => LaneTable<TTrack, TClip>.Effect(position);
    public static float InverseEffect(ushort position) => LaneTable<TTrack, TClip>.InverseEffect(position);

    public static void Bind(TimelineAsset asset) => LaneTable<TTrack, TClip>.Bind(asset);
}

static unsafe class LaneTable<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    public static float* Forward;
    public static float* Backward;
    public static LaneMovementRecord* ForwardRecords;
    public static LaneMovementRecord* BackwardRecords;
    public static float* BackwardByPosition;
    public static ushort Duration;
    public static bool Looping;
    static void* _recordBlock;

    public static float Effect(ushort position) => Forward[position];

    public static float InverseEffect(ushort position) => Backward[position];

    public static void Bind(TimelineAsset asset)
    {
        LaneGuards.ValidatePair<TTrack, TClip>(asset);
        using var measured = MeasuredLanes.Measure(asset);
        var duration = measured.Duration;
        var looping = measured.Looping;
        var measuredFloats = (nuint)duration + 1u;
        var tableFloats = (nuint)Math.Max(8u, duration + 1u);
        var tableBytes = tableFloats * sizeof(float);
        var forward = (float*)NativeMemory.AlignedAlloc(tableBytes, 64);
        var backward = (float*)NativeMemory.AlignedAlloc(tableBytes, 64);
        Buffer.MemoryCopy(measured.Forward, forward, (long)tableBytes, (long)(measuredFloats * sizeof(float)));
        Buffer.MemoryCopy(measured.Backward, backward, (long)tableBytes, (long)(measuredFloats * sizeof(float)));
        new Span<float>(forward + measuredFloats, (int)(tableFloats - measuredFloats)).Clear();
        new Span<float>(backward + measuredFloats, (int)(tableFloats - measuredFloats)).Clear();
        var block = NativeMemory.AlignedAlloc((nuint)((duration + 1) * (2 * sizeof(LaneMovementRecord) + sizeof(float))), 64);
        var forwardRecords = (LaneMovementRecord*)block;
        var backwardRecords = forwardRecords + duration + 1;
        var backwardByPosition = (float*)(backwardRecords + duration + 1);
        LaneMovement.Bake(forward, backward, duration, looping, forwardRecords, backwardRecords, backwardByPosition);
        var previousBlock = _recordBlock;
        var previousForward = Forward;
        var previousBackward = Backward;
        Forward = forward;
        Backward = backward;
        ForwardRecords = forwardRecords;
        BackwardRecords = backwardRecords;
        BackwardByPosition = backwardByPosition;
        _recordBlock = block;
        Duration = duration;
        Looping = looping;
        if (previousForward != null)
        {
            NativeMemory.AlignedFree(previousForward);
            NativeMemory.AlignedFree(previousBackward!);
        }
        if (previousBlock != null) NativeMemory.AlignedFree(previousBlock);
        LaneAccelerator<BakedLane<TTrack, TClip>>.ForwardEffects = forward;
        LaneAccelerator<BakedLane<TTrack, TClip>>.BackwardEffects = backward;
        LaneAccelerator<BakedLane<TTrack, TClip>>.BackwardByPosition = backwardByPosition;
        LaneAccelerator<BakedLane<TTrack, TClip>>.Forward = forwardRecords;
        LaneAccelerator<BakedLane<TTrack, TClip>>.Backward = backwardRecords;
        LaneAccelerator<BakedLane<TTrack, TClip>>.Active = true;
    }
}
