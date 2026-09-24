using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl;

public sealed unsafe class MeasuredLanes : IDisposable
{
    internal float* Forward;
    internal float* Backward;
    private readonly nint _source;
    internal readonly int LaneCount;
    internal readonly float** LaneForward;
    internal readonly float** LaneBackward;
    internal readonly ulong* LaneKeys;
    bool _disposed;

    MeasuredLanes(byte* block, ushort duration, bool looping, nint source, int lanes)
    {
        LaneCount = lanes;
        var ticks = (nuint)(Math.Max(1u, duration) + 1u);
        LaneForward = (float**)(block + 2 * (nuint)lanes * ticks * sizeof(float));
        LaneBackward = LaneForward + lanes;
        LaneKeys = (ulong*)(LaneBackward + lanes);
        for (var l = 0; l < lanes; l++)
        {
            LaneForward[l] = (float*)block + (nuint)l * ticks;
            LaneBackward[l] = (float*)block + ((nuint)lanes + (nuint)l) * ticks;
        }
        Forward = LaneForward[0];
        Backward = LaneBackward[0];
        Duration = duration;
        Looping = looping;
        _source = source;
    }

    public ushort Duration { get; }

    public bool Looping { get; }

    public static MeasuredLanes Measure(TimelineAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return Measure(asset.Reference);
    }

    static MeasuredLanes Measure(TimelineRef reference) => Measure(reference, 0);

    internal static MeasuredLanes Measure(TimelineRef reference, ulong pairKey)
    {
        if (reference.Address == 0) throw new ArgumentException("Timeline asset is not loaded.");
        var header = (NativeHeader*)reference._p;
        var duration = header->Duration;
        if (duration > ushort.MaxValue) throw new ArgumentException($"Asset duration {duration} exceeds the 65535-tick lane position column.");
        var looping = header->Loops != 0;
        var pairs = checked((int)reference.PairCount);
        if (pairs > 256) throw new ArgumentException("Asset declares more than 256 timeline pairs; the typed lane cannot bind it.");
        int* chainProbe = stackalloc int[pairs];
        var lanes = ResultLanes(reference, pairKey, chainProbe);
        var ticks = (nuint)(Math.Max(1u, duration) + 1u);
        var block = (byte*)NativeMemory.AlignedAlloc(2 * (nuint)lanes * ticks * sizeof(float) + (nuint)lanes * 2 * (nuint)sizeof(float*) + (nuint)lanes * sizeof(ulong), 64);
        try
        {
            var measured = new MeasuredLanes(block, (ushort)duration, looping, reference.Address, lanes);
            Fill(reference, pairKey, measured, duration, looping);
            return measured;
        }
        catch
        {
            NativeMemory.AlignedFree(block);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NativeMemory.AlignedFree(Forward);
        Forward = null;
        Backward = null;
    }

    internal void ValidateBinding(TimelineAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ObjectDisposedException.ThrowIf(_disposed, this);
        var reference = asset.Reference;
        if (reference.Address == 0) throw new ArgumentException("Timeline asset is not loaded.");
        if (_source != reference.Address) throw new ArgumentException("Measured lanes were measured from a different TimelineAsset instance.");
    }

    static int GatherLanes(int* chains, int pairs, ulong* laneKeys, int* resLane, ulong* poolKeys, int* poolLane, out int poolCount)
    {
        var consumers = PairTable.ConsumerAt;
        var lanes = 0;
        var results = 0;
        var pools = 0;
        var rKeys = stackalloc ulong[PairTable.SlotRow];
        var rMeta = stackalloc byte[PairTable.SlotRow];
        int Pool(ulong key, byte width, bool shared)
        {
            if (shared)
                for (var q = 0; q < pools; q++) if (poolKeys[q] == key) return poolLane[q];
            var lane = lanes;
            if (shared) { poolKeys[pools] = key; poolLane[pools++] = lane; }
            if (laneKeys != null) laneKeys[lane] = key;
            if (width == 8)
            {
                if (laneKeys != null) laneKeys[lane + 1] = key ^ 0x8000000000000000UL;
                lanes += 2;
            }
            else lanes++;
            return lane;
        }
        for (var p = 0; p < pairs; p++)
            for (var e = chains[p]; e >= 0; e = consumers[e].Next)
            {
                if (consumers[e].DispatchOnly != 0) continue;
                var keys = consumers[e].Keys;
                if (keys == null) { _ = Pool(TypeKey<float>.Value, 4, true); continue; }
                var n = Math.Min(keys(rKeys, rMeta), PairTable.SlotRow);
                var outs = 0;
                for (var j = 0; j < n; j++)
                {
                    var shared = (rMeta[j] & 0x10) == 0;
                    var lane = Pool(rKeys[j], (byte)(rMeta[j] & 0xF), shared);
                    if (resLane != null) resLane[results++] = lane;
                    if (!shared) consumers[e].OutLanes[outs++] = lane;
                }
            }
        if (lanes == 0) _ = Pool(TypeKey<float>.Value, 4, true);
        poolCount = pools;
        return lanes;
    }

    static int ResultLanes(TimelineRef reference, ulong pairKey, int* chains)
    {
        var pairs = checked((int)reference.PairCount);
        var span = new Span<int>(chains, pairs);
        if (pairKey != 0) reference.Resolve(span, pairKey);
        else reference.Resolve(span);
        ulong* scratchKeys = stackalloc ulong[LaneBound];
        int* scratchLanes = stackalloc int[LaneBound];
        return GatherLanes(chains, pairs, null, null, scratchKeys, scratchLanes, out _);
    }

    static void Fill(TimelineRef reference, ulong pairKey, MeasuredLanes measured, uint duration, bool looping)
    {
        var pairs = checked((int)reference.PairCount);
        int* chains = stackalloc int[pairs];
        ResultLanes(reference, pairKey, chains);
        var consumers = PairTable.ConsumerAt;
        var lanes = measured.LaneCount;
        var laneKeys = measured.LaneKeys;
        int* resLane = stackalloc int[LaneBound];
        ulong* poolKeys = stackalloc ulong[LaneBound];
        int* poolLane = stackalloc int[LaneBound];
        var built = GatherLanes(chains, pairs, laneKeys, resLane, poolKeys, poolLane, out var pools);
        if (built != lanes) throw new InvalidOperationException("Measured lane count changed between passes.");
        byte* laneCell = stackalloc byte[lanes * 4];
        void** columns = stackalloc void*[PairTable.MaxPointers];
        Unsafe.InitBlock(columns, 0, PairTable.MaxPointers * (uint)sizeof(void*));
        var res = 0;
        for (var p = 0; p < pairs; p++)
            for (var e = chains[p]; e >= 0; e = consumers[e].Next)
            {
                if (consumers[e].DispatchOnly != 0 || consumers[e].Keys == null) continue;
                var n = consumers[e].Keys(null, null);
                var offset = consumers[e].Offset;
                for (var j = 0; j < n; j++) columns[offset + j] = laneCell + resLane[res++] * 4;
            }
        byte* indices = stackalloc byte[PairTable.MaxPointers];
        int* refreshSlots = stackalloc int[PairTable.MaxPointers];
        int* refreshColumns = stackalloc int[PairTable.MaxPointers];
        var refreshCount = 0;
        ulong boundMask = 0;
        PairTable.BindPair(reference, poolKeys, pools, indices, refreshSlots, refreshColumns, ref refreshCount, ref boundMask);
        for (var i = 0; i < refreshCount; i++) columns[refreshSlots[i]] = laneCell + poolLane[refreshColumns[i]] * 4;
        if (!PairTable.AnyWindowConstant)
        {
            void ProbeTick(uint position, bool reverse, float** table)
            {
                Unsafe.InitBlock(laneCell, 0, (uint)(lanes * 4));
                if (!reference.Select(reverse, (ushort)position, out var tick, out var flags))
                    throw new InvalidOperationException($"Timeline measurement did not advance from position {position}.");
                reference.ExecuteWindow(reverse, tick, flags, 0, new Span<int>(chains, pairs), columns, null, null, null);
                CopyLanes(table, lanes, laneCell, tick);
            }

            for (var tick = 0u; tick < duration; tick++)
            {
                var backwardPosition = tick + 1u == duration ? looping ? 0u : duration : tick + 1u;
                ProbeTick(tick, false, measured.LaneForward);
                ProbeTick(backwardPosition, true, measured.LaneBackward);
            }
        }
        else
        {
            FillWindows(reference, measured.LaneForward, measured.LaneBackward, lanes, laneCell, duration, looping, chains, pairs, columns);
        }
        for (var l = 0; l < lanes; l++) { measured.LaneForward[l][duration] = 0f; measured.LaneBackward[l][duration] = 0f; }
    }

    internal const int LaneBound = PairTable.MaxPointers / PairTable.SlotRow * (PairTable.MemoResults * 2 + 1);

    const int CacheStride = 64;

    static void AdvanceOrFail(TimelineRef reference, bool reverse, uint position, out ushort tick, out FrameFlags flags)
    {
        if (!reference.Advance(reverse, (ushort)position, out tick, out flags))
            throw new InvalidOperationException($"Timeline measurement did not advance from position {position}.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void CopyLanes(float** table, int lanes, byte* laneCell, uint tick)
    {
        for (var l = 0; l < lanes; l++) table[l][tick] = *(float*)(laneCell + l * 4);
    }

    static void FillWindows(TimelineRef reference, float** forward, float** backward, int lanes, byte* laneCell, uint duration, bool looping, int* chains, int pairs, void** columns)
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
        float* column = (float*)laneCell;
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
                    Unsafe.InitBlock(laneCell, 0, (uint)(lanes * 4));
                    reference.ExecuteWindow(false, forwardTick, forwardFlags, 0, new Span<int>(chains, pairs), columns, stepCached, stepCacheBase, forwardCache);
                    for (var tick = start; tick < end; tick++) CopyLanes(forward, lanes, laneCell, tick);
                    Unsafe.InitBlock(laneCell, 0, (uint)(lanes * 4));
                    reference.ExecuteWindow(true, backwardTick, backwardFlags, 0, new Span<int>(chains, pairs), columns, stepCached, stepCacheBase, backwardCache);
                    for (var tick = start; tick < end; tick++) CopyLanes(backward, lanes, laneCell, tick);
                    continue;
                }

                for (var tick = start; tick < end; tick++)
                {
                    AdvanceOrFail(reference, false, tick, out var tickForward, out var forwardFlag);
                    Unsafe.InitBlock(laneCell, 0, (uint)(lanes * 4));
                    reference.ExecuteWindow(false, tickForward, forwardFlag, 0, new Span<int>(chains, pairs), columns, stepCached, stepCacheBase, forwardCache);
                    CopyLanes(forward, lanes, laneCell, tick);

                    var backwardPosition = tick + 1u == duration ? looping ? 0u : duration : tick + 1u;
                    AdvanceOrFail(reference, true, backwardPosition, out var tickBackward, out var backwardFlag);
                    Unsafe.InitBlock(laneCell, 0, (uint)(lanes * 4));
                    reference.ExecuteWindow(true, tickBackward, backwardFlag, 0, new Span<int>(chains, pairs), columns, stepCached, stepCacheBase, backwardCache);
                    CopyLanes(backward, lanes, laneCell, tick);
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
