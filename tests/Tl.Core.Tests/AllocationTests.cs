using Xunit;

namespace Tl.Core.Tests;

public class AllocationTests
{
    public readonly record struct FastClip(float V);
    public readonly struct FastTrack : IBlend<FastClip>
    {
        public void Blend(in FastClip first, in FastClip second, float t, out FastClip result)
            => result = new FastClip(first.V + (second.V - first.V) * t);
    }

    public struct ZeroAllocResult :
        ITrack<FastTrack, FastClip, NoInput, ZeroAllocResult>
    {
        public float Accumulator;

        public static void Forward(int ordinal, int count, ushort index,
            in FastTrack track, in FastClip clip, ClipState state,
            in uint tick, in NoInput input, ref ZeroAllocResult result)
            => result.Accumulator += clip.V;

        public static void Backward(int ordinal, int count, ushort index,
            in FastTrack track, in FastClip clip, ClipState state,
            in uint tick, in NoInput input, ref ZeroAllocResult result)
            => result.Accumulator -= clip.V;
    }

    [Fact]
    public void WarmPlaybackAllocatesZeroBytes()
    {
        var id = Timeline<FastTrack, FastClip>.Build(b =>
        {
            var t = b.Track(new FastTrack());
            b.Clip(in t, new FastClip(1.5f), 0, 50);
            b.Clip(in t, new FastClip(3.5f), 25, 75);
}).InMemory();

        // Warm up and bind
        Timeline<FastTrack, FastClip>.Bind<NoInput, ZeroAllocResult>(id);
        var result = new ZeroAllocResult();
        var input = default(NoInput);
        var pb = Timeline.Start(id);
        var cursor = default(Cursor);

        // Warm up both paths
        pb = Timeline.Forward(id, in pb, ref cursor, in input, ref result, 5u, 10u);
        // ...and the plain hub paths the measurements below take (single and
        // batch share the non-cursor, non-scratch runner; first execution of
        // a cold runner can pay one-time tiering/dictionary allocations that
        // are not the steady state this receipt pins).
        pb = Timeline.Forward(id, in pb, in input, ref result, 16u, 17u);

        // Measure single tick. The batch/scratch receipts below pass their
        // tick spans as VARIABLES: on this SDK the call-site expansion of
        // `params ReadOnlySpan<uint>` from literal arguments lowers to code
        // that allocates per call — a compiler artifact at the call site,
        // not the library (span-variable, `in uint tick` and cursor shapes
        // all measure 0), and it failed this receipt at v0.2 baseline too.
        long beforeSingle = GC.GetAllocatedBytesForCurrentThread();
        pb = Timeline.Forward(id, in pb, in input, ref result, 21u);
        long afterSingle = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, afterSingle - beforeSingle);

        // Measure batch ticks
        ReadOnlySpan<uint> batch = [22u, 23u, 24u, 25u];
        long beforeBatch = GC.GetAllocatedBytesForCurrentThread();
        pb = Timeline.Forward(id, in pb, in input, ref result, batch);
        long afterBatch = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, afterBatch - beforeBatch);

        // Measure cursor-primed ticks
        long beforeCursor = GC.GetAllocatedBytesForCurrentThread();
        pb = Timeline.Forward(id, in pb, ref cursor, in input, ref result, 26u);
        long afterCursor = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, afterCursor - beforeCursor);

        Timeline.Destroy(id);
    }
}
