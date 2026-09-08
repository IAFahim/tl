using Tl.Gen.Model;

namespace Tl.Gen.Analysis;

/// <summary>
/// One materialized work of one region, the emission-time mirror of the
/// runtime WorkSlot (src/Tl.Core/Tracks.cs). Field semantics are identical:
/// Index is the authored track index (its payload-table row), First/Second
/// are the authored clip indices of the single clip or the blend pair
/// (Second carries the Single sentinel for standalone clips), EnterF/EnterB
/// are the work's OUTER window edges (forward entry, backward entry), and
/// FactorStart/FactorLength mark the blend window (zero length = standalone).
/// </summary>
public readonly record struct EmittedWorkSlot(
    ushort Index,
    ushort First,
    ushort Second,
    uint EnterF,
    uint EnterB,
    uint FactorStart,
    uint FactorLength,
    ushort BlendOrdinal)
{
    public const ushort Single = ushort.MaxValue;
}

/// <summary>
/// Build-time work-slot materialization over an analyzed plan. This is the
/// emission-time re-derivation of PlaybackCore.MaterializeWorkSlots
/// (src/Tl.Core/Internal/Playback.cs) specialized for the compiled kernel's
/// invariant — no storage dedup, so the payload map is empty and authored
/// clip indices are payload indices — with blend-scratch ordinals dense per
/// region (region-local counters), matching the runtime convention the
/// scratch sizing (MaxActiveBlends) assumes. Behavior must stay identical to
/// the runtime materializer; the samples/Compiled parity battery is the gate.
/// </summary>
public static class WorkSlotMaterializer
{
    public static EmittedWorkSlot[][] ForRegions(TimelinePlan plan)
    {
        var slots = new EmittedWorkSlot[plan.RegionRows.Length][];
        var clipRows = plan.ClipRows;
        var edges = plan.ClipEdges;

        for (var r = 0; r < plan.RegionRows.Length; r++)
        {
            var row = plan.RegionRows[r];
            var region = new EmittedWorkSlot[row.TrackCount];
            var blendOrdinal = 0;

            for (var i = 0; i < row.TrackCount; i++)
            {
                var trackRow = plan.TrackRows[row.TrackStart + i];
                var first = clipRows[trackRow.ClipStart];

                if (trackRow.ClipCount != 2)
                {
                    var edge = edges[first.ClipIndex];
                    region[i] = new EmittedWorkSlot(
                        trackRow.TrackIndex,
                        first.ClipIndex,
                        EmittedWorkSlot.Single,
                        edge.Start,
                        edge.End,
                        0u,
                        0u,
                        0);
                    continue;
                }

                var second = clipRows[trackRow.ClipStart + 1];
                var a = edges[first.ClipIndex];
                var b = edges[second.ClipIndex];
                region[i] = new EmittedWorkSlot(
                    trackRow.TrackIndex,
                    first.ClipIndex,
                    second.ClipIndex,
                    a.Start < b.Start ? a.Start : b.Start,
                    a.End > b.End ? a.End : b.End,
                    first.FactorStart,
                    first.FactorLength,
                    checked((ushort)blendOrdinal++));
            }

            slots[r] = region;
        }

        return slots;
    }
}
