using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class PreparePlayTests : IDisposable
{
    readonly TimelineAsset[] _owned = Enumerable.Range(0, 8).Select(Asset).ToArray();

    internal static TimelineAsset Asset(int pattern)
        => TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<PlanTrack, PlanClip>(new PlanTrack(pattern))
            .Clip(0, 0, (ushort)(48 + pattern * 16), new PlanClip(pattern + 1f))
            .Bake()));

    public void Dispose()
    {
        foreach (var asset in _owned) asset.Dispose();
    }

    [Fact]
    public void PreparePlayMatchesTheConvenienceApiBitForBit()
    {
        var ids = new ushort[_owned.Length * 3];
        var clocks = new ushort[ids.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = _owned[i % _owned.Length].Index;
            clocks[i] = (ushort)(i * 7 % 33);
        }
        var convenience = new int[ids.Length];
        var columns = new ColumnSet();
        columns.Add(new float[ids.Length]);
        columns.Add(convenience);
        Timeline<PlanTrack, PlanClip>.Apply(ids, clocks, true, in columns);

        var split = new int[ids.Length];
        var fold = new float[ids.Length];
        fixed (float* f = fold)
        fixed (int* e = split)
        {
            var splitColumns = new ColumnSet();
            splitColumns.Add(fold);
            splitColumns.Add(split);
            var state = Timeline<PlanTrack, PlanClip>.Prepare(ids, in splitColumns);
            try
            {
                Assert.Equal(0, state.Misses);
                Timeline<PlanTrack, PlanClip>.Play(in state, clocks, true);
            }
            finally { Timeline<PlanTrack, PlanClip>.Release(ref state); }
        }
        Assert.Equal(convenience, split);
    }

    [Fact]
    public void PlayIsTotalOverRandomIdsIncludingDeadAndAbsent()
    {
        var random = new Random(20261025);
        const int rows = 2048;
        var dead = Asset(63);
        var deadIndex = dead.Index;
        dead.Dispose();
        var absent = new ushort[] { ushort.MaxValue, ushort.MaxValue - 1, 30_001, 30_002, 30_003 };
        foreach (var id in absent)
            Assert.Throws<ArgumentException>(() => Timeline<PlanTrack, PlanClip>.View(id));
        var ids = new ushort[rows];
        var clocks = new ushort[rows];
        for (var i = 0; i < rows; i++)
        {
            var pick = random.Next(3);
            ids[i] = pick == 0 ? _owned[random.Next(_owned.Length)].Index
                : pick == 1 ? deadIndex
                : absent[random.Next(absent.Length)];
            clocks[i] = (ushort)random.Next(ushort.MaxValue);
        }
        var live = _owned.Select(asset => asset.Index).ToHashSet();
        var expectedMisses = Enumerable.Range(0, rows).Count(i => !live.Contains(ids[i]));
        var effects = Enumerable.Repeat(-12345, rows).ToArray();
        var fold = new float[rows];
        var before = clocks.ToArray();
        fixed (float* f = fold)
        fixed (int* e = effects)
        {
            var columns = new ColumnSet();
            columns.Add(fold);
            columns.Add(effects);
            var state = Timeline<PlanTrack, PlanClip>.Prepare(ids, in columns);
            try
            {
                Assert.Equal(expectedMisses, state.Misses);
                Timeline<PlanTrack, PlanClip>.Play(in state, clocks, false);
                Timeline<PlanTrack, PlanClip>.Play(in state, clocks, true);
            }
            finally { Timeline<PlanTrack, PlanClip>.Release(ref state); }
        }
        Assert.Equal(before, clocks);
        for (var i = 0; i < rows; i++)
            if (!live.Contains(ids[i])) Assert.Equal(-12345, effects[i]);
    }

    [Fact]
    public void PlayAllocatesNothingOnTheWarmPath()
    {
        var ids = new ushort[_owned.Length * 32];
        var clocks = new ushort[ids.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = _owned[i % _owned.Length].Index;
            clocks[i] = (ushort)(i % 33);
        }
        var effects = new int[ids.Length];
        var fold = new float[ids.Length];
        fixed (float* f = fold)
        fixed (int* e = effects)
        {
            var columns = new ColumnSet();
            columns.Add(fold);
            columns.Add(effects);
            var state = Timeline<PlanTrack, PlanClip>.Prepare(ids, in columns);
            try
            {
                Timeline<PlanTrack, PlanClip>.Play(in state, clocks, true);
                Timeline<PlanTrack, PlanClip>.Play(in state, clocks, false);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 100; i++) Timeline<PlanTrack, PlanClip>.Play(in state, clocks, i % 2 == 0);
                Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
            }
            finally { Timeline<PlanTrack, PlanClip>.Release(ref state); }
        }
    }

    [Fact]
    public void AStateIsASnapshotOfTheRegistryItWasPreparedAgainst()
    {
        var ids = new ushort[_owned.Length];
        var clocks = new ushort[ids.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = _owned[i].Index;
            clocks[i] = (ushort)(i * 5 % 33);
        }
        var effects = new int[ids.Length];
        var fold = new float[ids.Length];
        fixed (float* f = fold)
        fixed (int* e = effects)
        {
            var columns = new ColumnSet();
            columns.Add(fold);
            columns.Add(effects);
            var state = Timeline<PlanTrack, PlanClip>.Prepare(ids, in columns);
            try
            {
                Timeline<PlanTrack, PlanClip>.Play(in state, clocks, true);
                var snapshot = effects.ToArray();
                LatePlanConsumers.Install();
                Array.Fill(effects, 0);
                Timeline<PlanTrack, PlanClip>.Play(in state, clocks, true);
                Assert.Equal(snapshot, effects);
            }
            finally { Timeline<PlanTrack, PlanClip>.Release(ref state); }
        }
    }

    [Fact]
    public void LiveAssetsOfAForeignPairAreIdentityMisses()
    {
        using var foreign = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<ForeignTrack, ForeignClip>(new ForeignTrack(1))
            .Clip(0, 0, 16, new ForeignClip(2f))
            .Bake()));
        var ids = new ushort[4];
        ids[0] = _owned[0].Index;
        ids[1] = foreign.Index;
        ids[2] = _owned[1].Index;
        ids[3] = foreign.Index;
        var clocks = new ushort[] { 3, 5, 7, 9 };
        var effects = new[] { -1, -1, -1, -1 };
        var fold = new float[4];
        fixed (float* f = fold)
        fixed (int* e = effects)
        {
            var columns = new ColumnSet();
            columns.Add(fold);
            columns.Add(effects);
            var state = Timeline<PlanTrack, PlanClip>.Prepare(ids, in columns);
            try
            {
                Assert.Equal(2, state.Misses);
                Timeline<PlanTrack, PlanClip>.Play(in state, clocks, true);
            }
            finally { Timeline<PlanTrack, PlanClip>.Release(ref state); }
        }
        Assert.Equal(-1, effects[1]);
        Assert.Equal(-1, effects[3]);
        Assert.NotEqual(-1, effects[0]);
        Assert.NotEqual(-1, effects[2]);
    }
}

public readonly record struct ForeignClip(float Amount);
public readonly record struct ForeignTrack(int Pattern) : IBlend<ForeignClip>
{
    public void Blend(in ForeignClip a, in ForeignClip b, float factor, out ForeignClip result) => result = a;
}

public sealed unsafe class PreparePlayReplayTests : IDisposable
{
    readonly TimelineAsset[] _owned = Enumerable.Range(0, 8).Select(PreparePlayTests.Asset).ToArray();

    public void Dispose()
    {
        foreach (var asset in _owned) asset.Dispose();
    }

    [Fact]
    public void ReplayingOneStateWithNewClocksMatchesFreshConvenienceFrames()
    {
        var ids = new ushort[_owned.Length * 4];
        for (var i = 0; i < ids.Length; i++) ids[i] = _owned[i % _owned.Length].Index;
        var effects = new int[ids.Length];
        var fold = new float[ids.Length];
        fixed (float* f = fold)
        fixed (int* e = effects)
        {
            var columns = new ColumnSet();
            columns.Add(fold);
            columns.Add(effects);
            var state = Timeline<PlanTrack, PlanClip>.Prepare(ids, in columns);
            try
            {
                for (var frame = 0; frame < 8; frame++)
                {
                    var clocks = new ushort[ids.Length];
                    for (var i = 0; i < clocks.Length; i++) clocks[i] = (ushort)((i * 3 + frame * 5) % 33);
                    Array.Fill(effects, -1);
                    Timeline<PlanTrack, PlanClip>.Play(in state, clocks, true);
                    var replayed = effects.ToArray();
                    var direct = new int[ids.Length];
                    var directColumns = new ColumnSet();
                    directColumns.Add(new float[ids.Length]);
                    directColumns.Add(direct);
                    Timeline<PlanTrack, PlanClip>.Apply(ids, clocks, true, in directColumns);
                    Assert.Equal(direct, replayed);
                }
            }
            finally { Timeline<PlanTrack, PlanClip>.Release(ref state); }
        }
    }
}
