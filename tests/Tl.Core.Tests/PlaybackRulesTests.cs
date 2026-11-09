using Xunit;

namespace Tl.Core.Tests;

public unsafe class PlaybackRulesTests
{
    struct ListSlots : IPlaybackSlots
    {
        public Playback[] Items;
        public int Count;
        public int Closed;
        public int Purged;
        public bool Pending;

        public int Length => Count;
        public ref Playback this[int index] => ref Items[index];
        public bool StartsPending => Pending;

        public bool TryAdd(in Playback playback)
        {
            if (Count == Items.Length) return false;
            Items[Count++] = playback;
            return true;
        }

        public bool Admit(ushort timeline) => true;

        public void Purge(int index) => Purged++;

        public void Close(int index)
        {
            Closed++;
            Items[index] = default;
            while (Count > 0 && Items[Count - 1].Timeline == 0) Count--;
        }
    }

    static readonly LibraryTimeline[] Timelines =
    {
        new() { Id = 1, Channel = 1, Loop = 1, Duration = 10 },
        new() { Id = 2, Channel = 1, Next = 1, Duration = 6 },
        new() { Id = 3, Channel = 2, Duration = 8 },
        new() { Id = 4, Duration = 4 },
    };

    static ListSlots Slots(int capacity = 4) => new() { Items = new Playback[capacity] };

    static bool Start(ref ListSlots slots, ushort timeline, ushort at = 0)
    {
        fixed (LibraryTimeline* t = Timelines) return PlaybackRules.Start(ref slots, t, Timelines.Length, timeline, at, out _);
    }

    [Fact]
    public void StartClampsAndPendsOnlyPastTickZero()
    {
        var slots = Slots();
        Assert.True(Start(ref slots, 3, 20));
        Assert.Equal(7, slots[0].Tick);
        Assert.Equal(PlaybackFlags.StartPending, slots[0].Flags);
        Assert.True(Start(ref slots, 1, 23));
        Assert.Equal(3, slots[1].Tick);
        Assert.True(Start(ref slots, 4));
        Assert.Equal(PlaybackFlags.None, slots[2].Flags);
        var kinds = Slots();
        kinds.Pending = true;
        Assert.True(Start(ref kinds, 4));
        Assert.Equal(PlaybackFlags.StartPending, kinds[0].Flags);
    }

    [Fact]
    public void ChannelStartReplacesAndDuplicateIsRejected()
    {
        var slots = Slots();
        Assert.True(Start(ref slots, 1));
        Assert.True(Start(ref slots, 2));
        Assert.Equal(1, slots.Closed);
        Assert.Equal(1, slots.Length);
        Assert.Equal(2, slots[0].Timeline);
        Assert.True(Start(ref slots, 2, 3));
        Assert.Equal(2, slots.Closed);
        Assert.Equal(3, slots[0].Tick);
        Assert.True(Start(ref slots, 4));
        Assert.False(Start(ref slots, 4));
        Assert.Equal(2, slots.Length);
    }

    [Fact]
    public void StopsPauseAndQueries()
    {
        var slots = Slots();
        Start(ref slots, 1);
        Start(ref slots, 3, 2);
        Start(ref slots, 4);
        Assert.True(PlaybackRules.Find(ref slots, 2, out var found));
        Assert.Equal(2, found.Tick);
        Assert.False(PlaybackRules.Find(ref slots, 0, out _));
        Assert.True(PlaybackRules.Pause(ref slots, 4, true));
        Assert.Equal(PlaybackFlags.Paused, slots[2].Flags);
        Assert.True(PlaybackRules.StopChannel(ref slots, 2));
        Assert.False(PlaybackRules.IsPlaying(ref slots, 3));
        Assert.Equal(0, slots[1].Timeline);
        Assert.True(PlaybackRules.Stop(ref slots, 1));
        PlaybackRules.StopAll(ref slots);
        Assert.Equal(0, slots.Length);
        Assert.Equal(3, slots.Closed);
    }

    [Fact]
    public void StartReusesTheFirstHoleAndFailsWhenFull()
    {
        var slots = Slots(2);
        Start(ref slots, 3);
        Start(ref slots, 4);
        PlaybackRules.Stop(ref slots, 3);
        Assert.Equal(2, slots.Length);
        Assert.True(Start(ref slots, 1));
        Assert.Equal(1, slots[0].Timeline);
        Assert.False(Start(ref slots, 3));
    }

    static AdvanceResult Advance(ref Playback slot, bool forward = true)
    {
        fixed (LibraryTimeline* t = Timelines) return PlaybackRules.Advance(ref slot, t, Timelines.Length, forward);
    }

    [Fact]
    public void AdvanceWrapsLoopsAndHandsOverToNext()
    {
        var run = new Playback { Timeline = 1, Tick = 9, Channel = 1 };
        Assert.Equal(AdvanceResult.Kept, Advance(ref run));
        Assert.Equal(0, run.Tick);
        Assert.Equal(AdvanceResult.Kept, Advance(ref run, false));
        Assert.Equal(9, run.Tick);
        var jump = new Playback { Timeline = 2, Tick = 5, Channel = 7, Flags = PlaybackFlags.StartPending };
        Assert.Equal(AdvanceResult.Kept, Advance(ref jump));
        Assert.Equal(1, jump.Timeline);
        Assert.Equal(0, jump.Tick);
        Assert.Equal(1, jump.Channel);
        Assert.Equal(PlaybackFlags.StartPending, jump.Flags);
        Assert.Equal(AdvanceResult.Kept, Advance(ref jump));
        Assert.Equal(PlaybackFlags.None, jump.Flags);
    }

    [Fact]
    public void AdvanceEndsRemovesPausesAndReverses()
    {
        var slide = new Playback { Timeline = 3, Tick = 7 };
        Assert.Equal(AdvanceResult.Removed, Advance(ref slide));
        var back = new Playback { Timeline = 3, Tick = 4, Flags = PlaybackFlags.Reverse };
        Assert.Equal(AdvanceResult.Kept, Advance(ref back));
        Assert.Equal(3, back.Tick);
        var paused = new Playback { Timeline = 1, Tick = 4, Flags = PlaybackFlags.Paused | PlaybackFlags.StartPending };
        Assert.Equal(AdvanceResult.Kept, Advance(ref paused));
        Assert.Equal(4, paused.Tick);
        Assert.Equal(PlaybackFlags.Paused, paused.Flags);
        var lost = new Playback { Timeline = 9 };
        Assert.Equal(AdvanceResult.Unknown, Advance(ref lost));
    }

    [Fact]
    public void AReversedPlaybackParksAtItsStartAndBeginsWhenItFacesForward()
    {
        var slide = new Playback { Timeline = 2, Tick = 0, Channel = 1, Flags = PlaybackFlags.Reverse, Reserved = 0x0305 };
        Assert.Equal(AdvanceResult.Kept, Advance(ref slide));
        Assert.Equal(PlaybackRules.Parked | PlaybackFlags.Reverse, slide.Flags);
        Assert.Equal((ushort)2, slide.Timeline);
        Assert.Equal(0, slide.Tick);
        Assert.Equal(0x0305, slide.Reserved);
        Assert.Equal(AdvanceResult.Kept, Advance(ref slide));
        Assert.Equal(PlaybackRules.Parked | PlaybackFlags.Reverse, slide.Flags);
        Assert.False(PlaybackRules.Resumes(slide.Flags, true));
        Assert.True(PlaybackRules.Resumes(slide.Flags, false));
        var slots = Slots();
        slots.Items[0] = slide;
        slots.Count = 1;
        Apply(ref slots, RequestOp.Reverse, 2, 0);
        Assert.Equal(PlaybackRules.Parked, slots[0].Flags);
        Apply(ref slots, RequestOp.Pause, 2);
        var paused = slots[0];
        Assert.Equal(AdvanceResult.Kept, Advance(ref paused));
        Assert.Equal(PlaybackRules.Parked | PlaybackFlags.Paused, paused.Flags);
        Apply(ref slots, RequestOp.Resume, 2);
        var go = slots[0];
        Assert.Equal(AdvanceResult.Kept, Advance(ref go));
        Assert.Equal(1, go.Tick);
        Assert.Equal(PlaybackFlags.None, go.Flags);
        Apply(ref slots, RequestOp.Seek, 2, 4);
        Assert.Equal(4, slots[0].Tick);
        Assert.Equal(PlaybackFlags.StartPending, slots[0].Flags);
        Assert.Equal(0, slots.Purged);
        var world = new Playback { Timeline = 3, Tick = 0 };
        Assert.Equal(AdvanceResult.Removed, Advance(ref world, false));
        var loop = new Playback { Timeline = 1, Tick = 0, Flags = PlaybackFlags.Reverse };
        Assert.Equal(AdvanceResult.Kept, Advance(ref loop));
        Assert.Equal(9, loop.Tick);
        Assert.Equal(PlaybackFlags.Reverse, loop.Flags);
    }

    static void Apply(ref ListSlots slots, RequestOp op, ushort timeline, ushort at = 0)
    {
        fixed (LibraryTimeline* t = Timelines) PlaybackRules.Apply(ref slots, t, Timelines.Length, op, timeline, at);
    }

    [Fact]
    public void SeekClampsWrapsPurgesAndKeepsPauseAndDirection()
    {
        var slots = Slots();
        slots.Items[0] = new Playback { Timeline = 3, Tick = 7, Channel = 2, Flags = PlaybackFlags.Ended | PlaybackFlags.Paused | PlaybackFlags.Reverse, Reserved = 0x1234 };
        slots.Items[1] = new Playback { Timeline = 1, Tick = 4, Channel = 1, Flags = PlaybackFlags.Began };
        slots.Count = 2;
        Apply(ref slots, RequestOp.Seek, 3, 500);
        Assert.Equal(7, slots[0].Tick);
        Assert.Equal(PlaybackFlags.Paused | PlaybackFlags.Reverse | PlaybackFlags.ReenterPending, slots[0].Flags);
        Assert.Equal(0x1234, slots[0].Reserved);
        Assert.Equal(1, slots.Purged);
        Apply(ref slots, RequestOp.SeekChannel, 1, 23);
        Assert.Equal(3, slots[1].Tick);
        Assert.Equal(PlaybackFlags.ReenterPending, slots[1].Flags);
        Assert.Equal(2, slots.Purged);
        Apply(ref slots, RequestOp.SeekChannel, 0, 1);
        Apply(ref slots, RequestOp.Seek, 2, 1);
        Assert.Equal(2, slots.Purged);
    }

    [Fact]
    public void SeekOnAPendingStartOnlyMovesWhereItBegins()
    {
        var slots = Slots();
        slots.Items[0] = new Playback { Timeline = 3, Tick = 2, Flags = PlaybackFlags.StartPending };
        slots.Count = 1;
        Apply(ref slots, RequestOp.Seek, 3, 5);
        Assert.Equal(5, slots[0].Tick);
        Assert.Equal(PlaybackFlags.StartPending, slots[0].Flags);
        Assert.Equal(0, slots.Purged);
    }

    [Fact]
    public void ReverseLeavesAHoldOnlyAtTheEndItFacesAway()
    {
        var slots = Slots();
        slots.Items[0] = new Playback { Timeline = 3, Tick = 7, Channel = 2, Flags = PlaybackFlags.Ended };
        slots.Items[1] = new Playback { Timeline = 4, Tick = 0, Flags = PlaybackFlags.Ended | PlaybackFlags.Reverse };
        slots.Count = 2;
        Apply(ref slots, RequestOp.Reverse, 3, 0);
        Assert.Equal(PlaybackFlags.Ended, slots[0].Flags);
        Apply(ref slots, RequestOp.ReverseChannel, 2, 1);
        Assert.Equal(PlaybackFlags.Reverse | PlaybackFlags.StartPending, slots[0].Flags);
        Assert.Equal(7, slots[0].Tick);
        Apply(ref slots, RequestOp.Reverse, 4, 1);
        Assert.Equal(PlaybackFlags.Ended | PlaybackFlags.Reverse, slots[1].Flags);
        Apply(ref slots, RequestOp.Reverse, 4, 0);
        Assert.Equal(PlaybackFlags.StartPending, slots[1].Flags);
        Assert.Equal(0, slots.Purged);
    }

    [Fact]
    public void ChannelPauseAndResumeTouchOnlyThatChannel()
    {
        var slots = Slots();
        slots.Items[0] = new Playback { Timeline = 3, Channel = 2 };
        slots.Items[1] = new Playback { Timeline = 4 };
        slots.Count = 2;
        Apply(ref slots, RequestOp.PauseChannel, 0);
        Assert.Equal(PlaybackFlags.None, slots[1].Flags);
        Apply(ref slots, RequestOp.PauseChannel, 2);
        Assert.Equal(PlaybackFlags.Paused, slots[0].Flags);
        Assert.Equal(PlaybackFlags.None, slots[1].Flags);
        Apply(ref slots, RequestOp.ResumeChannel, 2);
        Assert.Equal(PlaybackFlags.None, slots[0].Flags);
    }
}
