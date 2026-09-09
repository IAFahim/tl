using Tl;
using Tl.Compiled;

namespace Pulse;

public readonly record struct PulseClip(float Amount);

public readonly struct PulseTrack(int offset) : IBlend<PulseClip>
{
    public readonly int Offset = offset;

    public void Blend(in PulseClip first, in PulseClip second, float t, out PulseClip result)
        => result = new PulseClip(first.Amount + (second.Amount - first.Amount) * t);
}

public static class Authoring
{
    // The ONE authored timeline instance, played by both paths:
    //   - the in-memory interpreter leg registers it through
    //     Timeline<PulseTrack, PulseClip>.Build(Author) at runtime;
    //   - the Tl.Gen compile reader interprets this same method at build
    //     time and emits the CompiledPulse kernel.
    // Vitals-shaped (benchmarks/Hooks.cs): 4 tracks, 19 clips, 3 blend
    // pairs, a gap at [11,18), duration 600, looping — so the parity battery
    // exercises blends, gaps, wraps, LastLoopFrame and cycle accounting.
    public static void Author(scoped TimelineBuilder<PulseTrack, PulseClip> b)
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

public static class Decls
{
    // The declaration the compile reader specializes into CompiledPulse
    // (the variable name names the kernel). Compiled consumers call the
    // kernel class directly, so this holder is deliberately never touched —
    // the declaration site never executes and nothing registers at runtime.
    public static readonly CompiledTimelineInfo Pulse = Timeline<PulseTrack, PulseClip>.Build(Authoring.Author).Compile();
}
