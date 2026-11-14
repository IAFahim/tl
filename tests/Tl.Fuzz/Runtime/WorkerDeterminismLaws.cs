using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Xunit;

namespace Tl.Fuzz.Runtime;

public sealed unsafe class WorkerDeterminismLaws
{
    static readonly int[] FixedWorkers = [1, 2, 3, 8, 11];

    struct FaultState
    {
        public int* Visits;
        public int* Firsts;
        public int* Ends;
        public int ChunkCount;
        public ulong FailMask;
        public int Running;
        public int BadBounds;
    }

    static void RunFaultChunk(void* statePtr, int first, int end, int chunk)
    {
        var st = (FaultState*)statePtr;
        Interlocked.Increment(ref st->Running);
        if ((uint)chunk >= (uint)st->ChunkCount || first != st->Firsts[chunk] || end != st->Ends[chunk])
            Interlocked.Increment(ref st->BadBounds);
        Thread.SpinWait(((chunk & 7) + 1) * 16);
        Interlocked.Increment(ref st->Visits[chunk]);
        Interlocked.Decrement(ref st->Running);
        if (chunk < 64 && (st->FailMask & (1UL << chunk)) != 0)
            throw new InvalidOperationException($"fault chunk {chunk}");
    }

    static (ulong Hash, byte[] Motion, byte[] Stats, byte[] Cues, PlaybackTable Players) RunPlainScenario(
        GeneratedLibrary lib,
        PlainInput[] input,
        FuzzRequestOp[] ops,
        int[] opsPerStep,
        bool[] forwards,
        int entities,
        int slotsPerEntity,
        int workerCount,
        TlWorkers? sharedPool)
    {
        using var world = new TimelineWorld(lib.Bytes);
        var players = new PlaybackTable(entities, slotsPerEntity);
        using var cues = new Events<FuzzCue>(1 << 16);
        using var owned = sharedPool == null && workerCount > 0 ? new TlWorkers(workerCount) : null;
        var pool = sharedPool ?? owned;

        var motion = new PlainMotion[entities];
        var stats = new PlainStats[entities];
        var hash = TlStateHash.Begin();
        var cueList = new List<EventRecord<FuzzCue>>();

        var opCursor = 0;
        for (var step = 0; step < forwards.Length; step++)
        {
            var count = opsPerStep[step];
            for (var k = 0; k < count; k++)
                LibraryGen.ApplyOp(players, in ops[opCursor++]);

            world.ApplyRequests(players);
            var sys = new PlainFuzzSystem
            {
                Motion = motion,
                Stats = stats,
                Input = input,
                Requests = world.Requests(players),
                Cues = cues.Writer,
            };
            if (pool == null) sys.Run(world, players, forwards[step]);
            else sys.Run(world, players, forwards[step], pool);
            world.Advance(players, forwards[step]);

            var drained = cues.Drain();
            for (var i = 0; i < drained.Length; i++)
                cueList.Add(drained[i]);

            fixed (PlainMotion* pm = motion) hash.Bytes((byte*)pm, motion.Length * sizeof(PlainMotion));
            fixed (PlainStats* ps = stats) hash.Bytes((byte*)ps, stats.Length * sizeof(PlainStats));
            fixed (EventRecord<FuzzCue>* pd = drained)
                if (drained.Length > 0) hash.Bytes((byte*)pd, drained.Length * sizeof(EventRecord<FuzzCue>));
            hash.Int(cues.Dropped);
            FuzzRunner.MixPlaybacks(ref hash, players);
        }

        return (
            hash.Value,
            MemoryMarshal.AsBytes<PlainMotion>(motion).ToArray(),
            MemoryMarshal.AsBytes<PlainStats>(stats).ToArray(),
            MemoryMarshal.AsBytes<EventRecord<FuzzCue>>(CollectionsMarshal.AsSpan(cueList)).ToArray(),
            players);
    }

    static (ulong Hash, byte[] Combine, byte[] Each, byte[] Idle, byte[] Flt, PlaybackTable Players) RunKindScenario(
        GeneratedLibrary lib,
        KindInputCol[] input,
        FuzzRequestOp[] ops,
        int[] opsPerStep,
        (int Entity, int Value)[] pulses,
        int[] pulsesPerStep,
        bool[] forwards,
        int entities,
        int slotsPerEntity,
        int workerCount,
        TlWorkers? sharedPool)
    {
        using var world = new TimelineWorld(lib.Bytes);
        var players = new PlaybackTable(entities, slotsPerEntity);
        using var store = new KindStore(world, players);
        using var owned = sharedPool == null && workerCount > 0 ? new TlWorkers(workerCount) : null;
        var pool = sharedPool ?? owned;

        var combine = new KindIntCol[entities];
        var each = new KindEachCol[entities];
        var idle = new KindIdleCol[entities];
        var flt = new KindFloatCol[entities];
        var hash = TlStateHash.Begin();
        var pulseWriter = store.Pulses<int>("Tl.Fuzz.RuntimeDomain.KCombineInt");

        var opCursor = 0;
        var pCursor = 0;
        for (var step = 0; step < forwards.Length; step++)
        {
            var count = opsPerStep[step];
            for (var k = 0; k < count; k++)
                LibraryGen.ApplyOp(players, in ops[opCursor++]);

            var pCount = pulsesPerStep[step];
            for (var k = 0; k < pCount; k++)
            {
                var p = pulses[pCursor++];
                pulseWriter.To(p.Entity, p.Value);
            }

            world.Tick(players, forwards[step]);
            var sys = new KindFuzzSystem
            {
                CombineCol = combine,
                EachCol = each,
                IdleCol = idle,
                FloatCol = flt,
                Input = input,
            };
            if (pool == null) sys.Run(world, players, forwards[step]);
            else sys.Run(world, players, forwards[step], pool);

            fixed (KindIntCol* p1 = combine) hash.Bytes((byte*)p1, combine.Length * sizeof(KindIntCol));
            fixed (KindEachCol* p2 = each) hash.Bytes((byte*)p2, each.Length * sizeof(KindEachCol));
            fixed (KindIdleCol* p3 = idle) hash.Bytes((byte*)p3, idle.Length * sizeof(KindIdleCol));
            fixed (KindFloatCol* p4 = flt) hash.Bytes((byte*)p4, flt.Length * sizeof(KindFloatCol));
            FuzzRunner.MixPlaybacks(ref hash, players);
        }

        return (
            hash.Value,
            MemoryMarshal.AsBytes<KindIntCol>(combine).ToArray(),
            MemoryMarshal.AsBytes<KindEachCol>(each).ToArray(),
            MemoryMarshal.AsBytes<KindIdleCol>(idle).ToArray(),
            MemoryMarshal.AsBytes<KindFloatCol>(flt).ToArray(),
            players);
    }

    [Fact]
    public void WorkersDeterminism_PlainAndKindRunsMatchSequentialFrom1To32Workers()
    {
        FuzzRunner.Run("T2-WorkerParity", 0x54320001UL, 14, (rng, _) =>
        {
            var entities = 64 + rng.NextInt(900);
            var slotsPerEntity = 2 + rng.NextInt(14);
            var timelines = 1 + rng.NextInt(5);
            var channels = 1 + rng.NextInt(4);
            var steps = 20 + rng.NextInt(45);

            var plainLib = LibraryGen.BuildPlainLibrary(ref rng, timelines, channels, 120, true, true, true);
            var kindLib = LibraryGen.BuildKindLibrary(ref rng, timelines, channels, 120, true, true, true, true);

            var plainInput = new PlainInput[entities];
            var kindInput = new KindInputCol[entities];
            for (var e = 0; e < entities; e++)
            {
                plainInput[e] = new PlainInput { Mult = 1 + rng.NextInt(3) };
                kindInput[e] = new KindInputCol { Factor = 1 + rng.NextInt(3) };
            }

            var ops = new List<FuzzRequestOp>();
            var opsPerStep = new int[steps];
            var pulses = new List<(int, int)>();
            var pulsesPerStep = new int[steps];
            var forwards = new bool[steps];
            var durations = plainLib.Durations;

            for (var s = 0; s < steps; s++)
            {
                forwards[s] = rng.NextInt(5) != 0;
                var nOps = s == 0 ? Math.Min(entities * 2, 128) : rng.NextInt(32);
                opsPerStep[s] = nOps;
                for (var k = 0; k < nOps; k++)
                    ops.Add(LibraryGen.NextRequest(ref rng, entities, timelines, channels, durations));

                var nPulses = rng.NextInt(16);
                pulsesPerStep[s] = nPulses;
                for (var k = 0; k < nPulses; k++)
                    pulses.Add((rng.NextInt(entities), rng.NextInt(25) - 12));
            }

            var opArr = ops.ToArray();
            var pulseArr = pulses.ToArray();

            var basePlain = RunPlainScenario(plainLib, plainInput, opArr, opsPerStep, forwards, entities, slotsPerEntity, 0, null);
            var baseKind = RunKindScenario(kindLib, kindInput, opArr, opsPerStep, pulseArr, pulsesPerStep, forwards, entities, slotsPerEntity, 0, null);
            try
            {
                Span<int> workerCounts = stackalloc int[6];
                FixedWorkers.CopyTo(workerCounts);
                workerCounts[5] = 1 + rng.NextInt(32);

                for (var wi = 0; wi < workerCounts.Length; wi++)
                {
                    var w = workerCounts[wi];
                    var parPlain = RunPlainScenario(plainLib, plainInput, opArr, opsPerStep, forwards, entities, slotsPerEntity, w, null);
                    try
                    {
                        Assert.Equal(basePlain.Hash, parPlain.Hash);
                        Assert.True(FuzzRunner.CompareBytes(basePlain.Motion, parPlain.Motion, $"w={w} Motion") is null);
                        Assert.True(FuzzRunner.CompareBytes(basePlain.Stats, parPlain.Stats, $"w={w} Stats") is null);
                        Assert.True(FuzzRunner.CompareBytes(basePlain.Cues, parPlain.Cues, $"w={w} Cues") is null);
                        var pbErr = FuzzRunner.ComparePlaybacks(basePlain.Players, parPlain.Players);
                        Assert.True(pbErr == null, $"w={w} Plain Playbacks: {pbErr}");
                    }
                    finally
                    {
                        parPlain.Players.Dispose();
                    }

                    var parKind = RunKindScenario(kindLib, kindInput, opArr, opsPerStep, pulseArr, pulsesPerStep, forwards, entities, slotsPerEntity, w, null);
                    try
                    {
                        Assert.Equal(baseKind.Hash, parKind.Hash);
                        Assert.True(FuzzRunner.CompareBytes(baseKind.Combine, parKind.Combine, $"w={w} Combine") is null);
                        Assert.True(FuzzRunner.CompareBytes(baseKind.Each, parKind.Each, $"w={w} Each") is null);
                        Assert.True(FuzzRunner.CompareBytes(baseKind.Idle, parKind.Idle, $"w={w} Idle") is null);
                        Assert.True(FuzzRunner.CompareBytes(baseKind.Flt, parKind.Flt, $"w={w} Flt") is null);
                        var pbErr = FuzzRunner.ComparePlaybacks(baseKind.Players, parKind.Players);
                        Assert.True(pbErr == null, $"w={w} Kind Playbacks: {pbErr}");
                    }
                    finally
                    {
                        parKind.Players.Dispose();
                    }
                }
            }
            finally
            {
                basePlain.Players.Dispose();
                baseKind.Players.Dispose();
            }
        });
    }

    [Fact]
    public void WorkersFaultInjection_CompletesEveryChunkAndStaysReusableForFullScenario()
    {
        FuzzRunner.Run("T2-WorkerFaults", 0x54320002UL, 30, (rng, _) =>
        {
            var workerCount = 2 + rng.NextInt(31);
            var chunkCount = 1 + rng.NextInt(64);
            var ends = new int[chunkCount];
            var firsts = new int[chunkCount];
            var visits = new int[chunkCount];

            var cursor = 0;
            for (var c = 0; c < chunkCount; c++)
            {
                firsts[c] = cursor;
                cursor += rng.NextInt(9);
                ends[c] = cursor;
            }

            using var pool = new TlWorkers(workerCount);
            fixed (int* pVisits = visits, pFirsts = firsts, pEnds = ends)
            {
                var rounds = 1 + rng.NextInt(3);
                for (var round = 0; round < rounds; round++)
                {
                    Array.Clear(visits);
                    ulong mask = 0;
                    var failCount = 1 + rng.NextInt(Math.Min(4, chunkCount));
                    for (var f = 0; f < failCount; f++)
                        mask |= 1UL << rng.NextInt(chunkCount);

                    var st = new FaultState
                    {
                        Visits = pVisits,
                        Firsts = pFirsts,
                        Ends = pEnds,
                        ChunkCount = chunkCount,
                        FailMask = mask,
                    };

                    var threw = false;
                    try
                    {
                        pool.Run(&RunFaultChunk, &st, pEnds, chunkCount);
                    }
                    catch (InvalidOperationException)
                    {
                        threw = true;
                    }

                    Assert.True(threw);
                    Assert.Equal(0, Volatile.Read(ref st.Running));
                    Assert.Equal(0, st.BadBounds);
                    for (var c = 0; c < chunkCount; c++)
                    {
                        var v = Volatile.Read(ref visits[c]);
                        if (v != 1)
                            throw new InvalidOperationException($"workerCount={workerCount} round={round} chunk={c}/{chunkCount} mask=0x{mask:X} visits={v}");
                    }
                }

                Array.Clear(visits);
                var okState = new FaultState
                {
                    Visits = pVisits,
                    Firsts = pFirsts,
                    Ends = pEnds,
                    ChunkCount = chunkCount,
                    FailMask = 0,
                };
                pool.Run(&RunFaultChunk, &okState, pEnds, chunkCount);
                Assert.Equal(0, Volatile.Read(ref okState.Running));
                Assert.Equal(0, okState.BadBounds);
                for (var c = 0; c < chunkCount; c++)
                    Assert.Equal(1, Volatile.Read(ref visits[c]));
            }

            var entities = 64 + rng.NextInt(200);
            var lib = LibraryGen.BuildPlainLibrary(ref rng, 3, 2, 60, true, true, true);
            var input = new PlainInput[entities];
            for (var e = 0; e < entities; e++) input[e] = new PlainInput { Mult = 1 };
            var steps = 15;
            var ops = new FuzzRequestOp[steps * 4];
            var opsPerStep = new int[steps];
            var forwards = new bool[steps];
            for (var s = 0; s < steps; s++)
            {
                forwards[s] = true;
                opsPerStep[s] = 4;
                for (var k = 0; k < 4; k++)
                    ops[s * 4 + k] = LibraryGen.NextRequest(ref rng, entities, lib.TimelineCount, lib.ChannelCount, lib.Durations);
            }

            var seq = RunPlainScenario(lib, input, ops, opsPerStep, forwards, entities, 6, 0, null);
            var reused = RunPlainScenario(lib, input, ops, opsPerStep, forwards, entities, 6, workerCount, pool);
            try
            {
                Assert.Equal(seq.Hash, reused.Hash);
                Assert.True(FuzzRunner.ComparePlaybacks(seq.Players, reused.Players) is null);
            }
            finally
            {
                seq.Players.Dispose();
                reused.Players.Dispose();
            }
        });
    }
}
