namespace Tl;

/// <summary>
/// Cold, read-only introspection over the process-wide timeline tables and the per-pair banks.
/// The surface exists so a hosting tool can report runtime state without reflecting over engine
/// internals: it allocates freely on the caller's thread, never runs during playback, and never
/// folds, disposes, or reclaims. A bank slot's state is reported exactly as observed; driving a
/// fold is the caller's own operation between two snapshots.
/// </summary>
public static unsafe class Inspection
{
    /// <summary>Observed fold state of one bank slot; snapshotting never resolves it.</summary>
    public enum FoldState
    {
        Absent,
        Pending,
        Folded,
    }

    public sealed record InternSnapshot(
        int Capacity,
        int Live,
        int Distinct,
        long GraveyardBlocks,
        long GraveyardBytes,
        long AllocBytes,
        long PeakLiveBytes);

    public sealed record PairSnapshot(
        int Slots,
        int Pairs,
        int LoadFactorPermille,
        int Consumers,
        int ConsumerCapacity,
        int SlotRowBytes);

    public sealed record BakeSnapshot(int Slots, int Entries);

    public sealed record TablesSnapshot(InternSnapshot Intern, PairSnapshot Pair, BakeSnapshot Bake);

    public sealed record LaneShape(ulong Key, int Distinct, int LongestRun);

    public sealed record ViewSnapshot(
        int Index,
        FoldState State,
        ushort? Duration,
        bool? Looping,
        int? Ticks,
        int? Lanes,
        int? AbiVersion,
        long? Generation,
        IReadOnlyList<LaneShape>? LaneShapes);

    public sealed record BankSnapshot(
        string Track,
        string Clip,
        ulong PairKey,
        int Count,
        int Holes,
        int Blocks,
        int DedupeHits,
        long Generation,
        long HeaderBytes,
        long TableBytes,
        long DirectoryBytes,
        long ArenaBytes,
        long RetainedBytes,
        IReadOnlyList<ViewSnapshot> Views);

    /// <summary>Process-wide table occupancy at the moment of the call.</summary>
    public static TablesSnapshot Tables() => new(
        new InternSnapshot(
            TimelineTable._capacity,
            TimelineTable.LiveEntries,
            TimelineTable.DistinctContents,
            TimelineTable.GraveyardBlocks,
            TimelineTable.GraveyardBytes,
            TimelineTable.AllocBytes,
            TimelineTable.PeakLiveBytes),
        Pair(),
        new BakeSnapshot(BakeTable.SlotCount, BakeTable.Count));

    /// <summary>
    /// Snapshot of the <c>(TTrack, TClip)</c> bank, or null when the pair has never been touched.
    /// Folded views carry duration, ticks, lanes, AbiVersion, generation, and per-lane value-shape
    /// summaries (distinct bit patterns and longest equal run); pending and absent views carry
    /// their state only. No pointer values are returned.
    /// </summary>
    public static BankSnapshot? Bank<TTrack, TClip>()
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        var bank = Timeline<TTrack, TClip>._bank;
        if (bank is null)
            return null;
        var count = bank._count;
        var views = new ViewSnapshot[count];
        for (var index = 0; index < count; index++)
        {
            var slot = bank.FoldedView((ushort)index);
            if (slot is null)
            {
                views[index] = new ViewSnapshot(
                    index,
                    bank.IsAbsent((ushort)index) ? FoldState.Absent : FoldState.Pending,
                    null, null, null, null, null, null, null);
                continue;
            }
            views[index] = new ViewSnapshot(
                index,
                FoldState.Folded,
                slot->Duration,
                slot->Looping != 0,
                (int)slot->TableTicks,
                (int)slot->ResultCount,
                (int)slot->AbiVersion,
                unchecked((long)slot->Generation),
                Shapes(slot));
        }
        return new BankSnapshot(
            typeof(TTrack).FullName ?? typeof(TTrack).Name,
            typeof(TClip).FullName ?? typeof(TClip).Name,
            PairRuntime<TTrack, TClip>.Key,
            count,
            bank._holes,
            bank.BlockCount,
            bank.SharedHits,
            unchecked((long)bank._generation),
            bank.HeaderBytes,
            bank.TableBytes,
            bank.DirectoryBytes,
            bank.ArenaBytes,
            bank.RetainedBytes,
            views);
    }

    static PairSnapshot Pair()
    {
        var slots = PairTable._slotsBase == 0 ? 0 : *(int*)PairTable._slotsBase;
        return new PairSnapshot(
            slots,
            PairTable._pairs,
            slots > 0 ? PairTable._pairs * 1000 / slots : 0,
            PairTable.ConsumerCount,
            PairTable._consumerCapacity,
            PairTable.SlotRow);
    }

    static List<LaneShape> Shapes(SlotView* slot)
    {
        var ticks = (int)slot->TableTicks;
        var lanes = (int)slot->ResultCount;
        var shapes = new List<LaneShape>(lanes);
        var keys = slot->LaneKeys;
        for (var lane = 0; lane < lanes; lane++)
        {
            var values = slot->Forward + (long)lane * ticks;
            var seen = new HashSet<uint>(ticks);
            var longest = 0;
            var run = 0;
            uint prior = 0;
            for (var tick = 0; tick < ticks; tick++)
            {
                var bits = *(uint*)(values + tick);
                seen.Add(bits);
                run = tick > 0 && bits == prior ? run + 1 : 1;
                if (run > longest)
                    longest = run;
                prior = bits;
            }
            shapes.Add(new(keys[lane], seen.Count, longest));
        }
        return shapes;
    }
}
