using Tl;
namespace Pulse;

public readonly record struct PulseClip(float Amount);

public readonly struct PulseTrack(int offset) : IBlend<PulseClip>
{
    public readonly int Offset = offset;

    public void Blend(in PulseClip first, in PulseClip second, float t, out PulseClip result)
        => result = new PulseClip(first.Amount + (second.Amount - first.Amount) * t);
}

public readonly partial struct PulseTimeline : ITimeline<PulseTrack, PulseClip>
{
    public static void Define(scoped TimelineBuilder<PulseTrack, PulseClip> b)
    {
        var core = b.Track(new PulseTrack(1));
        var burst = b.Track(new PulseTrack(2));
        var sustain = b.Track(new PulseTrack(3));
        var recovery = b.Track(new PulseTrack(4));

        b.Clip(in core, new PulseClip(1f), start: 0, end: 7);
        b.Clip(in core, new PulseClip(13f), start: 29, end: 47);
        b.Clip(in core, new PulseClip(21f), start: 29, end: 47);
        b.Clip(in core, new PulseClip(13f), start: 47, end: 76);
        b.Clip(in core, new PulseClip(1f), start: 321, end: 515);

        b.Clip(in burst, new PulseClip(2f), start: 3, end: 7);
        b.Clip(in burst, new PulseClip(3f), start: 3, end: 11);
        b.Clip(in burst, new PulseClip(8f), start: 18, end: 29);
        b.Clip(in burst, new PulseClip(8f), start: 76, end: 123);
        b.Clip(in burst, new PulseClip(3f), start: 515, end: 600);

        b.Clip(in sustain, new PulseClip(2f), start: 47, end: 76);
        b.Clip(in sustain, new PulseClip(2f), start: 123, end: 321);
        b.Clip(in sustain, new PulseClip(8f), start: 321, end: 515);

        b.Clip(in recovery, new PulseClip(5f), start: 3, end: 11);
        b.Clip(in recovery, new PulseClip(5f), start: 18, end: 29);
        b.Clip(in recovery, new PulseClip(34f), start: 76, end: 123);
        b.Clip(in recovery, new PulseClip(55f), start: 76, end: 123);
        b.Clip(in recovery, new PulseClip(5f), start: 200, end: 321);
        b.Clip(in recovery, new PulseClip(5f), start: 321, end: 515);

        b.Looping();
    }
}
