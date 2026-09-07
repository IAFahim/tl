using System;
using Tl;
using Tl.Aot;
using Tl.Generation;

Console.WriteLine("Running NativeAOT smoke tests...");

// 1. Runtime Authoring + Explicit Bind of the (input, result) pair
ushort id = Timeline<AotTrack, AotClip>.Build(static b =>
{
    var t0 = b.Track(new AotTrack(0));
    var t1 = b.Track(new AotTrack(1));
    b.Clip(in t0, new AotClip(10f), 0, 10);
    b.Clip(in t0, new AotClip(20f), 5, 15);
    b.Clip(in t1, new AotClip(5f), 2, 8);
    b.Clip(in t1, new AotClip(15f), 10, 18);
});

Timeline<AotTrack, AotClip>.Bind<AotInput, AotConsumer>(id);

// 2. Both directions: Forward and Backward
var input = new AotInput(Scale: 2f);
var consumer = new AotConsumer();
var pb = Timeline.Start(id, 0u);
pb = Timeline.Forward(id, in pb, in input, ref consumer, 1u, 3u, 6u);
if (consumer.Count != 3)
    throw new Exception($"Expected 3 steps, got {consumer.Count}");

pb = Timeline.Backward(id, in pb, in input, ref consumer, 4u, 2u);
if (consumer.Count != 5)
    throw new Exception($"Expected 5 steps, got {consumer.Count}");

// 3. Cursor-primed forward and backward
var cursor = default(Cursor);
pb = Timeline.Forward(id, in pb, ref cursor, in input, ref consumer, 5u, 7u);
pb = Timeline.Backward(id, in pb, ref cursor, in input, ref consumer, 3u);

// 4. Scratch overloads: Forward and Backward
Span<AotClip> scratch = stackalloc AotClip[2];
pb = Timeline<AotTrack, AotClip>.Forward(id, in pb, in input, ref consumer, scratch, 6u, 8u);
pb = Timeline<AotTrack, AotClip>.Backward(id, in pb, in input, ref consumer, scratch, 4u);

// 5. Stateless sampling in both directions
Timeline.Forward(id, in input, ref consumer, 9u, 11u);
Timeline.Backward(id, in input, ref consumer, 11u);

// 6. Generated Playback (same input/result pair against the generated closure)
var genConsumer = new AotConsumer();
var genPb = GeneratedTimeline<GeneratedAotTimeline, AotClip>.Start(0u);
genPb = GeneratedTimeline<GeneratedAotTimeline, AotClip>.Forward(in genPb, in input, ref genConsumer, 1u, 3u, 6u);
genPb = GeneratedTimeline<GeneratedAotTimeline, AotClip>.Backward(in genPb, in input, ref genConsumer, 4u, 2u);
if (genConsumer.Count != 5)
    throw new Exception($"Expected 5 generated steps, got {genConsumer.Count}");

// 7. Unbound consumer failure test under NativeAOT
var unbound = new UnboundConsumer();
bool unboundCaught = false;
try
{
    var unboundInput = default(AotInput);
    Timeline.Forward(id, in pb, in unboundInput, ref unbound, 1u);
}
catch (NotSupportedException ex)
{
    unboundCaught = true;
    Console.WriteLine($"Unbound consumer failure caught as expected: {ex.Message}");
}
if (!unboundCaught)
    throw new Exception("Expected unbound consumer to fail under NativeAOT.");

// 8. Wrong consumer closure failure test
bool wrongClosureCaught = false;
try
{
    Span<OtherClip> otherScratch = stackalloc OtherClip[2];
    var other = new OtherConsumer();
    var otherInput = default(OtherInput);
    Timeline<OtherTrack, OtherClip>.Forward(id, in pb, in otherInput, ref other, otherScratch, 1u);
}
catch (ArgumentException ex)
{
    wrongClosureCaught = true;
    Console.WriteLine($"Wrong closure failure caught as expected: {ex.Message}");
}
if (!wrongClosureCaught)
    throw new Exception("Expected wrong closure to fail.");

// 9. Checksums: the hub and generated walks above accumulate through the
// input's scale; print them so a run leaves verifiable numbers behind.
Console.WriteLine($"hub checksum: value={consumer.Value:G9} count={consumer.Count}");
Console.WriteLine($"generated checksum: value={genConsumer.Value:G9} count={genConsumer.Count}");

// 10. The input must survive every walk bit-identical.
if (input.Scale != 2f)
    throw new Exception("Input was mutated during playback.");

// 11. Lifecycle cleanup
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

public readonly record struct AotInput(float Scale);

public struct AotConsumer :
    IForward<AotTrack, AotClip, AotInput, AotConsumer>,
    IBackward<AotTrack, AotClip, AotInput, AotConsumer>,
    IForward<GeneratedAotTimeline, AotClip, AotInput, AotConsumer>,
    IBackward<GeneratedAotTimeline, AotClip, AotInput, AotConsumer>
{
    public float Value;
    public int Count;

    public void Forward(in Tracks<AotTrack, AotClip> tracks, in AotInput input, in uint tick, ref AotConsumer result)
    {
        foreach (var work in tracks)
            result.Value += work.Clip.Amount * input.Scale;
        result.Count++;
    }

    public void Backward(in Tracks<AotTrack, AotClip> tracks, in AotInput input, in uint tick, ref AotConsumer result)
    {
        foreach (var work in tracks)
            result.Value -= work.Clip.Amount * input.Scale;
        result.Count++;
    }

    public void Forward(in Tracks<GeneratedAotTimeline, AotClip> tracks, in AotInput input, in uint tick, ref AotConsumer result)
    {
        foreach (var work in tracks)
            result.Value += work.Clip.Amount * input.Scale;
        result.Count++;
    }

    public void Backward(in Tracks<GeneratedAotTimeline, AotClip> tracks, in AotInput input, in uint tick, ref AotConsumer result)
    {
        foreach (var work in tracks)
            result.Value -= work.Clip.Amount * input.Scale;
        result.Count++;
    }
}

public struct UnboundConsumer :
    IForward<AotTrack, AotClip, AotInput, UnboundConsumer>,
    IBackward<AotTrack, AotClip, AotInput, UnboundConsumer>
{
    public void Forward(in Tracks<AotTrack, AotClip> tracks, in AotInput input, in uint tick, ref UnboundConsumer result) { }
    public void Backward(in Tracks<AotTrack, AotClip> tracks, in AotInput input, in uint tick, ref UnboundConsumer result) { }
}

public readonly record struct OtherClip(double V);
public readonly struct OtherTrack : IBlend<OtherClip>
{
    public void Blend(in OtherClip first, in OtherClip second, float t, out OtherClip result)
        => result = new OtherClip(first.V * (1 - t) + second.V * t);
}
public readonly struct OtherInput;

public struct OtherConsumer :
    IForward<OtherTrack, OtherClip, OtherInput, OtherConsumer>,
    IBackward<OtherTrack, OtherClip, OtherInput, OtherConsumer>
{
    public void Forward(in Tracks<OtherTrack, OtherClip> tracks, in OtherInput input, in uint tick, ref OtherConsumer result) { }
    public void Backward(in Tracks<OtherTrack, OtherClip> tracks, in OtherInput input, in uint tick, ref OtherConsumer result) { }
}
