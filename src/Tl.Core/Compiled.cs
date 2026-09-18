using System.ComponentModel;

namespace Tl;

public interface ITimeline<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged;

public readonly ref struct Frame<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    private readonly ref readonly TTrack _track;
    private readonly ref readonly TClip _clip;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Frame(
        in TTrack track,
        in TClip clip,
        ushort timelineTick,
        ushort clipLength,
        ushort withinClip,
        ushort trackIndex,
        FrameFlags flags)
    {
        _track = ref track;
        _clip = ref clip;
        TimelineTick = timelineTick;
        ClipLength = clipLength;
        WithinClip = withinClip;
        TrackIndex = trackIndex;
        Flags = flags;
    }

    public ref readonly TTrack Track => ref _track;
    public ref readonly TClip Clip => ref _clip;
    public ushort TimelineTick { get; }
    public ushort ClipLength { get; }
    public ushort WithinClip { get; }
    public ushort TrackIndex { get; }
    public FrameFlags Flags { get; }
    public int Direction => Has(FrameFlags.Reverse) ? -1 : 1;
    public bool IsBackward => Has(FrameFlags.Reverse);
    public bool Has(FrameFlags flags) => (Flags & flags) == flags;
}
