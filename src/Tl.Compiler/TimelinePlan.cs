using System.Collections.Immutable;

namespace Tl.Compiler;

public readonly record struct OperationId(string Value);

public readonly record struct TrackPlan(
    ushort Index,
    uint Payload,
    OperationId Operation);

public readonly record struct ClipPlan(
    ushort TrackIndex,
    uint Payload,
    uint Start,
    uint End);

public sealed record TimelinePlan
{
    public const ushort FormatVersion = 1;

    public TimelinePlan(
        string identity,
        ushort runtimeId,
        bool loops,
        IEnumerable<TrackPlan> tracks,
        IEnumerable<ClipPlan> clips)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tracks);
        ArgumentNullException.ThrowIfNull(clips);
        Identity = identity;
        RuntimeId = runtimeId;
        Loops = loops;
        Tracks = [.. tracks];
        Clips = [.. clips];
    }

    public string Identity { get; }
    public ushort RuntimeId { get; }
    public bool Loops { get; }
    public ImmutableArray<TrackPlan> Tracks { get; }
    public ImmutableArray<ClipPlan> Clips { get; }
    public uint Duration => Clips.IsEmpty ? 0u : Clips.Max(static clip => clip.End);
}
