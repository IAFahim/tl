using System.Buffers.Binary;
using System.Security.Cryptography;
using Xunit;

namespace Tl.Core.Tests;

public unsafe class BindCacheTests
{
    static bool ProbeKernel(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta) => false;

    [Fact]
    public void RepeatedConstructionOverSameRowsAndColumnsRebindsInPlace()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
            .Clip(0, 0, 2, new AlphaClip(11))
            .Clip(0, 2, 4, new AlphaClip(22))
            .Bake());

        var controlRows = new[] { new TimelineComponent(asset.Reference) };
        var controlHealth = new Health[1];
        for (var frame = 1; frame <= 3; frame++)
            Timeline.Rows(controlRows).Write(controlHealth).Tick((uint)frame, 1);

        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];
        for (var frame = 1; frame <= 3; frame++)
            Timeline.Rows(rows).Write(health).Tick((uint)frame, 1);

        Assert.Equal(controlRows[0].Position, rows[0].Position);
        Assert.Equal(controlRows[0].Cycle, rows[0].Cycle);
        Assert.Equal(controlHealth[0].Value, health[0].Value);
    }

    [Fact]
    public void DistinctColumnShapesBindIndependentlyOverTheSameRows()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
            .Clip(0, 0, 1, new AlphaClip(11))
            .Looping()
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];
        var resistance = new Resistance[1];
        for (var frame = 0; frame < 3; frame++)
        {
            Timeline.Rows(rows).Write(health).Tick((uint)(frame * 2), 1);
            Timeline.Rows(rows).Write(resistance).Tick((uint)(frame * 2 + 1), 1);
        }
        Assert.Equal(11f * 5 * 3, health[0].Value);
        Assert.Equal(0f, resistance[0].Scale);
        Assert.Equal(6, rows[0].Cycle);
    }

    [Fact]
    public void MultiRowUniformConstructionRebinds()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
            .Clip(0, 0, 3, new AlphaClip(11))
            .Bake());

        var controlRows = new[]
        {
            new TimelineComponent(asset.Reference),
            new TimelineComponent(asset.Reference),
            new TimelineComponent(asset.Reference),
        };
        var controlHealth = new Health[3];
        Timeline.Rows(controlRows).Write(controlHealth).Tick(5u, 1);
        Timeline.Rows(controlRows).Write(controlHealth).Tick(6u, 1);

        var rows = new[]
        {
            new TimelineComponent(asset.Reference),
            new TimelineComponent(asset.Reference),
            new TimelineComponent(asset.Reference),
        };
        var health = new Health[3];
        Timeline.Rows(rows).Write(health).Tick(5u, 1);
        Timeline.Rows(rows).Write(health).Tick(6u, 1);

        Assert.Equal(controlHealth, health);
        Assert.Equal(controlRows[0].Position, rows[0].Position);
        Assert.Equal(controlRows[2].Position, rows[2].Position);
    }

    [Fact]
    public void DisposeInvalidatesTheCachedBindIdentity()
    {
        var alpha = TimelineAsset.Load(new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(5))
            .Clip(0, 0, 1, new AlphaClip(11))
            .Looping()
            .Bake());
        var rows = new[] { new TimelineComponent(alpha.Reference) };
        var health = new Health[1];
        Timeline.Rows(rows).Write(health).Tick(1u, 1);
        Assert.Equal(11f * 5, health[0].Value);
        alpha.Dispose();

        using var phi = TimelineAsset.Load(new Baker()
            .Track<PhiTrack, PhiClip>(new PhiTrack(9))
            .Clip(0, 0, 1, new PhiClip(3))
            .Looping()
            .Bake());
        DataTests.Records.Clear();
        var reloaded = new[] { new TimelineComponent(phi.Reference) };
        Timeline.Rows(reloaded).Write(health).Tick(1u, 1);

        Assert.Equal(11f * 5, health[0].Value);
        Assert.Equal(2, DataTests.Records.Count);
        Assert.Equal(['2', '1'], DataTests.Records.Select(record => record.Kind).ToList());
        Assert.All(DataTests.Records, record => Assert.Equal(9, record.Code));
    }

    [Fact]
    public void KernelFindRunsOncePerCachedIdentity()
    {
        var baked = new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(77))
            .Clip(0, 0, 2, new AlphaClip(404))
            .Clip(0, 2, 5, new AlphaClip(808))
            .Bake();
        using var asset = TimelineAsset.Load(baked);
        var hash = SHA256.HashData(baked);
        TimelineKernels.Register(
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(0)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(8)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(16)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(24)),
            &ProbeKernel);

        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];
        var bound = TimelineKernels.Bound;
        Timeline.Rows(rows).Write(health).Tick(1u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        Timeline.Rows(rows).Write(health).Tick(2u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        Timeline.Rows(rows).Write(health).Tick(3u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        Assert.Equal((404f * 2 + 808f) * 77, health[0].Value);
        Assert.Equal(3u, rows[0].Position);
    }
}
