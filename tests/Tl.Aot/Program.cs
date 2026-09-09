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
}).InMemory();

Timeline<AotTrack, AotClip>.Bind<AotInput, AotConsumer>(id);
Timeline<AotTrack, AotClip>.Bind<AotInput, OverflowConsumer<BindingTag0>>(id);
Timeline<AotTrack, AotClip>.Bind<AotInput, OverflowConsumer<BindingTag1>>(id);
Timeline<AotTrack, AotClip>.Bind<AotInput, OverflowConsumer<BindingTag2>>(id);
Timeline<AotTrack, AotClip>.Bind<AotInput, OverflowConsumer<BindingTag3>>(id);
Timeline<AotTrack, AotClip>.Bind<AotInput, OverflowConsumer<BindingTag4>>(id);

var overflowConsumer = new OverflowConsumer<BindingTag4>();
var overflowPlayback = Timeline.Start(id);
overflowPlayback = Timeline.Forward(id, in overflowPlayback, new AotInput(2f), ref overflowConsumer, 3u);
if (overflowConsumer.Value != 30f)
    throw new Exception($"Overflow binding failed under NativeAOT: {overflowConsumer.Value:G9}");
Console.WriteLine($"overflow binding checksum: value={overflowConsumer.Value:G9}");

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

// 9. Checksums: the hub and generated walks above accumulate through the
// input's scale; print them so a run leaves verifiable numbers behind.
Console.WriteLine($"hub checksum: value={consumer.Value:G9} count={consumer.Count}");
Console.WriteLine($"generated checksum: value={genConsumer.Value:G9} count={genConsumer.Count}");

var parityConsumer = new AotConsumer();
var parityPlayback = Timeline.Start(id);
parityPlayback = Timeline.Forward(id, in parityPlayback, in input, ref parityConsumer, 1u, 3u, 6u);
var repeatedConsumer = new AotConsumer();
var repeatedPlayback = Timeline.Start(id);
repeatedPlayback = Timeline.Forward(id, in repeatedPlayback, in input, ref repeatedConsumer, 1u, 3u, 6u);
if (parityConsumer.Value != repeatedConsumer.Value || parityConsumer.Count != repeatedConsumer.Count)
    throw new Exception($"Repeated walk diverged under AOT: {parityConsumer.Value} vs {repeatedConsumer.Value}");
Console.WriteLine($"repeat checksum: value={parityConsumer.Value:G9} count={parityConsumer.Count}");

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
    ITrack<AotTrack, AotClip, AotInput, AotConsumer>,
    ITrack<GeneratedAotTimeline, AotClip, AotInput, AotConsumer>
{
    public float Value;
    public int Count;

    public static void Forward(int ordinal, int count, ushort index,
        in AotTrack track, in AotClip clip, ClipState state,
        in uint tick, in AotInput input, ref AotConsumer result)
    {
        result.Value += clip.Amount * input.Scale;
        if (ordinal + 1 == count)
            result.Count++;
    }

    public static void Backward(int ordinal, int count, ushort index,
        in AotTrack track, in AotClip clip, ClipState state,
        in uint tick, in AotInput input, ref AotConsumer result)
    {
        result.Value -= clip.Amount * input.Scale;
        if (ordinal + 1 == count)
            result.Count++;
    }

    public static void Forward(int ordinal, int count, ushort index,
        in GeneratedAotTimeline track, in AotClip clip, ClipState state,
        in uint tick, in AotInput input, ref AotConsumer result)
    {
        result.Value += clip.Amount * input.Scale;
        if (ordinal + 1 == count)
            result.Count++;
    }

    public static void Backward(int ordinal, int count, ushort index,
        in GeneratedAotTimeline track, in AotClip clip, ClipState state,
        in uint tick, in AotInput input, ref AotConsumer result)
    {
        result.Value -= clip.Amount * input.Scale;
        if (ordinal + 1 == count)
            result.Count++;
    }
}

public struct UnboundConsumer :
    ITrack<AotTrack, AotClip, AotInput, UnboundConsumer>
{
    public static void Forward(int ordinal, int count, ushort index, in AotTrack track, in AotClip clip, ClipState state, in uint tick, in AotInput input, ref UnboundConsumer result) { }
    public static void Backward(int ordinal, int count, ushort index, in AotTrack track, in AotClip clip, ClipState state, in uint tick, in AotInput input, ref UnboundConsumer result) { }
}

public struct OverflowConsumer<TTag> : ITrack<AotTrack, AotClip, AotInput, OverflowConsumer<TTag>>
    where TTag : unmanaged
{
    public float Value;

    public static void Forward(int ordinal, int count, ushort index,
        in AotTrack track, in AotClip clip, ClipState state,
        in uint tick, in AotInput input, ref OverflowConsumer<TTag> result)
        => result.Value += clip.Amount * input.Scale;

    public static void Backward(int ordinal, int count, ushort index,
        in AotTrack track, in AotClip clip, ClipState state,
        in uint tick, in AotInput input, ref OverflowConsumer<TTag> result)
        => result.Value -= clip.Amount * input.Scale;
}

public enum BindingTag0 : byte { Value }
public enum BindingTag1 : byte { Value }
public enum BindingTag2 : byte { Value }
public enum BindingTag3 : byte { Value }
public enum BindingTag4 : byte { Value }

public readonly record struct OtherClip(double V);
public readonly struct OtherTrack : IBlend<OtherClip>
{
    public void Blend(in OtherClip first, in OtherClip second, float t, out OtherClip result)
        => result = new OtherClip(first.V * (1 - t) + second.V * t);
}
public readonly struct OtherInput;

public struct OtherConsumer :
    ITrack<OtherTrack, OtherClip, OtherInput, OtherConsumer>
{
    public static void Forward(int ordinal, int count, ushort index, in OtherTrack track, in OtherClip clip, ClipState state, in uint tick, in OtherInput input, ref OtherConsumer result) { }
    public static void Backward(int ordinal, int count, ushort index, in OtherTrack track, in OtherClip clip, ClipState state, in uint tick, in OtherInput input, ref OtherConsumer result) { }
}
