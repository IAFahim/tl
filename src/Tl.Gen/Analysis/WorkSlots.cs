using Tl.Gen.Model;

namespace Tl.Gen.Analysis;

public readonly record struct EmittedWorkSlot(
    ushort Index,
    ushort First,
    ushort Second,
    uint EnterF,
    uint EnterB,
    uint FactorStart,
    uint FactorLength)
{
    public const ushort Single = ushort.MaxValue;
}

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
                        0u);
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
                    first.FactorLength);
            }

            slots[r] = region;
        }

        return slots;
    }
}
