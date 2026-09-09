namespace Tl.Gen.Model;

public enum SlotMode : byte
{
    Input,
    Reference,
    Output,
}

public sealed record TimelineSlot(string Name, string TypeName, SlotMode Mode);

public sealed record HeterogeneousClip(
    int TrackIndex,
    string TypeName,
    string Expression,
    uint Start,
    uint End);

public sealed record HeterogeneousTrack(
    int Index,
    string TypeName,
    string ClipTypeName,
    string Expression,
    IReadOnlyList<TimelineSlot> ForwardSlots,
    IReadOnlyList<TimelineSlot> BackwardSlots);

public sealed record TimelineHook(
    string TypeName,
    IReadOnlyList<TimelineSlot> ForwardSlots,
    IReadOnlyList<TimelineSlot> BackwardSlots);

public sealed record HeterogeneousTimeline(
    string Name,
    string Namespace,
    string File,
    int Line,
    bool Loops,
    IReadOnlyList<string> Usings,
    IReadOnlyList<HeterogeneousTrack> Tracks,
    IReadOnlyList<HeterogeneousClip> Clips,
    IReadOnlyList<TimelineHook> BeforeHooks,
    IReadOnlyList<TimelineHook> AfterHooks,
    IReadOnlyList<TimelineSlot> Inputs,
    IReadOnlyList<TimelineSlot> Outputs)
{
    public uint Duration => Clips.Count == 0 ? 0u : Clips.Max(static clip => clip.End);
}
