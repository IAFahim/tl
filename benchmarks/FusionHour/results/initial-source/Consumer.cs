using Pulse;
using Tl;
using Tl.Compiled;

namespace Tl.FusionExperiment;

public struct SumConsumer :
    IForward<PulseTrack, PulseClip, PulseInput, SumConsumer>,
    IBackward<PulseTrack, PulseClip, PulseInput, SumConsumer>,
    ICompiledForward<PulseTrack, PulseClip, PulseInput, SumConsumer>,
    ICompiledBackward<PulseTrack, PulseClip, PulseInput, SumConsumer>
{
    public float Sum;

    public void Forward(in Tracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            result.Sum += work.Clip.Amount;
    }

    public void Backward(in Tracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            result.Sum -= work.Clip.Amount;
    }

    public void Forward(in CompiledTracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            result.Sum += work.Clip.Amount;
    }

    public void Backward(in CompiledTracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref SumConsumer result)
    {
        foreach (var work in tracks)
            result.Sum -= work.Clip.Amount;
    }
}

public readonly record struct SumReceipt(Playback Playback, int SumBits)
{
    public static SumReceipt Capture(in Playback playback, float sum)
        => new(playback, BitConverter.SingleToInt32Bits(sum));
}
