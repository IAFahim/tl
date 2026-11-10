using Xunit;

namespace Tl.Core.Tests;

public unsafe class MotionLawTests
{
    const uint Duration = 8;

    static readonly LibraryTimeline[] BaseTimelines =
    {
        new() { Id = 1, Channel = 1, Duration = Duration },
        new() { Id = 2, Channel = 2, Duration = Duration },
    };

    static AdvanceResult OldAdvance(ref Playback slot, LibraryTimeline* timelines, int timelineCount, bool forward)
    {
        var flags = slot.Flags;
        if ((flags & PlaybackFlags.ReenterPending) != 0)
        {
            slot.Flags = flags & ~(PlaybackFlags.ReenterPending | PlaybackFlags.Began | PlaybackFlags.StartPending);
            return AdvanceResult.Kept;
        }
        if (PlaybackRules.IsParked(flags))
        {
            if (!PlaybackRules.Unpark(ref slot, forward)) return AdvanceResult.Kept;
            flags = slot.Flags;
        }
        if ((flags & (PlaybackFlags.Ended | PlaybackFlags.Paused)) != 0)
        {
            slot.Flags = flags & ~PlaybackFlags.StartPending;
            return AdvanceResult.Kept;
        }
        if (slot.Timeline == 0 || (uint)(slot.Timeline - 1) >= (uint)timelineCount) return AdvanceResult.Unknown;
        var tl = timelines + (slot.Timeline - 1);
        var duration = tl->Duration;
        if (duration == 0)
        {
            slot.Flags = (flags | PlaybackFlags.Ended) & ~PlaybackFlags.StartPending;
            return AdvanceResult.Kept;
        }
        var handover = false;
        if (((flags & PlaybackFlags.Reverse) != 0) == forward)
        {
            if (slot.Tick == 0)
            {
                if (tl->Loop != 0) slot.Tick = (ushort)(duration - 1);
                else if (forward)
                {
                    slot.Flags = flags | PlaybackRules.Parked;
                    return AdvanceResult.Kept;
                }
                else if (tl->Hold != 0) slot.Flags = flags | PlaybackFlags.Ended;
                else return AdvanceResult.Removed;
            }
            else slot.Tick--;
        }
        else if ((uint)slot.Tick + 1u >= duration)
        {
            if (tl->Loop != 0) slot.Tick = 0;
            else if (tl->Next != 0 && (uint)(tl->Next - 1) < (uint)timelineCount)
            {
                slot.Timeline = tl->Next;
                slot.Tick = 0;
                slot.Channel = (byte)timelines[tl->Next - 1].Channel;
                handover = true;
            }
            else if (tl->Hold != 0)
            {
                slot.Tick = (ushort)(duration - 1);
                slot.Flags = flags | PlaybackFlags.Ended;
            }
            else return AdvanceResult.Removed;
        }
        else slot.Tick++;
        slot.Flags = handover ? slot.Flags | PlaybackFlags.StartPending : slot.Flags & ~PlaybackFlags.StartPending;
        return AdvanceResult.Kept;
    }

    static PlaybackMotion OldTickTail(ref Playback slot, LibraryTimeline* timelines, int timelineCount, bool forward)
    {
        var stepDir = forward ? 1 : -1;
        var dir = (slot.Flags & PlaybackFlags.Reverse) != 0 ? -stepDir : stepDir;
        var id = slot.Timeline;
        var tl = timelines + (id - 1);
        var duration = (int)tl->Duration;
        var tick = slot.Tick + dir;
        if ((uint)tick < (uint)duration)
        {
            slot.Tick = (ushort)tick;
            return PlaybackMotion.Stepped;
        }
        if (tl->Loop != 0)
        {
            tick = dir > 0 ? 0 : duration - 1;
            slot.Tick = (ushort)tick;
            return PlaybackMotion.Wrapped;
        }
        if (tick < 0 && stepDir > 0)
        {
            slot.Tick = 0;
            slot.Flags |= PlaybackRules.Parked;
            return PlaybackMotion.Parked;
        }
        if (tl->Next != 0)
        {
            var next = timelines + (tl->Next - 1);
            slot.Timeline = tl->Next;
            slot.Channel = (byte)next->Channel;
            slot.Tick = (ushort)(dir > 0 ? 0 : next->Duration - 1);
            return PlaybackMotion.Handed;
        }
        if (tl->Hold != 0)
        {
            slot.Tick = (ushort)(duration - 1);
            slot.Flags |= PlaybackFlags.Ended;
            return PlaybackMotion.Held;
        }
        return PlaybackMotion.Removed;
    }

    [Fact]
    public void MoveIsTotalAndMatchesAdvanceExceptBackwardExitTable()
    {
        var failures = 0;
        var tickComparisons = 0;
        var divergences = 0;
        var cases = 0;
        foreach (var flagsByte in Enumerable.Range(0, 256))
        foreach (var tick in new ushort[] { 0, (ushort)(Duration / 2), (ushort)(Duration - 1) })
        foreach (var loop in new byte[] { 0, 1 })
        foreach (var hold in new byte[] { 0, 1 })
        foreach (var next in new ushort[] { 0, 2 })
        foreach (var rate in new byte[] { 0, 8, 32 })
        {
            cases++;
            var timelines = (LibraryTimeline[])BaseTimelines.Clone();
            timelines[0].Loop = loop;
            timelines[0].Hold = hold;
            timelines[0].Next = next;
            timelines[0].Channel = 1;
            timelines[1].Loop = 0;
            timelines[1].Hold = 0;
            timelines[1].Next = 0;
            timelines[1].Channel = 2;
            var slot = new Playback
            {
                Timeline = 1,
                Tick = tick,
                Channel = 1,
                Flags = (PlaybackFlags)flagsByte,
                Rate = rate,
                Phase = 0,
            };
            const bool forward = false;
            Playback moveSlot;
            PlaybackMotion motion;
            Playback advSlot;
            AdvanceResult advResult;
            fixed (LibraryTimeline* t = timelines)
            {
                moveSlot = slot;
                motion = PlaybackRules.Move(ref moveSlot, t, timelines.Length, forward);
                advSlot = slot;
                advResult = OldAdvance(ref advSlot, t, timelines.Length, forward);
            }
            if ((int)motion < 0 || (int)motion > 7) failures++;
            if ((uint)moveSlot.Tick >= Duration) failures++;
            var mapped = motion == PlaybackMotion.Removed ? AdvanceResult.Removed : motion == PlaybackMotion.Unknown ? AdvanceResult.Unknown : AdvanceResult.Kept;
            if (mapped != advResult) failures++;
            if (moveSlot.Timeline != advSlot.Timeline || moveSlot.Tick != advSlot.Tick || moveSlot.Channel != advSlot.Channel || moveSlot.Flags != advSlot.Flags || moveSlot.Rate != advSlot.Rate || moveSlot.Phase != advSlot.Phase) failures++;
            var pre = slot;
            if ((pre.Flags & (PlaybackFlags.Began | PlaybackFlags.ReenterPending)) == (PlaybackFlags.Began | PlaybackFlags.ReenterPending)) pre.Flags &= ~PlaybackFlags.ReenterPending;
            pre.Flags &= ~PlaybackFlags.Began;
            var early = false;
            if ((pre.Flags & PlaybackFlags.Stopping) != 0) early = true;
            else
            {
                if (PlaybackRules.IsParked(pre.Flags) && !PlaybackRules.Unpark(ref pre, forward)) early = true;
                else if ((pre.Flags & PlaybackFlags.StartPending) != 0) early = true;
                else if ((pre.Flags & PlaybackFlags.ReenterPending) != 0) early = true;
                else if ((pre.Flags & PlaybackFlags.Paused) != 0) early = true;
                else if ((pre.Flags & PlaybackFlags.Ended) != 0) early = true;
            }
            if (early) continue;
            tickComparisons++;
            Playback movePre;
            PlaybackMotion movePreMotion;
            Playback oldPre;
            PlaybackMotion oldMotion;
            fixed (LibraryTimeline* t = timelines)
            {
                movePre = pre;
                movePreMotion = PlaybackRules.Move(ref movePre, t, timelines.Length, forward);
                oldPre = pre;
                oldMotion = OldTickTail(ref oldPre, t, timelines.Length, forward);
            }
            if ((uint)movePre.Tick >= Duration || (uint)oldPre.Tick >= Duration) failures++;
            var same = movePre.Timeline == oldPre.Timeline && movePre.Tick == oldPre.Tick && movePreMotion == oldMotion;
            if (same) continue;
            var dir = ((pre.Flags & PlaybackFlags.Reverse) != 0) ? 1 : -1;
            var isBackwardExit = pre.Tick == 0 && dir < 0 && loop == 0 && (next != 0 || hold != 0);
            if (!isBackwardExit)
            {
                failures++;
                continue;
            }
            divergences++;
            if (movePre.Tick != advSlot.Tick || movePre.Timeline != advSlot.Timeline) failures++;
            if (hold != 0 && next == 0)
            {
                if (movePreMotion != PlaybackMotion.Held || movePre.Tick != 0) failures++;
                if (oldMotion != PlaybackMotion.Held || oldPre.Tick != Duration - 1) failures++;
            }
            else if (next != 0 && hold == 0)
            {
                if (movePreMotion != PlaybackMotion.Removed) failures++;
                if (oldMotion != PlaybackMotion.Handed || oldPre.Timeline != next || oldPre.Tick != Duration - 1) failures++;
            }
            else if (next != 0 && hold != 0)
            {
                if (movePreMotion != PlaybackMotion.Held || movePre.Tick != 0) failures++;
                if (oldMotion != PlaybackMotion.Handed || oldPre.Timeline != next || oldPre.Tick != Duration - 1) failures++;
            }
        }
        Assert.Equal(18432, cases);
        Assert.Equal(0, failures);
        Assert.Equal(864, tickComparisons);
        Assert.Equal(54, divergences);
    }

    [Fact]
    public void SteppedReversesItsTick()
    {
        var timelines = (LibraryTimeline[])BaseTimelines.Clone();
        timelines[0].Loop = 0;
        timelines[0].Hold = 0;
        timelines[0].Next = 0;
        var slot = new Playback { Timeline = 1, Tick = 4, Channel = 1, Flags = PlaybackFlags.None, Rate = 0, Phase = 0 };
        fixed (LibraryTimeline* t = timelines)
        {
            var motion = PlaybackRules.Move(ref slot, t, timelines.Length, true);
            Assert.Equal(PlaybackMotion.Stepped, motion);
            Assert.Equal(5, slot.Tick);
            var back = PlaybackRules.Move(ref slot, t, timelines.Length, false);
            Assert.Equal(PlaybackMotion.Stepped, back);
            Assert.Equal(4, slot.Tick);
        }
    }
}
