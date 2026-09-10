using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp;
using Tl.Gen.CSharp.Model;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class HeterogeneousCompositionTests
{
    private const string Contracts = """
        using Tl;

        namespace Fix;

        public readonly record struct ClipA(float Value);
        public readonly record struct ClipB(float Value);
        public readonly record struct InputA(float Value);
        public readonly record struct InputB(float Value);
        public readonly record struct OutputA(float Value);
        public readonly record struct OutputB(float Value);

        public readonly struct TrackA : ITrack<ClipA>
        {
            public void Blend(in ClipA first, in ClipA second, float factor, out ClipA result) => result = first;
            public static void Seek(in Frame<TrackA, ClipA> frame, in InputA inputA, ref OutputA outputA) { }
        }

        public readonly struct TrackB : ITrack<ClipB>
        {
            public void Blend(in ClipB first, in ClipB second, float factor, out ClipB result) => result = first;
            public static void Seek(in Frame<TrackB, ClipB> frame, in InputB inputB, out OutputB outputB) => outputB = default;
        }

        public readonly struct BaseBefore : IHook
        {
            public static void Forward(in InputA inputA) { }
            public static void Backward(in InputA inputA) { }
        }

        public readonly struct BaseAfter : IHook
        {
            public static void Forward(ref OutputA outputA) { }
            public static void Backward(ref OutputA outputA) { }
        }

        public readonly struct OuterBefore : IHook
        {
            public static void Forward(in InputB inputB) { }
            public static void Backward(in InputB inputB) { }
        }

        public readonly struct OuterAfter : IHook
        {
            public static void Forward(out OutputB outputB) => outputB = default;
            public static void Backward(out OutputB outputB) => outputB = default;
        }
        """;

    [Fact]
    public void IncludeFlattensTracksClipsAndHooksInDefinitionOrder()
    {
        var source = Contracts + """

            public readonly partial struct Base : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Before<BaseBefore>();
                    var track = builder.Track(new TrackA());
                    builder.Clip(track, new ClipA(1f), 0u, 8u);
                    builder.After<BaseAfter>();
                }
            }

            public readonly partial struct Outer : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Before<OuterBefore>();
                    builder.Include<Base>();
                    var track = builder.Track(new TrackB());
                    builder.Clip(track, new ClipB(2f), 0u, 8u);
                    builder.After<OuterAfter>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Fix.cs", source)]);

        Assert.Empty(diagnostics);
        var outer = Assert.Single(timelines, static timeline => timeline.Name == "Outer");
        Assert.Equal([0, 1], outer.Tracks.Select(static track => track.Index));
        Assert.Equal([0, 1], outer.Clips.Select(static clip => clip.TrackIndex));
        Assert.Equal(["global::Fix.OuterBefore", "global::Fix.BaseBefore"], outer.BeforeHooks.Select(static hook => hook.TypeName));
        Assert.Equal(["global::Fix.BaseAfter", "global::Fix.OuterAfter"], outer.AfterHooks.Select(static hook => hook.TypeName));
        Assert.Equal(["inputA", "inputB"], outer.ReadOnlySlots.Select(static slot => slot.Name));
        Assert.Equal(["outputA", "outputB"], outer.WritableSlots.Select(static slot => slot.Name));
        Assert.All(outer.WritableSlots, static slot => Assert.Equal(SlotMode.Reference, slot.Mode));

        var generated = HeterogeneousEmitter.Emit(outer);
        Assert.Contains("public const int TrackCount = 2;", generated);
        Assert.Contains("public const int ClipCount = 2;", generated);
        Assert.Contains("public const int RegionCount = 1;", generated);
        Assert.Contains("public static nuint StaticDataBytes", generated);
        var outerBefore = generated.IndexOf("global::Fix.OuterBefore.Forward", StringComparison.Ordinal);
        var baseBefore = generated.IndexOf("global::Fix.BaseBefore.Forward", StringComparison.Ordinal);
        var trackA = generated.IndexOf("global::Fix.TrackA.Seek", StringComparison.Ordinal);
        var trackB = generated.IndexOf("global::Fix.TrackB.Seek", StringComparison.Ordinal);
        var baseAfter = generated.IndexOf("global::Fix.BaseAfter.Forward", StringComparison.Ordinal);
        var outerAfter = generated.IndexOf("global::Fix.OuterAfter.Forward", StringComparison.Ordinal);
        Assert.True(outerBefore < baseBefore && baseBefore < trackA && trackA < trackB && trackB < baseAfter && baseAfter < outerAfter);
    }

    [Fact]
    public void OuterAfterPrecedesIncludeInSourceButFollowsIncludedAfter()
    {
        var source = Contracts + """

            public readonly partial struct Base : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackA());
                    builder.Clip(track, new ClipA(1f), 0u, 8u);
                    builder.After<BaseAfter>();
                }
            }

            public readonly partial struct Outer : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.After<OuterAfter>();
                    builder.Include<Base>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("AfterOrder.cs", source)]);

        Assert.Empty(diagnostics);
        var outer = Assert.Single(timelines, static timeline => timeline.Name == "Outer");
        Assert.Equal(["global::Fix.BaseAfter", "global::Fix.OuterAfter"], outer.AfterHooks.Select(static hook => hook.TypeName));
    }

    [Fact]
    public void OuterBeforeFollowsIncludeInSourceButPrecedesIncludedBefore()
    {
        var source = Contracts + """

            public readonly partial struct Base : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Before<BaseBefore>();
                    var track = builder.Track(new TrackA());
                    builder.Clip(track, new ClipA(1f), 0u, 8u);
                }
            }

            public readonly partial struct Outer : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Include<Base>();
                    builder.Before<OuterBefore>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("BeforeOrder.cs", source)]);

        Assert.Empty(diagnostics);
        var outer = Assert.Single(timelines, static timeline => timeline.Name == "Outer");
        Assert.Equal(["global::Fix.OuterBefore", "global::Fix.BaseBefore"], outer.BeforeHooks.Select(static hook => hook.TypeName));
    }

    [Fact]
    public void MultipleIncludesAreRejected()
    {
        var source = Contracts + """

            public readonly partial struct First : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackA());
                    builder.Clip(track, new ClipA(1f), 0u, 8u);
                }
            }

            public readonly partial struct Second : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackB());
                    builder.Clip(track, new ClipB(2f), 0u, 8u);
                }
            }

            public readonly partial struct Outer : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Include<First>();
                    builder.Include<Second>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("MultipleIncludes.cs", source)]);

        Assert.DoesNotContain(timelines, static timeline => timeline.Name == "Outer");
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN48");
    }

    [Fact]
    public void HooksEmitOncePerDirectionAndCoverGaps()
    {
        var source = Contracts + """

            public readonly partial struct Gapped : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Before<BaseBefore>();
                    var track = builder.Track(new TrackA());
                    builder.Clip(track, new ClipA(1f), 2u, 4u);
                    builder.After<BaseAfter>();
                }
            }
            """;
        var (timelines, diagnostics) = HeterogeneousReader.Read([("Fix.cs", source)]);

        Assert.Empty(diagnostics);
        var generated = HeterogeneousEmitter.Emit(Assert.Single(timelines));
        Assert.Equal(1, Occurrences(generated, "global::Fix.BaseBefore.Forward"));
        Assert.Equal(1, Occurrences(generated, "global::Fix.BaseAfter.Forward"));
        Assert.Equal(1, Occurrences(generated, "global::Fix.BaseBefore.Backward"));
        Assert.Equal(1, Occurrences(generated, "global::Fix.BaseAfter.Backward"));
    }

    [Fact]
    public void BlendOperandsFollowStartThenAuthoredOrder()
    {
        var track = new HeterogeneousTrack(0, "global::Fix.TrackA", "global::Fix.ClipA", "new global::Fix.TrackA()", []);
        var later = new HeterogeneousClip(0, "global::Fix.ClipA", "new global::Fix.ClipA(2f)", 4u, 12u);
        var earlier = new HeterogeneousClip(0, "global::Fix.ClipA", "new global::Fix.ClipA(1f)", 0u, 8u);
        var timeline = new HeterogeneousTimeline("Ordered", "Fix", false, [], [track], [later, earlier], [], [], [], []);

        var generated = HeterogeneousEmitter.Emit(timeline);

        Assert.Contains("var factor_1_0 = (local - 4u) / 3f", generated);
        Assert.Contains("Blend(in s_clip1, in s_clip0, factor_1_0", generated);

        var tiedFirst = new HeterogeneousClip(0, "global::Fix.ClipA", "new global::Fix.ClipA(3f)", 0u, 8u);
        var tiedSecond = new HeterogeneousClip(0, "global::Fix.ClipA", "new global::Fix.ClipA(4f)", 0u, 8u);
        timeline = timeline with { Clips = [tiedFirst, tiedSecond] };

        generated = HeterogeneousEmitter.Emit(timeline);

        Assert.Contains("Blend(in s_clip0, in s_clip1", generated);
    }

    [Fact]
    public void DataInitializationPrecedesLazyDynamicRegistration()
    {
        var (timelines, diagnostics) = HeterogeneousReader.Read([("Fix.cs", Contracts + """

            public readonly partial struct Ordered : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackA());
                    builder.Clip(track, new ClipA(1f), 0u, 8u);
                }
            }
            """)]);

        Assert.Empty(diagnostics);
        var generated = HeterogeneousEmitter.Emit(Assert.Single(timelines));
        var data = generated.IndexOf("s_track0 =", StringComparison.Ordinal);
        var holder = generated.IndexOf("private static class Dynamic", StringComparison.Ordinal);
        var publication = generated.IndexOf("internal static readonly ushort Id = global::Tl.Timeline.RegisterCompiled", StringComparison.Ordinal);

        Assert.True(data >= 0 && data < holder && holder < publication);
        Assert.Contains("public static ushort Id => Dynamic.Id", generated);
    }

    [Fact]
    public void IncludeCycleIsRejected()
    {
        var source = """
            using Tl;
            namespace Fix;

            public readonly partial struct First : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Include<Second>();
                }
            }

            public readonly partial struct Second : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Include<First>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Cycle.cs", source)]);

        Assert.Empty(timelines);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN42");
    }

    [Fact]
    public void LoopingIncludeCannotChangeDuration()
    {
        var source = Contracts + """

            public readonly partial struct Loop : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackA());
                    builder.Clip(track, new ClipA(1f), 0u, 8u);
                    builder.Looping();
                }
            }

            public readonly partial struct Extended : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Include<Loop>();
                    var track = builder.Track(new TrackB());
                    builder.Clip(track, new ClipB(2f), 0u, 12u);
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Loops.cs", source)]);

        Assert.Single(timelines, static timeline => timeline.Name == "Loop");
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN47");
    }

    [Fact]
    public void HookRequiresBothOperations()
    {
        var source = Contracts + """

            public readonly struct Incomplete : IHook
            {
                public static void Forward(in InputA inputA) { }
            }

            public readonly partial struct Broken : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    builder.Before<Incomplete>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Hook.cs", source)]);

        Assert.Empty(timelines);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN45");
    }

    [Fact]
    public void SemanticContractsResolveAliasesConstantsAndPartialTypes()
    {
        var declarations = """
            global using TimelineContract = Tl.ITimeline;
            global using BuilderContract = Tl.Builder;
            global using TrackContract = Tl.ITrack<Domain.Clip>;
            global using FrameContract = Tl.Frame<Domain.Track, Domain.Clip>;
            namespace Domain;

            public readonly record struct Clip(int Value);
            public readonly record struct Input(int Value);
            public readonly record struct Output(int Value);

            public readonly partial struct Track : TrackContract
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            }

            public readonly partial struct Attack : TimelineContract
            {
                private const uint ClipStart = 1u;
            }
            """;
        var operations = """
            namespace Domain;

            public readonly partial struct Track
            {
                public static void Seek(in FrameContract frame, in Input input, ref Output output) { }
            }

            public readonly partial struct Attack
            {
                public static void Define(scoped BuilderContract builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(3), ClipStart, ClipStart + 7u);
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Declarations.cs", declarations), ("Operations.cs", operations)]);

        Assert.Empty(diagnostics);
        var timeline = Assert.Single(timelines);
        Assert.Equal("global::Domain.Track", Assert.Single(timeline.Tracks).TypeName);
        Assert.Equal("global::Domain.Clip", Assert.Single(timeline.Clips).TypeName);
        Assert.Equal(1u, timeline.Clips[0].Start);
        Assert.Equal(8u, timeline.Clips[0].End);
        Assert.Equal("global::Domain.Input", Assert.Single(timeline.ReadOnlySlots).TypeName);
        Assert.Equal("global::Domain.Output", Assert.Single(timeline.WritableSlots).TypeName);
    }

    [Fact]
    public void ManagedComponentsAreRejected()
    {
        var source = """
            using Tl;
            namespace Fix;

            public readonly record struct Clip(int Value);

            public readonly struct Track : ITrack<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
                public static void Seek(in Frame<Track, Clip> frame, in string text) { }
            }

            public readonly partial struct Broken : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(1), 0u, 1u);
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Managed.cs", source)]);

        Assert.Empty(timelines);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN32");
    }

    [Fact]
    public void OutTrackSlotAndRefAfterHookShareOneWritableContextSlot()
    {
        var source = Contracts + """

            public readonly struct RefineOutput : IHook
            {
                public static void Forward(ref OutputB outputB) { }
                public static void Backward(ref OutputB outputB) { }
            }

            public readonly partial struct Writable : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackB());
                    builder.Clip(track, new ClipB(2f), 0u, 8u);
                    builder.After<RefineOutput>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Writable.cs", source)]);

        Assert.Empty(diagnostics);
        var timeline = Assert.Single(timelines);
        var output = Assert.Single(timeline.WritableSlots);
        Assert.Equal("outputB", output.Name);
        Assert.Equal(SlotMode.Reference, output.Mode);
        Assert.Equal(SlotMode.Output, Assert.Single(timeline.Tracks).SeekSlots.Single(static slot => slot.Name == "outputB").Mode);
        Assert.Equal(SlotMode.Reference, Assert.Single(timeline.AfterHooks).ForwardSlots.Single().Mode);
        var generated = HeterogeneousEmitter.Emit(timeline);
        Assert.Contains("out data._outputB", generated);
        Assert.Contains("ref data._outputB", generated);
    }

    [Fact]
    public void PureOutAndRefTimelinesShareOneCanonicalSchema()
    {
        var source = Contracts + """

            public readonly struct RefTrackB : ITrack<ClipB>
            {
                public void Blend(in ClipB first, in ClipB second, float factor, out ClipB result) => result = first;
                public static void Seek(in Frame<RefTrackB, ClipB> frame, in InputB inputB, ref OutputB outputB) { }
            }

            public readonly partial struct PureOut : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackB());
                    builder.Clip(track, new ClipB(1f), 0u, 1u);
                }
            }

            public readonly partial struct ByRef : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new RefTrackB());
                    builder.Clip(track, new ClipB(1f), 0u, 1u);
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("CompatibleWrites.cs", source)]);

        Assert.Empty(diagnostics);
        Assert.Equal(2, timelines.Count);
        Assert.All(timelines, static timeline => Assert.Equal(SlotMode.Reference, Assert.Single(timeline.WritableSlots).Mode));
        var artifacts = HeterogeneousEmitter.EmitCompilation(timelines);
        Assert.Single(artifacts, static artifact => artifact.RelativePath.StartsWith("TlSchema", StringComparison.Ordinal));
    }

    [Fact]
    public void WritableSlotsWithTheSameNameAndDifferentTypesAreRejected()
    {
        var source = Contracts + """

            public readonly struct IncompatibleOutput : IHook
            {
                public static void Forward(ref OutputA outputB) { }
                public static void Backward(ref OutputA outputB) { }
            }

            public readonly partial struct Broken : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackB());
                    builder.Clip(track, new ClipB(2f), 0u, 8u);
                    builder.After<IncompatibleOutput>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Broken.cs", source)]);

        Assert.Empty(timelines);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN38");
    }

    [Fact]
    public void ReadAndWriteSlotsWithTheSameNameAreRejected()
    {
        var source = """
            using Tl;
            namespace Fix;

            public readonly record struct Clip(float Value);
            public readonly record struct Value(float Data);

            public readonly struct Track : ITrack<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
                public static void Seek(in Frame<Track, Clip> frame, in Value shared, ref Value result) { }
            }

            public readonly struct WriteShared : IHook
            {
                public static void Forward(ref Value shared) { }
                public static void Backward(ref Value shared) { }
            }

            public readonly partial struct Broken : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track());
                    builder.Clip(track, new Clip(1f), 0u, 2u);
                    builder.After<WriteShared>();
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("ReadWrite.cs", source)]);

        Assert.Empty(timelines);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TLGEN38" && diagnostic.Message.Contains("both", StringComparison.Ordinal));
    }

    [Fact]
    public void ClipArgumentsFollowBoundParameterOrdinals()
    {
        var source = Contracts + """

            public readonly partial struct Reordered : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new TrackA());
                    builder.Clip(end: 9u, clip: new ClipA(3f), track: track, start: 2u);
                }
            }
            """;

        var (timelines, diagnostics) = HeterogeneousReader.Read([("Reordered.cs", source)]);

        Assert.Empty(diagnostics);
        var clip = Assert.Single(Assert.Single(timelines).Clips);
        Assert.Equal(2u, clip.Start);
        Assert.Equal(9u, clip.End);
        Assert.Contains("ClipA(3F)", clip.Expression, StringComparison.OrdinalIgnoreCase);
    }

    private static int Occurrences(string source, string value)
    {
        var count = 0;
        for (var start = 0; (start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0; start += value.Length)
            count++;
        return count;
    }
}
