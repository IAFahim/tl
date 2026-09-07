namespace Tl;

public interface IBlend<TClip>
    where TClip : struct
{
    void Blend(in TClip first, in TClip second, float t, out TClip result);
}

public interface IForward<TTrack, TClip, TInput, TResult>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TInput : struct
    where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
{
    void Forward(in Tracks<TTrack, TClip> tracks, in TInput input, in uint tick, ref TResult result);
}

public interface IBackward<TTrack, TClip, TInput, TResult>
    where TTrack : struct, IBlend<TClip>
    where TClip : struct
    where TInput : struct
    where TResult : struct, IForward<TTrack, TClip, TInput, TResult>, IBackward<TTrack, TClip, TInput, TResult>
{
    void Backward(in Tracks<TTrack, TClip> tracks, in TInput input, in uint tick, ref TResult result);
}
