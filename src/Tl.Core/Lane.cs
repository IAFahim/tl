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
    public static TimelineLane<T> Seek(Span<ushort> positions, bool forward) => new(positions, forward);

    public static void Advance(Span<ushort> positions, bool forward, Span<float> effects)
        => new TimelineLane<T>(positions, forward).Apply(effects);
}

public ref struct TimelineLane<T>
    where T : unmanaged, ITimelineLane<T>
{
    const int Chunk = 4096;

    readonly Span<ushort> _positions;
    readonly bool _forward;

    internal TimelineLane(Span<ushort> positions, bool forward)
    {
        _positions = positions;
        _forward = forward;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe void Apply(Span<float> effects)
    {
        Check(effects);
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
                var end = RunEnd(positions, i);
                float delta;
                int next;
                if (forward)
                {
                    if (position >= duration) { i = end; continue; }
                    var tick = position;
                    delta = T.Effect(tick);
                    next = tick + 1;
                    if (looping && next == duration) next = 0;
                }
                else
                {
                    if (position == 0 && !looping || position > duration || looping && position == duration) { i = end; continue; }
                    ushort tick;
                    if (position == 0) tick = (ushort)(duration - 1);
                    else tick = (ushort)(position - 1);
                    delta = T.InverseEffect(tick);
                    next = tick;
                }
                if (end == i + 1)
                {
                    effects[i] += delta;
                    positions[i] = (ushort)next;
                }
                else
                {
                    Add(effects, i, end, delta);
                    Fill(positions, i, end, (ushort)next);
                }
            i = end;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    unsafe void ApplyAccelerated(Span<float> effects)
    {
        var positions = _positions;
        var count = positions.Length;
        var duration = T.Duration;
        if (count == 0 || duration == 0) return;
        var looping = T.Looping;
        var forward = _forward;
        var i = 0;
        var records = forward ? LaneAccelerator<T>.Forward : LaneAccelerator<T>.Backward;
        var gather = looping && duration > 1 && Avx2.IsSupported;
        while (i < count)
        {
            var chunkEnd = i + Chunk;
            if (chunkEnd > count) chunkEnd = count;
            if (gather && SingletonChunk(positions, i, chunkEnd))
            {
                var blockEnd = i + ((chunkEnd - i) >> 4 << 4);
                if (blockEnd > i)
                {
                    if (forward)
                        GatherForward(positions, effects, i, blockEnd);
                    else
                        GatherBackward(positions, effects, i, blockEnd);
                    i = blockEnd;
                    continue;
                }
            }
            while (i < chunkEnd)
            {
                var position = positions[i];
                var end = RunEnd(positions, i);
                if (end == i + 1)
                {
                    if (forward)
                    {
                        if (position < duration)
                        {
                            ref var r = ref records[position];
                            effects[i] += r.Effect;
                            positions[i] = r.Next;
                        }
                    }
                    else if (position <= duration)
                    {
                        ref var r = ref records[position];
                        if (r.Next != LaneAccelerator<T>.Skipped)
                        {
                            effects[i] += r.Effect;
                            positions[i] = r.Next;
                        }
                    }
                    i = end;
                    continue;
                }
                float delta;
                int next;
                if (forward)
                {
                    if (position >= duration) { i = end; continue; }
                    var tick = position;
                    delta = T.Effect(tick);
                    next = tick + 1;
                    if (looping && next == duration) next = 0;
                }
                else
                {
                    if (position == 0 && !looping || position > duration || looping && position == duration) { i = end; continue; }
                    ushort tick;
                    if (position == 0) tick = (ushort)(duration - 1);
                    else tick = (ushort)(position - 1);
                    delta = T.InverseEffect(tick);
                    next = tick;
                }
                Add(effects, i, end, delta);
                Fill(positions, i, end, (ushort)next);
                i = end;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static unsafe void GatherForward(Span<ushort> positions, Span<float> effects, int i, int limit)
    {
        var duration = T.Duration;
        var eff = LaneAccelerator<T>.ForwardEffects;
        var last = (ushort)(duration - 1);
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create(last);
        var zero = Vector256<ushort>.Zero;
        var one = Vector256.Create((ushort)1);
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var wrapMask = Vector256.Equals(pos, lastVector);
            var skipMask = Vector256.GreaterThan(pos, lastVector);
            var next = pos + one;
            next = Vector256.ConditionalSelect(wrapMask, zero, next);
            next = Vector256.ConditionalSelect(skipMask, pos, next);
            next.StoreUnsafe(ref p, (nuint)i);

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
    static unsafe void GatherBackward(Span<ushort> positions, Span<float> effects, int i, int limit)
    {
        var duration = T.Duration;
        var eff = LaneAccelerator<T>.BackwardByPosition;
        var durationVector = Vector256.Create(duration);
        var durationWide = Vector256.Create((uint)duration);
        var lastVector = Vector256.Create((ushort)(duration - 1));
        var zero = Vector256<ushort>.Zero;
        var step = Vector256.Create((ushort)0xFFFF);
        ref var p = ref MemoryMarshal.GetReference(positions);
        ref var e = ref MemoryMarshal.GetReference(effects);
        while (i < limit)
        {
            var pos = Vector256.LoadUnsafe(ref p, (nuint)i);
            var wrapMask = Vector256.Equals(pos, zero);
            var skipMask = Vector256.GreaterThan(pos, durationVector) | Vector256.Equals(pos, durationVector);
            var next = pos + step;
            next = Vector256.ConditionalSelect(wrapMask, lastVector, next);
            next = Vector256.ConditionalSelect(skipMask, pos, next);
            next.StoreUnsafe(ref p, (nuint)i);

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

    static bool RunShaped(Span<ushort> positions)
    {
        var sample = positions.Length;
        if (sample > 256) sample = 256;
        var pairs = 0;
        for (var i = 1; i < sample; i++)
            if (positions[i] == positions[i - 1]) pairs++;
        return pairs >= sample / 2;
    }

    static bool SingletonChunk(Span<ushort> positions, int start, int end)
    {
        var probe = start + 64;
        if (probe > end) probe = end;
        for (var i = start + 1; i < probe; i++)
            if (positions[i] == positions[i - 1]) return false;
        return true;
    }

    void Check(Span<float> effects)
    {
        var positions = _positions;
        if (effects.Length != positions.Length)
            throw new ArgumentException("Column length must equal position count.");
        if (MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(effects)))
            throw new ArgumentException("Lane columns must not overlap.");
    }

    static int RunEnd(Span<ushort> positions, int start)
    {
        var count = positions.Length;
        var value = positions[start];
        var end = start + 1;
        if (Vector512.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(positions);
            var search = Vector512.Create(value);
            var limit = count - 32;
            while (end <= limit)
            {
                var mask = Vector512.ExtractMostSignificantBits(Vector512.Equals(Vector512.LoadUnsafe(ref first, (nuint)end), search));
                if (mask != 0xFFFF_FFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 32;
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(positions);
            var search = Vector256.Create(value);
            var limit = count - 16;
            while (end <= limit)
            {
                var mask = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.LoadUnsafe(ref first, (nuint)end), search));
                if (mask != 0xFFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 16;
            }
        }
        while (end < count && positions[end] == value) end++;
        return end;
    }

    static void Add(Span<float> values, int start, int end, float delta)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(delta);
            var limit = length & ~15;
            for (; i < limit; i += 16) Vector512.Add(Vector512.LoadUnsafe(ref values[start], (nuint)i), vector).StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(delta);
            var limit = length & ~7;
            for (; i < limit; i += 8) Vector256.Add(Vector256.LoadUnsafe(ref values[start], (nuint)i), vector).StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] += delta;
    }

    static void Fill(Span<ushort> values, int start, int end, ushort value)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(value);
            var limit = length & ~31;
            for (; i < limit; i += 32) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(value);
            var limit = length & ~15;
            for (; i < limit; i += 16) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] = value;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct LaneMovementRecord
{
    public const ushort Skipped = 0xFFFF;
    public float Effect;
    public ushort Next;
    public ushort Pad;
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
        var tableFloats = (nuint)(Math.Max(1u, duration) + 1u);
        var tableBytes = tableFloats * sizeof(float);
        var forward = (float*)NativeMemory.AlignedAlloc(tableBytes, 64);
        var backward = (float*)NativeMemory.AlignedAlloc(tableBytes, 64);
        Buffer.MemoryCopy(measured.Forward, forward, (long)tableBytes, (long)tableBytes);
        Buffer.MemoryCopy(measured.Backward, backward, (long)tableBytes, (long)tableBytes);
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
        LaneAccelerator<BakedLane<TTrack, TClip>>.BackwardByPosition = backwardByPosition;
        LaneAccelerator<BakedLane<TTrack, TClip>>.Forward = forwardRecords;
        LaneAccelerator<BakedLane<TTrack, TClip>>.Backward = backwardRecords;
        LaneAccelerator<BakedLane<TTrack, TClip>>.Active = true;
    }
}
