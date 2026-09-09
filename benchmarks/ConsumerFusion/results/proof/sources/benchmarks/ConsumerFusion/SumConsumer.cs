using System.Runtime.CompilerServices;
using Pulse;
using Tl.Compiled;

namespace Tl.ConsumerFusion;

public struct SumConsumer : IConsumer<SumConsumer>
{
    public float Sum;

    public readonly ConsumerReceipt Capture(in Playback playback)
        => new(playback, BitConverter.SingleToInt32Bits(Sum), 0, 0, 0, 0, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Apply(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref SumConsumer result, bool backward)
    {
        if (backward)
            result.Sum -= amount;
        else
            result.Sum += amount;
    }

    public void Forward(in Tracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, false);
    }

    public void Forward(in CompiledTracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forward(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref SumConsumer result)
        => Apply(index, offset, amount, state, tick, in input, ref result, false);

    public void Backward(in Tracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, true);
    }

    public void Backward(in CompiledTracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Backward(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref SumConsumer result)
        => Apply(index, offset, amount, state, tick, in input, ref result, true);
}
