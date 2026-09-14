using Tl;

namespace PlaybackPrototype;

internal sealed record LaneResult(
    string Lane,
    string Kind,
    int Rows,
    bool ParityOk,
    double FacadeNsPerPass,
    double TableNsPerPass,
    double FacadeAllocBytesPerPass,
    double TableAllocBytesPerPass,
    string Detail);

internal static class LaneSupport
{
    public const int Iterations = 15;

    public static bool SequencesEqual(long[] first, long[] second)
    {
        if (first.Length != second.Length) return false;
        for (var i = 0; i < first.Length; i++)
            if (first[i] != second[i])
                return false;
        return true;
    }

    public static (long Positions, long Cycles) StateSummary(TimelineComponent[] rows)
    {
        long positions = 0;
        long cycles = 0;
        for (var i = 0; i < rows.Length; i++)
        {
            positions += rows[i].Position;
            cycles += rows[i].Cycle;
        }

        return (positions, cycles);
    }

    public static double Ratio(double facade, double table) => table <= 0 ? double.PositiveInfinity : facade / table;
}

internal sealed class SweepLane
{
    private const int RowCount = 1_000_000;
    private const int ParityPasses = 32;
    private const int Warmups = 5;
    private const uint FirstTick = 200_000u;

    public string Name => "sweep";

    public int Rows => RowCount;

    public required AssetFixture Fixture { get; init; }

    private sealed class World
    {
        public required TimelineAsset Asset;
        public required TimelineComponent[] TimelineRows;
        public required Input[] Inputs;
        public required Acc[] Accs;
        public uint GameTick = FirstTick;
    }

    private World NewWorld(TimelineAsset asset)
    {
        var rows = new TimelineComponent[RowCount];
        for (var i = 0; i < RowCount; i++)
            rows[i] = new TimelineComponent(asset.Reference) { Position = (uint)(i % (int)Fixture.Duration) };
        return new World { Asset = asset, TimelineRows = rows, Inputs = Inputs.Fill(RowCount, 7u), Accs = new Acc[RowCount] };
    }

    private PlaybackTable<MoveTrack, MoveClip> AttachTable(TimelineAsset asset)
    {
        var table = PlaybackTable<MoveTrack, MoveClip>.Attach(
            asset, Fixture.Duration, Fixture.Loops, new TableOptions(RowCount));
        for (var i = 0; i < RowCount; i++)
            table.Spawn((uint)(i % (int)Fixture.Duration));
        return table;
    }

    private static long[] RunFacade(World world, int passes, bool full)
    {
        var sequence = new long[passes];
        var query = Timeline.Rows(world.TimelineRows).Read(world.Inputs).Write(world.Accs);
        for (var pass = 0; pass < passes; pass++)
        {
            Inputs.Mutate(world.Inputs, world.GameTick);
            query.Tick(world.GameTick, 1);
            world.GameTick++;
            sequence[pass] = Checksums.Sample(world.Accs, world.Accs.Length, full);
        }

        return sequence;
    }

    private static long[] RunTable(World world, PlaybackTable<MoveTrack, MoveClip> table, int passes, bool full)
    {
        var sequence = new long[passes];
        for (var pass = 0; pass < passes; pass++)
        {
            Inputs.Mutate(world.Inputs, world.GameTick);
            sequence[pass] = table.Advance(world.GameTick, world.Inputs, full);
            world.GameTick++;
        }

        return sequence;
    }

    private (double NsPerPass, double AllocBytes, long EndChecksum) TimeFacade(World world)
    {
        var query = Timeline.Rows(world.TimelineRows).Read(world.Inputs).Write(world.Accs);
        var gameTick = world.GameTick;
        long seed = 0;
        var samples = new double[LaneSupport.Iterations];

        for (var warmup = 0; warmup < Warmups; warmup++)
        {
            Inputs.Mutate(world.Inputs, gameTick);
            query.Tick(gameTick, 1);
            gameTick++;
            seed = Checksums.Mix(seed, Checksums.Sample(world.Accs, world.Accs.Length, false));
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < samples.Length; iteration++)
        {
            sw.Restart();
            Inputs.Mutate(world.Inputs, gameTick);
            query.Tick(gameTick, 1);
            gameTick++;
            seed = Checksums.Mix(seed, Checksums.Sample(world.Accs, world.Accs.Length, false));
            sw.Stop();
            samples[iteration] = sw.Elapsed.TotalMilliseconds * 1e6;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(samples);
        world.GameTick = gameTick;
        var end = Checksums.Sample(world.Accs, world.Accs.Length, true) ^ seed;
        return (samples[samples.Length / 2], allocated / (double)samples.Length, end);
    }

    private (double NsPerPass, double AllocBytes, long EndChecksum) TimeTable(World world, PlaybackTable<MoveTrack, MoveClip> table)
    {
        var gameTick = world.GameTick;
        long seed = 0;
        var measurement = Meter.Measure(
            () =>
            {
                Inputs.Mutate(world.Inputs, gameTick);
                seed = Checksums.Mix(seed, table.Advance(gameTick, world.Inputs, false));
                gameTick++;
            },
            Warmups,
            LaneSupport.Iterations,
            1);
        world.GameTick = gameTick;
        var end = Checksums.Sample(table.Accs, table.Count, true) ^ seed;
        return (measurement.MedianNsPerPass, measurement.AllocatedBytesPerPass, end);
    }

    public LaneResult Run()
    {
        using var asset = Fixtures.LoadAsset(Fixture);

        var facadeWorld = NewWorld(asset);
        var facadeSequence = RunFacade(facadeWorld, ParityPasses, full: true);
        var facadeState = LaneSupport.StateSummary(facadeWorld.TimelineRows);

        var tableWorld = NewWorld(asset);
        using var parityTable = AttachTable(asset);
        var tableSequence = RunTable(tableWorld, parityTable, ParityPasses, full: true);
        var tableState = parityTable.StateSummary();
        var parity = LaneSupport.SequencesEqual(facadeSequence, tableSequence)
            && facadeState == tableState;

        facadeWorld = NewWorld(asset);
        var (facadeNs, facadeAlloc, facadeEnd) = TimeFacade(facadeWorld);

        tableWorld = NewWorld(asset);
        using var timedTable = AttachTable(asset);
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

internal sealed class MovementLane
{
    private const int RowCount = 1_000_000;
    private const int Duration = 64;
    private const int Warmups = 5;
    private const uint FirstTick = 200_000u;

    public string Name => "movement";

    public int Rows => RowCount;

    public LaneResult Run()
    {
        var positions = new uint[RowCount];
        var cycles = new long[RowCount];
        for (var i = 0; i < RowCount; i++) positions[i] = (uint)(i % Duration);
        var gameTick = FirstTick;
        long seed = 0;
        var measurement = Meter.Measure(
            () =>
            {
                for (var row = 0; row < RowCount; row++)
                {
                    var state = new Tl.TimelineState(1u, positions[row], cycles[row]);
                    if (!Tl.TimelineMovement.Select(in state, Duration, true, false, out var next, out _, out _, out _))
                        continue;
                    positions[row] = next.Position;
                    cycles[row] = next.Cycle;
                }

                for (var row = 0; row < RowCount; row += 4099)
                    seed = unchecked(seed * 31 + positions[row] + cycles[row]);
                gameTick++;
            },
            Warmups,
            LaneSupport.Iterations,
            1);
        return new LaneResult(
            Name,
            "context",
            RowCount,
            true,
            measurement.MedianNsPerPass,
            double.NaN,
            measurement.AllocatedBytesPerPass,
            double.NaN,
            $"seed={seed}");
    }
}

internal sealed class FloorLane
{
    private const int RowCount = 1_000_000;
    private const int Warmups = 5;
    private const uint FirstTick = 200_000u;

    public string Name => "floor";

    public int Rows => RowCount;

    public LaneResult Run()
    {
        var inputs = Inputs.Fill(RowCount, 11u);
        var accs = new Acc[RowCount];
        var gameTick = FirstTick;
        long seed = 0;
        var measurement = Meter.Measure(
            () =>
            {
                Inputs.Mutate(inputs, gameTick);
                for (var i = 0; i < accs.Length; i++)
                    accs[i].Value = unchecked(accs[i].Value * 31 + (int)inputs[i].Value);
                seed = Checksums.Mix(seed, accs[gameTick % RowCount].Value);
                gameTick++;
            },
            Warmups,
            LaneSupport.Iterations,
            1);
        return new LaneResult(
            Name,
            "context",
            RowCount,
            true,
            measurement.MedianNsPerPass,
            double.NaN,
            measurement.AllocatedBytesPerPass,
            double.NaN,
            $"seed={seed}");
    }
}
