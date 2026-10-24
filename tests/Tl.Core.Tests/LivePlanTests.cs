using System.Runtime.CompilerServices;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct PlanClip(float Amount);
public readonly record struct PlanTrack(int Pattern) : IBlend<PlanClip>
{
    public void Blend(in PlanClip a, in PlanClip b, float factor, out PlanClip result) => result = a;
}

internal static unsafe class LivePlanConsumers
{
    [ModuleInitializer]
    internal static void Install()
    {
        var fold = PairRuntime<PlanTrack, PlanClip>.Consume(&Fold, &FoldKeys, "tl\0PlanFold");
        PairRuntime<PlanTrack, PlanClip>.ConsumeDispatch(&Row, &RowKeys, &Diag, "tl\0PlanRow", fold);
    }

    private static int FoldKeys(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x10; }
        return 1;
    }

    private static void Fold(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] = tick;

    private static int RowKeys(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value; meta[0] = 4 | 0x40;
            keys[1] = TypeKey<int>.Value; meta[1] = 4;
        }
        return 2;
    }

    private static void Diag(ulong key, long slot) => throw new ArgumentException("Timeline<PlanTrack, PlanClip> requires its named fold feed.");

    private static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((int*)columns[1])[row] = (int)(*(float*)columns[0] * 1000);
}

internal static unsafe class LatePlanConsumers
{
    internal static int Installed;

    internal static void Install()
    {
        if (Interlocked.Exchange(ref Installed, 1) != 0) return;
        PairRuntime<PlanTrack, PlanClip>.ConsumeDispatch(&Row, &RowKeys, &Diag, "tl\0PlanLateRow");
    }

    private static int RowKeys(ulong* keys, byte* meta)
    {
        if (keys != null) { keys[0] = TypeKey<float>.Value; meta[0] = 4; }
        return 1;
    }

    private static void Diag(ulong key, long slot) => throw new ArgumentException("Timeline<PlanTrack, PlanClip> requires its named fold feed.");

    private static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => ((float*)columns[0])[row] = 777f + row;
}

public sealed unsafe class LivePlanTests
{
    const int Variants = 8;

    static TimelineAsset Asset(int pattern)
        => TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<PlanTrack, PlanClip>(new PlanTrack(pattern))
            .Clip(0, 0, (ushort)(48 + pattern * 16), new PlanClip(pattern + 1f))
            .Bake()));

    [Fact]
    public void AlternatingIdsMatchGroupedRunsAndSingles()
    {
        var owned = Enumerable.Range(0, Variants).Select(Asset).ToArray();
        try
        {
            var ids = new ushort[owned.Length * 3];
            var clocks = new ushort[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = owned[i % owned.Length].Index;
                clocks[i] = (ushort)(i * 7 % 33);
            }
            var alternating = new int[ids.Length];
            var columns = new ColumnSet();
            columns.Add(new float[ids.Length]);
            columns.Add(alternating);
            Timeline<PlanTrack, PlanClip>.Apply(ids, clocks, true, in columns);

            var order = Enumerable.Range(0, ids.Length).OrderBy(i => ids[i]).ThenBy(i => clocks[i]).ToArray();
            var grouped = new int[ids.Length];
            var groupedIds = new ushort[ids.Length];
            var groupedClocks = new ushort[ids.Length];
            for (var j = 0; j < order.Length; j++)
            {
                groupedIds[j] = ids[order[j]];
                groupedClocks[j] = clocks[order[j]];
            }
            var groupedColumns = new ColumnSet();
            groupedColumns.Add(new float[ids.Length]);
            groupedColumns.Add(grouped);
            Timeline<PlanTrack, PlanClip>.Apply(groupedIds, groupedClocks, true, in groupedColumns);

            var singles = new int[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                var single = new ColumnSet();
                single.Add(new float[1]);
                single.Add(singles.AsSpan(i, 1));
                Timeline<PlanTrack, PlanClip>.Apply(ids.AsSpan(i, 1), clocks.AsSpan(i, 1), true, in single);
            }

            var rank = new int[ids.Length];
            for (var j = 0; j < order.Length; j++) rank[order[j]] = j;
            for (var i = 0; i < ids.Length; i++)
            {
                Assert.Equal(alternating[i], singles[i]);
                Assert.Equal(alternating[i], grouped[rank[i]]);
            }
        }
        finally
        {
            foreach (var asset in owned) asset.Dispose();
        }
    }

    [Fact]
    public void DistinctColumnSetShapesKeepDistinctPlans()
    {
        var owned = Enumerable.Range(0, Variants).Select(Asset).ToArray();
        try
        {
            var ids = new ushort[owned.Length * 3];
            var clocks = new ushort[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = owned[i % owned.Length].Index;
                clocks[i] = (ushort)(i * 7 % 33);
            }
            var first = new int[ids.Length];
            var second = new int[ids.Length];
            var scratch = new byte[ids.Length];
            for (var round = 0; round < 4; round++)
            {
                var columns = new ColumnSet();
                if ((round & 1) == 0)
                {
                    columns.Add(new float[ids.Length]);
                    columns.Add(first);
                    columns.Add(scratch);
                }
                else
                {
                    columns.Add(scratch);
                    columns.Add(new float[ids.Length]);
                    columns.Add(second);
                }
                Timeline<PlanTrack, PlanClip>.Apply(ids, clocks, true, in columns);
            }
            Assert.Equal(first, second);
        }
        finally
        {
            foreach (var asset in owned) asset.Dispose();
        }
    }

    [Fact]
    public void RegistryGrowthAfterFirstPlanRebindsIt()
    {
        var owned = Enumerable.Range(0, Variants).Select(Asset).ToArray();
        try
        {
            var ids = new ushort[owned.Length * 3];
            var clocks = new ushort[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = owned[i % owned.Length].Index;
                clocks[i] = (ushort)(i * 7 % 33);
            }
            var effects = new int[ids.Length];
            var carrier = new float[ids.Length];
            var columns = new ColumnSet();
            columns.Add(carrier);
            columns.Add(effects);
            Timeline<PlanTrack, PlanClip>.Apply(ids, clocks, true, in columns);
            var before = effects.ToArray();
            LatePlanConsumers.Install();
            Timeline<PlanTrack, PlanClip>.Apply(ids, clocks, true, in columns);
            Assert.Equal(before, effects);
            for (var i = 0; i < carrier.Length; i++)
                Assert.Equal(777f + i, carrier[i]);
        }
        finally
        {
            foreach (var asset in owned) asset.Dispose();
        }
    }
}
