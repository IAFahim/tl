using Tl;
using Tl.Generation;

namespace Tl.Gen.Model;

public enum EmissionStrategy
{
    GeneralTables,
    SpecializedBake,
}

public sealed class TimelinePlan
{
    public required TimelineDefinition Definition { get; init; }
    public required uint[] RegionStarts { get; init; }
    public required RegionRow[] RegionRows { get; init; }
    public required byte[] RegionFlags { get; init; }
    public required TrackRow[] TrackRows { get; init; }
    public required ClipRow[] ClipRows { get; init; }
    public required ClipEdge[] ClipEdges { get; init; }
    public required int MaxActiveTracks { get; init; }
    public required int MaxActiveBlends { get; init; }
    public required uint Duration { get; init; }
    public EmissionStrategy Strategy { get; set; } = EmissionStrategy.GeneralTables;
}
