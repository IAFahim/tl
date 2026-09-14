using Tl;

namespace PlaybackPrototype;

internal sealed class ChurnLane
{
    private const int BatchSize = 100_000;
    private const int Capacity = BatchSize * 21 + RetireBatch;
    private const int ParityPasses = 32;
    private const int Warmups = 25;
    private const int RetireBatch = 1_000;
    private const int RetireAfterPass = 23;
    private const int RetireSourcePass = 12;

    public string Name => "churn";

    public int Rows => Capacity;

    public AssetFixture Fixture => Fixtures.Churn;

    private sealed class World
    {
        public required TimelineAsset Asset;
        public required TimelineComponent[] TimelineRows;
        public required Acc[] Accs;
        public required int[] RowHandle;
        public required int[] HandleRow;
        public required int[] FreeHandles;
        public int FreeCount;
        public int HandleCount;
        public int Count;
        public uint GameTick = LaneSupport.FirstTick;

        public int Spawn(TimelineAsset asset)
        {
            if (Count == TimelineRows.Length)
                throw new InvalidOperationException("Facade capacity exhausted.");
            var row = Count++;
            TimelineRows[row] = new TimelineComponent(asset.Reference);
            Accs[row] = default;
            int handle;
            if (FreeCount > 0) handle = FreeHandles[--FreeCount];
            else
            {
                if (HandleCount == HandleRow.Length)
                    throw new InvalidOperationException("Facade handle capacity exhausted.");
                handle = HandleCount++;
            }

            RowHandle[row] = handle;
            HandleRow[handle] = row;
            return handle;
        }

        public void Retire(int handle)
        {
            var row = HandleRow[handle];
            var last = --Count;
            if (row != last)
            {
                TimelineRows[row] = TimelineRows[last];
                Accs[row] = Accs[last];
                var moved = RowHandle[last];
                RowHandle[row] = moved;
                HandleRow[moved] = row;
            }

            FreeHandles[FreeCount++] = handle;
        }
    }

    private World NewWorld(TimelineAsset asset)
    {
        return new World
        {
            Asset = asset,
            TimelineRows = new TimelineComponent[Capacity],
            Accs = new Acc[Capacity],
            RowHandle = new int[Capacity],
            HandleRow = new int[Capacity],
            FreeHandles = new int[Capacity],
        };
    }

    private PlaybackTable<ChurnTrack, ChurnClip> AttachTable(TimelineAsset asset)
    {
        return PlaybackTable<ChurnTrack, ChurnClip>.Attach(
            asset, Fixture.Duration, Fixture.Loops, new TableOptions(Capacity, Handles: true));
    }

    private static int FacadePass(World world, int spawns)
    {
        var duration = Fixtures.Churn.Duration;
        var query = Timeline.Rows(world.TimelineRows).Write(world.Accs);
        query.Tick(world.GameTick, 1);
        var retired = 0;
        var row = 0;
        while (row < world.Count)
        {
            var component = world.TimelineRows[row];
            var state = new TimelineState(1u, component.Position, component.Cycle);
            if (!TimelineMovement.Select(in state, duration, false, false, out _, out _, out _, out _))
            {
                var handle = world.RowHandle[row];
                world.Retire(handle);
                retired++;
                continue;
            }

            row++;
        }

        for (var i = 0; i < spawns; i++) world.Spawn(world.Asset);
        world.GameTick++;
        return retired;
    }

    private static int TablePass(World world, PlaybackTable<ChurnTrack, ChurnClip> table, int spawns)
    {
        var retired = table.AdvanceChurn(world.GameTick);
        for (var i = 0; i < spawns; i++) table.Spawn(0);
        world.GameTick++;
        return retired;
    }

    private static long HandleSample(World world)
    {
        long sampled = 0;
        for (var handle = 0; handle < world.HandleCount; handle += 4099)
            sampled = unchecked(sampled * 31 + world.Accs[world.HandleRow[handle]].Value);
        return sampled;
    }

    private static long TableHandleSample(PlaybackTable<ChurnTrack, ChurnClip> table)
    {
        long sampled = 0;
        for (var handle = 0; handle < table.HandleCount; handle += 4099)
            sampled = unchecked(sampled * 31 + table.AccValue(handle));
        return sampled;
    }

    private static long FullChecksum(World world, long seed)
        => Checksums.Sample(world.Accs, world.Count, full: true) ^ (long)world.Count ^ seed;

    private static long TableFullChecksum(World world, PlaybackTable<ChurnTrack, ChurnClip> table, long seed)
        => Checksums.Sample(table.Accs, table.Count, full: true) ^ (long)table.Count ^ seed;

    private (double NsPerPass, double AllocBytes) TimeFacade(World world)
    {
        var samples = new double[LaneSupport.Iterations];
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var warmup = 0; warmup < Warmups; warmup++) FacadePass(world, BatchSize);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < samples.Length; iteration++)
        {
            sw.Restart();
            FacadePass(world, BatchSize);
            sw.Stop();
            samples[iteration] = sw.Elapsed.TotalMilliseconds * 1e6;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(samples);
        return (samples[samples.Length / 2], allocated / (double)samples.Length);
    }

    private (double NsPerPass, double AllocBytes) TimeTable(World world, PlaybackTable<ChurnTrack, ChurnClip> table)
    {
        var measurement = Meter.Measure(
            () =>
            {
                TablePass(world, table, BatchSize);
            },
            Warmups,
            LaneSupport.Iterations,
            1);
        return (measurement.MedianNsPerPass, measurement.AllocatedBytesPerPass);
    }

    public LaneResult Run()
    {
        using var asset = Fixtures.LoadAsset(Fixture);

        var facadeWorld = NewWorld(asset);
        var facadeRetired = new int[ParityPasses];
        var facadeSamples = new long[ParityPasses];
        var retireBatch = new int[RetireBatch];
        for (var pass = 0; pass < ParityPasses; pass++)
        {
            if (pass == RetireSourcePass)
                for (var k = 0; k < RetireBatch; k++) retireBatch[k] = facadeWorld.Spawn(asset);
            facadeRetired[pass] = FacadePass(facadeWorld, BatchSize);
            facadeSamples[pass] = HandleSample(facadeWorld);
            if (pass == RetireAfterPass)
                foreach (var handle in retireBatch)
                    facadeWorld.Retire(handle);
        }

        var tableWorld = NewWorld(asset);
        using var parityTable = AttachTable(asset);
        var tableRetired = new int[ParityPasses];
        var tableSamples = new long[ParityPasses];
        var tableRetireBatch = new int[RetireBatch];
        for (var pass = 0; pass < ParityPasses; pass++)
        {
            if (pass == RetireSourcePass)
                for (var k = 0; k < RetireBatch; k++) tableRetireBatch[k] = parityTable.Spawn(0);
            tableRetired[pass] = TablePass(tableWorld, parityTable, BatchSize);
            tableSamples[pass] = TableHandleSample(parityTable);
            if (pass == RetireAfterPass)
                foreach (var handle in tableRetireBatch)
                    parityTable.Retire(handle);
        }

        var retiredMatch = LaneSupport.SequencesEqual(facadeRetired, tableRetired);
        var sampleMatch = retiredMatch && LaneSupport.SequencesEqual(facadeSamples, tableSamples);
        var fullMatch = sampleMatch && FullChecksum(facadeWorld, 0) == TableFullChecksum(tableWorld, parityTable, 0);
        var parity = retiredMatch && sampleMatch && fullMatch;
        var detail = $"count={facadeWorld.Count} retired={facadeRetired[ParityPasses - 1]}/pass";
        if (!parity)
        {
            if (!retiredMatch)
            {
                for (var i = 0; i < ParityPasses; i++)
                {
                    if (facadeRetired[i] != tableRetired[i])
                    {
                        detail += $" retiredDiff@{i}: {facadeRetired[i]}/{tableRetired[i]}";
                        break;
                    }
                }
            }
            else if (!sampleMatch)
            {
                for (var i = 0; i < ParityPasses; i++)
                {
                    if (facadeSamples[i] != tableSamples[i])
                    {
                        detail += $" sampleDiff@{i}: {facadeSamples[i]}/{tableSamples[i]}";
                        break;
                    }
                }
            }
            else
            {
                detail += $" fullDiff: {FullChecksum(facadeWorld, 0)}/{TableFullChecksum(tableWorld, parityTable, 0)}";
            }
        }

        var facadeTimed = NewWorld(asset);
        var (facadeNs, facadeAlloc) = TimeFacade(facadeTimed);

        var tableTimedWorld = NewWorld(asset);
        using var timedTable = AttachTable(asset);
        var (tableNs, tableAlloc) = TimeTable(tableTimedWorld, timedTable);

        return new LaneResult(
            Name,
            "facade-vs-table",
            Capacity,
            parity,
            facadeNs,
            tableNs,
            facadeAlloc,
            tableAlloc,
            detail);
    }
}
