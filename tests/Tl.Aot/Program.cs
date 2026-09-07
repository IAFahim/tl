using System;
using Tl;
using Tl.Aot;
using Tl.Generation;

Console.WriteLine("Running NativeAOT smoke tests...");

// 1. Runtime Authoring + Explicit Bind
ushort id = Timeline<AotTrack, AotClip>.Build(static b =>
{
    var t0 = b.Track(new AotTrack(0));
    var t1 = b.Track(new AotTrack(1));
    b.Clip(in t0, new AotClip(10f), 0, 10);
    b.Clip(in t0, new AotClip(20f), 5, 15);
    b.Clip(in t1, new AotClip(5f), 2, 8);
    b.Clip(in t1, new AotClip(15f), 10, 18);
});

Timeline<AotTrack, AotClip>.Bind<AotConsumer>(id);

// 2. Both directions: Forward and Backward
var consumer = new AotConsumer();
var pb = Timeline.Start(id, 0u);
pb = Timeline.Forward(id, in pb, ref consumer, 1u, 3u, 6u);
if (consumer.Count != 3)
    throw new Exception($"Expected 3 steps, got {consumer.Count}");

pb = Timeline.Backward(id, in pb, ref consumer, 4u, 2u);
if (consumer.Count != 5)
    throw new Exception($"Expected 5 steps, got {consumer.Count}");

// 3. Cursor-primed forward and backward
var cursor = default(Cursor);
pb = Timeline.Forward(id, in pb, ref cursor, ref consumer, 5u, 7u);
pb = Timeline.Backward(id, in pb, ref cursor, ref consumer, 3u);

// 4. Scratch overloads: Forward and Backward
Span<AotClip> scratch = stackalloc AotClip[2];
pb = Timeline<AotTrack, AotClip>.Forward(id, in pb, ref consumer, scratch, 6u, 8u);
pb = Timeline<AotTrack, AotClip>.Backward(id, in pb, ref consumer, scratch, 4u);

// 5. Generated Playback
var genConsumer = new AotConsumer();
var genPb = GeneratedTimeline<GeneratedAotTimeline, AotClip>.Start(0u);
genPb = GeneratedTimeline<GeneratedAotTimeline, AotClip>.Forward(in genPb, ref genConsumer, 1u, 3u, 6u);
genPb = GeneratedTimeline<GeneratedAotTimeline, AotClip>.Backward(in genPb, ref genConsumer, 4u, 2u);
if (genConsumer.Count != 5)
    throw new Exception($"Expected 5 generated steps, got {genConsumer.Count}");

// 6. Unbound consumer failure test under NativeAOT
var unbound = new UnboundConsumer();
bool unboundCaught = false;
try
{
    Timeline.Forward(id, in pb, ref unbound, 1u);
}
catch (NotSupportedException ex)
{
    unboundCaught = true;
    Console.WriteLine($"Unbound consumer failure caught as expected: {ex.Message}");
}
if (!unboundCaught)
    throw new Exception("Expected unbound consumer to fail under NativeAOT.");

// 7. Wrong consumer closure failure test
bool wrongClosureCaught = false;
try
{
    Span<OtherClip> otherScratch = stackalloc OtherClip[2];
    var other = new OtherConsumer();
    Timeline<OtherTrack, OtherClip>.Forward(id, in pb, ref other, otherScratch, 1u);
}
catch (ArgumentException ex)
{
    wrongClosureCaught = true;
    Console.WriteLine($"Wrong closure failure caught as expected: {ex.Message}");
}
if (!wrongClosureCaught)
    throw new Exception("Expected wrong closure to fail.");

// 8. Lifecycle cleanup
pb = Timeline.Stop(id, in pb);
Timeline.Destroy(id);

Console.WriteLine("All NativeAOT smoke tests passed successfully!");
return 0;

public readonly record struct AotClip(float Amount);

public readonly struct AotTrack : IBlend<AotClip>
{
    public readonly int Id;
    public AotTrack(int id = 0) => Id = id;

    public void Blend(in AotClip first, in AotClip second, float t, out AotClip result)
        => result = new AotClip(first.Amount * (1f - t) + second.Amount * t);
}

public struct AotConsumer :
    IForward<AotTrack, AotClip, AotConsumer>,
    IBackward<AotTrack, AotClip, AotConsumer>,
    IForward<GeneratedAotTimeline, AotClip, AotConsumer>,
    IBackward<GeneratedAotTimeline, AotClip, AotConsumer>
{
    public float Value;
    public int Count;

    public void Forward(ref AotConsumer data, in Tracks<AotTrack, AotClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
            data.Value += work.Clip.Amount;
        data.Count++;
    }

    public void Backward(ref AotConsumer data, in Tracks<AotTrack, AotClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
            data.Value -= work.Clip.Amount;
        data.Count++;
    }

    public void Forward(ref AotConsumer data, in Tracks<GeneratedAotTimeline, AotClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
            data.Value += work.Clip.Amount;
        data.Count++;
    }

    public void Backward(ref AotConsumer data, in Tracks<GeneratedAotTimeline, AotClip> tracks, in uint tick)
    {
        foreach (var work in tracks)
            data.Value -= work.Clip.Amount;
        data.Count++;
    }
}

public struct UnboundConsumer :
    IForward<AotTrack, AotClip, UnboundConsumer>,
    IBackward<AotTrack, AotClip, UnboundConsumer>
{
    public void Forward(ref UnboundConsumer data, in Tracks<AotTrack, AotClip> tracks, in uint tick) { }
    public void Backward(ref UnboundConsumer data, in Tracks<AotTrack, AotClip> tracks, in uint tick) { }
}

public readonly record struct OtherClip(double V);
public readonly struct OtherTrack : IBlend<OtherClip>
{
    public void Blend(in OtherClip first, in OtherClip second, float t, out OtherClip result)
        => result = new OtherClip(first.V * (1 - t) + second.V * t);
}

public struct OtherConsumer :
    IForward<OtherTrack, OtherClip, OtherConsumer>,
    IBackward<OtherTrack, OtherClip, OtherConsumer>
{
    public void Forward(ref OtherConsumer data, in Tracks<OtherTrack, OtherClip> tracks, in uint tick) { }
    public void Backward(ref OtherConsumer data, in Tracks<OtherTrack, OtherClip> tracks, in uint tick) { }
}
