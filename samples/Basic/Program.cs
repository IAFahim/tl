using Tl;

ushort id = Timeline<HealthTrack, HealthClip>.Build(static builder =>
{
    var track = builder.Track(new HealthTrack());
    builder.Clip(in track, new HealthClip(2f), start: 0, end: 4);
}).InMemory();

Timeline<HealthTrack, HealthClip>.Bind<HealthInput, HealthResult>(id);
Timeline<HealthTrack, HealthClip>.Bind<HealthInput, DirectHealthResult>(id);

// Input is read-only context (here: the seed the run starts from).
// Result is the live state the hooks mutate through the ref parameter.
var input = new HealthInput(Seed: 100f);
var result = new HealthResult { Value = input.Seed };

var playback = Timeline.Start(id);
playback = Timeline.Forward(id, in playback, in input, ref result, 0u, 1u, 2u, 3u);

Console.WriteLine(result.Value); // 106: seed 100; ticks 0, 1 and 2 are Stay (+2 each); tick 3 is Exit.

if (result.Value != 106f)
    throw new InvalidOperationException($"Expected 106, got {result.Value}");

// v0.3: the per-tick view is indexable and sliceable. The same walk,
// addressed directly (this[int]) and rewound through zero-copy sub-views
// (Slice), must agree exactly with enumeration.
var direct = new DirectHealthResult { Value = input.Seed };
var indexed = Timeline.Start(id);
indexed = Timeline.Forward(id, in indexed, in input, ref direct, 0u, 1u, 2u, 3u);
indexed = Timeline.Backward(id, in indexed, in input, ref direct, 2u, 1u, 0u);

Console.WriteLine(direct.Value); // 102: forward 106; the rewind undoes the two interior Stay
                                 // frames (ticks 1 and 2); boundary frames notify only

if (direct.Value != 102f)
    throw new InvalidOperationException($"Indexed/sliced walk diverged: expected 102, got {direct.Value}");

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

// v0.3 flavor of the same consumer: reads works by index in Forward and
// consumes a zero-copy sub-view (Slice) in Backward — same numbers, no
// enumeration.
public struct DirectHealthResult :
    IForward<HealthTrack, HealthClip, HealthInput, DirectHealthResult>,
    IBackward<HealthTrack, HealthClip, HealthInput, DirectHealthResult>
{
    public float Value;

    public void Forward(in Tracks<HealthTrack, HealthClip> tracks,
        in HealthInput input, in uint tick, ref DirectHealthResult result)
    {
        for (var i = 0; i < tracks.Count; i++)
            if (tracks[i].State == ClipState.Stay)
                result.Value += tracks[i].Clip.Amount;
    }

    public void Backward(in Tracks<HealthTrack, HealthClip> tracks,
        in HealthInput input, in uint tick, ref DirectHealthResult result)
    {
        var half = tracks.Count / 2;
        foreach (var work in tracks.Slice(0, half))
            if (work.State == ClipState.Stay)
                result.Value -= work.Clip.Amount;
        foreach (var work in tracks.Slice(half, tracks.Count - half))
            if (work.State == ClipState.Stay)
                result.Value -= work.Clip.Amount;
    }
}
