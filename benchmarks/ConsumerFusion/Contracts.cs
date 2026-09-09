using Pulse;
using Tl.Compiled;

namespace Tl.ConsumerFusion;

public readonly record struct ConsumerInput(float Scale, float Bias, uint FailTick = uint.MaxValue, ushort FailTrack = ushort.MaxValue);

public interface IWorkOperation<TInput, TResult>
    where TInput : unmanaged
    where TResult : unmanaged
{
    static abstract void Forward(ushort index, int offset, float amount, ClipState state, uint tick, in TInput input, ref TResult result);
    static abstract void Backward(ushort index, int offset, float amount, ClipState state, uint tick, in TInput input, ref TResult result);
}

public interface IConsumer<TSelf> :
    IWorkOperation<ConsumerInput, TSelf>,
    IForward<PulseTrack, PulseClip, ConsumerInput, TSelf>,
    IBackward<PulseTrack, PulseClip, ConsumerInput, TSelf>,
    ICompiledForward<PulseTrack, PulseClip, ConsumerInput, TSelf>,
    ICompiledBackward<PulseTrack, PulseClip, ConsumerInput, TSelf>
    where TSelf : unmanaged, IConsumer<TSelf>
{
    ConsumerReceipt Capture(in Playback playback);
}

public readonly record struct ConsumerReceipt(
    Playback Playback, int SumBits, long TrackOffsets,
    int Enters, int Stays, int Exits, int Callbacks, ulong Audit);
