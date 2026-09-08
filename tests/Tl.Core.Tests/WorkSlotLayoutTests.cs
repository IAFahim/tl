using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public class WorkSlotLayoutTests
{
    public readonly record struct LayoutClip(float Value);

    public readonly struct LayoutTrack(int marker) : IBlend<LayoutClip>
    {
        public static int BlendCalls;

        public readonly int Marker = marker;

        public void Blend(in LayoutClip first, in LayoutClip second, float t, out LayoutClip result)
        {
            BlendCalls++;
            result = new LayoutClip(first.Value + (second.Value - first.Value) * t);
        }
    }

    public readonly record struct Visit(ushort Index, int Marker, uint Tick, ClipState State, float First, float Second);

    public struct LayoutResult :
        IForward<LayoutTrack, LayoutClip, NoInput, LayoutResult>,
        IBackward<LayoutTrack, LayoutClip, NoInput, LayoutResult>
    {
        public List<Visit> Visits = [];

        public LayoutResult()
        {
        }

        public void Forward(in Tracks<LayoutTrack, LayoutClip> tracks, in NoInput input, in uint tick, ref LayoutResult result)
            => Capture(in tracks, tick, ref result);

        public void Backward(in Tracks<LayoutTrack, LayoutClip> tracks, in NoInput input, in uint tick, ref LayoutResult result)
            => Capture(in tracks, tick, ref result);

        private static void Capture(in Tracks<LayoutTrack, LayoutClip> tracks, uint tick, ref LayoutResult result)
        {
            foreach (var work in tracks)
            {
                var first = work.Clip.Value;
                var second = work.Clip.Value;
                result.Visits.Add(new Visit(work.Index, work.Track.Marker, tick, work.State, first, second));
            }
        }
    }

    [Fact]
    public void WorkSlotHasPackedStableFieldOffsets()
    {
        Assert.Equal(24, Unsafe.SizeOf<WorkSlot>());
        Assert.Equal(0, OffsetOf(nameof(WorkSlot.EnterF)));
        Assert.Equal(4, OffsetOf(nameof(WorkSlot.EnterB)));
        Assert.Equal(8, OffsetOf(nameof(WorkSlot.FactorStart)));
        Assert.Equal(12, OffsetOf(nameof(WorkSlot.FactorLength)));
        Assert.Equal(16, OffsetOf(nameof(WorkSlot.Index)));
        Assert.Equal(18, OffsetOf(nameof(WorkSlot.First)));
        Assert.Equal(20, OffsetOf(nameof(WorkSlot.Second)));
        Assert.Equal(22, OffsetOf(nameof(WorkSlot.BlendOrdinal)));
    }

    [Fact]
    public void ReorderedSlotsPreserveNativeBlendedPlayback()
    {
        var id = Timeline<LayoutTrack, LayoutClip>.Build(static b =>
        {
            var track = b.Track(new LayoutTrack(37));
            b.Clip(in track, new LayoutClip(2f), 0, 7);
            b.Clip(in track, new LayoutClip(8f), 3, 9);
        }).InMemory();

        try
        {
            Timeline<LayoutTrack, LayoutClip>.Bind<NoInput, LayoutResult>(id);
            var input = default(NoInput);
            var result = new LayoutResult();
            var playback = Timeline.Start(id, 2);
            Span<LayoutClip> scratch = stackalloc LayoutClip[1];
            LayoutTrack.BlendCalls = 0;

            playback = Timeline<LayoutTrack, LayoutClip>.Forward(
                id,
                in playback,
                in input,
                ref result,
                scratch,
                [3u, 4u, 5u, 6u, 7u, 8u]);

            Assert.Equal(
                [
                    new Visit(0, 37, 3, ClipState.Stay, BlendAt(3), BlendAt(3)),
                    new Visit(0, 37, 4, ClipState.Stay, BlendAt(4), BlendAt(4)),
                    new Visit(0, 37, 5, ClipState.Stay, BlendAt(5), BlendAt(5)),
                    new Visit(0, 37, 6, ClipState.Stay, BlendAt(6), BlendAt(6)),
                    new Visit(0, 37, 7, ClipState.Stay, 8f, 8f),
                    new Visit(0, 37, 8, ClipState.Exit, 8f, 8f),
                ],
                result.Visits);
            Assert.Equal(8, LayoutTrack.BlendCalls);
            Assert.Equal(8u, playback.Tick);
            Assert.True(playback.Has(PlaybackFlags.Completed));
        }
        finally
        {
            Timeline.Destroy(id);
        }
    }

    private static int OffsetOf(string field)
        => Marshal.OffsetOf<WorkSlot>(field).ToInt32();

    private static float BlendAt(uint tick)
        => 2f + (8f - 2f) * ((tick - 3u) / (float)(4u - 1u));
}
