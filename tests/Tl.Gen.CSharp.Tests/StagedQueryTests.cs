using System.Reflection;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class StagedQueryTests
{
    private const string Source = """
        using System;
        using Tl;
        namespace StageFixture;
        public readonly record struct Clip(int Value);
        public readonly record struct Track(byte Id) : IBlend<Clip>
        {
            public void Blend(in Clip first, in Clip second, float factor, out Clip result)
                => result = new Clip((int)(first.Value + (second.Value - first.Value) * factor));
        }
        public struct Trace
        {
            public long Order;
            public long ClipSum;
            public long CycleOrder;
            public uint LastGameTick;
            public FrameFlags Flags;
        }
        public readonly struct BeforeAfter : IHook
        {
            public static void Execute(in TimelineFrame frame, ref Trace trace)
            {
                trace.Order = unchecked(trace.Order * 10 + 8);
                trace.LastGameTick = frame.GameTick;
                trace.Flags |= frame.Flags;
            }
        }
        public readonly struct A : ITimelineJob<Track, Clip>
        {
            public static void Execute(in Frame<Track, Clip> frame, in int bias, ref Trace trace)
            {
                trace.Order = unchecked(trace.Order * 10 + frame.Track.Id + bias);
                trace.ClipSum += frame.Clip.Value;
                trace.LastGameTick = frame.GameTick;
                trace.Flags |= frame.Flags;
                trace.CycleOrder = unchecked(trace.CycleOrder * 10 + frame.Cycle);
            }
        }
        public readonly struct B : ITimelineJob<Track, Clip>
        {
            public static void Execute(in Frame<Track, Clip> frame, in short scale, ref Trace trace)
            {
                if (scale < 0) throw new InvalidOperationException("stage failure");
                trace.Order = unchecked(trace.Order * 10 + frame.Track.Id * scale);
                trace.ClipSum += frame.Clip.Value;
                trace.LastGameTick = frame.GameTick;
                trace.Flags |= frame.Flags;
            }
        }
        public readonly partial struct Ordered : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                builder.Before<BeforeAfter>();
                var first = builder.Track(new Track(1)).Use<A>();
                var middle = builder.Track(new Track(2)).Use<B>();
                var last = builder.Track(new Track(3)).Use<A>();
                builder.Clip(first, new Clip(10), 0u, 2u);
                builder.Clip(first, new Clip(20), 0u, 2u);
                builder.Clip(middle, new Clip(5), 0u, 2u);
                builder.Clip(last, new Clip(7), 0u, 2u);
                builder.After<BeforeAfter>();
            }
        }
        public readonly partial struct Loop : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new Track(1)).Use<A>();
                builder.Clip(track, new Clip(1), 0u, 1u);
                builder.Looping();
            }
        }
        public readonly struct OrderedRows;
        public readonly struct LoopRows;
        public readonly partial struct Catalog : ITimelineCatalog
        {
            public static void Define(scoped CatalogBuilder builder)
            {
                builder.Schema<OrderedRows>().Asset<Ordered>();
                builder.Schema<LoopRows>().Asset<Loop>();
            }
        }
        public static class Receipt
        {
            public static long Run()
            {
                var states = new[] { new Catalog.State(Catalog.Asset.Ordered) };
                var traces = new Trace[1];
                var bias = new[] { 0 };
                var scale = new short[] { 1 };
                var query = new Catalog.Query().OrderedRows(states, traces, bias, scale);
                query.Tick(100u);
                Require(states[0].Position == 1u && traces[0].Order == 81238 && traces[0].ClipSum == 22 && traces[0].LastGameTick == 100u, "first forward frame");
                query.Tick(101u);
                Require(states[0].Position == 2u && traces[0].Order == 8123881238 && traces[0].ClipSum == 54 && traces[0].LastGameTick == 101u, "second forward frame");
                traces[0] = default;
                query.Tick(102u, -1);
                Require(states[0].Position == 1u && traces[0].Order == 83218 && traces[0].ClipSum == 32 && traces[0].LastGameTick == 101u, "first reverse frame");
                query.Tick(101u, -1);
                Require(states[0].Position == 0u && traces[0].Order == 8321883218 && traces[0].ClipSum == 54 && traces[0].LastGameTick == 100u, "second reverse frame");
                Require((traces[0].Flags & (FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.TimelineEnd | FrameFlags.ClipStart | FrameFlags.ClipEnd))
                    == (FrameFlags.Reverse | FrameFlags.TimelineStart | FrameFlags.TimelineEnd | FrameFlags.ClipStart | FrameFlags.ClipEnd), "frame flags");
                query.Tick(200u);
                traces[0] = default;
                scale[0] = -1;
                try
                {
                    query.Tick(201u);
                    throw new Exception("stage exception did not propagate");
                }
                catch (InvalidOperationException exception) when (exception.Message == "stage failure")
                {
                }
                Require(states[0].Position == 1u && traces[0].Order == 81, "commit occurred after failed stage");
                traces[0] = default;
                scale[0] = 1;
                query.Tick(201u);
                Require(states[0].Position == 2u && traces[0].Order == 81238, "pending selection was not replaced");

                var loopStates = new[] { new Catalog.State(Catalog.Asset.Loop) };
                var loopTraces = new Trace[1];
                var loopQuery = new Catalog.Query().LoopRows(loopStates, bias, loopTraces);
                loopQuery.Tick(300u, 3);
                Require(loopStates[0].Position == 0u && loopStates[0].Cycle == 3 && loopTraces[0].CycleOrder == 12, "forward loop coordinates");
                loopTraces[0] = default;
                loopQuery.Tick(303u, -3);
                Require(loopStates[0].Position == 0u && loopStates[0].Cycle == 0 && loopTraces[0].CycleOrder == 210, "reverse loop coordinates");
                Require(Catalog.AssetCount == 2 && Catalog.StateBytes == 24 && Catalog.StaticDataBytes > 0, "generated memory report");
                return traces[0].Order + loopTraces[0].Order;
            }
            private static void Require(bool value, string name)
            {
                if (!value) throw new Exception(name);
            }
        }
        """;

    [Fact]
    public void GeneratedStagesPreserveTypedSlotsOrderFlagsLoopsAndCommitBoundary()
    {
        var assembly = GeneratedJobTests.Generate(Source);
        var result = assembly.GetType("StageFixture.Receipt")!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
        Assert.IsType<long>(result);
    }
}
