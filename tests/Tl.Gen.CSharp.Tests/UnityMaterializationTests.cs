using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class UnityMaterializationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tl-unity-materializer-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void UnityBackendWritesStablePhysicalCSharpNineJobs()
    {
        Directory.CreateDirectory(_directory);
        var source = Path.Combine(_directory, "Catalog.tl");
        var output = Path.Combine(_directory, "Generated");
        var references = Path.Combine(_directory, "references.txt");
        File.WriteAllText(source, Source);
        File.WriteAllLines(references,
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(ITimeline).Assembly.Location).Distinct(StringComparer.Ordinal));

        Assert.Equal(0, GeneratorCli.Main([
            "--compile", "--backend", "unity-entities", "--output", output,
            "--source", source, "--reference-list", references,
        ]));

        var generated = Directory.GetFiles(output, "*.g.cs").OrderBy(static path => path, StringComparer.Ordinal).ToArray();
        Assert.Equal(4, generated.Length);
        foreach (var path in generated)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.CSharp9), path);
            Assert.DoesNotContain(tree.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }
        var content = string.Join("\n", generated.Select(File.ReadAllText));
        Assert.Contains("IJobChunk", content);
        Assert.Contains("IJobEntity", content);
        Assert.Contains("public struct Scheduler", content);
        Assert.Contains("public void OnCreate(ref global::Unity.Entities.SystemState state)", content);
        Assert.True(content.IndexOf("__tlOperation0Data.Init(ref state, true);", StringComparison.Ordinal)
            < content.IndexOf("public void Tick(ref global::Unity.Entities.SystemState state", StringComparison.Ordinal));
        Assert.Contains("state.GetEntityQuery(global::Unity.Entities.ComponentType.ReadWrite<Catalog.TimelineComponent>())", content);
        Assert.Contains("internal struct __TlCatalogClearJob : global::Unity.Entities.IJobChunk", content);
        Assert.Contains("internal struct __TlCatalogSelect0Job : global::Unity.Entities.IJobChunk", content);
        Assert.Contains("chunk.Has(ref SchemaHandle)", content);
        Assert.Contains("global::Unity.Entities.IEnableableComponent", content);
        Assert.Contains("chunk.IsComponentEnabled(ref SchemaHandle, index)", content);
        Assert.Contains("chunk.Has(ref Slot", content);
        Assert.Contains("public struct Role0Bias : global::Unity.Entities.IComponentData", content);
        Assert.Contains("public struct Role2Secondary : global::Unity.Entities.IComponentData", content);
        Assert.Contains("in Catalog.Role0Bias @bias", content);
        Assert.Contains("in Catalog.Role2Secondary @secondary", content);
        Assert.Contains("in @bias.Value", content);
        Assert.Contains("in @secondary.Value", content);
        Assert.Contains("for (ushort stage = 0; stage < 5; stage++)", content);
        Assert.Contains("__TlCatalogOperation0Job", content);
        Assert.Contains("__TlCatalogOperation1Job", content);
        Assert.Contains("__TlCatalogOperation2Job", content);
        Assert.Contains("ExecuteForward", content);
        Assert.Contains("ExecuteReverse", content);
        Assert.Contains(".Blend(in __tlData", content);
        Assert.Contains("TimelineMovement.Select", content);
        Assert.Contains("[WithOptions(global::Unity.Entities.EntityQueryOptions.IgnoreComponentEnabledState)]", content);
        Assert.DoesNotContain("Stage0", content);
        Assert.DoesNotContain("WithAll", content);
        var clear = content.Substring(content.IndexOf("internal struct __TlCatalogClearJob", StringComparison.Ordinal));
        clear = clear.Substring(0, clear.IndexOf("internal struct __TlCatalogSelect0Job", StringComparison.Ordinal));
        Assert.DoesNotContain("SchemaHandle", clear);
        Assert.DoesNotContain("Slot0Handle", clear);
        Assert.DoesNotContain("[global::Unity.Entities.With", content);
        Assert.DoesNotContain("namespace UnityFixture;", content);
        Assert.DoesNotContain("scoped", content);
        Assert.DoesNotContain("record struct", content);
        var report = File.ReadAllText(Path.Combine(output, CompileGenerationCache.ReportFileName));
        Assert.Contains("catalog\tUnityFixture.Catalog\tschemas=2\tassets=3\toperation-kinds=3\tmax-stages=5\tscheduled-jobs-per-step=19", report);
        Assert.Equal("unity-entities", report.Split('\n')
            .Single(static line => line.StartsWith("backend\t", StringComparison.Ordinal)).Split('\t')[1]);

        var timestamp = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        foreach (var path in generated)
            File.SetLastWriteTimeUtc(path, timestamp);
        Assert.Equal(0, GeneratorCli.Main([
            "--compile", "--backend", "unity-entities", "--output", output,
            "--source", source, "--reference-list", references,
        ]));
        Assert.All(generated, path => Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path)));
    }

    [Fact]
    public void PackageReadmeAuthoringSampleCompilesAndMaterializesItsDocumentedSlots()
    {
        Directory.CreateDirectory(_directory);
        var readme = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tl.Unity.README.md"));
        const string fence = "```csharp\n";
        var sourceStart = readme.IndexOf(fence, StringComparison.Ordinal);
        Assert.True(sourceStart >= 0);
        sourceStart += fence.Length;
        var sourceEnd = readme.IndexOf("\n```", sourceStart, StringComparison.Ordinal);
        Assert.True(sourceEnd >= 0);

        var source = Path.Combine(_directory, "Combat.tl");
        var output = Path.Combine(_directory, "Generated");
        var references = Path.Combine(_directory, "references.txt");
        File.WriteAllText(source, readme.Substring(sourceStart, sourceEnd - sourceStart));
        File.WriteAllLines(references,
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(ITimeline).Assembly.Location).Distinct(StringComparer.Ordinal));

        Assert.Equal(0, GeneratorCli.Main([
            "--compile", "--backend", "unity-entities", "--output", output,
            "--source", source, "--reference-list", references,
        ]));

        var content = string.Join("\n", Directory.GetFiles(output, "*.g.cs").OrderBy(static path => path, StringComparer.Ordinal).Select(File.ReadAllText));
        Assert.Contains("public struct Role0Bias : global::Unity.Entities.IComponentData", content);
        Assert.Contains("public struct Role1Trace : global::Unity.Entities.IComponentData", content);
        Assert.Contains("in Combat.Role0Bias @bias", content);
        Assert.Contains("ref Combat.Role1Trace @trace", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    private const string Source = """
        using Tl;
        namespace UnityFixture;
        public readonly struct Clip
        {
            public readonly int Value;
            public Clip(int value) { Value = value; }
        }
        public readonly struct Track : IBlend<Clip>
        {
            public void Blend(in Clip first, in Clip second, float factor, out Clip result) { result = first; }
        }
        public struct Trace { public long Value; }
        public struct Bias { public int Value; }
        public struct Scale { public short Value; }
        public readonly struct Hook : IHook
        {
            public static void Execute(in TimelineFrame frame, ref Trace trace) { trace.Value += 8; }
        }
        public readonly struct A : ITimelineJob<Track, Clip>
        {
            public static void Execute(in Frame<Track, Clip> frame, in Bias bias, ref Trace trace) { trace.Value += frame.Clip.Value + bias.Value; }
        }
        public readonly struct B : ITimelineJob<Track, Clip>
        {
            public static void Execute(in Frame<Track, Clip> frame, in Bias secondary, in Scale scale, ref Trace trace) { trace.Value += frame.Clip.Value * scale.Value + secondary.Value; }
        }
        public readonly partial struct Attack : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                builder.Before<Hook>();
                var first = builder.Track(new Track()).Use<A>();
                var middle = builder.Track(new Track()).Use<B>();
                var last = builder.Track(new Track()).Use<A>();
                builder.Clip(first, new Clip(1), 0u, 3u);
                builder.Clip(first, new Clip(3), 0u, 3u);
                builder.Clip(middle, new Clip(2), 0u, 3u);
                builder.Clip(last, new Clip(4), 0u, 3u);
                builder.After<Hook>();
            }
        }
        public readonly partial struct Loop : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new Track()).Use<A>();
                builder.Clip(track, new Clip(5), 0u, 1u);
                builder.Looping();
            }
        }
        public readonly partial struct Defense : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                builder.Before<Hook>();
                var first = builder.Track(new Track()).Use<B>();
                var last = builder.Track(new Track()).Use<A>();
                builder.Clip(first, new Clip(6), 0u, 2u);
                builder.Clip(last, new Clip(7), 0u, 2u);
                builder.After<Hook>();
            }
        }
        public readonly struct Rows { }
        public readonly struct LoopRows { }
        public readonly partial struct Catalog : ITimelineCatalog
        {
            public static void Define(scoped CatalogBuilder builder)
            {
                builder.Schema<Rows>().Asset<Attack>().Asset<Defense>();
                builder.Schema<LoopRows>().Asset<Loop>();
            }
        }
        """;
}
