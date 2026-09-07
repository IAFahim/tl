namespace Tl;

public interface IBlend<TClip>
    where TClip : struct
{
    void Blend(in TClip first, in TClip second, float t, out TClip result);
}

public interface IForward<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TData : struct
{
    void Forward(ref TData data, in Tracks<TTrack, TClip> tracks, in uint tick);
}

public interface IBackward<TTrack, TClip, TData>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TData : struct
{
    void Backward(ref TData data, in Tracks<TTrack, TClip> tracks, in uint tick);
}
