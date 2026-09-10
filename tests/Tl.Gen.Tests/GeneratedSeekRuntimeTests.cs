using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.Analysis;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Gen.Tests;

public sealed class GeneratedSeekRuntimeTests
{
    private const string Source = """
        using System;
        using Tl;
        namespace RuntimeReceipt;

        public readonly record struct Clip(int Value);
        public struct Input { public int Throw; }
        public struct State
        {
            public uint GameTick;
            public uint TimelineTick;
            public long Cycle;
            public FrameFlags Flags;
            public int Count;
        }
        public struct Trace { public int Value; }

        public readonly struct Track : ITrack<Clip>
        {
            public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            public static void Seek(in Frame<Track, Clip> frame, in Input input, ref State state, ref Trace trace)
            {
                trace.Value = trace.Value * 10 + (frame.Direction == 1 ? 2 : 5);
                if (input.Throw != 0)
                    throw new InvalidOperationException();
                state.GameTick = frame.GameTick;
                state.TimelineTick = frame.TimelineTick;
                state.Cycle = frame.Cycle;
                state.Flags = frame.Flags;
                state.Count++;
            }
        }

        public readonly struct Before : IHook
        {
            public static void Forward(ref Trace trace) => trace.Value = trace.Value * 10 + 1;
            public static void Backward(ref Trace trace) => trace.Value = trace.Value * 10 + 4;
        }

        public readonly struct After : IHook
        {
            public static void Forward(ref Trace trace) => trace.Value = trace.Value * 10 + 3;
            public static void Backward(ref Trace trace) => trace.Value = trace.Value * 10 + 6;
        }

        public readonly partial struct Finite : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                builder.Before<Before>();
                var track = builder.Track(new Track());
                builder.Clip(track, new Clip(1), 0u, 3u);
                builder.After<After>();
            }
        }

        public readonly partial struct Loop : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                builder.Before<Before>();
                var track = builder.Track(new Track());
                builder.Clip(track, new Clip(1), 0u, 1u);
                builder.After<After>();
                builder.Looping();
            }
        }

        public static class Receipt
        {
            public static int Run()
            {
                var input = default(Input);
                var state = default(State);
                var trace = default(Trace);
                var playback = Finite.Start(uint.MaxValue);
                var data = new Finite.Data(ref playback, in input, ref state, ref trace);
                Check(playback.Position == 0L && playback.GameTick == uint.MaxValue, 1);

                var unchanged = playback;
                Check(Finite.TrySeek(ref data, 0), 2);
                Check(playback == unchanged && trace.Value == 0 && state.Count == 0, 3);
                Check(Finite.TrySeek(ref data, 1), 4);
                Check(playback.Position == 1L && playback.GameTick == 0u && trace.Value == 123, 5);
                Check(state.GameTick == uint.MaxValue && state.TimelineTick == 0u && state.Cycle == 0L, 6);
                Check(state.Flags == (FrameFlags.TimelineStart | FrameFlags.ClipStart), 7);

                trace.Value = 0;
                Check(Finite.TrySeek(ref data, 2), 8);
                Check(playback.Position == 3L && playback.GameTick == 2u && trace.Value == 123123, 9);
                var forwardLast = state;
                Check(forwardLast.GameTick == 1u && forwardLast.TimelineTick == 2u, 10);
                Check(forwardLast.Flags == (FrameFlags.TimelineEnd | FrameFlags.ClipEnd | FrameFlags.CompletedAfter), 11);

                trace.Value = 0;
                Check(Finite.TrySeek(ref data, -1), 12);
                Check(playback.Position == 2L && playback.GameTick == 1u && trace.Value == 654, 13);
                Check(state.GameTick == forwardLast.GameTick && state.TimelineTick == forwardLast.TimelineTick, 14);
                Check(state.Flags == (FrameFlags.Reverse | FrameFlags.TimelineEnd | FrameFlags.ClipEnd | FrameFlags.CompletedBefore), 15);

                unchanged = playback;
                trace.Value = 0;
                Check(!Finite.TrySeek(ref data, 2), 16);
                Check(playback == unchanged && trace.Value == 0, 17);
                Check(!Finite.TrySeek(ref data, int.MinValue), 18);
                Check(playback == unchanged && trace.Value == 0, 19);

                var validPlayback = playback;
                playback = global::Tl.Timeline.CreateTypedPlayback<Finite>(-1L, 31u, PlaybackFlags.Started);
                var invalidTyped = playback;
                trace.Value = 0;
                Check(!Finite.TrySeek(ref data, 1), 41);
                Check(playback == invalidTyped && trace.Value == 0, 42);
                playback = global::Tl.Timeline.CreateTypedPlayback<Finite>((long)Finite.Duration + 1L, 32u, PlaybackFlags.Started);
                invalidTyped = playback;
                Check(!Finite.TrySeek(ref data, -1), 43);
                Check(playback == invalidTyped && trace.Value == 0, 44);
                playback = validPlayback;

                input.Throw = 1;
                trace.Value = 0;
                var threw = false;
                try { Finite.TrySeek(ref data, 1); }
                catch (InvalidOperationException) { threw = true; }
                Check(threw && playback == unchanged && trace.Value == 12, 20);
                input.Throw = 0;

                Check(Finite.TryStop(in playback, out var stopped), 21);
                playback = stopped;
                trace.Value = 0;
                Check(!Finite.TrySeek(ref data, -1) && trace.Value == 0, 22);
                var empty = default(Finite.Data);
                Check(!Finite.TrySeek(ref empty, 0), 23);

                Check(global::Tl.Timeline.TryStart(Finite.Id, 9u, out var dynamicPlayback), 24);
                state = default;
                trace = default;
                var dynamicData = new Finite.DynamicData(ref dynamicPlayback, in input, ref state, ref trace);
                Check(global::Tl.Timeline.TrySeek(Finite.Id, ref dynamicData, 1), 25);
                Check(dynamicPlayback.Position == 1L && dynamicPlayback.GameTick == 10u && trace.Value == 123, 26);
                trace.Value = 0;
                unchanged = global::Tl.Timeline.CreateTypedPlayback<Finite>(dynamicPlayback.Position, dynamicPlayback.GameTick, dynamicPlayback.Flags);
                Check(!global::Tl.Timeline.TrySeek(Loop.Id, ref dynamicData, 1), 27);
                Check(dynamicPlayback.Position == unchanged.Position && trace.Value == 0, 28);

                var validDynamicPlayback = dynamicPlayback;
                dynamicPlayback = global::Tl.Timeline.CreateCompiledPlayback(Finite.Id, -1L, 33u, PlaybackFlags.Started);
                var invalidDynamic = dynamicPlayback;
                Check(!global::Tl.Timeline.TrySeek(Finite.Id, ref dynamicData, 1), 45);
                Check(dynamicPlayback == invalidDynamic && trace.Value == 0, 46);
                dynamicPlayback = global::Tl.Timeline.CreateCompiledPlayback(Finite.Id, (long)Finite.Duration + 1L, 34u, PlaybackFlags.Started);
                invalidDynamic = dynamicPlayback;
                Check(!global::Tl.Timeline.TrySeek(Finite.Id, ref dynamicData, -1), 47);
                Check(dynamicPlayback == invalidDynamic && trace.Value == 0, 48);
                dynamicPlayback = validDynamicPlayback;
                var emptyDynamic = default(Finite.DynamicData);
                Check(!global::Tl.Timeline.TrySeek(Finite.Id, ref emptyDynamic, 0), 29);

                Check(global::Tl.Timeline.TryStart(Loop.Id, 0u, out dynamicPlayback), 30);
                state = default;
                trace = default;
                dynamicData = new Finite.DynamicData(ref dynamicPlayback, in input, ref state, ref trace);
                Check(global::Tl.Timeline.TrySeek(Loop.Id, ref dynamicData, 1), 31);
                Check(dynamicPlayback.Position == 1L && trace.Value == 123, 32);
                Check(state.Flags == (FrameFlags.Looping | FrameFlags.TimelineStart | FrameFlags.TimelineEnd | FrameFlags.ClipStart | FrameFlags.ClipEnd), 33);

                var loopPlayback = Loop.Start(0u);
                state = default;
                trace = default;
                var loopData = new Loop.Data(ref loopPlayback, in input, ref state, ref trace);
                Check(Loop.TrySeek(ref loopData, -3), 34);
                Check(loopPlayback.Position == -3L && loopPlayback.GameTick == uint.MaxValue - 2u, 35);
                Check(state.TimelineTick == 0u && state.Cycle == -3L, 36);
                Check(trace.Value == 654654654, 37);
                Check(Loop.TrySeek(ref loopData, 1), 38);
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (var index = 0; index < 64; index++)
                    Check(Loop.TrySeek(ref loopData, 1), 39);
                Check(GC.GetAllocatedBytesForCurrentThread() == allocatedBefore, 40);
                return 0;
            }

            private static void Check(bool condition, int code)
            {
                if (!condition)
                    throw new InvalidOperationException(code.ToString());
            }
        }
        """;

    [Fact]
    public void SignedPlaybackIsAtomicAndDirectionallyExact()
    {
        var (timelines, diagnostics) = HeterogeneousReader.Read([("Receipt.cs", Source)]);
        Assert.Empty(diagnostics);
        var artifacts = HeterogeneousEmitter.EmitCompilation(timelines);
        var generatedBytes = artifacts.Sum(static artifact => System.Text.Encoding.UTF8.GetByteCount(artifact.Content));
        Assert.Equal(23_910, generatedBytes);
        var assembly = Compile(Source, artifacts);

        var result = assembly.GetType("RuntimeReceipt.Receipt")!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);

        Assert.Equal(0, result);
    }

    private static Assembly Compile(string source, IReadOnlyList<CompileArtifact> artifacts)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var trees = new[] { CSharpSyntaxTree.ParseText(source, parseOptions, "Receipt.cs") }
            .Concat(artifacts.Select(artifact => CSharpSyntaxTree.ParseText(artifact.Content, parseOptions, artifact.RelativePath)));
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITimeline).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(static path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "GeneratedSeekRuntime" + Guid.NewGuid().ToString("N"),
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }
}
