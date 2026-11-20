namespace Tl.Systems.Tests;

static unsafe class KindSpans
{
    public static int Listed(TimelineWorld world, PlaybackTable players, KindStore store, int entity, string kindName)
    {
        var kind = store.IndexOf(kindName);
        var score = store.Score;
        var view = players.View;
        var spans = 0;
        for (var i = 0; i < view.CountOf(entity); i++)
        {
            var slot = view.SlotOf(entity, i);
            if (slot->Timeline == 0 || !KindScore.Listed(slot->Flags)) continue;
            var cell = score.CellOf(slot->Timeline, slot->Tick, kind);
            if (cell >= 0) spans += score.Start[cell + 1] - score.Start[cell];
        }
        return spans;
    }
}
