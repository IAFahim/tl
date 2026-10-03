using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Diagnostics.CodeAnalysis;

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

internal readonly ref struct TimelineLane<T>
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
        if (forward) LaneOps.AdvanceForward(duration, T.Looping, positions, next, 0, count);
        else LaneOps.AdvanceBackward(duration, T.Looping, positions, next, 0, count);
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
        var i = 0;
        if (active && duration > 1 && (Avx2.IsSupported || duration <= 8 && Vector128.IsHardwareAccelerated))
        {
            var end = count - (count & 15);
            if (forward)
            {
                if (duration <= 8)
                    LaneOps.EffPermuteForward(LaneAccelerator<T>.ForwardEffects, duration, looping, positions, next, effects, 0, end);
                else
                    LaneOps.EffectForward<FlatSource>(LaneAccelerator<T>.ForwardEffects, duration, looping, positions, next, effects, 0, end);
            }
            else
            {
                if (duration <= 8)
                    LaneOps.EffPermuteBackward(LaneAccelerator<T>.BackwardEffects, duration, looping, positions, next, effects, 0, end);
                else
                    LaneOps.EffectBackward<FlatSource>(LaneAccelerator<T>.BackwardEffects, duration, looping, positions, next, effects, 0, end);
            }
            i = end;
        }
        for (; i < count; i++)
        {
            var pos = positions[i];
            if (forward)
            {
                if (pos < duration)
                {
                    effects[i] += T.Effect(pos);
                    next[i] = looping && pos + 1 == duration ? (ushort)0 : (ushort)(pos + 1);
                }
                else next[i] = pos;
            }
            else
            {
                if (looping ? pos < duration : pos > 0 && pos <= duration)
                {
                    var tick = pos == 0 ? (ushort)(duration - 1) : (ushort)(pos - 1);
                    effects[i] += T.InverseEffect(tick);
                    next[i] = tick;
                }
                else next[i] = pos;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Apply(Span<float> effects)
    {
        Checked.Columns(_positions, effects);
        var positions = _positions;
        var count = positions.Length;
        var duration = T.Duration;
        if (count == 0 || duration == 0) return;
        if (!LaneAccelerator<T>.Active || RunShaped(_positions))
        {
            ApplyRuns(effects);
            return;
        }
        ApplyAccelerated(effects);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    void ApplyRuns(Span<float> effects)
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
        var permute = duration <= 8 && (Avx2.IsSupported || Vector128.IsHardwareAccelerated);
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            var blockEnd = i + ((chunkEnd - i) >> 4 << 4);
            if ((gather || permute) && duration > 1 && blockEnd > i && LaneOps.StaggeredEnds(positions, i, blockEnd))
            {
                if (forward)
                {
                    if (duration <= 8)
                        LaneOps.EffPermuteForward(LaneAccelerator<T>.ForwardEffects, T.Duration, looping, positions, default, effects, i, blockEnd);
                    else
                        LaneOps.EffectForward<FlatSource>(LaneAccelerator<T>.ForwardEffects, T.Duration, looping, positions, default, effects, i, blockEnd);
                }
                else
                {
                    if (duration <= 8)
                        LaneOps.EffPermuteBackward(LaneAccelerator<T>.BackwardEffects, T.Duration, looping, positions, default, effects, i, blockEnd);
                    else
                        LaneOps.EffectBackward<FlatSource>(LaneAccelerator<T>.BackwardEffects, T.Duration, looping, positions, default, effects, i, blockEnd);
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

internal static class LaneOps
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
        else if (Vector128.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(values);
            var search = Vector128.Create(value);
            var bound = limit - 8;
            while (end <= bound)
            {
                var mask = Vector128.ExtractMostSignificantBits(Vector128.Equals(Vector128.LoadUnsafe(ref first, (nuint)end), search)) & 0xFF;
                if (mask != 0xFF) return end + BitOperations.TrailingZeroCount(~mask & 0xFF);
                end += 8;
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
        ref var origin = ref MemoryMarshal.GetReference(values);
        if (Avx.IsSupported)
        {
            var vector = Vector256.Create(delta);
            var limit = length & ~7;
            for (; i < limit; i += 8)
                (Vector256.LoadUnsafe(ref origin, (nuint)(start + i)) + vector).StoreUnsafe(ref origin, (nuint)(start + i));
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var vector = Vector128.Create(delta);
            var limit = length & ~3;
            for (; i < limit; i += 4)
                (Vector128.LoadUnsafe(ref origin, (nuint)(start + i)) + vector).StoreUnsafe(ref origin, (nuint)(start + i));
        }
        for (; i < length; i++) values[start + i] += delta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void Fill(Span<ushort> values, int start, int end, ushort value)
    {
        var length = end - start;
        var i = 0;
        ref var origin = ref MemoryMarshal.GetReference(values);
        if (Avx2.IsSupported)
        {
            var vector = Vector256.Create(value);
            var limit = length & ~15;
            for (; i < limit; i += 16) vector.StoreUnsafe(ref origin, (nuint)(start + i));
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var vector = Vector128.Create(value);
            var limit = length & ~7;
            for (; i < limit; i += 8) vector.StoreUnsafe(ref origin, (nuint)(start + i));
        }
        for (; i < length; i++) values[start + i] = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("ReSharper", "RedundantUnsafeContext")]
    internal static unsafe void EffPermuteForward(float* eff, ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, Span<float> effects, int i, int limit)
    {
        var hasNext = !nextColumn.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        ref var e = ref MemoryMarshal.GetReference(effects);
        var last = (ushort)(duration - 1);
        if (Vector256.IsHardwareAccelerated)
        {
            var table = Vector256.Load(eff);
            var lastWide = Vector256.Create((uint)last);
            var lastVector = Vector256.Create(last);
            var zero = Vector256<ushort>.Zero;
            var one = Vector256.Create((ushort)1);
            while (i + 16 <= limit)
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
                var (wideLo, wideHi) = Vector256.Widen(pos);
                var gatherLo = Avx2.PermuteVar8x32(table, wideLo.AsInt32());
                var gatherHi = Avx2.PermuteVar8x32(table, wideHi.AsInt32());
                var skipLo = Vector256.GreaterThan(wideLo, lastWide).AsSingle();
                var skipHi = Vector256.GreaterThan(wideHi, lastWide).AsSingle();
                ApplyGather(ref e, (nuint)i, skipLo, skipHi, gatherLo, gatherHi);
                i += 16;
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var tableLow = Vector128.Load(eff);
            var tableHigh = Vector128.Load(eff + 4);
            var lastWide = Vector128.Create((uint)last);
            var lastVector = Vector128.Create(last);
            var zero = Vector128<ushort>.Zero;
            var one = Vector128.Create((ushort)1);
            var laneLow = Vector128.Create(3u);
            var laneHigh = Vector128.Create(4u);
            var end = limit - 7;
            while (i < end)
            {
                var pos = Vector128.LoadUnsafe(ref p, (nuint)i);
                if (hasNext)
                {
                    var skipMask = Vector128.GreaterThan(pos, lastVector);
                    var next = pos + one;
                    if (wrap) next = Vector128.ConditionalSelect(Vector128.Equals(pos, lastVector), zero, next);
                    next = Vector128.ConditionalSelect(skipMask, pos, next);
                    next.StoreUnsafe(ref n, (nuint)i);
                }
                var (wideLo, wideHi) = Vector128.Widen(pos);
                var effectLo = Vector128.LoadUnsafe(ref e, (nuint)i);
                Vector128.ConditionalSelect(Vector128.GreaterThan(wideLo, lastWide).AsSingle(), effectLo, effectLo + Permute8(tableLow, tableHigh, wideLo, laneLow, laneHigh)).StoreUnsafe(ref e, (nuint)i);
                var effectHi = Vector128.LoadUnsafe(ref e, (nuint)(i + 4));
                Vector128.ConditionalSelect(Vector128.GreaterThan(wideHi, lastWide).AsSingle(), effectHi, effectHi + Permute8(tableLow, tableHigh, wideHi, laneLow, laneHigh)).StoreUnsafe(ref e, (nuint)(i + 4));
                i += 8;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static Vector128<float> Permute8(Vector128<float> tableLow, Vector128<float> tableHigh, Vector128<uint> index, Vector128<uint> laneLow, Vector128<uint> laneHigh)
    {
        var lanes = index & laneLow;
        var fromLow = Vector128.Shuffle(tableLow, lanes.AsInt32());
        var fromHigh = Vector128.Shuffle(tableHigh, lanes.AsInt32());
        return Vector128.ConditionalSelect(Vector128.Equals(index & laneHigh, laneHigh).AsSingle(), fromHigh, fromLow);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void ApplyGather(ref float e, nuint i, Vector256<float> skipLo, Vector256<float> skipHi, Vector256<float> gatherLo, Vector256<float> gatherHi)
    {
        var effectLo = Vector256.LoadUnsafe(ref e, i);
        Vector256.ConditionalSelect(skipLo, effectLo, effectLo + gatherLo).StoreUnsafe(ref e, i);
        var effectHi = Vector256.LoadUnsafe(ref e, i + 8);
        Vector256.ConditionalSelect(skipHi, effectHi, effectHi + gatherHi).StoreUnsafe(ref e, i + 8);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("ReSharper", "RedundantUnsafeContext")]
    internal static unsafe void EffPermuteBackward(float* backward, ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, Span<float> effects, int i, int limit)
    {
        var hasNext = !nextColumn.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        ref var e = ref MemoryMarshal.GetReference(effects);
        var last = (ushort)(duration - 1);
        if (Vector256.IsHardwareAccelerated)
        {
            var table = Vector256.Load(backward);
            var lastVector = Vector256.Create(last);
            var lastWide = Vector256.Create((uint)last);
            var durationWide = Vector256.Create((uint)duration);
            var zeroUint = Vector256<uint>.Zero;
            var zero = Vector256<ushort>.Zero;
            var step = Vector256.Create((ushort)0xFFFF);
            while (i + 16 <= limit)
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
                var (indexLo, indexHi) = Vector256.Widen(index);
                var gatherLo = Avx2.PermuteVar8x32(table, indexLo.AsInt32());
                var gatherHi = Avx2.PermuteVar8x32(table, indexHi.AsInt32());
                var (posLo, posHi) = Vector256.Widen(pos);
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
                ApplyGather(ref e, (nuint)i, skipLo, skipHi, gatherLo, gatherHi);
                i += 16;
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var tableLow = Vector128.Load(backward);
            var tableHigh = Vector128.Load(backward + 4);
            var lastVector = Vector128.Create(last);
            var lastWide = Vector128.Create((uint)last);
            var durationWide = Vector128.Create((uint)duration);
            var zeroUint = Vector128<uint>.Zero;
            var zero = Vector128<ushort>.Zero;
            var step = Vector128.Create((ushort)0xFFFF);
            var laneLow = Vector128.Create(3u);
            var laneHigh = Vector128.Create(4u);
            var end = limit - 7;
            while (i < end)
            {
                var pos = Vector128.LoadUnsafe(ref p, (nuint)i);
                var index = Vector128.ConditionalSelect(Vector128.Equals(pos, zero), lastVector, pos + step);
                if (hasNext)
                {
                    Vector128<ushort> moveMask;
                    if (wrap) moveMask = Vector128.LessThanOrEqual(pos, lastVector);
                    else moveMask = Vector128.GreaterThan(pos, zero) & Vector128.LessThanOrEqual(pos, Vector128.Create(duration));
                    var next = Vector128.ConditionalSelect(moveMask, index, pos);
                    next.StoreUnsafe(ref n, (nuint)i);
                }
                var (indexLo, indexHi) = Vector128.Widen(index);
                var (posLo, posHi) = Vector128.Widen(pos);
                var effectLo = Vector128.LoadUnsafe(ref e, (nuint)i);
                Vector128.ConditionalSelect(wrap ? Vector128.GreaterThan(posLo, lastWide).AsSingle() : (Vector128.Equals(posLo, zeroUint) | Vector128.GreaterThan(posLo, durationWide)).AsSingle(), effectLo, effectLo + Permute8(tableLow, tableHigh, indexLo, laneLow, laneHigh)).StoreUnsafe(ref e, (nuint)i);
                var effectHi = Vector128.LoadUnsafe(ref e, (nuint)(i + 4));
                Vector128.ConditionalSelect(wrap ? Vector128.GreaterThan(posHi, lastWide).AsSingle() : (Vector128.Equals(posHi, zeroUint) | Vector128.GreaterThan(posHi, durationWide)).AsSingle(), effectHi, effectHi + Permute8(tableLow, tableHigh, indexHi, laneLow, laneHigh)).StoreUnsafe(ref e, (nuint)(i + 4));
                i += 8;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("ReSharper", "RedundantUnsafeContext")]
    internal static unsafe void EffectForward<TSrc>(void* source, ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, Span<float> effects, int i, int limit) where TSrc : struct, IEffectSource
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
        while (i + 16 <= limit)
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
            var (wideLo, wideHi) = Vector256.Widen(clamped);
            var gatherLo = TSrc.Gather(source, wideLo);
            var gatherHi = TSrc.Gather(source, wideHi);
            var skipLo = Vector256.Equals(wideLo, durationWide).AsSingle();
            var skipHi = Vector256.Equals(wideHi, durationWide).AsSingle();
            ApplyGather(ref e, (nuint)i, skipLo, skipHi, gatherLo, gatherHi);
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("ReSharper", "RedundantUnsafeContext")]
    internal static unsafe void EffectBackward<TSrc>(void* source, ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, Span<float> effects, int i, int limit) where TSrc : struct, IEffectSource
    {
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastWide = Vector256.Create((uint)(duration - 1));
        var zeroUint = Vector256<uint>.Zero;
        var zero = Vector256<ushort>.Zero;
        var oneUint = Vector256.Create(1u);
        var step = Vector256.Create((ushort)0xFFFF);
        var hasNext = !nextColumn.IsEmpty;
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i + 16 <= limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            if (hasNext)
            {
                var next = pos + step;
                Vector256<ushort> moveMask;
                if (wrap)
                {
                    moveMask = Vector256.LessThanOrEqual(pos, Vector256.Create((ushort)(duration - 1)));
                    next = Vector256.ConditionalSelect(Vector256.Equals(pos, zero), Vector256.Create((ushort)(duration - 1)), next);
                }
                else
                {
                    moveMask = Vector256.GreaterThan(pos, zero) & Vector256.LessThanOrEqual(pos, durationVector);
                }
                next = Vector256.ConditionalSelect(moveMask, next, pos);
                next.StoreUnsafe(ref n, (nuint)i);
            }
            var clamped = Vector256.Min(pos, durationVector);
            var (clampedLo, clampedHi) = Vector256.Widen(clamped);
            var indexLo = Vector256.Subtract(clampedLo, oneUint);
            indexLo = Vector256.ConditionalSelect(Vector256.Equals(clampedLo, zeroUint), lastWide, indexLo);
            var indexHi = Vector256.Subtract(clampedHi, oneUint);
            indexHi = Vector256.ConditionalSelect(Vector256.Equals(clampedHi, zeroUint), lastWide, indexHi);
            var gatherLo = TSrc.Gather(source, indexLo);
            var gatherHi = TSrc.Gather(source, indexHi);
            Vector256<float> skipLo, skipHi;
            if (wrap)
            {
                skipLo = Vector256.Equals(clampedLo, durationWide).AsSingle();
                skipHi = Vector256.Equals(clampedHi, durationWide).AsSingle();
            }
            else
            {
                var (posLo, posHi) = Vector256.Widen(pos);
                skipLo = (Vector256.Equals(posLo, zeroUint) | Vector256.GreaterThan(posLo, durationWide)).AsSingle();
                skipHi = (Vector256.Equals(posHi, zeroUint) | Vector256.GreaterThan(posHi, durationWide)).AsSingle();
            }
            ApplyGather(ref e, (nuint)i, skipLo, skipHi, gatherLo, gatherHi);
            i += 16;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static unsafe Vector256<float> SegGather(uint* directory, LaneSegment* segments, float* dense, Vector256<uint> ticks)
    {
        var entry = Avx2.GatherVector256((int*)directory, Vector256.ShiftRightLogical(ticks, LaneEncoding.BucketShift).AsInt32(), 4).AsUInt32();
        var escape = Vector256.Create(LaneEncoding.EscapeBit);
        var bad = Vector256.Equals(entry & escape, escape);
        var index = entry & Vector256.Create(~LaneEncoding.EscapeBit);
        var value = Avx2.GatherVector256((float*)segments, (index * Vector256.Create(4u) + Vector256.Create(2u)).AsInt32(), 4);
        if (bad != Vector256<uint>.Zero)
        {
            Span<float> patch = stackalloc float[8];
            value.StoreUnsafe(ref MemoryMarshal.GetReference(patch), 0);
            var bits = bad.ExtractMostSignificantBits();
            while (bits != 0)
            {
                var lane = BitOperations.TrailingZeroCount(bits);
                patch[lane] = SegValue(directory, segments, dense, ticks.GetElement(lane));
                bits &= bits - 1;
            }
            value = Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(patch), 0);
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static unsafe float SegValue(uint* directory, LaneSegment* segments, float* dense, uint tick)
    {
        var entry = directory[tick >> LaneEncoding.BucketShift];
        var segment = segments + (entry & ~LaneEncoding.EscapeBit);
        if ((entry & LaneEncoding.EscapeBit) != 0)
            while (tick < segment->Start || tick > segment->End)
                segment++;
        return segment->Kind == LaneSegment.Constant ? segment->V0
            : segment->Kind == LaneSegment.Ramp ? LaneEncoding.Ramp(segment->V0, segment->V1, segment->Start, segment->End, (int)tick)
            : dense[(nuint)LaneEncoding.DenseOffset(*segment) + (tick - (uint)segment->Start)];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void AdvanceForward(ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, int i, int limit)
    {
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        var last = (ushort)(duration - 1);
        if (Vector256.IsHardwareAccelerated)
        {
            var lastVector = Vector256.Create(last);
            var zero = Vector256<ushort>.Zero;
            var one = Vector256.Create((ushort)1);
            while (i + 16 <= limit)
            {
                var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
                var next = pos + one;
                if (wrap) next = Vector256.ConditionalSelect(Vector256.Equals(pos, lastVector), zero, next);
                next = Vector256.ConditionalSelect(Vector256.LessThanOrEqual(pos, lastVector), next, pos);
                next.StoreUnsafe(ref n, (nuint)i);
                i += 16;
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var lastVector = Vector128.Create(last);
            var zero = Vector128<ushort>.Zero;
            var one = Vector128.Create((ushort)1);
            while (i + 8 <= limit)
            {
                var pos = Vector128.LoadUnsafe(ref p, (nuint)i);
                var next = pos + one;
                if (wrap) next = Vector128.ConditionalSelect(Vector128.Equals(pos, lastVector), zero, next);
                next = Vector128.ConditionalSelect(Vector128.LessThanOrEqual(pos, lastVector), next, pos);
                next.StoreUnsafe(ref n, (nuint)i);
                i += 8;
            }
        }
        for (; i < limit; i++)
        {
            var pos = positions[i];
            nextColumn[i] = pos > last ? pos : wrap && pos == last ? (ushort)0 : (ushort)(pos + 1);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void AdvanceBackward(ushort duration, bool wrap, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, int i, int limit)
    {
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        var last = (ushort)(duration - 1);
        if (Vector256.IsHardwareAccelerated)
        {
            var lastVector = Vector256.Create(last);
            var durationVector = Vector256.Create(duration);
            var zero = Vector256<ushort>.Zero;
            var step = Vector256.Create((ushort)0xFFFF);
            while (i + 16 <= limit)
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
        else if (Vector128.IsHardwareAccelerated)
        {
            var lastVector = Vector128.Create(last);
            var durationVector = Vector128.Create(duration);
            var zero = Vector128<ushort>.Zero;
            var step = Vector128.Create((ushort)0xFFFF);
            while (i + 8 <= limit)
            {
                var pos = Vector128.LoadUnsafe(ref p, (nuint)i);
                var next = pos + step;
                Vector128<ushort> move;
                if (wrap)
                {
                    move = Vector128.LessThanOrEqual(pos, lastVector);
                    next = Vector128.ConditionalSelect(Vector128.Equals(pos, zero), lastVector, next);
                }
                else
                {
                    move = Vector128.GreaterThan(pos, zero) & Vector128.LessThanOrEqual(pos, durationVector);
                }
                next = Vector128.ConditionalSelect(move, next, pos);
                next.StoreUnsafe(ref n, (nuint)i);
                i += 8;
            }
        }
        for (; i < limit; i++)
        {
            var pos = positions[i];
            nextColumn[i] = (wrap ? pos > last : pos == 0 || pos > duration) ? pos : (ushort)(pos == 0 ? last : pos - 1);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("ReSharper", "RedundantUnsafeContext")]
    internal static unsafe void AdvanceRows(uint* motion, ReadOnlySpan<ushort> ids, ReadOnlySpan<ushort> positions, Span<ushort> nextColumn, bool forward, int i, int limit)
    {
        ref var idRef = ref MemoryMarshal.GetReference(ids);
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var n = ref MemoryMarshal.GetReference(nextColumn);
        var one = Vector256.Create(1u);
        var zero = Vector256<uint>.Zero;
        var low = Vector256.Create(0xFFFFu);
        while (i + 16 <= limit && Avx2.IsSupported)
        {
            var idv = Vector256.LoadUnsafe(ref idRef, (nuint)i);
            var (idLo, idHi) = Vector256.Widen(idv);
            var mLo = Avx2.GatherVector256((int*)motion, idLo.AsInt32(), 4).AsUInt32();
            var mHi = Avx2.GatherVector256((int*)motion, idHi.AsInt32(), 4).AsUInt32();
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var (posLo, posHi) = Vector256.Widen(pos);
            Vector256<uint> nextLo, nextHi;
            if (forward)
                (nextLo, nextHi) = (AdvanceForwardWide(posLo, mLo & low, Vector256.LessThan(mLo.AsInt32(), Vector256<int>.Zero), one, zero), AdvanceForwardWide(posHi, mHi & low, Vector256.LessThan(mHi.AsInt32(), Vector256<int>.Zero), one, zero));
            else
                (nextLo, nextHi) = (AdvanceBackwardWide(posLo, mLo & low, Vector256.LessThan(mLo.AsInt32(), Vector256<int>.Zero), one, zero), AdvanceBackwardWide(posHi, mHi & low, Vector256.LessThan(mHi.AsInt32(), Vector256<int>.Zero), one, zero));
            Vector256.Narrow(nextLo, nextHi).StoreUnsafe(ref n, (nuint)i);
            i += 16;
        }
        if (Vector128.IsHardwareAccelerated && limit - i >= 8)
        {
            var end = i + (limit - i & ~7);
            var oneNarrow = Vector128.Create(1u);
            var zeroNarrow = Vector128<uint>.Zero;
            while (i < end)
            {
                var idv = Vector128.LoadUnsafe(ref idRef, (nuint)i);
                var (idFirst, idSecond) = Vector128.Widen(idv);
                var mFirst = Vector128.Create(
                    motion[idFirst.ToScalar()],
                    motion[idFirst.GetElement(1)],
                    motion[idFirst.GetElement(2)],
                    motion[idFirst.GetElement(3)]);
                var mSecond = Vector128.Create(
                    motion[idSecond.ToScalar()],
                    motion[idSecond.GetElement(1)],
                    motion[idSecond.GetElement(2)],
                    motion[idSecond.GetElement(3)]);
                var pos = Vector128.LoadUnsafe(ref p, (nuint)i);
                var (posFirst, posSecond) = Vector128.Widen(pos);
                Vector128<uint> nextFirst, nextSecond;
                if (forward)
                    (nextFirst, nextSecond) = (AdvanceForwardNarrow(posFirst, mFirst & Vector128.Create(0xFFFFu), Vector128.LessThan(mFirst.AsInt32(), Vector128<int>.Zero), oneNarrow, zeroNarrow), AdvanceForwardNarrow(posSecond, mSecond & Vector128.Create(0xFFFFu), Vector128.LessThan(mSecond.AsInt32(), Vector128<int>.Zero), oneNarrow, zeroNarrow));
                else
                    (nextFirst, nextSecond) = (AdvanceBackwardNarrow(posFirst, mFirst & Vector128.Create(0xFFFFu), Vector128.LessThan(mFirst.AsInt32(), Vector128<int>.Zero), oneNarrow, zeroNarrow), AdvanceBackwardNarrow(posSecond, mSecond & Vector128.Create(0xFFFFu), Vector128.LessThan(mSecond.AsInt32(), Vector128<int>.Zero), oneNarrow, zeroNarrow));
                Vector128.Narrow(nextFirst, nextSecond).StoreUnsafe(ref n, (nuint)i);
                i += 8;
            }
        }
        for (; i < limit; i++)
        {
            var m = motion[ids[i]];
            var duration = (ushort)(m & 0xFFFF);
            var looping = (m & 0x80000000u) != 0;
            var pos = positions[i];
            if (forward)
                nextColumn[i] = pos < duration ? (looping && pos + 1 == duration ? (ushort)0 : (ushort)(pos + 1)) : pos;
            else
                nextColumn[i] = looping
                    ? pos < duration ? (pos == 0 ? (ushort)(duration - 1) : (ushort)(pos - 1)) : pos
                    : pos > 0 && pos <= duration ? (ushort)(pos - 1) : pos;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static Vector128<uint> AdvanceForwardNarrow(Vector128<uint> pos, Vector128<uint> duration, Vector128<int> loop, Vector128<uint> one, Vector128<uint> zero)
    {
        var next = pos + one;
        next = Vector128.ConditionalSelect(Vector128.Equals(pos, duration - one) & loop.AsUInt32(), zero, next);
        return Vector128.ConditionalSelect(Vector128.LessThan(pos, duration), next, pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static Vector128<uint> AdvanceBackwardNarrow(Vector128<uint> pos, Vector128<uint> duration, Vector128<int> loop, Vector128<uint> one, Vector128<uint> zero)
    {
        var prev = pos - one;
        var loopNext = Vector128.ConditionalSelect(Vector128.Equals(pos, zero), duration - one, prev);
        var loopMove = Vector128.LessThan(pos, duration);
        var finiteMove = Vector128.GreaterThan(pos, zero) & Vector128.LessThanOrEqual(pos, duration);
        var next = Vector128.ConditionalSelect(loop.AsUInt32(), loopNext, prev);
        var move = Vector128.ConditionalSelect(loop.AsUInt32(), loopMove, finiteMove);
        return Vector128.ConditionalSelect(move, next, pos);
    }
}

internal interface IEffectSource
{
    static abstract unsafe Vector256<float> Gather(void* source, Vector256<uint> ticks);
}

internal readonly unsafe struct FlatSource : IEffectSource
{
    public static Vector256<float> Gather(void* source, Vector256<uint> ticks)
        => Avx2.GatherVector256((float*)source, ticks.AsInt32(), 4);
}

internal readonly unsafe struct SegmentedForwardSource : IEffectSource
{
    public static Vector256<float> Gather(void* source, Vector256<uint> ticks)
    {
        var slot = (SlotView*)source;
        return LaneOps.SegGather(slot->Directory, slot->Segments, slot->Dense, ticks);
    }
}

internal readonly unsafe struct SegmentedBackwardSource : IEffectSource
{
    public static Vector256<float> Gather(void* source, Vector256<uint> ticks)
    {
        var slot = (SlotView*)source;
        return LaneOps.SegGather(slot->Directory + (nuint)LaneEncoding.Buckets(slot->TableTicks), slot->Segments, slot->Dense, ticks);
    }
}

internal static class LaneMovement
{
    internal static ushort ForwardNext(int position, int duration, bool looping)
        => (ushort)(looping && position + 1 == duration ? 0 : position + 1);

    internal static bool BackwardPlayable(int position, int duration, bool looping)
        => looping ? position < duration : position > 0 && position <= duration;

    internal static ushort BackwardNext(int position, int duration)
        => (ushort)(position == 0 ? duration - 1 : position - 1);
}

internal static unsafe class LaneEncoding
{
    internal const int BucketShift = 6;
    internal const int BucketTicks = 1 << BucketShift;
    internal const int FlatMaxTicks = 9;
    internal const int SegmentedMinTicks = 2049;
    internal const int MinConstantRun = 5;
    internal const int MinRampSpan = 4;
    internal const int MaxRampSpan = 512;
    internal const int MaxDirectionSegments = ushort.MaxValue;
    internal const uint EscapeBit = 0x80000000u;

    internal static int Buckets(uint ticks) => (int)((ticks + (BucketTicks - 1)) >> BucketShift);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Bits(float value) => Unsafe.As<float, uint>(ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BackwardTick(int position, int duration) => position == 0 ? duration - 1 : position - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float Ramp(float v0, float v1, int start, int end, int p)
        => p == start ? v0 : p == end ? v1 : v0 + (v1 - v0) * (p - start) / (end - start);

    internal static int DenseOffset(LaneSegment segment)
    {
        var bits = segment.V0;
        return Unsafe.As<float, int>(ref bits);
    }

    internal static float Value(SlotView* slot, nuint lane, bool forward, int tick)
    {
        var ticks = slot->TableTicks;
        if (forward)
        {
            var flat = slot->Forward;
            if (flat != null) return flat[lane * ticks + (nuint)tick];
        }
        else
        {
            var flat = slot->Backward;
            if (flat != null) return flat[lane * ticks + (nuint)tick];
        }
        var buckets = (nuint)Buckets(ticks);
        var dir = slot->Directory + lane * buckets * 2 + (forward ? 0u : (uint)buckets);
        var entry = dir[(nuint)tick >> BucketShift];
        var segment = slot->Segments + (entry & ~EscapeBit);
        if ((entry & EscapeBit) != 0)
            while (tick < segment->Start || tick > segment->End)
                segment++;
        return segment->Kind switch
        {
            LaneSegment.Constant => segment->V0,
            LaneSegment.Ramp => Ramp(segment->V0, segment->V1, segment->Start, segment->End, tick),
            _ => slot->Dense[(nuint)DenseOffset(*segment) + (nuint)(tick - segment->Start)]
        };
    }

    internal static int Encode(float* values, int ticks, List<LaneSegment> segments, List<float> dense)
    {
        var first = segments.Count;
        var t = 0;
        while (t < ticks)
        {
            var head = values[t];
            var run = t + 1;
            while (run < ticks && SameBits(values[run], head) && run - t <= ushort.MaxValue + 1)
                run++;
            if (run - t >= MinConstantRun)
            {
                segments.Add(new LaneSegment { Start = (ushort)t, End = (ushort)(run - 1), Kind = LaneSegment.Constant, V0 = head });
                t = run;
                continue;
            }
            if (RampHead(values, ticks, t))
            {
                var end = t + MinRampSpan - 1;
                while (end + 1 < Math.Min(ticks, t + MaxRampSpan) && RampFits(values, t, end + 1))
                    end++;
                segments.Add(new LaneSegment { Start = (ushort)t, End = (ushort)end, Kind = LaneSegment.Ramp, V0 = head, V1 = values[end] });
                t = end + 1;
                continue;
            }
            var denseStart = t;
            while (++t < ticks)
            {
                head = values[t];
                run = t + 1;
                while (run < ticks && SameBits(values[run], head) && run - t <= ushort.MaxValue + 1)
                    run++;
                if (run - t >= MinConstantRun || RampHead(values, ticks, t))
                    break;
            }
            var offset = dense.Count;
            var offsetBits = offset;
            for (var k = denseStart; k < t; k++)
                dense.Add(values[k]);
            segments.Add(new LaneSegment
            {
                Start = (ushort)denseStart,
                End = (ushort)(t - 1),
                Kind = LaneSegment.Dense,
                V0 = Unsafe.As<int, float>(ref offsetBits)
            });
        }
        return segments.Count - first;
    }

    internal static void BuildDirectory(LaneSegment* segments, int baseIndex, int count, int ticks, uint* directory)
    {
        var buckets = Buckets((uint)ticks);
        var last = baseIndex + count;
        for (var b = 0; b < buckets; b++)
        {
            var lo = b << BucketShift;
            var hi = Math.Min(lo + BucketTicks - 1, ticks - 1);
            var s = baseIndex;
            while (s < last && segments[s].End < lo)
                s++;
            var segment = segments[s];
            directory[b] = segment.Kind == LaneSegment.Constant && segment.Start <= lo && segment.End >= hi
                ? (uint)s
                : EscapeBit | (uint)s;
        }
    }

    static bool SameBits(float a, float b)
        => Unsafe.As<float, uint>(ref a) == Unsafe.As<float, uint>(ref b);

    static bool RampHead(float* values, int ticks, int t)
        => t + MinRampSpan <= ticks && RampFits(values, t, t + MinRampSpan - 1);

    static bool RampFits(float* values, int t, int end)
    {
        var v0 = values[t];
        var v1 = values[end];
        for (var i = t + 1; i < end; i++)
            if (!SameBits(values[i], Ramp(v0, v1, t, end, i)))
                return false;
        return true;
    }
}

internal static unsafe class LaneAccelerator<T>

    where T : unmanaged, ITimelineLane<T>
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public static float* ForwardEffects;
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public static float* BackwardEffects;
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
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
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static float* Forward;
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static float* Backward;
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public static ushort Duration;
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public static bool Looping;

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
        var previousForward = Forward;
        var previousBackward = Backward;
        Forward = forward;
        Backward = backward;
        Duration = duration;
        Looping = looping;
        if (previousForward != null)
        {
            NativeMemory.AlignedFree(previousForward);
            NativeMemory.AlignedFree(previousBackward!);
        }
        LaneAccelerator<BakedLane<TTrack, TClip>>.ForwardEffects = forward;
        LaneAccelerator<BakedLane<TTrack, TClip>>.BackwardEffects = backward;
        LaneAccelerator<BakedLane<TTrack, TClip>>.Active = true;
    }
}
