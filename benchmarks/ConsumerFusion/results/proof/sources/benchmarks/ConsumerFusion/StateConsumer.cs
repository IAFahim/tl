using System.Runtime.CompilerServices;
using Pulse;
using Tl.Compiled;

namespace Tl.ConsumerFusion;

public struct StateConsumer : IConsumer<StateConsumer>
{
    public float Sum;
    public long TrackOffsets;
    public int Enters;
    public int Stays;
    public int Exits;

    public readonly ConsumerReceipt Capture(in Playback playback)
        => new(playback, BitConverter.SingleToInt32Bits(Sum), TrackOffsets, Enters, Stays, Exits, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Apply(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref StateConsumer result, bool backward)
    {
        result.TrackOffsets += backward ? -offset : offset;
        switch (state)
        {
            case ClipState.Enter:
                result.Enters++;
                break;
            case ClipState.Stay:
                result.Stays++;
                var contribution = amount * input.Scale + input.Bias;
                if (backward)
                    result.Sum -= contribution;
                else
                    result.Sum += contribution;
                break;
            case ClipState.Exit:
                result.Exits++;
                break;
        }
    }

    public void Forward(in Tracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref StateConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, false);
    }

    public void Forward(in CompiledTracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref StateConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forward(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref StateConsumer result)
        => Apply(index, offset, amount, state, tick, in input, ref result, false);

    public void Backward(in Tracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref StateConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, true);
    }

    public void Backward(in CompiledTracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref StateConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Backward(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref StateConsumer result)
        => Apply(index, offset, amount, state, tick, in input, ref result, true);
}
