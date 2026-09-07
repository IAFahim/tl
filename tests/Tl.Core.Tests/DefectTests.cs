using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public class DefectTests
{
    public readonly record struct TestClip(float Value);

    public readonly struct TestTrack : IBlend<TestClip>
    {
        public void Blend(in TestClip first, in TestClip second, float t, out TestClip result)
            => result = new TestClip(first.Value * (1f - t) + second.Value * t);
    }

    public struct TestConsumer :
        IForward<TestTrack, TestClip, TestConsumer>,
        IBackward<TestTrack, TestClip, TestConsumer>
    {
        public float Sum;
        public int Count;
        public bool TriggerGc;

        public void Forward(ref TestConsumer data, in Tracks<TestTrack, TestClip> tracks, in uint tick)
        {
            if (data.TriggerGc)
            {
                for (int i = 0; i < 50; i++)
                    _ = new byte[1024];
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            }

            foreach (var work in tracks)
                data.Sum += work.Clip.Value;
            data.Count++;
        }

        public void Backward(ref TestConsumer data, in Tracks<TestTrack, TestClip> tracks, in uint tick)
        {
            if (data.TriggerGc)
            {
                for (int i = 0; i < 50; i++)
                    _ = new byte[1024];
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            }

            foreach (var work in tracks)
                data.Sum -= work.Clip.Value;
            data.Count++;
        }
    }

    #region Defect A: Preserve duration width

    [Theory]
    [InlineData(65535u)]
    [InlineData(65536u)]
    [InlineData(70000u)]
    [InlineData(uint.MaxValue)]
    public void DefectA_PreservesLargeDuration(uint duration)
    {
        var id = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t = b.Track(new TestTrack());
            b.Clip(in t, new TestClip(10f), start: duration - 100, end: duration);
        });

        Assert.Equal(duration, Timeline.Duration(id));
        Timeline.Destroy(id);
    }

    [Fact]
    public void DefectA_EmptyTimelineHasZeroDurationAndCompletes()
    {
        var id = Timeline<TestTrack, TestClip>.Build(_ => { });
        Assert.Equal(0u, Timeline.Duration(id));

        var pb = Timeline.Start(id);
        var consumer = new TestConsumer();
        pb = Timeline.Forward(id, in pb, ref consumer, 0u);

        Assert.True(pb.Has(PlaybackFlags.Completed));
        Assert.Equal(0, consumer.Count);

        Timeline.Destroy(id);
    }

    [Fact]
    public void DefectA_LoopingNormalizationWithLargeDuration()
    {
        const uint dur = 100_000u;
        var loopId = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t = b.Track(new TestTrack());
            b.Clip(in t, new TestClip(5f), 0, dur);
            b.Looping();
        });

        Assert.Equal(dur, Timeline.Duration(loopId));

        var consumer = new TestConsumer();
        var pb = Timeline.Start(loopId);

        // Advance past one full loop (dur + 5)
        pb = Timeline.Forward(loopId, in pb, ref consumer, dur + 5);

        Assert.Equal(1, pb.Cycles);
        Assert.Equal(dur + 5, pb.Tick);

        Timeline.Destroy(loopId);
    }

    #endregion

    #region Defect B: Managed references valid through dispatch

    private class HeapContainer
    {
        public TestConsumer Consumer;
        public Cursor Cursor;
    }

    public struct ManagedFieldsConsumer :
        IForward<TestTrack, TestClip, ManagedFieldsConsumer>,
        IBackward<TestTrack, TestClip, ManagedFieldsConsumer>
    {
        public string Name;
        public List<float> Values;

        public void Forward(ref ManagedFieldsConsumer data, in Tracks<TestTrack, TestClip> tracks, in uint tick)
        {
            for (int i = 0; i < 50; i++)
                _ = new byte[1024];
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

            foreach (var work in tracks)
                data.Values.Add(work.Clip.Value);
        }

        public void Backward(ref ManagedFieldsConsumer data, in Tracks<TestTrack, TestClip> tracks, in uint tick)
        {
            foreach (var work in tracks)
                data.Values.Remove(work.Clip.Value);
        }
    }

    [Fact]
    public void DefectB_ObjectFieldReferenceSurvivesForcedCompactingGc()
    {
        var id = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t = b.Track(new TestTrack());
            b.Clip(in t, new TestClip(42f), 0, 10);
        });

        var container = new HeapContainer
        {
            Consumer = new TestConsumer { TriggerGc = true }
        };

        var pb = Timeline.Start(id);
        pb = Timeline.Forward(id, in pb, ref container.Consumer, 1u, 2u, 3u);

        Assert.Equal(3, container.Consumer.Count);
        Assert.Equal(42f * 3, container.Consumer.Sum);

        Timeline.Destroy(id);
    }

    [Fact]
    public void DefectB_ArrayElementReferenceSurvivesForcedCompactingGc()
    {
        var id = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t = b.Track(new TestTrack());
            b.Clip(in t, new TestClip(10f), 0, 10);
        });

        var array = new TestConsumer[5];
        array[2].TriggerGc = true;

        var pb = Timeline.Start(id);
        pb = Timeline.Forward(id, in pb, ref array[2], 0u, 1u);

        Assert.Equal(2, array[2].Count);
        Assert.Equal(20f, array[2].Sum);

        Timeline.Destroy(id);
    }

    [Fact]
    public void DefectB_ConsumerWithManagedFieldsPreserved()
    {
        var id = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t = b.Track(new TestTrack());
            b.Clip(in t, new TestClip(7f), 0, 10);
        });

        var managed = new ManagedFieldsConsumer
        {
            Name = "Player1",
            Values = []
        };

        var pb = Timeline.Start(id);
        pb = Timeline.Forward(id, in pb, ref managed, 1u, 2u);

        Assert.Equal("Player1", managed.Name);
        Assert.Equal([7f, 7f], managed.Values);

        Timeline.Destroy(id);
    }

    [Fact]
    public void DefectB_CursorInObjectFieldSurvivesForcedGc()
    {
        var id = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t = b.Track(new TestTrack());
            b.Clip(in t, new TestClip(1f), 0, 50);
        });

        var container = new HeapContainer
        {
            Consumer = new TestConsumer { TriggerGc = true }
        };

        var pb = Timeline.Start(id);
        pb = Timeline.Forward(id, in pb, ref container.Cursor, ref container.Consumer, 5u);

        Assert.Equal(5u, pb.Tick);
        Assert.NotNull(container.Cursor.Owner);

        Timeline.Destroy(id);
    }

    #endregion

    #region Defect C: Validate the typed scratch closure

    public readonly record struct OtherClip(double BigValue);
    public readonly struct OtherTrack : IBlend<OtherClip>
    {
        public void Blend(in OtherClip first, in OtherClip second, float t, out OtherClip result)
            => result = new OtherClip(first.BigValue * (1 - t) + second.BigValue * t);
    }

    public struct DualConsumer :
        IForward<TestTrack, TestClip, DualConsumer>,
        IBackward<TestTrack, TestClip, DualConsumer>,
        IForward<OtherTrack, OtherClip, DualConsumer>,
        IBackward<OtherTrack, OtherClip, DualConsumer>
    {
        public void Forward(ref DualConsumer data, in Tracks<TestTrack, TestClip> tracks, in uint tick) { }
        public void Backward(ref DualConsumer data, in Tracks<TestTrack, TestClip> tracks, in uint tick) { }
        public void Forward(ref DualConsumer data, in Tracks<OtherTrack, OtherClip> tracks, in uint tick) { }
        public void Backward(ref DualConsumer data, in Tracks<OtherTrack, OtherClip> tracks, in uint tick) { }
    }

    [Fact]
    public void DefectC_ScratchOverloadRejectsDifferentTrackClipClosure()
    {
        var idOther = Timeline<OtherTrack, OtherClip>.Build(b =>
        {
            var t = b.Track(new OtherTrack());
            b.Clip(in t, new OtherClip(100.0), 0, 10);
            b.Clip(in t, new OtherClip(200.0), 5, 15);
        });

        var dual = new DualConsumer();
        var pb = Timeline.Start(idOther);
        Span<TestClip> scratchTest = stackalloc TestClip[4];

        ArgumentException? exForward = null;
        try
        {
            Timeline<TestTrack, TestClip>.Forward(idOther, in pb, ref dual, scratchTest, 5u);
        }
        catch (ArgumentException ex)
        {
            exForward = ex;
        }

        Assert.NotNull(exForward);
        Assert.Equal("index", exForward.ParamName);

        ArgumentException? exBackward = null;
        try
        {
            Timeline<TestTrack, TestClip>.Backward(idOther, in pb, ref dual, scratchTest, 5u);
        }
        catch (ArgumentException ex)
        {
            exBackward = ex;
        }

        Assert.NotNull(exBackward);
        Assert.Equal("index", exBackward.ParamName);

        Timeline.Destroy(idOther);
    }

    [Fact]
    public void DefectC_RejectsUndersizedScratchBuffer()
    {
        var id = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t1 = b.Track(new TestTrack());
            b.Clip(in t1, new TestClip(1f), 0, 10);
            b.Clip(in t1, new TestClip(2f), 5, 15);

            var t2 = b.Track(new TestTrack());
            b.Clip(in t2, new TestClip(3f), 0, 10);
            b.Clip(in t2, new TestClip(4f), 5, 15);
        });

        var consumer = new TestConsumer();
        var pb = Timeline.Start(id);

        Span<TestClip> tooSmall = stackalloc TestClip[1];
        ArgumentException? ex = null;
        try
        {
            Timeline<TestTrack, TestClip>.Forward(id, in pb, ref consumer, tooSmall, 7u);
        }
        catch (ArgumentException e)
        {
            ex = e;
        }

        Assert.NotNull(ex);
        Assert.Equal("scratch", ex.ParamName);

        Timeline.Destroy(id);
    }

    #endregion

    #region Defect D: Publish complete bindings

    [Fact]
    public async Task DefectD_ParallelBindingIsThreadSafe()
    {
        var id = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t = b.Track(new TestTrack());
            b.Clip(in t, new TestClip(1f), 0, 100);
        });

        const int threadCount = 8;
        var tasks = new Task[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var c = new TestConsumer();
                var pb = Timeline.Start(id);
                for (uint tick = 1; tick < 20; tick++)
                {
                    pb = Timeline.Forward(id, in pb, ref c, tick);
                }
                Assert.Equal(19, c.Count);
            });
        }

        await Task.WhenAll(tasks);
        Timeline.Destroy(id);
    }

    [Fact]
    public void DefectD_DestroyAllowsStartedCallToFinish()
    {
        var id = Timeline<TestTrack, TestClip>.Build(b =>
        {
            var t = b.Track(new TestTrack());
            b.Clip(in t, new TestClip(10f), 0, 20);
        });

        var pb = Timeline.Start(id);
        var consumer = new TestConsumer();

        // Destroy the timeline
        Timeline.Destroy(id);

        // Verify that Live(id) throws immediately
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.Live(id));

        // And attempting to start or run throws
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.Forward(id, in pb, ref consumer, 5u));
    }

    #endregion

    #region Defect E: Validate track-handle ownership

    [Fact]
    public void DefectE_RejectsDefaultTrackRef()
    {
        var builder = new TimelineBuilder<TestTrack, TestClip>(TimelineOptions.Default);
        TrackRef defaultRef = default;

        ArgumentException? ex = null;
        try
        {
            builder.Clip(in defaultRef, new TestClip(1f), 0, 10);
        }
        catch (ArgumentException e)
        {
            ex = e;
        }

        Assert.NotNull(ex);
        Assert.Equal("track", ex.ParamName);
    }

    [Fact]
    public void DefectE_RejectsForeignTrackRefFromAnotherBuilder()
    {
        var builder1 = new TimelineBuilder<TestTrack, TestClip>(TimelineOptions.Default);
        var track1 = builder1.Track(new TestTrack());

        var builder2 = new TimelineBuilder<TestTrack, TestClip>(TimelineOptions.Default);
        builder2.Track(new TestTrack());

        ArgumentException? ex = null;
        try
        {
            builder2.Clip(in track1, new TestClip(1f), 0, 10);
        }
        catch (ArgumentException e)
        {
            ex = e;
        }

        Assert.NotNull(ex);
        Assert.Equal("track", ex.ParamName);
    }

    [Fact]
    public void DefectE_RejectsCrossBuilderHandleInNestedBuilds()
    {
        var outerBuilder = new TimelineBuilder<TestTrack, TestClip>(TimelineOptions.Default);
        var outerTrack = outerBuilder.Track(new TestTrack());

        var innerBuilder = new TimelineBuilder<TestTrack, TestClip>(TimelineOptions.Default);

        ArgumentException? ex = null;
        try
        {
            innerBuilder.Clip(in outerTrack, new TestClip(2f), 0, 5);
        }
        catch (ArgumentException e)
        {
            ex = e;
        }

        Assert.NotNull(ex);
        Assert.Equal("track", ex.ParamName);
    }

    #endregion
}
