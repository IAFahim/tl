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
        return Measure(asset.Reference);
    }

    internal static MeasuredLanes Measure(TimelineRef reference)
    {
        if (reference.Address == 0) throw new ArgumentException("Timeline asset is not loaded.");
        var header = (NativeHeader*)reference._p;
        var duration = header->Duration;
        if (duration > ushort.MaxValue) throw new ArgumentException($"Asset duration {duration} exceeds the 65535-tick lane position column.");
        var looping = header->Loops != 0;
        var forward = (float*)NativeMemory.AlignedAlloc(((Math.Max(1u, duration) + 1u) * sizeof(float)), 64);
        var backward = (float*)NativeMemory.AlignedAlloc(((Math.Max(1u, duration) + 1u) * sizeof(float)), 64);
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
        PairTable.BindPair(reference, keys, 1, indices, refreshSlots, refreshColumns, ref refreshCount, ref boundMask);
        float* column = stackalloc float[1];
        void** bases = stackalloc void*[1];
        bases[0] = column;
        void** columns = stackalloc void*[256];
        for (var i = 0; i < 256; i++) columns[i] = null;
        for (var i = 0; i < refreshCount; i++) columns[refreshSlots[i]] = bases[refreshColumns[i]];

        if (!PairTable.AnyWindowConstant)
        {
            float ProbeColumn(uint position, bool reverse)
            {
                *column = 0f;
                if (!reference.Select(reverse, (ushort)position, out _, out var tick, out var flags))
                    throw new InvalidOperationException($"Timeline measurement did not advance from position {position}.");
                reference.Execute(reverse, tick, flags, 0, new Span<int>(chains, pairs), columns);
                return *column;
            }

            for (var tick = 0u; tick < duration; tick++)
            {
                var backwardPosition = tick + 1u == duration ? looping ? 0u : duration : tick + 1u;
                forward[tick] = ProbeColumn(tick, false);
                backward[tick] = ProbeColumn(backwardPosition, true);
            }
        }
        else
        {
            FillWindows(reference, forward, backward, duration, looping, chains, pairs, columns, column);
        }
        forward[duration] = 0f;
        backward[duration] = 0f;
    }

    const int CacheStride = 64;

    static void AdvanceOrFail(TimelineRef reference, bool reverse, uint position, out ushort tick, out FrameFlags flags)
    {
        if (!reference.Advance(reverse, (ushort)position, out _, out tick, out flags))
            throw new InvalidOperationException($"Timeline measurement did not advance from position {position}.");
    }

    static void FillWindows(TimelineRef reference, float* forward, float* backward, uint duration, bool looping, int* chains, int pairs, void** columns, float* column)
    {
        var stageCount = reference.StageCount;
        var stages = reference.Stages;
        var maxProgram = 0;
        for (var s = 0; s < stageCount; s++) maxProgram = Math.Max(maxProgram, (int)stages[s].ProgramCount);
        var steps = Math.Max(maxProgram, 1);
        var cacheCapacity = steps * CacheStride;
        var scratchBytes = (long)steps * (sizeof(byte) + sizeof(int)) + (long)cacheCapacity * 2 * sizeof(float);
        byte* scratch = null;
        byte* stepCached;
        int* stepCacheBase;
        float* forwardCache;
        float* backwardCache;
        if (scratchBytes <= 8192)
        {
            byte* cachedBytes = stackalloc byte[steps];
            int* cachedBases = stackalloc int[steps];
            float* cachedFloats = stackalloc float[cacheCapacity * 2];
            stepCached = cachedBytes;
            stepCacheBase = cachedBases;
            forwardCache = cachedFloats;
            backwardCache = cachedFloats + cacheCapacity;
        }
        else
        {
            scratch = (byte*)NativeMemory.AlignedAlloc((nuint)scratchBytes, 64);
            stepCached = scratch;
            stepCacheBase = (int*)(scratch + steps);
            forwardCache = (float*)(scratch + steps * (sizeof(byte) + sizeof(int)));
            backwardCache = forwardCache + cacheCapacity;
        }
        try
        {
            for (var s = 0; s < stageCount; s++)
            {
                var stage = stages + s;
                var start = stage->Start;
                var end = stage->End;
                var program = (NativeStep*)(reference._p + stage->ProgramOffset);
                var count = (int)stage->ProgramCount;
                var cacheTotal = 0;
                var cachedSteps = 0;
                for (var i = 0; i < count; i++)
                {
                    stepCacheBase[i] = 0;
                    stepCached[i] = 0;
                    var head = chains[(int)program[i].Pair];
                    if (head < 0 || !PairTable.ChainWindowConstant(head, out var blendConstant) || !blendConstant(reference._p + program[i].Slot)) continue;
                    stepCacheBase[i] = cacheTotal;
                    cacheTotal += PairTable.ChainLength(head);
                    stepCached[i] = 1;
                    cachedSteps++;
                }

                AdvanceOrFail(reference, false, start, out var forwardTick, out var forwardFlags);
                Capture(reference, program, count, stepCached, stepCacheBase, forwardCache, chains, columns, column, forwardTick, forwardFlags, false);
                var representativePosition = start + 1u == duration ? looping ? 0u : duration : start + 1u;
                AdvanceOrFail(reference, true, representativePosition, out var backwardTick, out var backwardFlags);
                Capture(reference, program, count, stepCached, stepCacheBase, backwardCache, chains, columns, column, backwardTick, backwardFlags, true);

                if (cachedSteps == count)
                {
                    *column = 0f;
                    reference.ExecuteWindow(false, forwardTick, forwardFlags, 0, new Span<int>(chains, pairs), columns, stepCached, stepCacheBase, forwardCache);
                    var forwardValue = *column;
                    *column = 0f;
                    reference.ExecuteWindow(true, backwardTick, backwardFlags, 0, new Span<int>(chains, pairs), columns, stepCached, stepCacheBase, backwardCache);
                    var backwardValue = *column;
                    for (var tick = start; tick < end; tick++)
                    {
                        forward[tick] = forwardValue;
                        backward[tick] = backwardValue;
                    }
                    continue;
                }

                for (var tick = start; tick < end; tick++)
                {
                    AdvanceOrFail(reference, false, tick, out var tickForward, out var forwardFlag);
                    *column = 0f;
                    reference.ExecuteWindow(false, tickForward, forwardFlag, 0, new Span<int>(chains, pairs), columns, stepCached, stepCacheBase, forwardCache);
                    forward[tick] = *column;

                    var backwardPosition = tick + 1u == duration ? looping ? 0u : duration : tick + 1u;
                    AdvanceOrFail(reference, true, backwardPosition, out var tickBackward, out var backwardFlag);
                    *column = 0f;
                    reference.ExecuteWindow(true, tickBackward, backwardFlag, 0, new Span<int>(chains, pairs), columns, stepCached, stepCacheBase, backwardCache);
                    backward[tick] = *column;
                }
            }
        }
        finally
        {
            if (scratch != null) NativeMemory.AlignedFree(scratch);
        }
    }

    static void Capture(TimelineRef reference, NativeStep* program, int count, byte* stepCached, int* stepCacheBase, float* cacheValues, int* chains, void** columns, float* column, ushort tick, FrameFlags flags, bool reverse)
    {
        for (var i = 0; i < count; i++)
        {
            if (stepCached[i] == 0) continue;
            var slot = reference._p + program[i].Slot;
            var pair = (byte*)(reference.Pairs + program[i].Pair);
            var head = chains[(int)program[i].Pair];
            PairTable.RunChain(head, reverse, slot, pair, tick, flags, columns, 0, column, cacheValues + stepCacheBase[i]);
        }
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
        if (PairTable.HeadOf(key) < 0) throw new ArgumentException($"No consumer is registered for the timeline pair ({typeof(TTrack).Name}, {typeof(TClip).Name}).");
    }
}
