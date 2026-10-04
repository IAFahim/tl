using System.Runtime.CompilerServices;
using System.Text.Json;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

public readonly record struct CopyClip(float Amount);
public readonly record struct CopyTrack(int Pattern) : IBlend<CopyClip>
{
    public void Blend(in CopyClip a, in CopyClip b, float factor, out CopyClip result) => result = a;
}

internal static unsafe class CopyConsumer
{
    internal static int Calls;

    [ModuleInitializer]
    internal static void Install() => PairRuntime<CopyTrack, CopyClip>.Consume(&Fold, null, &Keys);

    private static int Keys(ulong* keys, byte* modes)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value; modes[0] = 20;
            keys[1] = TypeKey<long>.Value; modes[1] = 24;
            keys[2] = TypeKey<int>.Value; modes[2] = 20;
            keys[3] = TypeKey<short>.Value; modes[3] = 18;
            keys[4] = TypeKey<byte>.Value; modes[4] = 17;
            keys[5] = TypeKey<bool>.Value; modes[5] = 17;
        }
        return 6;
    }

    private static void Fold(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Calls++;
        CopyClip scratch = default;
        var frame = TickFrame.ToFrame<CopyTrack, CopyClip>(slot, pair, tick, flags, ref scratch);
        var amount = frame.Track.Pattern switch
        {
            0 => frame.Clip.Amount,
            1 => tick * 0.25f,
            _ => (tick * 271 % 31 - 15) * 0.25f
        };
        ((float*)columns[0])[row] = amount * frame.Direction;
        ((long*)columns[1])[row] = ((1L << 53) + 123 + tick) * frame.Direction;
        ((int*)columns[2])[row] = (tick + 17) * frame.Direction;
        ((short*)columns[3])[row] = (short)(-tick % 30000);
        ((byte*)columns[4])[row] = (byte)(tick % 251);
        ((byte*)columns[5])[row] = (byte)(tick % 2);
    }
}

public sealed class LaneCopyTests
{
    [Fact]
    public void UnresolvedOrMissingInspectionDoesNotCreateOrFoldABank()
    {
        Assert.Null(Inspection.CopyLanes<DormantTrack, DormantClip>(0));
        using var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<CopyTrack, CopyClip>(new CopyTrack(0)).Clip(0, 0, 4, new CopyClip(19)).Bake()));
        var calls = CopyConsumer.Calls;
        var prior = Inspection.Bank<CopyTrack, CopyClip>();
        Assert.Null(Inspection.CopyLanes<CopyTrack, CopyClip>(asset.Index));
        Assert.Null(Inspection.CopyLanes<CopyTrack, CopyClip>(ushort.MaxValue));
        Assert.Equal(calls, CopyConsumer.Calls);
        Assert.Equal(prior?.Count, Inspection.Bank<CopyTrack, CopyClip>()?.Count);
    }

    [Theory]
    [InlineData(8, 0, false)]
    [InlineData(4096, 0, false)]
    [InlineData(4096, 1, false)]
    [InlineData(4096, 2, false)]
    [InlineData(4096, 2, true)]
    public void RawCopiesAgreeWithRealTypedPlaybackAcrossEncodings(int duration, int pattern, bool looping)
    {
        var baker = new DomainBaker().Track<CopyTrack, CopyClip>(new CopyTrack(pattern))
            .Clip(0, 0, (uint)duration, new CopyClip(3.5f));
        if (looping) baker.Looping();
        using var asset = TimelineAsset.Of(TimelineAsset.Load(baker.Bake()));
        var view = Timeline<CopyTrack, CopyClip>.View(asset);
        var calls = CopyConsumer.Calls;
        var copy = Inspection.CopyLanes<CopyTrack, CopyClip>(asset.Index)!;
        Assert.NotNull(copy);
        Assert.Equal(duration, copy.Duration);
        Assert.Equal(looping, copy.Looping);
        Assert.Equal((int)view.AbiVersion, copy.AbiVersion);
        Assert.Equal(unchecked((long)view.Generation), copy.Generation);
        Assert.Equal(7, copy.Lanes.Count);
        Assert.Equal(calls, CopyConsumer.Calls);
        var ticks = Enumerable.Range(0, duration + 1).Select(t => (ushort)t).ToArray();
        var ids = Enumerable.Repeat(asset.Index, ticks.Length).ToArray();
        foreach (var forward in new[] { true, false })
        {
            var floats = new float[ticks.Length]; var longs = new long[ticks.Length];
            Timeline<CopyTrack, CopyClip>.ApplyChunk<ushort, ushort, float, long>(ids, ticks, forward, floats, longs);
            var ints = new int[ticks.Length]; var shorts = new short[ticks.Length];
            Timeline<CopyTrack, CopyClip>.ApplyChunk<ushort, ushort, int, short>(ids, ticks, forward, ints, shorts);
            var bytes = new byte[ticks.Length]; var bools = new bool[ticks.Length];
            Timeline<CopyTrack, CopyClip>.ApplyChunk<ushort, ushort, byte, bool>(ids, ticks, forward, bytes, bools);
            for (var position = 0; position <= duration; position++)
            {
                var index = forward ? position : position == 0 ? duration - 1 : position - 1;
                var inactive = forward ? position == duration : position == 0 && !looping || position == duration && looping;
                uint Word(int lane) => inactive ? 0 : (forward ? copy.Lanes[lane].Forward : copy.Lanes[lane].Backward)[index];
                Assert.True(Word(0) == BitConverter.SingleToUInt32Bits(floats[position]), $"float: forward={forward}, position={position}, expected={Word(0)}, actual={BitConverter.SingleToUInt32Bits(floats[position])}");
                Assert.True(((ulong)Word(1) | (ulong)Word(2) << 32) == unchecked((ulong)longs[position]), $"long: forward={forward}, position={position}, expected={((ulong)Word(1) | (ulong)Word(2) << 32)}, actual={unchecked((ulong)longs[position])}");
                Assert.True(Word(3) == unchecked((uint)ints[position]), $"forward={forward}, position={position}, laneTick={index}, expected={Word(3)}, actual={unchecked((uint)ints[position])}");
                Assert.Equal(unchecked((short)Word(4)), shorts[position]);
                Assert.Equal((byte)Word(5), bytes[position]);
                Assert.Equal(Word(6) != 0, bools[position]);
            }
        }
        Assert.Equal(calls, CopyConsumer.Calls);
    }

    [Fact]
    public void CallerMutationAndDisposalCannotChangeCopiedStorageOrPlayback()
    {
        var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<CopyTrack, CopyClip>(new CopyTrack(0)).Clip(0, 0, 8, new CopyClip(6.25f)).Bake()));
        Timeline<CopyTrack, CopyClip>.View(asset);
        var copy = Inspection.CopyLanes<CopyTrack, CopyClip>(asset.Index)!;
        var preserved = copy.Lanes[1].Forward.ToArray();
        copy.Lanes[0].Forward[0] = 0xdeadbeefu;
        var effect = new float[1];
        Timeline<CopyTrack, CopyClip>.Apply(asset.Index, 0, true, effect);
        Assert.Equal(6.25f, effect[0]);
        asset.Dispose();
        Assert.Equal(preserved, copy.Lanes[1].Forward);
        Assert.Equal(0xdeadbeefu, copy.Lanes[0].Forward[0]);
    }

    [Fact]
    public void InRangePendingAndAbsentCopiesPreserveAllObservedState()
    {
        using var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<CopyTrack, CopyClip>(new CopyTrack(0)).Clip(0, 0, 8, new CopyClip(8.25f)).Bake()));
        using var measured = MeasuredLanes.Measure(asset);
        using var isolated = new TimelineSet<CopyTrack, CopyClip>();
        isolated.AddAt(2, measured);
        isolated.MarkAbsent(1);
        var prior = Timeline<CopyTrack, CopyClip>._bank;
        try
        {
            Timeline<CopyTrack, CopyClip>._bank = isolated;
            var before = Inspection.Bank<CopyTrack, CopyClip>()!;
            Assert.Equal(Inspection.FoldState.Pending, before.Views[0].State);
            Assert.Equal(Inspection.FoldState.Absent, before.Views[1].State);
            var tables = Inspection.Tables();
            var calls = CopyConsumer.Calls;
            Assert.Null(Inspection.CopyLanes<CopyTrack, CopyClip>(0));
            Assert.Null(Inspection.CopyLanes<CopyTrack, CopyClip>(1));
            Assert.Null(Inspection.CopyLanes<CopyTrack, CopyClip>(3));
            Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(Inspection.Bank<CopyTrack, CopyClip>()));
            Assert.Equal(tables, Inspection.Tables());
            Assert.Equal(calls, CopyConsumer.Calls);
        }
        finally { Timeline<CopyTrack, CopyClip>._bank = prior; }
    }

    [Fact]
    public void CopiesRemainIndependentAfterNativeBankFreeAndAssetReclamation()
    {
        var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<CopyTrack, CopyClip>(new CopyTrack(2)).Clip(0, 0, 4096, new CopyClip(9.5f)).Bake()));
        var isolated = new TimelineSet<CopyTrack, CopyClip>();
        var prior = Timeline<CopyTrack, CopyClip>._bank;
        try
        {
            var index = isolated.Add(asset);
            Timeline<CopyTrack, CopyClip>._bank = isolated;
            var copy = Inspection.CopyLanes<CopyTrack, CopyClip>(index)!;
            var expected = JsonSerializer.Serialize(copy);
            isolated.Dispose();
            Assert.True(isolated._disposed);
            Assert.Equal(0, isolated.RetainedBytes);
            asset.Dispose();
            TimelineTable.Drain();
            Assert.Equal(0, TimelineTable.GraveyardBlocks);
            Assert.Equal(0, TimelineTable.GraveyardBytes);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            Assert.Equal(expected, JsonSerializer.Serialize(copy));
            copy.Lanes[0].Forward[0] = 0xdeadbeefu;
            Assert.Equal(0xdeadbeefu, copy.Lanes[0].Forward[0]);
        }
        finally { Timeline<CopyTrack, CopyClip>._bank = prior; isolated.Dispose(); asset.Dispose(); }
    }
}
