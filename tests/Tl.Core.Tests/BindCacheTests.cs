using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct CacheProbeClip(int Value);

public readonly record struct CacheProbeTrack(int Code) : IBlend<CacheProbeClip>
{
    public void Blend(in CacheProbeClip first, in CacheProbeClip second, float factor, out CacheProbeClip result) => result = first;
}

public unsafe class BindCacheTests
{
    static int _probeConsumers;

    static bool ProbeKernel(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta) => false;

    static void EmptyBind(ulong* keys, int keyCount, byte* table) { }

    static void ProbeExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => RecordProbe('P', slot, gameTick, tick, cycle, flags);

    static void SecondProbeExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
        => RecordProbe('Q', slot, gameTick, tick, cycle, flags);

    static void RecordProbe(char kind, byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags)
    {
        CacheProbeClip scratch = default;
        var frame = TickFrame.ToFrame<CacheProbeTrack, CacheProbeClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        DataTests.Records.Add(new Record(kind, frame.Track.Code, frame.Clip.Value, frame.TimelineTick, frame.GameTick, frame.Cycle, frame.Flags));
    }

    static void EnsureBaseProbeConsumer()
    {
        if (System.Threading.Interlocked.Exchange(ref _probeConsumers, 1) == 0)
            PairRuntime<CacheProbeTrack, CacheProbeClip>.Consume(&ProbeExecute, &EmptyBind);
    }


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

    [Fact]
    public void LateConsumerInstallIsDispatchedOnTheNextConstruction()
    {
        EnsureBaseProbeConsumer();
        using var asset = TimelineAsset.Load(new Baker()
            .Track<CacheProbeTrack, CacheProbeClip>(new CacheProbeTrack(4))
            .Clip(0, 0, 1, new CacheProbeClip(9))
            .Looping()
            .Bake());

        var rows = new[] { new TimelineComponent(asset.Reference) };
        DataTests.Records.Clear();
        Timeline.Rows(rows).Tick(1u, 1);
        Assert.Equal(['P'], DataTests.Records.Select(record => record.Kind).ToArray());

        PairRuntime<CacheProbeTrack, CacheProbeClip>.Consume(&SecondProbeExecute, &EmptyBind);

        DataTests.Records.Clear();
        Timeline.Rows(rows).Tick(2u, 1);
        var controlRows = new[] { new TimelineComponent(asset.Reference) };
        Timeline.Rows(controlRows).Tick(2u, 1);

        var kinds = DataTests.Records.Select(record => record.Kind).ToArray();
        Assert.Equal(4, kinds.Length);
        Assert.Equal(['Q', 'P', 'Q', 'P'], kinds);
        Assert.All(DataTests.Records, record => Assert.Equal(4, record.Code));
        Assert.Equal(2, rows[0].Cycle);
        Assert.Equal(1, controlRows[0].Cycle);
    }

    [Fact]
    public void LateKernelRegistrationIsAdoptedOnTheNextConstruction()
    {
        TimelineKernels.Register(1, 2, 3, 4, &ProbeKernel);
        var baked = new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(78))
            .Clip(0, 0, 2, new AlphaClip(405))
            .Clip(0, 2, 6, new AlphaClip(809))
            .Bake();
        using var asset = TimelineAsset.Load(baked);
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];

        var bound = TimelineKernels.Bound;
        Timeline.Rows(rows).Write(health).Tick(1u, 1);
        Assert.Equal(bound, TimelineKernels.Bound);

        var hash = SHA256.HashData(baked);
        TimelineKernels.Register(
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(0)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(8)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(16)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(24)),
            &ProbeKernel);

        Timeline.Rows(rows).Write(health).Tick(2u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        Timeline.Rows(rows).Write(health).Tick(3u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        Assert.Equal((405f * 2 + 809f) * 78, health[0].Value);
        Assert.Equal(3u, rows[0].Position);
    }

    [Fact]
    public void StableNegativeIdentityStopsReHashingWhileKernelRegistrationIsUnchanged()
    {
        TimelineKernels.Register(5, 6, 7, 8, &ProbeKernel);
        var baked = new Baker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(80))
            .Clip(0, 0, 2, new AlphaClip(410))
            .Clip(0, 2, 6, new AlphaClip(820))
            .Bake();
        using var asset = TimelineAsset.Load(baked);
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];

        Timeline.Rows(rows).Write(health).Tick(1u, 1);
        var bound = TimelineKernels.Bound;
        var findCallsField = typeof(TimelineKernels).GetField("FindCalls", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(findCallsField);
        var findCalls = (int)findCallsField!.GetValue(null)!;

        Timeline.Rows(rows).Write(health).Tick(2u, 1);
        Timeline.Rows(rows).Write(health).Tick(3u, 1);

        Assert.Equal(findCalls, (int)findCallsField.GetValue(null)!);
        Assert.Equal(bound, TimelineKernels.Bound);

        var hash = SHA256.HashData(baked);
        TimelineKernels.Register(
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(0)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(8)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(16)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(24)),
            &ProbeKernel);

        Timeline.Rows(rows).Write(health).Tick(4u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        Timeline.Rows(rows).Write(health).Tick(5u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        Assert.Equal((410f * 2 + 820f * 3) * 80, health[0].Value);
        Assert.Equal(5u, rows[0].Position);
    }
}
