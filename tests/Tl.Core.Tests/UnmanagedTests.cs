using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct SigmaClip(int Value);

public readonly record struct SigmaTrack(int Code) : IBlend<SigmaClip>
{
    public void Blend(in SigmaClip first, in SigmaClip second, float factor, out SigmaClip result)
        => result = factor < 0.5f ? first : second;
}

public readonly record struct TauClip(int Value);

public readonly record struct TauTrack(int Code) : IBlend<TauClip>
{
    public void Blend(in TauClip first, in TauClip second, float factor, out TauClip result) => result = first;
}

public readonly record struct RhoClip(float Amount);

public readonly record struct RhoTrack(float Scale) : IBlend<RhoClip>
{
    public void Blend(in RhoClip first, in RhoClip second, float factor, out RhoClip result)
    {
        UnmanagedTests.RhoBlends++;
        result = new RhoClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

public readonly record struct OmegaClip(int Value);

public readonly record struct OmegaTrack(int Code) : IBlend<OmegaClip>
{
    public void Blend(in OmegaClip first, in OmegaClip second, float factor, out OmegaClip result)
        => result = factor < 0.5f ? first : second;
}

public unsafe class UnmanagedTests
{
    internal static readonly List<Record> Log = [];
    internal static int RhoBlends;

    static UnmanagedTests()
    {
        PairRuntime<SigmaTrack, SigmaClip>.Consume(&SigmaFirst, &SigmaBind);
        PairRuntime<SigmaTrack, SigmaClip>.Consume(&SigmaSecond, &NoBind);
        PairRuntime<TauTrack, TauClip>.Consume(&TauExecute, &NoBind);
        PairRuntime<RhoTrack, RhoClip>.Consume(&RhoExecute, &NoBind);
        PairRuntime<SigmaTrack, SigmaClip>.ConsumeUnmanaged(&SigmaUnmanagedFirst, &SigmaUnmanagedBind);
        PairRuntime<SigmaTrack, SigmaClip>.ConsumeUnmanaged(&SigmaUnmanagedSecond, &NoBindUnmanaged);
        PairRuntime<TauTrack, TauClip>.ConsumeUnmanaged(&TauUnmanaged, &NoBindUnmanaged);
        PairRuntime<RhoTrack, RhoClip>.ConsumeUnmanaged(&RhoUnmanaged, &NoBindUnmanaged);
        PairRuntime<OmegaTrack, OmegaClip>.ConsumeUnmanaged(&OmegaUnmanaged, &OmegaUnmanagedBind);
    }

    private static void NoBind(ulong* keys, int keyCount, byte* table)
    {
    }

    [UnmanagedCallersOnly]
    private static void NoBindUnmanaged(ulong* keys, int keyCount, byte* table)
    {
    }

    private static void SigmaBind(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
        {
            if (keys[i] == TypeKey<Health>.Value)
            {
                table[0] = (byte)(i + 1);
                break;
            }
        }
    }

    [UnmanagedCallersOnly]
    private static void SigmaUnmanagedBind(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
        {
            if (keys[i] == TypeKey<Health>.Value)
            {
                table[0] = (byte)(i + 1);
                break;
            }
        }
    }

    [UnmanagedCallersOnly]
    private static void OmegaUnmanagedBind(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
        {
            if (keys[i] == TypeKey<Health>.Value)
            {
                table[0] = (byte)(i + 1);
                break;
            }
        }
    }

    private static void SigmaFirst(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        SigmaClip scratch = default;
        var current = TickFrame.ToFrame<SigmaTrack, SigmaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Log.Add(new Record('F', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
        var health = (Health*)columns[0];
        if (health != null)
            health[row].Value += current.Clip.Value * current.Track.Code;
    }

    private static void SigmaSecond(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        SigmaClip scratch = default;
        var current = TickFrame.ToFrame<SigmaTrack, SigmaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Log.Add(new Record('S', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    [UnmanagedCallersOnly]
    private static void SigmaUnmanagedFirst(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        SigmaClip scratch = default;
        var current = TickFrame.ToFrame<SigmaTrack, SigmaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Log.Add(new Record('f', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
        var health = (Health*)columns[0];
        health[row].Value += current.Clip.Value * current.Track.Code;
    }

    [UnmanagedCallersOnly]
    private static void SigmaUnmanagedSecond(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        SigmaClip scratch = default;
        var current = TickFrame.ToFrame<SigmaTrack, SigmaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Log.Add(new Record('s', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    private static void TauExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        TauClip scratch = default;
        var current = TickFrame.ToFrame<TauTrack, TauClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Log.Add(new Record('T', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    [UnmanagedCallersOnly]
    private static void TauUnmanaged(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        TauClip scratch = default;
        var current = TickFrame.ToFrame<TauTrack, TauClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Log.Add(new Record('t', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    private static void RhoExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        RhoClip scratch = default;
        var current = TickFrame.ToFrame<RhoTrack, RhoClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Log.Add(new Record('R', (int)current.Track.Scale, current.Clip.Amount, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    [UnmanagedCallersOnly]
    private static void RhoUnmanaged(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        RhoClip scratch = default;
        var current = TickFrame.ToFrame<RhoTrack, RhoClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Log.Add(new Record('r', (int)current.Track.Scale, current.Clip.Amount, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    [UnmanagedCallersOnly]
    private static void OmegaUnmanaged(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        OmegaClip scratch = default;
        var current = TickFrame.ToFrame<OmegaTrack, OmegaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        ((Health*)columns[0])[row].Value += current.Clip.Value * current.Track.Code;
    }

    private static unsafe void TickManaged(TimelineComponent[] rows, Health[] health, uint gameTick, int delta)
        => Timeline.Rows(rows).Read(new Resistance[rows.Length]).Write(health).Tick(gameTick, delta);

    private static unsafe void TickUnmanaged(UnmanagedTickState* state, TimelineComponent[] rows, Health[] health, uint gameTick, int delta)
    {
        fixed (TimelineComponent* r = rows)
        fixed (Health* h = health)
        {
            var resistance = stackalloc Resistance[rows.Length];
            void** bases = stackalloc void*[2] { resistance, h };
            ulong* keys = stackalloc ulong[2] { TypeKey<Resistance>.Value, TypeKey<Health>.Value };
            Timeline.TickUnmanaged(state, r, rows.Length, bases, keys, 2, gameTick, delta);
        }
    }

    private static unsafe void TickUnmanagedHealth(UnmanagedTickState* state, TimelineComponent[] rows, Health[] health, uint gameTick, int delta)
    {
        fixed (TimelineComponent* r = rows)
        fixed (Health* h = health)
        {
            void** bases = stackalloc void*[1] { h };
            ulong* keys = stackalloc ulong[1] { TypeKey<Health>.Value };
            Timeline.TickUnmanaged(state, r, rows.Length, bases, keys, 1, gameTick, delta);
        }
    }

    private static (char Kind, int Code, float Value, uint Tick, uint Game, long Cycle, FrameFlags Flags)[] Snapshot()
        => Log.Select(record => (char.ToUpperInvariant(record.Kind), record.Code, record.Value, record.Tick, record.Game, record.Cycle, record.Flags)).ToArray();

    [Fact]
    public void UnmanagedTickMatchesTheManagedArmForwardBackwardAndClamp()
    {
        var baked = new Baker()
            .Track<SigmaTrack, SigmaClip>(new SigmaTrack(5))
            .Clip(0, 0, 1, new SigmaClip(11))
            .Clip(0, 2, 3, new SigmaClip(22))
            .Bake();
        using var managedAsset = TimelineAsset.Load(baked);
        using var unmanagedAsset = TimelineAsset.Load(baked);
        var managedRows = new[] { new TimelineComponent(managedAsset.Reference) };
        var unmanagedRows = new[] { new TimelineComponent(unmanagedAsset.Reference) };
        var managedHealth = new Health[1];
        var unmanagedHealth = new Health[1];
        var state = default(UnmanagedTickState);

        Log.Clear();
        TickManaged(managedRows, managedHealth, 100u, 5);
        var forward = Snapshot();
        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, unmanagedHealth, 100u, 5);
        Assert.Equal(forward, Snapshot());
        Assert.Equal(4, forward.Length);
        Assert.Equal(3u, managedRows[0].Position);
        Assert.Equal(3u, unmanagedRows[0].Position);
        Assert.Equal(11f * 5 + 22f * 5, managedHealth[0].Value);
        Assert.Equal(managedHealth[0].Value, unmanagedHealth[0].Value);

        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, unmanagedHealth, 200u, int.MaxValue);
        Assert.Empty(Log);
        Assert.Equal(3u, unmanagedRows[0].Position);
        TickUnmanaged(&state, unmanagedRows, unmanagedHealth, 200u, 0);
        Assert.Empty(Log);
        Assert.Equal(3u, unmanagedRows[0].Position);

        Log.Clear();
        TickManaged(managedRows, managedHealth, 103u, -5);
        var backward = Snapshot();
        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, unmanagedHealth, 103u, -5);
        Assert.Equal(backward, Snapshot());
        Assert.Equal(0u, managedRows[0].Position);
        Assert.Equal(0u, unmanagedRows[0].Position);
        Assert.Equal(0, managedRows[0].Cycle);
        Assert.Equal(0, unmanagedRows[0].Cycle);
        Assert.Equal((11f * 5 + 22f * 5) * 2, unmanagedHealth[0].Value);
        Assert.Equal(managedHealth[0].Value, unmanagedHealth[0].Value);
    }

    [Fact]
    public void UnmanagedTickMirrorsAuthoredOrderAndTwoConsumerBackwardMirror()
    {
        var baked = new Baker()
            .Track<SigmaTrack, SigmaClip>(new SigmaTrack(1))
            .Track<TauTrack, TauClip>(new TauTrack(4))
            .Track<SigmaTrack, SigmaClip>(new SigmaTrack(2))
            .Clip(0, 0, 1, new SigmaClip(10))
            .Clip(1, 0, 1, new TauClip(7))
            .Clip(2, 0, 1, new SigmaClip(20))
            .Bake();
        using var managedAsset = TimelineAsset.Load(baked);
        using var unmanagedAsset = TimelineAsset.Load(baked);
        var managedRows = new[] { new TimelineComponent(managedAsset.Reference) };
        var unmanagedRows = new[] { new TimelineComponent(unmanagedAsset.Reference) };
        var managedHealth = new Health[1];
        var unmanagedHealth = new Health[1];
        var state = default(UnmanagedTickState);

        Log.Clear();
        TickManaged(managedRows, managedHealth, 50u, 1);
        Assert.Equal(
        [
            ('S', 1, 10f, 0u, 50u, 0L),
            ('F', 1, 10f, 0u, 50u, 0L),
            ('T', 4, 7f, 0u, 50u, 0L),
            ('S', 2, 20f, 0u, 50u, 0L),
            ('F', 2, 20f, 0u, 50u, 0L),
        ], Log.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game, record.Cycle)).ToList());
        var managedForward = Snapshot();
        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, unmanagedHealth, 50u, 1);
        Assert.Equal(managedForward, Snapshot());
        Assert.Equal(10f * 1 + 20f * 2, unmanagedHealth[0].Value);
        Assert.Equal(1u, unmanagedRows[0].Position);

        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, unmanagedHealth, 51u, -1);
        Assert.Equal(
        [
            ('f', 2, 20f, 0u, 50u, 0L),
            ('s', 2, 20f, 0u, 50u, 0L),
            ('t', 4, 7f, 0u, 50u, 0L),
            ('f', 1, 10f, 0u, 50u, 0L),
            ('s', 1, 10f, 0u, 50u, 0L),
        ], Log.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game, record.Cycle)).ToList());
        Assert.Equal(0u, unmanagedRows[0].Position);
        Assert.Equal((10f * 1 + 20f * 2) * 2, unmanagedHealth[0].Value);

        Log.Clear();
        TickManaged(managedRows, managedHealth, 51u, -1);
        Assert.Equal(
        [
            ('F', 2, 20f, 0u, 50u, 0L),
            ('S', 2, 20f, 0u, 50u, 0L),
            ('T', 4, 7f, 0u, 50u, 0L),
            ('F', 1, 10f, 0u, 50u, 0L),
            ('S', 1, 10f, 0u, 50u, 0L),
        ], Log.Select(record => (record.Kind, record.Code, record.Value, record.Tick, record.Game, record.Cycle)).ToList());
        Assert.Equal(0u, managedRows[0].Position);
    }

    [Fact]
    public void UnmanagedTickResolvesBlendOncePerVisitedFrameIncludingTheSpanOneWindow()
    {
        var baked = new Baker()
            .Track<RhoTrack, RhoClip>(new RhoTrack(1f))
            .Clip(0, 0, 4, new RhoClip(0f))
            .Clip(0, 2, 6, new RhoClip(10f))
            .Bake();
        using var managedAsset = TimelineAsset.Load(baked);
        using var unmanagedAsset = TimelineAsset.Load(baked);
        var managedRows = new[] { new TimelineComponent(managedAsset.Reference) };
        var unmanagedRows = new[] { new TimelineComponent(unmanagedAsset.Reference) };
        var state = default(UnmanagedTickState);

        RhoBlends = 0;
        var managedAmounts = new List<float>();
        for (var index = 0; index < 6; index++)
        {
            Log.Clear();
            TickManaged(managedRows, new Health[1], 300u + (uint)index, 1);
            managedAmounts.Add(Log.Single().Value);
        }
        Assert.Equal(2, RhoBlends);

        RhoBlends = 0;
        var unmanagedAmounts = new List<float>();
        for (var index = 0; index < 6; index++)
        {
            Log.Clear();
            TickUnmanaged(&state, unmanagedRows, new Health[1], 300u + (uint)index, 1);
            unmanagedAmounts.Add(Log.Single().Value);
        }
        Assert.Equal(2, RhoBlends);
        Assert.Equal([0f, 0f, 0f, 10f, 10f, 10f], unmanagedAmounts);
        Assert.Equal(managedAmounts, unmanagedAmounts);

        var spanOne = new Baker()
            .Track<RhoTrack, RhoClip>(new RhoTrack(2f))
            .Clip(0, 0, 3, new RhoClip(0f))
            .Clip(0, 2, 4, new RhoClip(8f))
            .Bake();
        using var spanManaged = TimelineAsset.Load(spanOne);
        using var spanUnmanaged = TimelineAsset.Load(spanOne);
        var spanManagedRows = new[] { new TimelineComponent(spanManaged.Reference) };
        var spanUnmanagedRows = new[] { new TimelineComponent(spanUnmanaged.Reference) };

        RhoBlends = 0;
        Log.Clear();
        TickManaged(spanManagedRows, new Health[1], 400u, 4);
        Assert.Equal(1, RhoBlends);
        var spanForward = Snapshot();
        RhoBlends = 0;
        var spanState = default(UnmanagedTickState);
        Log.Clear();
        TickUnmanaged(&spanState, spanUnmanagedRows, new Health[1], 400u, 4);
        Assert.Equal(spanForward, Snapshot());
        Assert.Equal(
        [
            (0f, 0u, 400u),
            (0f, 1u, 401u),
            (4f, 2u, 402u),
            (8f, 3u, 403u),
        ], Log.Select(record => (record.Value, record.Tick, record.Game)).ToList());
        Assert.Equal(1, RhoBlends);
        Assert.Equal(4u, spanUnmanagedRows[0].Position);
    }

    [Fact]
    public void UnmanagedTickClampsRowsIndependentlyWrapsGameTickAndCyclesLoops()
    {
        using var shortAsset = TimelineAsset.Load(new Baker()
            .Track<SigmaTrack, SigmaClip>(new SigmaTrack(1))
            .Clip(0, 0, 1, new SigmaClip(2))
            .Bake());
        using var longAsset = TimelineAsset.Load(new Baker()
            .Track<SigmaTrack, SigmaClip>(new SigmaTrack(2))
            .Clip(0, 0, 3, new SigmaClip(3))
            .Bake());
        var managedRows = new[]
        {
            new TimelineComponent(shortAsset.Reference),
            new TimelineComponent(longAsset.Reference),
        };
        var unmanagedRows = new[]
        {
            new TimelineComponent(shortAsset.Reference),
            new TimelineComponent(longAsset.Reference),
        };
        var managedHealth = new Health[2];
        var unmanagedHealth = new Health[2];
        var state = default(UnmanagedTickState);

        Log.Clear();
        TickManaged(managedRows, managedHealth, 7u, 3);
        var managedClamp = Snapshot();
        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, unmanagedHealth, 7u, 3);
        Assert.Equal(managedClamp, Snapshot());
        Assert.Equal(1u, unmanagedRows[0].Position);
        Assert.Equal(3u, unmanagedRows[1].Position);
        Assert.Equal(managedHealth[0].Value + managedHealth[1].Value, unmanagedHealth[0].Value + unmanagedHealth[1].Value);
        Assert.Equal(2f * 1 + 3f * 2 * 3, unmanagedHealth[0].Value + unmanagedHealth[1].Value);

        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, unmanagedHealth, 20u, int.MaxValue);
        Assert.Empty(Log);
        Assert.Equal(1u, unmanagedRows[0].Position);
        Assert.Equal(3u, unmanagedRows[1].Position);

        using var loopAsset = TimelineAsset.Load(new Baker()
            .Track<SigmaTrack, SigmaClip>(new SigmaTrack(7))
            .Clip(0, 0, 2, new SigmaClip(3))
            .Looping()
            .Bake());
        var managedLoop = new[] { new TimelineComponent(loopAsset.Reference) };
        var unmanagedLoop = new[] { new TimelineComponent(loopAsset.Reference) };
        var loopState = default(UnmanagedTickState);

        Log.Clear();
        TickManaged(managedLoop, new Health[1], 10u, 3);
        var managedLoopForward = Snapshot();
        Log.Clear();
        TickUnmanaged(&loopState, unmanagedLoop, new Health[1], 10u, 3);
        Assert.Equal(managedLoopForward, Snapshot());
        Assert.Equal(1u, unmanagedLoop[0].Position);
        Assert.Equal(1, unmanagedLoop[0].Cycle);

        Log.Clear();
        TickUnmanaged(&loopState, unmanagedLoop, new Health[1], 13u, -3);
        Assert.Equal(0u, unmanagedLoop[0].Position);
        Assert.Equal(0, unmanagedLoop[0].Cycle);

        var wrapRows = new[] { new TimelineComponent(loopAsset.Reference) };
        var wrapState = default(UnmanagedTickState);
        Log.Clear();
        TickUnmanaged(&wrapState, wrapRows, new Health[1], 1u, -3);
        Assert.Equal([0u, 0u, 4294967295u, 4294967295u, 4294967294u, 4294967294u], Log.Select(record => record.Game).ToList());
    }

    [Fact]
    public void UnmanagedTickTreatsDefaultsEmptyAssetsAndEmptyRowsAsTotalNoOps()
    {
        var rows = new[] { new TimelineComponent(), new TimelineComponent() };
        var health = new Health[2];
        var state = default(UnmanagedTickState);
        Log.Clear();
        TickUnmanaged(&state, rows, health, uint.MaxValue, int.MinValue);
        Assert.Empty(Log);
        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0u, rows[1].Position);
        Assert.Equal(0f, health[0].Value);

        using var empty = TimelineAsset.Load(new Baker().Bake());
        var emptyRows = new[] { new TimelineComponent(empty.Reference) };
        var emptyState = default(UnmanagedTickState);
        TickUnmanaged(&emptyState, emptyRows, new Health[1], 10u, 1000);
        TickUnmanaged(&emptyState, emptyRows, new Health[1], 1010u, -1000);
        Assert.Empty(Log);
        Assert.Equal(0u, emptyRows[0].Position);
        Assert.Equal(0, emptyRows[0].Cycle);

        Timeline.TickUnmanaged(&emptyState, null, 0, null, null, 0, 5u, 5);
        Assert.Empty(Log);
    }

    [Fact]
    public void DualConventionConsumersCoexistAndEachArmDispatchesOnlyItsOwnTable()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<SigmaTrack, SigmaClip>(new SigmaTrack(9))
            .Clip(0, 0, 1, new SigmaClip(4))
            .Bake());
        var managedRows = new[] { new TimelineComponent(asset.Reference) };
        var unmanagedRows = new[] { new TimelineComponent(asset.Reference) };
        var state = default(UnmanagedTickState);

        Log.Clear();
        TickManaged(managedRows, new Health[1], 60u, 1);
        Assert.Equal("SF", new string(Log.Select(record => record.Kind).ToArray()));
        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, new Health[1], 60u, 1);
        Assert.Equal("sf", new string(Log.Select(record => record.Kind).ToArray()));

        Log.Clear();
        TickUnmanaged(&state, unmanagedRows, new Health[1], 61u, -1);
        Assert.Equal("fs", new string(Log.Select(record => record.Kind).ToArray()));
        Log.Clear();
        TickManaged(managedRows, new Health[1], 61u, -1);
        Assert.Equal("FS", new string(Log.Select(record => record.Kind).ToArray()));

        Assert.Equal(0u, managedRows[0].Position);
        Assert.Equal(0u, unmanagedRows[0].Position);
    }

    [Fact]
    public void UnmanagedWarmTickAllocatesNoManagedMemory()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<OmegaTrack, OmegaClip>(new OmegaTrack(5))
            .Clip(0, 0, 1, new OmegaClip(3))
            .Looping()
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];
        var state = default(UnmanagedTickState);

        for (var index = 0; index < 1_000; index++)
            TickUnmanagedHealth(&state, rows, health, (uint)index, (index & 1) == 0 ? 1 : -1);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100_000; index++)
            TickUnmanagedHealth(&state, rows, health, (uint)index, (index & 1) == 0 ? 1 : -1);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(0u, rows[0].Position);
        Assert.Equal(0, rows[0].Cycle);
        Assert.Equal(15f * 101_000, health[0].Value);
    }

    [Fact]
    public void CompactingGcBetweenUnmanagedTicksKeepsDispatchWritingCurrentColumns()
    {
        using var asset = TimelineAsset.Load(new Baker()
            .Track<SigmaTrack, SigmaClip>(new SigmaTrack(2))
            .Clip(0, 0, 8, new SigmaClip(7))
            .Bake());
        var rows = new[] { new TimelineComponent(asset.Reference) };
        var health = new Health[1];
        var state = default(UnmanagedTickState);
        Log.Clear();
        TickUnmanaged(&state, rows, health, 1u, 1);
        var first = health[0].Value;
        Assert.Equal(14f, first);

        var address = (nint)Unsafe.AsPointer(ref health[0]);
        var moved = false;
        for (var attempt = 0; attempt < 20 && !moved; attempt++)
        {
            var junk = new byte[64 * 1024];
            junk[0] = 1;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            var current = (nint)Unsafe.AsPointer(ref health[0]);
            moved = current != address;
            address = current;
        }
        Assert.True(moved);

        TickUnmanaged(&state, rows, health, 2u, 1);
        Assert.Equal(first * 2, health[0].Value);
        Assert.Equal(4, Log.Count);
        Assert.Equal(2u, rows[0].Position);
    }
}
