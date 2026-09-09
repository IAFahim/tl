using Tl.Authoring;
using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

// v0.4 receipts for the unmanaged boundary: the retained runtime timeline
// is ONE native block with an explicit lifetime. What is pinned here:
//  (a) steady-state managed heap growth across Build->InMemory->play->
//      Destroy cycles is ZERO (transients die; nothing is retained),
//  (b) the managed authoring graph dies while the native timeline lives,
//  (c) Destroy frees — the handle rejects playback, double Destroy
//      rejects, aliased handles are independent, indexes are never reused,
//  (d) the threading contract: construction/destroy is externally
//      synchronized; playback is read-only over an immutable snapshot and
//      safe from any number of threads.
public class UnmanagedReceipts
{
    public readonly record struct MemClip(float Value);

    public readonly struct MemTrack : IBlend<MemClip>
    {
        public void Blend(in MemClip first, in MemClip second, float t, out MemClip result)
            => result = new MemClip(first.Value * (1f - t) + second.Value * t);
    }

    public struct MemAcc :
        ITrack<MemTrack, MemClip, NoInput, MemAcc>
    {
        public float Sum;
        public int Count;

        public static void Forward(int ordinal, int count, ushort index,
            in MemTrack track, in MemClip clip, ClipState state,
            in uint tick, in NoInput input, ref MemAcc result)
        {
            if (state == ClipState.Stay)
                result.Sum += clip.Value;
            if (ordinal + 1 == count)
                result.Count++;
        }

        public static void Backward(int ordinal, int count, ushort index,
            in MemTrack track, in MemClip clip, ClipState state,
            in uint tick, in NoInput input, ref MemAcc result)
        {
            if (state == ClipState.Stay)
                result.Sum -= clip.Value;
            if (ordinal + 1 == count)
                result.Count++;
        }
    }

    private static void Author(TimelineBuilder<MemTrack, MemClip> b)
    {
        var t1 = b.Track(new MemTrack());
        var t2 = b.Track(new MemTrack());
        var t3 = b.Track(new MemTrack());
        b.Clip(in t1, new MemClip(1f), 0, 40);
        b.Clip(in t1, new MemClip(3f), 20, 60);
        b.Clip(in t2, new MemClip(5f), 4, 12);
        b.Clip(in t2, new MemClip(7f), 8, 16);
        b.Clip(in t3, new MemClip(11f), 30, 50);
        b.Clip(in t3, new MemClip(13f), 10, 22);
    }

    private static (float Sum, int Count) Walk(ushort id, uint until)
    {
        var acc = new MemAcc();
        var pb = Timeline.Start(id);
        for (uint t = 0; t < until; t++)
            pb = Timeline.Forward(id, in pb, default(NoInput), ref acc, t);
        return (acc.Sum, acc.Count);
    }

    [Fact]
    public void ManagedAuthoringGraphsAreCollectibleAcrossCycles()
    {
        var authoring = new WeakReference[64];
        for (var i = 0; i < authoring.Length; i++)
            authoring[i] = CycleForWeakReferenceProbe();

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();

        Assert.All(authoring, static reference => Assert.False(reference.IsAlive));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CycleForWeakReferenceProbe()
    {
        var builder = new TimelineBuilder<MemTrack, MemClip>(TimelineOptions.Default);
        Author(builder);
        var authoring = new WeakReference(builder._state);
        var id = RuntimeLowering.Lower<MemTrack, MemClip>(builder._state);
        Timeline<MemTrack, MemClip>.Bind<NoInput, MemAcc>(id);
        _ = Walk(id, 40);
        Timeline.Destroy(id);
        return authoring;
    }

    // (a, cont.) Destroy itself allocates nothing on the managed heap — the
    // frees are NativeMemory calls.
    [Fact]
    public void DestroyAllocatesZeroManagedBytes()
    {
        var id = Timeline<MemTrack, MemClip>.Build(Author).InMemory();
        Timeline<MemTrack, MemClip>.Bind<NoInput, MemAcc>(id);
        _ = Walk(id, 20);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Timeline.Destroy(id);
        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    // (b) The managed authoring graph dies with the terminal operation;
    // the native timeline it lowered keeps playing without it.
    [Fact]
    public void AuthoringGraphDiesWhileNativeTimelineLives()
    {
        var (id, authoring) = LowerForWeakReferenceProbe();

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();

        Assert.False(authoring.IsAlive);
        Timeline<MemTrack, MemClip>.Bind<NoInput, MemAcc>(id);
        Assert.Equal(60u, Timeline.Duration(id));
        Assert.True(Walk(id, 40).Count > 0);

        Timeline.Destroy(id);
    }

    private static (ushort Id, WeakReference Authoring) LowerForWeakReferenceProbe()
    {
        var builder = new TimelineBuilder<MemTrack, MemClip>(TimelineOptions.Default);
        Author(builder);
        var weak = new WeakReference(builder._state);
        return (RuntimeLowering.Lower<MemTrack, MemClip>(builder._state), weak);
    }

    // (c) Destroy frees: every hub entry point rejects the dead handle.
    [Fact]
    public void DestroyedHandleRejectsEveryHubCall()
    {
        var id = Timeline<MemTrack, MemClip>.Build(Author).InMemory();
        Timeline<MemTrack, MemClip>.Bind<NoInput, MemAcc>(id);
        _ = Walk(id, 10);
        Timeline.Destroy(id);

        Assert.False(Timeline.IsValid(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.Start(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.Duration(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.IsLooping(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.Destroy(id));

        var acc = new MemAcc();
        var pb = new Playback(0, 0, PlaybackFlags.Started); // valid shape, dead handle below
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Timeline.Forward(id, in pb, default(NoInput), ref acc, 5u));
    }

    // (c) Explicit lifetime is one-shot: double Destroy rejects.
    [Fact]
    public void DoubleDestroyRejects()
    {
        var id = Timeline<MemTrack, MemClip>.Build(Author).InMemory();

        Timeline.Destroy(id);
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.Destroy(id));
    }

    // (c) Aliasing: the same authoring lowered twice yields two
    // independent handles — identical receipts, independent lifetimes, and
    // the freed index is never handed out again.
    [Fact]
    public void AliasedHandlesAreIndependentAndIndexesNeverReuse()
    {
        var first = Timeline<MemTrack, MemClip>.Build(Author).InMemory();
        var second = Timeline<MemTrack, MemClip>.Build(Author).InMemory();

        Assert.NotEqual(first, second);

        Timeline<MemTrack, MemClip>.Bind<NoInput, MemAcc>(first);
        Timeline<MemTrack, MemClip>.Bind<NoInput, MemAcc>(second);

        var walkFirst = Walk(first, 60);
        var walkSecond = Walk(second, 60);
        Assert.Equal(walkFirst, walkSecond);

        Timeline.Destroy(first);
        Assert.False(Timeline.IsValid(first));
        Assert.True(Timeline.IsValid(second));

        // The surviving alias still plays the same receipts.
        Assert.Equal(walkSecond, Walk(second, 60));

        // Never-reuse: a later timeline gets a fresh index, not either
        // destroyed/used one.
        var third = Timeline<MemTrack, MemClip>.Build(Author).InMemory();
        Assert.NotEqual(first, third);
        Assert.NotEqual(second, third);

        Timeline.Destroy(second);
        Timeline.Destroy(third);
    }

    // (d) The threading contract's playback half: after Build/InMemory/Bind
    // (all externally synchronized here), the timeline is an immutable
    // snapshot — any number of threads can walk it concurrently, every
    // walk seeing exactly the single-threaded receipt.
    [Fact]
    public async Task ParallelReadOnlyPlaybackMatchesSingleThreadedWalk()
    {
        var id = Timeline<MemTrack, MemClip>.Build(Author).InMemory();
        Timeline<MemTrack, MemClip>.Bind<NoInput, MemAcc>(id);

        var expected = Walk(id, 60);

        const int threads = 8;
        var tasks = new Task[threads];
        for (var t = 0; t < threads; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (var rep = 0; rep < 25; rep++)
                {
                    Assert.Equal(expected, Walk(id, 60));

                    // Mixed read-only shapes: stateless sampling and
                    // backward rewinds on the same snapshot.
                    var acc = new MemAcc();
                    Timeline.Forward(id, default(NoInput), ref acc, 7u, 19u, 31u);
                    var pb = Timeline.Start(id, 59u);
                    for (uint tick = 59; tick > 40; tick--)
                        pb = Timeline.Backward(id, in pb, default(NoInput), ref acc, tick);
                }
            });
        }

        await Task.WhenAll(tasks);
        Timeline.Destroy(id);
    }
}
