using Xunit;

namespace Tl.Core.Tests;

public unsafe class PlaybackRulesTests
{
    struct ListSlots : IPlaybackSlots
    {
        public Playback[] Items;
        public int Count;
        public int Closed;
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
}
