using System;

namespace Tl;

public sealed partial class TimelineWorld
{
    internal SystemRegistry? _systems;
    internal ulong _generation;

    public unsafe Requests Requests(PlaybackTable players)
    {
        if (players is null) throw new ArgumentNullException(nameof(players));
        return new Requests(players.Consumer, -1, -1);
    }

    public unsafe void ApplyRequests(PlaybackTable players)
    {
        if (players is null) throw new ArgumentNullException(nameof(players));
        if (players.Kinds != null) players.Kinds.Sync();
        else SyncPlaybacks(players);
        Drain(players, players.Consumer);
        Drain(players, players.Pending);
    }

    public unsafe void Tick(PlaybackTable players, bool forward = true)
    {
        if (players is null) throw new ArgumentNullException(nameof(players));
        var kinds = players.Kinds ?? throw new InvalidOperationException("Tick needs a KindStore attached to the PlaybackTable; use Advance for kind-free playback.");
        ClosingList.Clear(players.Closing);
        ApplyRequests(players);
        kinds.TickPass(forward);
    }

    public unsafe void Tick(PlaybackTable players, bool forward, TlWorkers workers)
    {
        if (players is null) throw new ArgumentNullException(nameof(players));
        if (workers is null) throw new ArgumentNullException(nameof(workers));
        var kinds = players.Kinds ?? throw new InvalidOperationException("Tick needs a KindStore attached to the PlaybackTable; use Advance for kind-free playback.");
        ClosingList.Clear(players.Closing);
        ApplyRequests(players);
        kinds.TickPassParallel(forward, workers);
    }

    public unsafe void Advance(PlaybackTable players, bool forward = true)
    {
        if (players is null) throw new ArgumentNullException(nameof(players));
        if (players.Kinds != null) throw new InvalidOperationException("A PlaybackTable with a KindStore advances through Tick, not Advance.");
        SyncPlaybacks(players);
        var timelines = _view.Timelines;
        if (timelines == null) return;
        var count = _view.TimelineCount;
        AdvanceRange(players, timelines, count, forward, 0, players.PlayingWords);
        ClosingList.Clear(players.Closing);
    }

    public unsafe void Advance(PlaybackTable players, bool forward, TlWorkers workers)
    {
        if (players is null) throw new ArgumentNullException(nameof(players));
        if (workers is null) throw new ArgumentNullException(nameof(workers));
        if (players.Kinds != null) throw new InvalidOperationException("A PlaybackTable with a KindStore advances through Tick, not Advance.");
        SyncPlaybacks(players);
        var timelines = _view.Timelines;
        if (timelines == null)
        {
            ClosingList.Clear(players.Closing);
            return;
        }
        var count = _view.TimelineCount;
        if (workers.WorkerCount == 1)
        {
            AdvanceRange(players, timelines, count, forward, 0, players.PlayingWords);
            ClosingList.Clear(players.Closing);
            return;
        }
        var words = players.PlayingWords;
        var chunks = Math.Min(workers.WorkerCount * 8, words);
        if (chunks <= 0)
        {
            ClosingList.Clear(players.Closing);
            return;
        }
        var ends = stackalloc int[chunks];
        TlWorkers.Split(players.Playing, words, ends, chunks);
        var view = players.View;
        var state = new AdvanceState
        {
            Playing = players.Playing,
            Slots = view.Slots,
            Counts = view.Counts,
            SlotsPerEntity = view.SlotsPerEntity,
            Timelines = timelines,
            TimelineCount = count,
            Forward = forward ? 1 : 0,
        };
        workers.Run(&AdvanceChunk, &state, ends, chunks);
        ClosingList.Clear(players.Closing);
    }

    unsafe void AdvanceRange(PlaybackTable players, LibraryTimeline* timelines, int count, bool forward, int firstWord, int endWord)
    {
        var playing = players.Playing;
        if (firstWord < 0) firstWord = 0;
        if (endWord > players.PlayingWords) endWord = players.PlayingWords;
        for (var w = firstWord; w < endWord; w++)
        {
            var bits = playing[w];
            while (bits != 0)
            {
                var entity = (w << 6) + KindStore.Tz(bits);
                bits &= bits - 1;
                AdvanceEntity(players, entity, timelines, count, forward);
            }
        }
    }

    unsafe struct AdvanceState
    {
        public ulong* Playing;
        public Playback* Slots;
        public int* Counts;
        public int SlotsPerEntity;
        public LibraryTimeline* Timelines;
        public int TimelineCount;
        public int Forward;
    }

    static unsafe void AdvanceChunk(void* p, int firstWord, int endWord, int chunk)
    {
        var s = (AdvanceState*)p;
        var playing = s->Playing;
        for (var w = firstWord; w < endWord; w++)
        {
            var bits = playing[w];
            while (bits != 0)
            {
                var entity = (w << 6) + KindStore.Tz(bits);
                bits &= bits - 1;
                AdvanceOne(s, entity);
            }
        }
    }

    static unsafe void AdvanceOne(AdvanceState* s, int entity)
    {
        var timelines = s->Timelines;
        var count = s->TimelineCount;
        var forward = s->Forward != 0;
        var slotsBase = s->Slots;
        var per = s->SlotsPerEntity;
        var counts = s->Counts;
        var slots = slotsBase + (nuint)entity * (nuint)per;
        var slotsCount = counts[entity];
        for (var i = 0; i < slotsCount; i++)
        {
            if (slots[i].Timeline == 0) continue;
            var result = PlaybackRules.Step(ref slots[i], timelines, count, forward);
            if (result == AdvanceResult.Removed)
            {
                slots[i] = default;
                var c = counts[entity];
                while (c > 0 && slots[c - 1].Timeline == 0) c--;
                counts[entity] = c;
            }
            else if (result == AdvanceResult.Unknown) throw new ArgumentException($"Playback slot {i} of entity {entity} names no timeline in the loaded library.");
        }
        var holds = false;
        var n = counts[entity];
        for (var i = 0; i < n; i++)
        {
            var slot = slots[i];
            if (slot.Timeline == 0) continue;
            var f = slot.Flags;
            if (PlaybackRules.Moves(f, forward) || PlaybackRules.IsParked(f) || (f & (PlaybackFlags.Stopping | PlaybackFlags.ReenterPending)) != 0 || ((f & (PlaybackFlags.Paused | PlaybackFlags.Ended)) != 0 && (f & PlaybackFlags.StartPending) != 0))
            {
                holds = true;
                break;
            }
        }
        if (!holds) s->Playing[entity >> 6] &= ~(1ul << (entity & 63));
#if TL_CHECKED
        Checked.Visited();
#endif
    }

    unsafe void AdvanceEntity(PlaybackTable players, int entity, LibraryTimeline* timelines, int count, bool forward)
    {
        var view = players.View;
        var state = new AdvanceState
        {
            Playing = players.Playing,
            Slots = view.Slots,
            Counts = view.Counts,
            SlotsPerEntity = view.SlotsPerEntity,
            Timelines = timelines,
            TimelineCount = count,
            Forward = forward ? 1 : 0,
        };
        AdvanceOne(&state, entity);
    }

    unsafe void Drain(PlaybackTable players, RequestQueue* queue)
    {
        if (queue->Count == 0) return;
        RequestQueue.SortStable(queue);
        for (var i = 0; i < queue->Count; i++)
        {
            ref readonly var record = ref queue->Item(i);
            ApplyRecord(players, record);
        }
        RequestQueue.Clear(queue);
    }

    unsafe void SyncPlaybacks(PlaybackTable players)
    {
        if (players.LibraryGeneration == _generation) return;
        for (var entity = 0; entity < players.EntityCapacity; entity++)
        {
            var slots = players.SlotsOf(entity);
            var count = players.CountOf(entity);
            for (var slot = 0; slot < count; slot++)
            {
                if (slots[slot].Timeline > _view.TimelineCount && (slots[slot].Flags & PlaybackFlags.Stopping) == 0)
                    throw new ArgumentException($"Playback slot {slot} of entity {entity} names no timeline in the reloaded library.");
                if (!PlaybackRules.Rebind(ref slots[slot], _view.Timelines, _view.TimelineCount)) players.RemoveSlot(entity, slot);
            }
        }
        players.LibraryGeneration = _generation;
    }

    unsafe void ApplyRecord(PlaybackTable players, in RequestRecord record)
    {
        if ((uint)record.Entity >= (uint)players.EntityCapacity)
            throw new ArgumentOutOfRangeException(nameof(record), $"Request names entity {record.Entity} outside the table capacity.");
        if (record.Op == RequestOp.Start && (record.Timeline == 0 || (uint)(record.Timeline - 1) >= (uint)_view.TimelineCount))
            throw new ArgumentException($"Request starts timeline {record.Timeline}; the loaded library holds {_view.TimelineCount}.");
        var slots = new PlaybackTable.Slots(players, record.Entity);
        PlaybackRules.Apply(ref slots, _view.Timelines, _view.TimelineCount, record.Op, record.Timeline, record.At);
        players.SetPlaying(record.Entity, true);
    }
}
