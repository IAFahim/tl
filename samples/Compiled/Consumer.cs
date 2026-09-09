using Tl;
namespace Pulse;

public readonly record struct PulseInput(float Seed);

public struct PulseResult :
    ITrack<PulseTrack, PulseClip, PulseInput, PulseResult>
{
    public float Sum;
    public long Ticks;
    public int Count;
    public int Enters;
    public int Stays;
    public int Exits;

    public static void Forward(int ordinal, int count, ushort index,
        in PulseTrack track, in PulseClip clip, ClipState state,
        in uint tick, in PulseInput input, ref PulseResult result)
        => Work(index, in clip, state, tick, +1, ref result);

    public static void Backward(int ordinal, int count, ushort index,
        in PulseTrack track, in PulseClip clip, ClipState state,
        in uint tick, in PulseInput input, ref PulseResult result)
        => Work(index, in clip, state, tick, -1, ref result);

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
