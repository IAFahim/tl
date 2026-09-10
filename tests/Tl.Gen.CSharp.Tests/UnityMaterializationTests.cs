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
        Assert.Equal(2, generated.Length);
        var content = string.Join("\n", generated.Select(File.ReadAllText));
        Assert.Contains("IJobEntity", content);
        Assert.Contains("ScheduleParallel", content);
        Assert.Contains("TimelineMovement.Select", content);
        Assert.Contains("[WithOptions(global::Unity.Entities.EntityQueryOptions.IgnoreComponentEnabledState)]", content);
        Assert.Contains("[WithAll(typeof(Catalog.Rows)", content);
        Assert.DoesNotContain("[global::Unity.Entities.With", content);
        Assert.DoesNotContain("namespace UnityFixture;", content);
        Assert.DoesNotContain("scoped", content);
        Assert.DoesNotContain("record struct", content);
        Assert.Equal("unity-entities", File.ReadLines(Path.Combine(output, CompileGenerationCache.ReportFileName))
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
        public readonly struct ValueJob : ITimelineJob<Track, Clip>
        {
            public static void Execute(in Frame<Track, Clip> frame, ref long value) { value += frame.Clip.Value; }
        }
        public readonly partial struct Attack : ITimeline
        {
            public static void Define(scoped Builder builder)
            {
                var track = builder.Track(new Track()).Use<ValueJob>();
                builder.Clip(track, new Clip(3), 0u, 2u);
            }
        }
        public readonly struct Rows { }
        public readonly partial struct Catalog : ITimelineCatalog
        {
            public static void Define(scoped CatalogBuilder builder)
            {
                builder.Schema<Rows>().Asset<Attack>();
            }
        }
        """;
}
