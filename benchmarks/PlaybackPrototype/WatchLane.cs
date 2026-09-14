using Tl;

namespace PlaybackPrototype;

internal sealed class WatchLane
{
    private const int RowCount = 1_000_000;
    private const int InputCount = 262_144;
    private const int OwnerCount = 1_000_000;
    private const int ParityPasses = 32;
    private const int Warmups = 5;

    public string Name => "watch";

    public int Rows => RowCount;

    public AssetFixture Fixture => Fixtures.Watch;

    private static void FillMappings(int[] targets, int[] owners)
    {
        var state = 0x9E3779B9u;
        for (var i = 0; i < targets.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            targets[i] = (int)((state >> 8) % InputCount);
            owners[i] = (int)(((ulong)i * 0x9E3779B97F4A7C15ul >> 24) % OwnerCount);
        }
    }

    private sealed class World
    {
        public required TimelineAsset Asset;
        public required TimelineComponent[] TimelineRows;
        public required Input[] SourceInputs;
        public required int[] Targets;
        public required int[] Owners;
        public required Gather[] GatherBuf;
        public required Observed[] ObservedBuf;
        public required Acc[] OwnerAccs;
        public uint GameTick = LaneSupport.FirstTick;
    }

    private World NewWorld(TimelineAsset asset)
    {
        var rows = new TimelineComponent[RowCount];
        for (var i = 0; i < RowCount; i++)
            rows[i] = new TimelineComponent(asset.Reference) { Position = (uint)(i % (int)Fixture.Duration) };
        var world = new World
        {
            Asset = asset,
            TimelineRows = rows,
            SourceInputs = Inputs.Fill(InputCount, 13u),
            Targets = new int[RowCount],
            Owners = new int[RowCount],
            GatherBuf = new Gather[RowCount],
            ObservedBuf = new Observed[RowCount],
            OwnerAccs = new Acc[OwnerCount],
            GameTick = LaneSupport.FirstTick,
        };
        FillMappings(world.Targets, world.Owners);
        return world;
    }

    private PlaybackTable<WatchTrack, WatchClip> AttachTable(World world)
    {
        var table = PlaybackTable<WatchTrack, WatchClip>.Attach(
            world.Asset, Fixture.Duration, Fixture.Loops,
            new TableOptions(RowCount, OwnerCount: OwnerCount, WatchColumns: true));
        for (var i = 0; i < RowCount; i++)
            table.Spawn((uint)(i % (int)Fixture.Duration), world.Targets[i], world.Owners[i]);
        return table;
    }

    private static long[] RunFacade(World world, int passes, bool full)
    {
        var sequence = new long[passes];
        var query = Timeline.Rows(world.TimelineRows).Read(world.GatherBuf).Write(world.ObservedBuf);
        for (var pass = 0; pass < passes; pass++)
        {
            Inputs.Mutate(world.SourceInputs, world.GameTick);
            for (var row = 0; row < RowCount; row++)
                world.GatherBuf[row] = new Gather(world.SourceInputs[world.Targets[row]].Value);
            query.Tick(world.GameTick, 1);
            for (var row = 0; row < RowCount; row++)
                world.OwnerAccs[world.Owners[row]].Value += world.ObservedBuf[row].Value;
            world.GameTick++;
            sequence[pass] = Checksums.Sample(world.OwnerAccs, world.OwnerAccs.Length, full);
        }

        return sequence;
    }

    private static long[] RunTable(World world, PlaybackTable<WatchTrack, WatchClip> table, int passes, bool full)
    {
        var sequence = new long[passes];
        for (var pass = 0; pass < passes; pass++)
        {
            Inputs.Mutate(world.SourceInputs, world.GameTick);
            sequence[pass] = table.Observe(world.GameTick, world.SourceInputs, full);
            world.GameTick++;
        }

        return sequence;
    }

    private (double NsPerPass, double AllocBytes, long EndChecksum) TimeFacade(World world)
    {
        var query = Timeline.Rows(world.TimelineRows).Read(world.GatherBuf).Write(world.ObservedBuf);
        var gameTick = world.GameTick;
        long seed = 0;
        var samples = new double[LaneSupport.Iterations];
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var warmup = 0; warmup < Warmups; warmup++)
        {
            Inputs.Mutate(world.SourceInputs, gameTick);
            for (var row = 0; row < RowCount; row++)
                world.GatherBuf[row] = new Gather(world.SourceInputs[world.Targets[row]].Value);
            query.Tick(gameTick, 1);
            for (var row = 0; row < RowCount; row++)
                world.OwnerAccs[world.Owners[row]].Value += world.ObservedBuf[row].Value;
            gameTick++;
            seed = Checksums.Mix(seed, Checksums.Sample(world.OwnerAccs, world.OwnerAccs.Length, false));
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < samples.Length; iteration++)
        {
            sw.Restart();
            Inputs.Mutate(world.SourceInputs, gameTick);
            for (var row = 0; row < RowCount; row++)
                world.GatherBuf[row] = new Gather(world.SourceInputs[world.Targets[row]].Value);
            query.Tick(gameTick, 1);
            for (var row = 0; row < RowCount; row++)
                world.OwnerAccs[world.Owners[row]].Value += world.ObservedBuf[row].Value;
            gameTick++;
            seed = Checksums.Mix(seed, Checksums.Sample(world.OwnerAccs, world.OwnerAccs.Length, false));
            sw.Stop();
            samples[iteration] = sw.Elapsed.TotalMilliseconds * 1e6;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(samples);
        world.GameTick = gameTick;
        var end = Checksums.Sample(world.OwnerAccs, world.OwnerAccs.Length, true) ^ seed;
        return (samples[samples.Length / 2], allocated / (double)samples.Length, end);
    }

    private (double NsPerPass, double AllocBytes, long EndChecksum) TimeTable(World world, PlaybackTable<WatchTrack, WatchClip> table)
    {
        var gameTick = world.GameTick;
        long seed = 0;
        var measurement = Meter.Measure(
            () =>
            {
                Inputs.Mutate(world.SourceInputs, gameTick);
                seed = Checksums.Mix(seed, table.Observe(gameTick, world.SourceInputs, false));
                gameTick++;
            },
            Warmups,
            LaneSupport.Iterations,
            1);
        world.GameTick = gameTick;
        var end = Checksums.Sample(table.Accs, table.Accs.Length, true) ^ seed;
        return (measurement.MedianNsPerPass, measurement.AllocatedBytesPerPass, end);
    }

    public LaneResult Run()
    {
        using var asset = Fixtures.LoadAsset(Fixture);

        var facadeWorld = NewWorld(asset);
        var facadeSequence = RunFacade(facadeWorld, ParityPasses, full: true);
        var facadeState = LaneSupport.StateSummary(facadeWorld.TimelineRows);

        var tableWorld = NewWorld(asset);
        using var parityTable = AttachTable(tableWorld);
        var tableSequence = RunTable(tableWorld, parityTable, ParityPasses, full: true);
        var tableState = parityTable.StateSummary();
        var parity = LaneSupport.SequencesEqual(facadeSequence, tableSequence)
            && facadeState == tableState;

        facadeWorld = NewWorld(asset);
        var (facadeNs, facadeAlloc, facadeEnd) = TimeFacade(facadeWorld);

        tableWorld = NewWorld(asset);
        using var timedTable = AttachTable(tableWorld);
        var (tableNs, tableAlloc, tableEnd) = TimeTable(tableWorld, timedTable);
        var timedParity = facadeEnd == tableEnd;

        return new LaneResult(
            Name,
            "facade-vs-table",
            RowCount,
            parity && timedParity,
            facadeNs,
            tableNs,
            facadeAlloc,
            tableAlloc,
            $"state={facadeState.Positions}/{facadeState.Cycles}");
    }
}
