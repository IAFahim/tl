using Tl;
using Tl.Compiled;

namespace Pulse;

public readonly record struct PulseInput(float Seed);

// The real consumer of the timeline: a result struct whose hooks run on both
// paths. It implements the in-memory interpreter hooks (IForward/IBackward
// over Tl.Tracks) AND the compiled hooks (ICompiledForward/ICompiledBackward
// over the generated view); the four bodies are one-liners over the same
// per-work core, so both paths consume through byte-identical logic — the
// parity battery then pins the ENGINES to each other.
public struct PulseResult :
    IForward<PulseTrack, PulseClip, PulseInput, PulseResult>,
    IBackward<PulseTrack, PulseClip, PulseInput, PulseResult>,
    ICompiledForward<PulseTrack, PulseClip, PulseInput, PulseResult>,
    ICompiledBackward<PulseTrack, PulseClip, PulseInput, PulseResult>
{
    public float Sum;
    public long Ticks;
    public int Count;
    public int Enters;
    public int Stays;
    public int Exits;

    public void Forward(in Tracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref PulseResult result)
    {
        foreach (var work in tracks)
            Work(work.Index, in work.Clip, work.State, tick, +1, ref result);
    }

    public void Backward(in Tracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref PulseResult result)
    {
        foreach (var work in tracks)
            Work(work.Index, in work.Clip, work.State, tick, -1, ref result);
    }

    public void Forward(in CompiledTracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref PulseResult result)
    {
        foreach (var work in tracks)
            Work(work.Index, in work.Clip, work.State, tick, +1, ref result);
    }

    public void Backward(in CompiledTracks<PulseTrack, PulseClip> tracks, in PulseInput input, in uint tick, ref PulseResult result)
    {
        foreach (var work in tracks)
            Work(work.Index, in work.Clip, work.State, tick, -1, ref result);
    }

    // Backward mirrors Forward's arithmetic exactly (sign flips), so the
    // backward-mirror battery checks the inverse walk too.
    private static void Work(ushort index, in PulseClip clip, ClipState state, uint tick, int sign, ref PulseResult result)
    {
        result.Ticks += sign * tick;
        result.Count++;
        switch (state)
        {
            case ClipState.Enter:
                result.Enters++;
                result.Sum += clip.Amount * 0.25f * sign;
                break;
            case ClipState.Stay:
                result.Stays++;
                result.Sum += clip.Amount * sign;
                break;
            case ClipState.Exit:
                result.Exits++;
                result.Sum -= clip.Amount * 0.125f * sign;
                break;
        }
    }
}
