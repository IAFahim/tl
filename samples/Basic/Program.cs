using Tl;

ushort id = Timeline<HealthTrack, HealthClip>.Build(static builder =>
{
    var track = builder.Track(new HealthTrack());
    builder.Clip(in track, new HealthClip(2f), start: 0, end: 4);
}).InMemory();

Timeline<HealthTrack, HealthClip>.Bind<HealthInput, HealthResult>(id);

var input = new HealthInput(Seed: 100f);
var result = new HealthResult { Value = input.Seed };

var playback = Timeline.Start(id);
playback = Timeline.Forward(id, in playback, in input, ref result, 0u, 1u, 2u, 3u);

Console.WriteLine(result.Value);

if (result.Value != 106f)
    throw new InvalidOperationException($"Expected 106, got {result.Value}");

var rewind = new HealthResult { Value = input.Seed };
var rewindPlayback = Timeline.Start(id);
rewindPlayback = Timeline.Forward(id, in rewindPlayback, in input, ref rewind, 0u, 1u, 2u, 3u);
rewindPlayback = Timeline.Backward(id, in rewindPlayback, in input, ref rewind, 2u, 1u, 0u);

Console.WriteLine(rewind.Value);

if (rewind.Value != 102f)
    throw new InvalidOperationException($"Rewind diverged: expected 102, got {rewind.Value}");

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
    ITrack<HealthTrack, HealthClip, HealthInput, HealthResult>
{
    public float Value;

    public static void Forward(int ordinal, int count, ushort index,
        in HealthTrack track, in HealthClip clip, ClipState state,
        in uint tick, in HealthInput input, ref HealthResult result)
        => result.Value += state == ClipState.Stay ? clip.Amount : 0f;

    public static void Backward(int ordinal, int count, ushort index,
        in HealthTrack track, in HealthClip clip, ClipState state,
        in uint tick, in HealthInput input, ref HealthResult result)
        => result.Value -= state == ClipState.Stay ? clip.Amount : 0f;
}
