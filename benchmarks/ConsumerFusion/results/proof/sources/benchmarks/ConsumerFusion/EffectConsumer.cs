using System.Runtime.CompilerServices;
using Pulse;
using Tl.Compiled;

namespace Tl.ConsumerFusion;

public struct EffectConsumer : IConsumer<EffectConsumer>
{
    public StateConsumer State;
    public int Callbacks;
    public ulong Audit;

    public readonly ConsumerReceipt Capture(in Playback playback)
        => new(playback, BitConverter.SingleToInt32Bits(State.Sum), State.TrackOffsets, State.Enters, State.Stays, State.Exits, Callbacks, Audit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Apply(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref EffectConsumer result, bool backward)
    {
        StateConsumer.Apply(index, offset, amount, state, tick, in input, ref result.State, backward);
        Notify(index, amount, state, tick, in input, ref result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Notify(ushort index, float amount, ClipState state, uint tick, in ConsumerInput input, ref EffectConsumer result)
    {
        result.Callbacks++;
        var value = ((ulong)tick << 32) ^ ((ulong)index << 16) ^ (uint)BitConverter.SingleToInt32Bits(amount) ^ (uint)state;
        result.Audit = unchecked((result.Audit ^ value) * 1099511628211UL);
        if (tick == input.FailTick && index == input.FailTrack)
            throw new ConsumerFailureException();
    }

    public void Forward(in Tracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref EffectConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, false);
    }

    public void Forward(in CompiledTracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref EffectConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forward(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref EffectConsumer result)
        => Apply(index, offset, amount, state, tick, in input, ref result, false);

    public void Backward(in Tracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref EffectConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, true);
    }

    public void Backward(in CompiledTracks<PulseTrack, PulseClip> tracks, in ConsumerInput input, in uint tick, ref EffectConsumer result)
    {
        foreach (var work in tracks)
            Apply(work.Index, work.Track.Offset, work.Clip.Amount, work.State, tick, in input, ref result, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Backward(ushort index, int offset, float amount, ClipState state, uint tick, in ConsumerInput input, ref EffectConsumer result)
        => Apply(index, offset, amount, state, tick, in input, ref result, true);
}

public sealed class ConsumerFailureException : Exception;
