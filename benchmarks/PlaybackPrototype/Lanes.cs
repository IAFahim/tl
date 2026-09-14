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
    public const uint FirstTick = 200_000u;

    public static bool SequencesEqual(long[] first, long[] second)
    {
        if (first.Length != second.Length) return false;
        for (var i = 0; i < first.Length; i++)
            if (first[i] != second[i])
                return false;
        return true;
    }

    public static bool SequencesEqual(int[] first, int[] second)
    {
        if (first.Length != second.Length) return false;
        for (var i = 0; i < first.Length; i++)
            if (first[i] != second[i])
                return false;
        return true;
    }

    public static double Ratio(double facade, double table) => table <= 0 ? double.PositiveInfinity : facade / table;

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
}

internal sealed class StaticLane<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>, ICodeTrack
    where TClip : unmanaged, IAmountClip
{
    private readonly string _name;
    private readonly bool _requirePulse;
    private readonly int _parityPasses;
    private readonly int _warmups;
    private readonly int _rowCount;

    public StaticLane(string name, AssetFixture fixture, int rowCount, bool requirePulse, int parityPasses = 32, int warmups = 5)
    {
        _name = name;
        Fixture = fixture;
        _rowCount = rowCount;
        _requirePulse = requirePulse;
        _parityPasses = parityPasses;
        _warmups = warmups;
    }

    public string Name => _name;

    public int Rows => _rowCount;

    public AssetFixture Fixture { get; }

    private bool Stagger => Fixture.Duration > 1;

    private sealed class World
    {
        public required TimelineAsset Asset;
        public required TimelineComponent[] TimelineRows;
        public required Input[] Inputs;
        public required Acc[] Accs;
        public uint GameTick = LaneSupport.FirstTick;
    }

    private World NewWorld(TimelineAsset asset)
    {
        var rows = new TimelineComponent[_rowCount];
        for (var i = 0; i < _rowCount; i++)
            rows[i] = new TimelineComponent(asset.Reference) { Position = Stagger ? (uint)(i % (int)Fixture.Duration) : 0u };
        return new World { Asset = asset, TimelineRows = rows, Inputs = Inputs.Fill(_rowCount, 7u), Accs = new Acc[_rowCount] };
    }

    private PlaybackTable<TTrack, TClip> AttachTable(TimelineAsset asset)
    {
        var table = PlaybackTable<TTrack, TClip>.Attach(
            asset, Fixture.Duration, Fixture.Loops, new TableOptions(_rowCount));
        for (var i = 0; i < _rowCount; i++)
            table.Spawn(Stagger ? (uint)(i % (int)Fixture.Duration) : 0u);
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

    private static long[] RunTable(World world, PlaybackTable<TTrack, TClip> table, int passes, bool full)
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
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var warmup = 0; warmup < _warmups; warmup++)
        {
            Inputs.Mutate(world.Inputs, gameTick);
            query.Tick(gameTick, 1);
            gameTick++;
            seed = Checksums.Mix(seed, Checksums.Sample(world.Accs, world.Accs.Length, false));
        }

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

    private (double NsPerPass, double AllocBytes, long EndChecksum) TimeTable(World world, PlaybackTable<TTrack, TClip> table)
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
            _warmups,
            LaneSupport.Iterations,
            1);
        world.GameTick = gameTick;
        var end = Checksums.Sample(table.Accs, table.Count, true) ^ seed;
        return (measurement.MedianNsPerPass, measurement.AllocatedBytesPerPass, end);
    }

    public LaneResult Run()
    {
        using var asset = Fixtures.LoadAsset(Fixture);
        if (_rowCount <= 0 || Fixture.Duration <= 0)
            return new LaneResult(_name, "facade-vs-table", _rowCount, false, 0, 0, 0, 0, $"rows={_rowCount} duration={Fixture.Duration}");

        var facadeWorld = NewWorld(asset);
        var facadeSequence = RunFacade(facadeWorld, _parityPasses, full: true);
        var facadeState = LaneSupport.StateSummary(facadeWorld.TimelineRows);

        var tableWorld = NewWorld(asset);
        using var parityTable = AttachTable(asset);
        if (_requirePulse && !parityTable.PulseClassified)
            return new LaneResult(_name, "facade-vs-table", _rowCount, false, 0, 0, 0, 0, "pulse classification missing");
        var tableSequence = RunTable(tableWorld, parityTable, _parityPasses, full: true);
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
            _name,
            "facade-vs-table",
            _rowCount,
            parity && timedParity,
            facadeNs,
            tableNs,
            facadeAlloc,
            tableAlloc,
            $"state={facadeState.Positions}/{facadeState.Cycles}");
    }
}
