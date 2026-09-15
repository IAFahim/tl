using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Tl;

public interface ITimelineLane<T>
    where T : unmanaged, ITimelineLane<T>
{
    static abstract uint Duration { get; }
    static abstract bool Looping { get; }
    static abstract float Effect(uint position);
    static abstract float InverseEffect(uint position);
}

public static class Timeline<T>
    where T : unmanaged, ITimelineLane<T>
{
    public static TimelineLane<T> Seek(Span<uint> positions, bool forward) => new(positions, forward);
}

public ref struct TimelineLane<T>
    where T : unmanaged, ITimelineLane<T>
{
    readonly Span<uint> _positions;
    readonly bool _forward;

    internal TimelineLane(Span<uint> positions, bool forward)
    {
        _positions = positions;
        _forward = forward;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Apply(Span<float> effects, Span<long> cycles)
    {
        Check(effects, cycles);
        var positions = _positions;
        var count = positions.Length;
        var duration = T.Duration;
        if (count == 0 || duration == 0) return;
        var looping = T.Looping;
        var forward = _forward;
        var i = 0;
        while (i < count)
        {
            var position = positions[i];
            var end = RunEnd(positions, i);
            float delta;
            uint next;
            long cycleDelta;
            bool reset;
            if (forward)
            {
                if (position >= duration) { i = end; continue; }
                var tick = position;
                delta = T.Effect(tick);
                next = tick + 1;
                cycleDelta = 0;
                reset = false;
                if (looping)
                {
                    if (next == duration) { next = 0; cycleDelta = 1; }
                }
                else reset = true;
            }
            else
            {
                if (position == 0 && !looping || position > duration || looping && position == duration) { i = end; continue; }
                uint tick;
                if (position == 0) { tick = duration - 1; cycleDelta = -1; }
                else { tick = position - 1; cycleDelta = 0; }
                delta = T.InverseEffect(tick);
                next = tick;
                reset = !looping;
            }
            if (end == i + 1)
            {
                effects[i] += delta;
                positions[i] = next;
                if (reset) cycles[i] = 0;
                else if (cycleDelta != 0) cycles[i] += cycleDelta;
            }
            else
            {
                Add(effects, i, end, delta);
                Fill(positions, i, end, next);
                if (reset) Zero(cycles, i, end);
                else if (cycleDelta != 0) Add(cycles, i, end, cycleDelta);
            }
            i = end;
        }
    }

    void Check(Span<float> effects, Span<long> cycles)
    {
        var positions = _positions;
        if (effects.Length != positions.Length || cycles.Length != positions.Length)
            throw new ArgumentException("Column length must equal position count.");
        if (MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(effects))
            || MemoryMarshal.AsBytes(positions).Overlaps(MemoryMarshal.AsBytes(cycles))
            || MemoryMarshal.AsBytes(effects).Overlaps(MemoryMarshal.AsBytes(cycles)))
            throw new ArgumentException("Lane columns must not overlap.");
    }

    static int RunEnd(Span<uint> positions, int start)
    {
        var count = positions.Length;
        var value = positions[start];
        var end = start + 1;
        if (Vector512.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(positions);
            var search = Vector512.Create(value);
            var limit = count - 16;
            while (end <= limit)
            {
                var mask = Vector512.ExtractMostSignificantBits(Vector512.Equals(Vector512.LoadUnsafe(ref first, (nuint)end), search));
                if (mask != 0xFFFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 16;
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            ref var first = ref MemoryMarshal.GetReference(positions);
            var search = Vector256.Create(value);
            var limit = count - 8;
            while (end <= limit)
            {
                var mask = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.LoadUnsafe(ref first, (nuint)end), search));
                if (mask != 0xFFu) return end + BitOperations.TrailingZeroCount(~mask);
                end += 8;
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

    static void Add(Span<long> values, int start, int end, long delta)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(delta);
            var limit = length & ~7;
            for (; i < limit; i += 8) Vector512.Add(Vector512.LoadUnsafe(ref values[start], (nuint)i), vector).StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(delta);
            var limit = length & ~3;
            for (; i < limit; i += 4) Vector256.Add(Vector256.LoadUnsafe(ref values[start], (nuint)i), vector).StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] += delta;
    }

    static void Fill(Span<uint> values, int start, int end, uint value)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(value);
            var limit = length & ~15;
            for (; i < limit; i += 16) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(value);
            var limit = length & ~7;
            for (; i < limit; i += 8) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] = value;
    }

    static void Zero(Span<long> values, int start, int end)
    {
        var length = end - start;
        var i = 0;
        if (Vector512.IsHardwareAccelerated)
        {
            var vector = Vector512.Create(0L);
            var limit = length & ~7;
            for (; i < limit; i += 8) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var vector = Vector256.Create(0L);
            var limit = length & ~3;
            for (; i < limit; i += 4) vector.StoreUnsafe(ref values[start], (nuint)i);
        }
        for (; i < length; i++) values[start + i] = 0;
    }
}

public readonly struct BakedLane<TTrack, TClip> : ITimelineLane<BakedLane<TTrack, TClip>>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    public static uint Duration => LaneTable<TTrack, TClip>.Duration;
    public static bool Looping => LaneTable<TTrack, TClip>.Looping;
    public static float Effect(uint position) => LaneTable<TTrack, TClip>.Effect(position);
    public static float InverseEffect(uint position) => LaneTable<TTrack, TClip>.InverseEffect(position);

    public static void Bind(TimelineAsset asset) => LaneTable<TTrack, TClip>.Bind(asset);
}

static unsafe class LaneTable<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    public static float* Forward;
    public static float* Backward;
    public static uint Duration;
    public static bool Looping;

    public static float Effect(uint position) => Forward[position];

    public static float InverseEffect(uint position) => Backward[position];

    public static void Bind(TimelineAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var reference = asset.Reference;
        if (reference.Address == 0) throw new ArgumentException("Timeline asset is not loaded.");
        var key = PairRuntime<TTrack, TClip>.Key;
        if (!reference.Uses(key)) throw new ArgumentException($"Asset does not contain the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
        if (PairTable.Head(key) < 0) throw new ArgumentException($"No consumer is registered for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
        var header = (NativeHeader*)reference._p;
        var duration = header->Duration;
        var looping = header->Loops != 0;
        var forward = (float*)NativeMemory.AlignedAlloc((nuint)(Math.Max(1u, duration) * sizeof(float)), 64);
        var backward = (float*)NativeMemory.AlignedAlloc((nuint)(Math.Max(1u, duration) * sizeof(float)), 64);
        var pairs = checked((int)reference.PairCount);
        if (pairs > 256) throw new ArgumentException("Asset declares more than 256 timeline pairs; the typed lane cannot bind it.");
        int* chains = stackalloc int[pairs];
        reference.Resolve(new Span<int>(chains, pairs));
        ulong* keys = stackalloc ulong[1];
        keys[0] = TypeKey<float>.Value;
        byte* indices = stackalloc byte[256];
        byte* refreshSlots = stackalloc byte[256];
        byte* refreshColumns = stackalloc byte[256];
        var refreshCount = 0;
        ulong boundMask = 0;
        PairTable.Bind(reference, keys, 1, indices, refreshSlots, refreshColumns, ref refreshCount, ref boundMask);
        float* column = stackalloc float[1];
        void** bases = stackalloc void*[1];
        bases[0] = column;
        void** columns = stackalloc void*[256];
        for (var i = 0; i < 256; i++) columns[i] = null;
        for (var i = 0; i < refreshCount; i++) columns[refreshSlots[i]] = bases[refreshColumns[i]];

        float Measure(uint position, long cycle, float seed, bool reverse)
        {
            *column = seed;
            if (!reference.Select(reverse, position, cycle, out _, out var tick, out var frameCycle, out var flags))
                throw new InvalidOperationException($"Timeline measurement did not advance from position {position}.");
            reference.Execute(reverse, tick, 0u, frameCycle, flags, 0, new Span<int>(chains, pairs), columns);
            return *column - seed;
        }

        static bool FoldsIndependentlyOfColumnValue(float baseline, float seeded)
        {
            if (baseline == seeded) return true;
            var scale = MathF.Max(7f, MathF.Max(Math.Abs(baseline), Math.Abs(seeded)));
            return MathF.Abs(seeded - baseline) <= 16f * (MathF.BitIncrement(scale) - scale);
        }

        try
        {
            for (var tick = 0u; tick < duration; tick++)
            {
                var backwardPosition = tick + 1u == duration ? looping ? 0u : duration : tick + 1u;
                var baseline = Measure(tick, 0, 0, false);
                var cycled = Measure(tick, 3, 0, false);
                var seeded = Measure(tick, 0, 7, false);
                if (baseline != cycled || !FoldsIndependentlyOfColumnValue(baseline, seeded))
                    throw new ArgumentException($"Consumers of the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}) are not position-pure; the typed lane cannot bind them.");
                forward[tick] = baseline;
                baseline = Measure(backwardPosition, 0, 0, true);
                cycled = Measure(backwardPosition, 3, 0, true);
                seeded = Measure(backwardPosition, 0, 7, true);
                if (baseline != cycled || !FoldsIndependentlyOfColumnValue(baseline, seeded))
                    throw new ArgumentException($"Consumers of the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}) are not position-pure; the typed lane cannot bind them.");
                backward[tick] = baseline;
            }
        }
        catch
        {
            NativeMemory.AlignedFree(forward);
            NativeMemory.AlignedFree(backward);
            throw;
        }
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
    }
}
