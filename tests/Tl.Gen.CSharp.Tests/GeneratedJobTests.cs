using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class GeneratedJobTests
{
    internal const string Source = """
        using System;
        using Tl;
        namespace JobFixture;
        public readonly record struct ClipA(int Value);
        public readonly record struct ClipB(int Value);
        public readonly struct TrackA : IBlend<ClipA>
        {
            public void Blend(in ClipA first, in ClipA second, float factor, out ClipA result)
                => result = new ClipA((int)(first.Value + (second.Value - first.Value) * factor));
        }
        public readonly struct TrackB : IBlend<ClipB>
        {
            public void Blend(in ClipB first, in ClipB second, float factor, out ClipB result) => result = first;
        }
        public readonly struct JobA : ITimelineJob<TrackA, ClipA>
        {
            public static void Execute(in Frame<TrackA, ClipA> frame, in long bias, ref long value)
                => value = unchecked(value * 31 + frame.Direction * frame.Clip.Value + bias);
        }
        public readonly struct JobB : ITimelineJob<TrackB, ClipB>
        {
            public static void Execute(in Frame<TrackB, ClipB> frame, in long bias, ref long value)
                => value = unchecked(value * 37 + frame.Direction * frame.Clip.Value + bias);
        }
        public readonly partial struct Combo : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var first = builder.Track(new TrackA()).Use<JobA>();
                var middle = builder.Track(new TrackB()).Use<JobB>();
                var last = builder.Track(new TrackA()).Use<JobA>();
                builder.Clip(first, new ClipA(1), 0u, 3u);
                builder.Clip(middle, new ClipB(2), 1u, 2u);
                builder.Clip(last, new ClipA(3), 0u, 3u);
            }
        }
        public readonly partial struct Short : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new TrackB()).Use<JobB>();
                builder.Clip(track, new ClipB(5), 0u, 1u);
            }
        }
        public readonly struct MixedRows;
        public readonly partial struct Combat : ITimelineCatalog
        {
            public static void Define(scoped CatalogBuilder builder)
            {
                builder.Schema<MixedRows>().Asset<Combo>().Asset<Short>();
            }
        }
        public static class Receipt
        {
            public static long Run()
            {
                const int count = 10000;
                var states = new Combat.State[count];
                var bias = new long[count];
                var actual = new long[count];
                var expected = new long[count];
                for (var row = 0; row < count; row++)
                {
                    states[row] = new Combat.State((row & 1) == 0 ? Combat.Asset.Combo : Combat.Asset.Short);
                    bias[row] = row;
                }
                var query = new Combat.Query().MixedRows(states, bias, actual);
                query.Tick(200000, int.MaxValue);
                for (var row = 0; row < count; row++)
                {
                    var duration = (row & 1) == 0 ? 3 : 1;
                    for (var tick = 0; tick < duration; tick++)
                        expected[row] = Oracle(expected[row], row, tick, duration == 3, false);
                    if (actual[row] != expected[row] || states[row].Position != duration)
                        throw new Exception("Forward order or shared commit mismatch.");
                }
                query.Tick(200003, int.MinValue);
                for (var row = 0; row < count; row++)
                {
                    var duration = (row & 1) == 0 ? 3 : 1;
                    for (var tick = duration - 1; tick >= 0; tick--)
                        expected[row] = Oracle(expected[row], row, tick, duration == 3, true);
                    if (actual[row] != expected[row] || states[row].Position != 0 || bias[row] != row)
                        throw new Exception("Reverse order, input or shared commit mismatch.");
                }
                query.Tick(0, 0);
                default(Combat.MixedRowsQuery).Tick(0, int.MinValue);
                query.Tick(0, 1);
                query.Tick(1, -1);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (var repeat = 0; repeat < 128; repeat++)
                {
                    query.Tick(0, 1);
                    query.Tick(1, -1);
                }
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                if (allocated != 0) throw new Exception("Warm query allocated " + allocated);
                return actual[0] ^ actual[count - 1];
            }
            private static long Oracle(long value, long bias, int tick, bool combo, bool reverse)
            {
                unchecked
                {
                    if (!combo) return value * 37 + (reverse ? -5 : 5) + bias;
                    value = value * 31 + (reverse ? -3 : 1) + bias;
                    if (tick == 1) value = value * 37 + (reverse ? -2 : 2) + bias;
                    return value * 31 + (reverse ? -1 : 3) + bias;
                }
            }
            public static void InvalidColumns()
            {
                var states = new[] { new Combat.State(Combat.Asset.Short) };
                var bias = new long[1];
                var value = new long[1];
                var query = new Combat.Query().MixedRows(states, bias, value);
                states[0] = new Combat.State((Combat.Asset)99);
                var rejected = false;
                try { query.Tick(0); } catch (ArgumentException) { rejected = true; }
                if (!rejected || value[0] != 0 || states[0].Position != 0)
                    throw new Exception("Mutable route bypassed whole-schema validation.");
                states[0] = new Combat.State(Combat.Asset.Short);
                rejected = false;
                try { _ = new Combat.Query().MixedRows(states, bias, bias); }
                catch (ArgumentException) { rejected = true; }
                if (!rejected) throw new Exception("Aliased columns were accepted.");
                rejected = false;
                try { _ = new Combat.Query().MixedRows(states, Array.Empty<long>(), value); }
                catch (ArgumentException) { rejected = true; }
                if (!rejected) throw new Exception("Mismatched lengths were accepted.");
            }
        }
        """;

    [Fact]
    public void RealGeneratedQueriesPreserveOrderSharedPlaybackAndBorrowedStorage()
    {
        var assembly = Generate(Source);
        var result = assembly.GetType("JobFixture.Receipt")!.GetMethod("Run")!.Invoke(null, null);
        Assert.IsType<long>(result);
        Assert.Equal(14, assembly.GetType("JobFixture.Combo")!.GetProperty("StaticDataBytes")!.GetValue(null));
        assembly.GetType("JobFixture.Receipt")!.GetMethod("InvalidColumns")!.Invoke(null, null);
    }

    [Fact]
    public void GeneratedBoundedExpressionsDoNotDependOnAuthoringUsings()
    {
        const string source = """
            using Tl;
            namespace Imported
            {
                public enum Mode : byte { None }
            }
            namespace Consumer
            {
                using Imported;
                public readonly struct Clip
                {
                    public readonly int Value;
                    public Clip(string value) => Value = value.Length;
                }
                public readonly record struct Track(Mode Mode) : IBlend<Clip>
                {
                    public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
                }
                public readonly struct Job : ITimelineJob<Track, Clip>
                {
                    public static void Execute(in Frame<Track, Clip> frame) { }
                }
                public readonly partial struct Qualified : ITimeline
                {
                    public static void Define(scoped Builder builder)
                    {
                        var track = builder.Track(new Track(default(Mode))).Use<Job>();
                        builder.Clip(track, new Clip(nameof(Imported.Mode)), 0u, 1u);
                    }
                }
            }
            """;

        var assembly = Generate(source);

        Assert.NotNull(assembly.GetType("Consumer.Qualified"));
    }

    [Fact]
    public void OneTickOverlapEmitsTheDefinedMidpointBlend()
    {
        const string source = """
            using Tl;
            namespace OneTick;
            public readonly record struct Clip(int Value);
            public readonly struct Track : IBlend<Clip>
            {
                public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
            }
            public readonly struct Job : ITimelineJob<Track, Clip>
            {
                public static void Execute(in Frame<Track, Clip> frame) { }
            }
            public readonly partial struct Asset : ITimeline
            {
                public static void Define(scoped Builder builder)
                {
                    var track = builder.Track(new Track()).Use<Job>();
                    builder.Clip(track, new Clip(1), 0u, 1u);
                    builder.Clip(track, new Clip(2), 0u, 1u);
                }
            }
            """;

        var assembly = Generate(source);

        Assert.NotNull(assembly.GetType("OneTick.Asset"));
    }

    internal static Assembly Generate(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITimeline).Assembly.Location).Distinct(StringComparer.Ordinal)
            .Select(static path => MetadataReference.CreateFromFile(path));
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("JobFixture" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, options, "Jobs.cs")], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics);
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics) + "\n" + string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(static tree => tree.ToString())));
        return Assembly.Load(stream.ToArray());
    }
}
