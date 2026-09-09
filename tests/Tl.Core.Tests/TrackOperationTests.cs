using Xunit;
using Tl.Generation;

namespace Tl.Core.Tests;

public class TrackOperationTests
{
    public readonly record struct ProbeClip(float Value);

    public readonly struct ProbeTrack(int marker) : IBlend<ProbeClip>
    {
        public int Marker { get; } = marker;

        public void Blend(in ProbeClip first, in ProbeClip second, float t, out ProbeClip result)
            => result = new(first.Value + (second.Value - first.Value) * t);
    }

    public readonly record struct Visit(int Ordinal, int Count, ushort Index, int Marker, ClipState State, float Value);

    public struct Probe : ITrack<ProbeTrack, ProbeClip, NoInput, Probe>
    {
        public List<Visit> Visits = [];

        public Probe()
        {
        }

        public static void Forward(int ordinal, int count, ushort index,
            in ProbeTrack track, in ProbeClip clip, ClipState state,
            in uint tick, in NoInput input, ref Probe result)
            => result.Visits.Add(new(ordinal, count, index, track.Marker, state, clip.Value));

        public static void Backward(int ordinal, int count, ushort index,
            in ProbeTrack track, in ProbeClip clip, ClipState state,
            in uint tick, in NoInput input, ref Probe result)
            => result.Visits.Add(new(ordinal, count, index, track.Marker, state, clip.Value));
    }

    public struct ThrowingResult<TTrack> : ITrack<TTrack, ProbeClip, NoInput, ThrowingResult<TTrack>>
        where TTrack : unmanaged, IBlend<ProbeClip>
    {
        public int Calls;

        public static void Forward(int ordinal, int count, ushort index,
            in TTrack track, in ProbeClip clip, ClipState state,
            in uint tick, in NoInput input, ref ThrowingResult<TTrack> result)
        {
            result.Calls++;
            if (tick == 2)
                throw new ArithmeticException();
        }

        public static void Backward(int ordinal, int count, ushort index,
            in TTrack track, in ProbeClip clip, ClipState state,
            in uint tick, in NoInput input, ref ThrowingResult<TTrack> result)
            => Forward(ordinal, count, index, in track, in clip, state, in tick, in input, ref result);
    }

    public readonly struct GeneratedProbe : ITrackTables<GeneratedProbe, ProbeClip>, IBlend<ProbeClip>
    {
        private static readonly uint[] s_regionStarts = [0, 10];
        private static readonly RegionRow[] s_regionRows = [new(0, 1), new(1, 0)];
        private static readonly TrackRow[] s_trackRows = [new(0, 0, 1)];
        private static readonly ClipRow[] s_clipRows = [new(0, 0, 0)];
        private static readonly ClipEdge[] s_clipEdges = [new(0, 10)];
        private static readonly GeneratedProbe[] s_trackData = [new()];
        private static readonly ProbeClip[] s_clipData = [new(9)];

        public static ReadOnlySpan<uint> RegionStarts => s_regionStarts;
        public static ReadOnlySpan<RegionRow> RegionRows => s_regionRows;
        public static ReadOnlySpan<TrackRow> TrackRows => s_trackRows;
        public static ReadOnlySpan<ClipRow> ClipRows => s_clipRows;
        public static ReadOnlySpan<ClipEdge> ClipEdges => s_clipEdges;
        public static ReadOnlySpan<GeneratedProbe> TrackData => s_trackData;
        public static ReadOnlySpan<ProbeClip> ClipData => s_clipData;
        public static bool Loops => false;

        public void Blend(in ProbeClip first, in ProbeClip second, float t, out ProbeClip result)
            => result = first;
    }

    [Fact]
    public void TrackOperationsPreserveOrderCountIdentityStateAndBlend()
    {
        var timeline = Timeline<ProbeTrack, ProbeClip>.Build(static builder =>
        {
            var blend = builder.Track(new ProbeTrack(11));
            var single = builder.Track(new ProbeTrack(22));
            builder.Clip(in blend, new ProbeClip(4), 0, 8);
            builder.Clip(in blend, new ProbeClip(8), 0, 8);
            builder.Clip(in single, new ProbeClip(3), 0, 8);
        }).InMemory();

        var result = new Probe();
        var playback = Timeline.Start(timeline);
        playback = Timeline.Forward(timeline, in playback, default(NoInput), ref result, 4u);

        Assert.Equal(2, result.Visits.Count);
        Assert.Equal(new Visit(0, 2, 0, 11, ClipState.Stay, 4f + 4f * (4f / 7f)), result.Visits[0]);
        Assert.Equal(new Visit(1, 2, 1, 22, ClipState.Stay, 3f), result.Visits[1]);

        Timeline.Destroy(timeline);
    }

    [Fact]
    public void CallbackExceptionsPreserveCallerStateAndEarlierMutations()
    {
        var timeline = Timeline<ProbeTrack, ProbeClip>.Build(static builder =>
        {
            var track = builder.Track(new ProbeTrack(1));
            builder.Clip(in track, new ProbeClip(9), 0, 10);
        }).InMemory();

        var playback = Timeline.Start(timeline);
        var runtime = new ThrowingResult<ProbeTrack>();
        Assert.Throws<ArithmeticException>(() =>
            Timeline.Forward(timeline, in playback, default(NoInput), ref runtime, 1u, 2u, 3u));
        Assert.Equal(Timeline.Start(timeline), playback);
        Assert.Equal(2, runtime.Calls);

        var cursor = default(Cursor);
        runtime = default;
        Assert.Throws<ArithmeticException>(() =>
            Timeline.Forward(timeline, in playback, ref cursor, default(NoInput), ref runtime, 1u, 2u, 3u));
        Assert.Equal(0, cursor.Owner);
        Assert.Equal(0u, cursor.Tick);
        Assert.Equal(0, cursor.Region);
        Assert.Equal(2, runtime.Calls);

        var generatedPlayback = GeneratedTimeline<GeneratedProbe, ProbeClip>.Start();
        var generated = new ThrowingResult<GeneratedProbe>();
        Assert.Throws<ArithmeticException>(() =>
            GeneratedTimeline<GeneratedProbe, ProbeClip>.Forward(
                in generatedPlayback, default(NoInput), ref generated, 1u, 2u, 3u));
        Assert.Equal(GeneratedTimeline<GeneratedProbe, ProbeClip>.Start(), generatedPlayback);
        Assert.Equal(2, generated.Calls);

        Timeline.Destroy(timeline);
    }
}
