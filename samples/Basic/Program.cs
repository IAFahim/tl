using Tl;

ushort id = Timeline<HealthTrack, HealthClip>.Build(static builder =>
{
    var track = builder.Track(new HealthTrack());
    builder.Clip(in track, new HealthClip(2f), start: 0, end: 4);
});

Timeline<HealthTrack, HealthClip>.Bind<Health>(id);

var health = new Health();
var playback = Timeline.Start(id);
playback = Timeline.Forward(id, in playback, ref health, 0u, 1u, 2u, 3u);

Console.WriteLine(health.Value); // 6: ticks 0, 1 and 2 are Stay; tick 3 is Exit.

if (health.Value != 6f)
    throw new InvalidOperationException($"Expected 6, got {health.Value}");

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

public struct Health :
    IForward<HealthTrack, HealthClip, Health>,
    IBackward<HealthTrack, HealthClip, Health>
{
    public float Value;

    public void Forward(ref Health data,
        in Tracks<HealthTrack, HealthClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
            if (work.State == ClipState.Stay)
                data.Value += work.Clip.Amount;
    }

    public void Backward(ref Health data,
        in Tracks<HealthTrack, HealthClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
            if (work.State == ClipState.Stay)
                data.Value -= work.Clip.Amount;
    }
}
