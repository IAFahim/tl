namespace PhaseClock;

public sealed class Case
{
    public required string Name;
    public required string Kernel;
    public required int Duration;
    public required float[] Lane;
    public required ushort[] Phase;
    public required int[] Row;
    public int[]? PhaseInt;
    public (int Start, int Length, ushort Phase)[]? Runs;
    public int Dir = 1;
    public int Pairs = 1;
    public int Cohorts;
    public int ActiveCohorts = 1;
    public int[]? ActiveMask;
    public Action<float[], int, Case, int> VectorTick = null!;
    public Action<float[], int, Case, int> ScalarTick = null!;
    public double Sink;
}

public static class Cases
{
    public static List<Case> All()
    {
        var list = new List<Case>();
        var const40 = Data.ConstantLane(Data.D40, 6f);
        var vary40 = Data.VaryingLane(Data.D40);
        var vary1024 = Data.VaryingLane(Data.D1024);
        var uniform = Data.UniformPhase();
        var mixed = Data.MixedPhase40();
        var unique = Data.UniquePhase1024();
        var dense = Data.DenseRows();
        var sparse = Data.SparseRows();
        Data.SortByPhase(mixed, dense, out var mixedSortedRow, out var mixedSortedPhase);
        var mixedRuns = Data.RunDescriptors(mixedSortedPhase);
        Data.SortByPhase(unique, sparse, out var uniqueSortedRow, out var uniqueSortedPhase);
        var uniqueRuns = Data.RunDescriptors(uniqueSortedPhase);
        var uniqueInt = unique.Select(p => (int)p).ToArray();

        list.Add(new Case
        {
            Name = "uniform-dense (plan 0.15-0.4)",
            Kernel = "BroadcastDense",
            Duration = Data.D40, Lane = const40, Phase = uniform, Row = dense,
            VectorTick = (acc, off, c, f) => Kernels.BroadcastDense(acc, off, c.Lane[Kernels.Tick(0, f, c.Duration, c.Dir)], Data.N),
            ScalarTick = (acc, off, c, f) => Kernels.ScalarPhase(acc, off, c.Row, c.Phase, c.Lane, f, c.Duration, c.Dir),
        });
        list.Add(new Case
        {
            Name = "uniform-sparse (plan 1.0-2.0)",
            Kernel = "BroadcastSparse",
            Duration = Data.D40, Lane = const40, Phase = uniform, Row = sparse,
            VectorTick = (acc, off, c, f) => Kernels.BroadcastSparse(acc, off, c.Row, 0, Data.N, c.Lane[Kernels.Tick(0, f, c.Duration, c.Dir)]),
            ScalarTick = (acc, off, c, f) => Kernels.ScalarPhase(acc, off, c.Row, c.Phase, c.Lane, f, c.Duration, c.Dir),
        });
        list.Add(new Case
        {
            Name = "mixed-sorted-sparse-40 (plan 1.0-2.0)",
            Kernel = "RunBroadcast",
            Duration = Data.D40, Lane = vary40, Phase = mixedSortedPhase, Row = mixedSortedRow, Runs = mixedRuns,
            VectorTick = (acc, off, c, f) => Kernels.RunBroadcast(acc, off, c.Row, c.Runs!, c.Lane, f, c.Duration, c.Dir),
            ScalarTick = (acc, off, c, f) => Kernels.ScalarPhase(acc, off, c.Row, c.Phase, c.Lane, f, c.Duration, c.Dir),
        });
        list.Add(new Case
        {
            Name = "unique-phase-sparse-1024 (risk case)",
            Kernel = "LaneGatherScalar",
            Duration = Data.D1024, Lane = vary1024, Phase = unique, Row = sparse, PhaseInt = uniqueInt,
            VectorTick = (acc, off, c, f) => Kernels.LaneGatherScalar(acc, off, c.Row, c.Phase, c.Lane, f, c.Duration, c.Dir),
            ScalarTick = (acc, off, c, f) => Kernels.ScalarPhase(acc, off, c.Row, c.Phase, c.Lane, f, c.Duration, c.Dir),
        });
                list.Add(new Case
        {
            Name = "lane-dense-synthetic",
            Kernel = "LaneDense",
            Duration = Data.D1024, Lane = vary1024, Phase = unique, Row = dense,
            VectorTick = (acc, off, c, f) => Kernels.LaneDense(acc, off, c.Lane, Data.N),
            ScalarTick = (acc, off, c, f) => { for (var i = 0; i < Data.N; i++) acc[off + i] += c.Lane[i]; },
        });
        list.Add(new Case
        {
            Name = "rewound uniform-dense",
            Kernel = "BroadcastDense(-1)",
            Duration = Data.D40, Lane = const40, Phase = uniform, Row = dense, Dir = -1,
            VectorTick = (acc, off, c, f) => Kernels.BroadcastDense(acc, off, c.Lane[Kernels.Tick(0, f, c.Duration, c.Dir)], Data.N),
            ScalarTick = (acc, off, c, f) => Kernels.ScalarPhase(acc, off, c.Row, c.Phase, c.Lane, f, c.Duration, c.Dir),
        });
        var four = new Case
        {
            Name = "four-pairs (plan 4x single)",
            Kernel = "4x BroadcastDense",
            Duration = Data.D40, Lane = const40, Phase = uniform, Row = dense, Pairs = 4,
            VectorTick = (acc, off, c, f) => { for (var k = 0; k < 4; k++) Kernels.BroadcastDense(acc, off, 6f + k, Data.N); },
            ScalarTick = (acc, off, c, f) => { for (var k = 0; k < 4; k++) for (var i = 0; i < Data.N; i++) acc[off + i] += 6f + k; },
        };
        list.Add(four);
        var two = new Case
        {
            Name = "two-playbacks (plan 2x single)",
            Kernel = "2x BroadcastDense",
            Duration = Data.D40, Lane = const40, Phase = uniform, Row = dense, Pairs = 2,
            VectorTick = (acc, off, c, f) => { Kernels.BroadcastDense(acc, off, 6f, Data.N); Kernels.BroadcastDense(acc, off, 7f, Data.N); },
            ScalarTick = (acc, off, c, f) => { for (var i = 0; i < Data.N; i++) { acc[off + i] += 6f; acc[off + i] += 7f; } },
        };
        list.Add(two);
        var mask = new int[64];
        for (var k = 0; k < 64; k++) mask[k] = k < 2 ? 1 : 0;
        list.Add(new Case
        {
            Name = "64-cohorts-2-active (plan 2x single)",
            Kernel = "2x BroadcastDense + mask-64",
            Duration = Data.D40, Lane = const40, Phase = uniform, Row = dense,
            Cohorts = 64, ActiveCohorts = 2, ActiveMask = mask,
            VectorTick = (acc, off, c, f) =>
            {
                for (var k = 0; k < c.Cohorts; k++)
                    if (c.ActiveMask![k] != 0) Kernels.BroadcastDense(acc, off, 6f, Data.N);
            },
            ScalarTick = (acc, off, c, f) =>
            {
                for (var k = 0; k < c.Cohorts; k++)
                    if (c.ActiveMask![k] != 0) for (var i = 0; i < Data.N; i++) acc[off + i] += 6f;
            },
        });
        return list;
    }
}
