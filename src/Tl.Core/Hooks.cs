namespace Tl;

public interface IBlend<TClip>
    where TClip : unmanaged
{
    void Blend(in TClip first, in TClip second, float t, out TClip result);
}

public interface IForward<TTrack, TClip, TInput, TResult>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
    where TInput : struct
    where TResult : struct, IForward<TTrack, TClip, TInput, TResult>
{
    static abstract void Forward(
        int ordinal,
        int count,
        ushort index,
        in TTrack track,
        in TClip clip,
        ClipState state,
        in uint tick,
        in TInput input,
        ref TResult result);
}

public interface IBackward<TTrack, TClip, TInput, TResult>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
    where TInput : struct
    where TResult : struct, IBackward<TTrack, TClip, TInput, TResult>
{
    static abstract void Backward(
        int ordinal,
        int count,
        ushort index,
        in TTrack track,
        in TClip clip,
        ClipState state,
        in uint tick,
        in TInput input,
        ref TResult result);
}

public interface ITrack<TTrack, TClip, TInput, TResult> :
    IForward<TTrack, TClip, TInput, TResult>,
    IBackward<TTrack, TClip, TInput, TResult>
    where TTrack : unmanaged, IBlend<TClip>
    where TClip : unmanaged
    where TInput : struct
    where TResult : struct, ITrack<TTrack, TClip, TInput, TResult>;
