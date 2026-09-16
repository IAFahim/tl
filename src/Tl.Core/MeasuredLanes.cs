using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

public sealed unsafe class MeasuredLanes : IDisposable
{
    internal float* Forward;
    internal float* Backward;
    internal nint Source;
    bool _disposed;

    MeasuredLanes(float* forward, float* backward, ushort duration, bool looping, nint source)
    {
        Forward = forward;
        Backward = backward;
        Duration = duration;
        Looping = looping;
        Source = source;
    }

    public ushort Duration { get; }

    public bool Looping { get; }

    public static MeasuredLanes Measure(TimelineAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var reference = asset.Reference;
        if (reference.Address == 0) throw new ArgumentException("Timeline asset is not loaded.");
        var header = (NativeHeader*)reference._p;
        var duration = header->Duration;
        if (duration > ushort.MaxValue) throw new ArgumentException($"Asset duration {duration} exceeds the 65535-tick lane position column.");
        var looping = header->Loops != 0;
        var forward = (float*)NativeMemory.AlignedAlloc((nuint)((Math.Max(1u, duration) + 1u) * sizeof(float)), 64);
        var backward = (float*)NativeMemory.AlignedAlloc((nuint)((Math.Max(1u, duration) + 1u) * sizeof(float)), 64);
        try
        {
            Fill(reference, forward, backward, duration, looping);
        }
        catch
        {
            NativeMemory.AlignedFree(forward);
            NativeMemory.AlignedFree(backward);
            throw;
        }
        return new MeasuredLanes(forward, backward, (ushort)duration, looping, reference.Address);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NativeMemory.AlignedFree(Forward);
        NativeMemory.AlignedFree(Backward);
        Forward = null;
        Backward = null;
    }

    internal void ValidateBinding(TimelineAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ObjectDisposedException.ThrowIf(_disposed, this);
        var reference = asset.Reference;
        if (reference.Address == 0) throw new ArgumentException("Timeline asset is not loaded.");
        if (Source != reference.Address) throw new ArgumentException("Measured lanes were measured from a different TimelineAsset instance.");
    }

    static void Fill(TimelineRef reference, float* forward, float* backward, uint duration, bool looping)
    {
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

        void Prove(uint tick)
        {
            var backwardPosition = tick + 1u == duration ? looping ? 0u : duration : tick + 1u;
            var baseline = Measure(tick, 0, 0, false);
            var cycled = Measure(tick, 3, 0, false);
            var seeded = Measure(tick, 0, 7, false);
            if (baseline != cycled || !FoldsIndependentlyOfColumnValue(baseline, seeded))
                throw new ArgumentException("Registered lane consumers are not position-pure; the measured tables would not reproduce live playback.");
            baseline = Measure(backwardPosition, 0, 0, true);
            cycled = Measure(backwardPosition, 3, 0, true);
            seeded = Measure(backwardPosition, 0, 7, true);
            if (baseline != cycled || !FoldsIndependentlyOfColumnValue(baseline, seeded))
                throw new ArgumentException("Registered lane consumers are not position-pure; the measured tables would not reproduce live playback.");
        }

        var stride = duration / 64u + 1u;
        for (var tick = 0u; tick < duration; tick += stride) Prove(tick);
        if (duration > 0u && (duration - 1u) % stride != 0u) Prove(duration - 1u);
        for (var tick = 0u; tick < duration; tick++)
        {
            var backwardPosition = tick + 1u == duration ? looping ? 0u : duration : tick + 1u;
            forward[tick] = Measure(tick, 0, 0, false);
            backward[tick] = Measure(backwardPosition, 0, 0, true);
        }
        forward[duration] = 0f;
        backward[duration] = 0f;
    }
}

internal static class LaneGuards
{
    internal static void ValidatePair<TTrack, TClip>(TimelineAsset asset)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        ArgumentNullException.ThrowIfNull(asset);
        var reference = asset.Reference;
        if (reference.Address == 0) throw new ArgumentException("Timeline asset is not loaded.");
        var key = PairRuntime<TTrack, TClip>.Key;
        if (!reference.Uses(key)) throw new ArgumentException($"Asset does not contain the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
        if (PairTable.Head(key) < 0) throw new ArgumentException($"No consumer is registered for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
    }
}
