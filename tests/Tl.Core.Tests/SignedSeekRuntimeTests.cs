using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

public class SignedSeekRuntimeTests
{
    public readonly struct TestTimeline : ITimeline
    {
        public static void Define(scoped Builder builder)
        {
        }
    }

    public readonly record struct TestClip(int Value);

    public readonly struct TestTrack : ITrack<TestClip>
    {
        public void Blend(in TestClip first, in TestClip second, float factor, out TestClip result)
            => result = factor < 0.5f ? first : second;
    }

    public ref struct TestData : ITimelineData<TestData>
    {
        public ushort Id;
        public int Delta;
        public int Calls;
        public bool Result;

        public static bool TrySeek(ushort id, scoped ref TestData data, int delta)
        {
            data.Id = id;
            data.Delta = delta;
            data.Calls++;
            return data.Result;
        }
    }

    [Fact]
    public void PlaybackLayoutsAreSixteenBytes()
    {
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(PlaybackFlags)));
        Assert.Equal(1, Unsafe.SizeOf<PlaybackFlags>());
        Assert.Equal(16, Unsafe.SizeOf<Playback>());
        Assert.Equal(16, Unsafe.SizeOf<Playback<TestTimeline>>());

        var typed = Timeline.CreateTypedPlayback<TestTimeline>(0, 4_000_000_000, PlaybackFlags.Started);
        Assert.Equal(0, typed.Position);
        Assert.Equal(4_000_000_000u, typed.GameTick);
        Assert.Equal(PlaybackFlags.Started, typed.Flags);
    }

    [Fact]
    public void FrameFlagsHaveFrozenByteValues()
    {
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(FrameFlags)));
        Assert.Equal(1, Unsafe.SizeOf<FrameFlags>());
        Assert.Equal(1, (byte)FrameFlags.ClipStart);
        Assert.Equal(2, (byte)FrameFlags.ClipEnd);
        Assert.Equal(4, (byte)FrameFlags.TimelineStart);
        Assert.Equal(8, (byte)FrameFlags.TimelineEnd);
        Assert.Equal(16, (byte)FrameFlags.CompletedBefore);
        Assert.Equal(32, (byte)FrameFlags.CompletedAfter);
        Assert.Equal(64, (byte)FrameFlags.Looping);
        Assert.Equal(128, (byte)FrameFlags.Reverse);
    }

    [Fact]
    public void FrameRepresentsIndependentBoundariesAndDerivedDirection()
    {
        var track = new TestTrack();
        var clip = new TestClip(31);
        var flags = FrameFlags.ClipStart | FrameFlags.ClipEnd |
            FrameFlags.TimelineStart | FrameFlags.TimelineEnd | FrameFlags.Reverse;
        var frame = new Frame<TestTrack, TestClip>(in track, in clip, 91, 0, -3, 7, flags);

        Assert.Equal(91u, frame.GameTick);
        Assert.Equal(0u, frame.TimelineTick);
        Assert.Equal(-3, frame.Cycle);
        Assert.Equal((ushort)7, frame.TrackIndex);
        Assert.Equal(31, frame.Clip.Value);
        Assert.Equal(-1, frame.Direction);
        Assert.True(frame.Has(FrameFlags.ClipStart | FrameFlags.ClipEnd));
        Assert.True(frame.Has(FrameFlags.TimelineStart | FrameFlags.TimelineEnd));
    }

    [Fact]
    public void DynamicStartUsesBoundaryZeroAndExecutesNothing()
    {
        var id = Timeline.RegisterCompiled(12, false, new CompiledRoute(0, 0));
        var data = new TestData { Result = true };

        Assert.True(Timeline.TryStart(id, 4_000_000_000, out var playback));
        Assert.Equal(0, playback.Position);
        Assert.Equal(4_000_000_000u, playback.GameTick);
        Assert.Equal(id, playback.Owner);
        Assert.Equal(PlaybackFlags.Started, playback.Flags);
        Assert.Equal(0, data.Calls);
    }

    [Fact]
    public void DynamicStopIsTotalAndIdempotent()
    {
        var firstId = Timeline.RegisterCompiled(12, false, new CompiledRoute(0, 0));
        var secondId = Timeline.RegisterCompiled(12, false, new CompiledRoute(0, 1));
        Assert.True(Timeline.TryStart(firstId, 73, out var started));

        Assert.False(Timeline.TryStop(secondId, in started, out var wrongOwner));
        Assert.Equal(started, wrongOwner);

        var initial = default(Playback);
        Assert.False(Timeline.TryStop(firstId, in initial, out var defaultResult));
        Assert.Equal(initial, defaultResult);

        Assert.False(Timeline.TryStop(ushort.MaxValue, in started, out var invalidId));
        Assert.Equal(started, invalidId);

        Assert.True(Timeline.TryStop(firstId, in started, out var stopped));
        Assert.True(stopped.Has(PlaybackFlags.Started | PlaybackFlags.Stopped));
        Assert.Equal(started.Position, stopped.Position);
        Assert.Equal(started.GameTick, stopped.GameTick);

        Assert.True(Timeline.TryStop(firstId, in stopped, out var stoppedAgain));
        Assert.Equal(stopped, stoppedAgain);
    }

    [Fact]
    public void InvalidDynamicStartReturnsDefaultState()
    {
        Assert.False(Timeline.TryStart(ushort.MaxValue, 97, out var playback));
        Assert.Equal(default, playback);
    }

    [Fact]
    public void DynamicSeekForwardsSignedDeltaWithoutTransformation()
    {
        var data = new TestData { Result = true };

        Assert.True(Timeline.TrySeek(41, ref data, int.MinValue));
        Assert.Equal((ushort)41, data.Id);
        Assert.Equal(int.MinValue, data.Delta);
        Assert.Equal(1, data.Calls);
    }
}
