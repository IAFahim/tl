namespace Tl;

public interface ITimeline<TTrack, TClip>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
{
    static abstract void Define(scoped TimelineBuilder<TTrack, TClip> timeline);
}
