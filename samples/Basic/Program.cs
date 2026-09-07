using Tl;

ushort id = Timeline<HealthTrack, HealthClip>.Build(static builder =>
{
    var track = builder.Track(new HealthTrack());
    builder.Clip(in track, new HealthClip(2f), start: 0, end: 4);
});

Timeline<HealthTrack, HealthClip>.Bind<HealthInput, HealthResult>(id);

// Input is read-only context (here: the seed the run starts from).
// Result is the live state the hooks mutate through the ref parameter.
var input = new HealthInput(Seed: 100f);
var result = new HealthResult { Value = input.Seed };

var playback = Timeline.Start(id);
playback = Timeline.Forward(id, in playback, in input, ref result, 0u, 1u, 2u, 3u);

Console.WriteLine(result.Value); // 106: seed 100; ticks 0, 1 and 2 are Stay (+2 each); tick 3 is Exit.

if (result.Value != 106f)
    throw new InvalidOperationException($"Expected 106, got {result.Value}");

playback = Timeline.Stop(id, in playback);
Timeline.Destroy(id);

public readonly record struct HealthClip(float Amount);

public readonly struct HealthTrack : IBlend<HealthClip>
{
    public void Blend(in HealthClip first, in HealthClip second,
        float t, out HealthClip result)
    {
        result = new HealthClip(first.Amount + (second.Amount - first.Amount) * t);
    }
}

public readonly record struct HealthInput(float Seed);

public struct HealthResult :
    IForward<HealthTrack, HealthClip, HealthInput, HealthResult>,
    IBackward<HealthTrack, HealthClip, HealthInput, HealthResult>
{
    public float Value;

    public void Forward(in Tracks<HealthTrack, HealthClip> tracks,
        in HealthInput input, in uint tick, ref HealthResult result)
    {
        foreach (var work in tracks)
            if (work.State == ClipState.Stay)
                result.Value += work.Clip.Amount;
    }

    public void Backward(in Tracks<HealthTrack, HealthClip> tracks,
        in HealthInput input, in uint tick, ref HealthResult result)
    {
        foreach (var work in tracks)
            if (work.State == ClipState.Stay)
                result.Value -= work.Clip.Amount;
    }
}
