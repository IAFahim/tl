using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Gen.Tests;

public sealed class CompileGenerationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tl-gen-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void CacheHitPreservesFilesAndChangedOutputsAreRepaired()
    {
        var source = WriteSource("Timeline.cs", Declaration("Alpha", 10));
        var output = Path.Combine(_root, "generated");

        Assert.Equal(0, Run(output, source));
        var kernel = Path.Combine(output, "CompiledAlpha.g.cs");
        var shared = Path.Combine(output, KernelEmitter.SharedFileName);
        var manifest = Path.Combine(output, "TlGenCompile.manifest.json");
        var sourceList = Path.Combine(output, "TlGenCompile.sources");
        var kernelContent = File.ReadAllText(kernel);
        var sharedContent = File.ReadAllText(shared);
        var sourceListContent = File.ReadAllText(sourceList);
        var preservedTime = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(kernel, preservedTime);
        File.SetLastWriteTimeUtc(shared, preservedTime);
        File.SetLastWriteTimeUtc(sourceList, preservedTime);
        File.SetLastWriteTimeUtc(manifest, preservedTime);

        Assert.Equal(0, Run(output, source));
        Assert.Equal(preservedTime, File.GetLastWriteTimeUtc(kernel));
        Assert.Equal(preservedTime, File.GetLastWriteTimeUtc(shared));
        Assert.Equal(preservedTime, File.GetLastWriteTimeUtc(sourceList));
        Assert.Equal(preservedTime, File.GetLastWriteTimeUtc(manifest));

        File.AppendAllText(kernel, "changed");
        File.Delete(shared);
        File.WriteAllText(sourceList, sourceListContent + "CompiledOld.g.cs\n");

        Assert.Equal(0, Run(output, source));
        Assert.Equal(kernelContent, File.ReadAllText(kernel));
        Assert.Equal(sharedContent, File.ReadAllText(shared));
        Assert.Equal(sourceListContent, File.ReadAllText(sourceList));
    }

    [Fact]
    public void SourceChangesRegenerateTheKernel()
    {
        var source = WriteSource("Timeline.cs", Declaration("Alpha", 10));
        var output = Path.Combine(_root, "generated");

        Assert.Equal(0, Run(output, source));
        var kernel = Path.Combine(output, "CompiledAlpha.g.cs");
        Assert.Contains("public const uint Duration = 10u;", File.ReadAllText(kernel));

        File.WriteAllText(source, Declaration("Alpha", 11));

        Assert.Equal(0, Run(output, source));
        Assert.Contains("public const uint Duration = 11u;", File.ReadAllText(kernel));
    }

    [Fact]
    public void PreprocessorSymbolsSelectDeclarationsAndInvalidateTheCache()
    {
        var source = WriteSource("Timeline.cs", $$"""
            #if FAST_TIMELINE
            {{Declaration("Fast", 10)}}
            #else
            {{Declaration("Compact", 20)}}
            #endif
            """);
        var output = Path.Combine(_root, "generated");

        Assert.Equal(0, RunWithDefines(output, "TRACE;FAST_TIMELINE", source));
        Assert.True(File.Exists(Path.Combine(output, "CompiledFast.g.cs")));
        Assert.False(File.Exists(Path.Combine(output, "CompiledCompact.g.cs")));

        Assert.Equal(0, RunWithDefines(output, "TRACE", source));
        Assert.False(File.Exists(Path.Combine(output, "CompiledFast.g.cs")));
        var compact = Path.Combine(output, "CompiledCompact.g.cs");
        Assert.True(File.Exists(compact));
        Assert.Contains("public const uint Duration = 20u;", File.ReadAllText(compact));
    }

    [Fact]
    public void PartialTimelineEmitsOnlyItsNamedKernel()
    {
        var source = WriteSource("PulseTimeline.cs", PartialDeclaration("PulseTimeline", 12));
        var output = Path.Combine(_root, "generated");

        Assert.Equal(0, Run(output, source));
        Assert.True(File.Exists(Path.Combine(output, "PulseTimeline.g.cs")));
        Assert.False(File.Exists(Path.Combine(output, KernelEmitter.SharedFileName)));
        Assert.Equal(["PulseTimeline.g.cs"], File.ReadAllLines(Path.Combine(output, "TlGenCompile.sources")));
    }

    [Fact]
    public void MembershipRenameAndZeroDeclarationsSynchronizeOwnedOutputs()
    {
        var alpha = WriteSource("Alpha.cs", Declaration("Alpha", 10));
        var beta = WriteSource("Beta.cs", Declaration("Beta", 20));
        var output = Path.Combine(_root, "generated");

        Assert.Equal(0, Run(output, alpha, beta));
        var unowned = Path.Combine(output, "Keep.g.cs");
        File.WriteAllText(unowned, "keep");
        Assert.True(File.Exists(Path.Combine(output, "CompiledAlpha.g.cs")));
        Assert.True(File.Exists(Path.Combine(output, "CompiledBeta.g.cs")));
        var manifest = Path.Combine(output, "TlGenCompile.manifest.json");
        var preservedTime = new DateTime(2020, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(manifest, preservedTime);

        Assert.Equal(0, Run(output, beta, alpha));
        Assert.Equal(preservedTime, File.GetLastWriteTimeUtc(manifest));

        File.WriteAllText(alpha, Declaration("Gamma", 30));

        Assert.Equal(0, Run(output, alpha));
        Assert.False(File.Exists(Path.Combine(output, "CompiledAlpha.g.cs")));
        Assert.False(File.Exists(Path.Combine(output, "CompiledBeta.g.cs")));
        Assert.True(File.Exists(Path.Combine(output, "CompiledGamma.g.cs")));
        Assert.True(File.Exists(unowned));

        File.WriteAllText(alpha, "namespace Fix; internal static class Empty { }");

        Assert.Equal(0, Run(output, alpha));
        Assert.False(File.Exists(Path.Combine(output, "CompiledGamma.g.cs")));
        Assert.False(File.Exists(Path.Combine(output, KernelEmitter.SharedFileName)));
        Assert.True(File.Exists(unowned));

        Assert.Equal(0, Run(output));
        Assert.True(File.Exists(unowned));
    }

    [Fact]
    public void ModifiedObsoleteOutputIsPreservedAndExcludedFromSourceList()
    {
        var alpha = WriteSource("Alpha.cs", Declaration("Alpha", 10));
        var beta = WriteSource("Beta.cs", Declaration("Beta", 20));
        var output = Path.Combine(_root, "generated");

        Assert.Equal(0, Run(output, alpha, beta));
        var modified = Path.Combine(output, "CompiledBeta.g.cs");
        File.AppendAllText(modified, "changed");

        Assert.Equal(0, Run(output, alpha));

        Assert.True(File.Exists(modified));
        Assert.DoesNotContain("CompiledBeta.g.cs", File.ReadAllLines(Path.Combine(output, "TlGenCompile.sources")));
    }

    [Fact]
    public void IdenticalSourcesAtDifferentPathsEmitIdenticalArtifacts()
    {
        var content = Declaration("Alpha", 10);
        var firstSource = WriteSource(Path.Combine("first", "Timeline.cs"), content);
        var secondSource = WriteSource(Path.Combine("second", "OtherName.cs"), content);
        var firstOutput = Path.Combine(_root, "first-output");
        var secondOutput = Path.Combine(_root, "second-output");

        Assert.Equal(0, Run(firstOutput, firstSource));
        Assert.Equal(0, Run(secondOutput, secondSource));

        Assert.Equal(
            File.ReadAllText(Path.Combine(firstOutput, "CompiledAlpha.g.cs")),
            File.ReadAllText(Path.Combine(secondOutput, "CompiledAlpha.g.cs")));
    }

    [Fact]
    public void DuplicateSourcePathsAreReadOnce()
    {
        var source = WriteSource("Timeline.cs", Declaration("Alpha", 10));
        var output = Path.Combine(_root, "generated");

        Assert.Equal(0, Run(output, source, source));
        Assert.True(File.Exists(Path.Combine(output, "CompiledAlpha.g.cs")));
    }

    [Fact]
    public void FailedAnalysisDoesNotReplaceTheSuccessfulCache()
    {
        var content = Declaration("Alpha", 10);
        var source = WriteSource("Timeline.cs", content);
        var output = Path.Combine(_root, "generated");

        Assert.Equal(0, Run(output, source));
        var manifest = Path.Combine(output, "TlGenCompile.manifest.json");
        var cached = File.ReadAllText(manifest);
        File.WriteAllText(source, content.Replace("static b =>", "b =>", StringComparison.Ordinal));

        Assert.Equal(2, Run(output, source));
        Assert.Equal(cached, File.ReadAllText(manifest));

        File.WriteAllText(source, content);
        Assert.Equal(0, Run(output, source));
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("[{\"relativePath\":null,\"contentHash\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\"}]")]
    [InlineData("[{\"relativePath\":\"CompiledAlpha.g.cs\",\"contentHash\":null}]")]
    public void InvalidManifestEntriesAreTreatedAsCacheMisses(string outputs)
    {
        var source = WriteSource("Timeline.cs", Declaration("Alpha", 10));
        var output = Path.Combine(_root, "generated");
        Directory.CreateDirectory(output);
        File.WriteAllText(
            Path.Combine(output, "TlGenCompile.manifest.json"),
            $"{{\"formatVersion\":2,\"cacheKey\":\"\",\"sourceListHash\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\",\"outputs\":{outputs}}}");

        Assert.Equal(0, Run(output, source));
        Assert.True(File.Exists(Path.Combine(output, "CompiledAlpha.g.cs")));
    }

    [Fact]
    public void SourceListMustMatchManifestOutputsOnCacheHit()
    {
        var source = WriteSource("Timeline.cs", Declaration("Alpha", 10));
        var output = Path.Combine(_root, "generated");
        Assert.Equal(0, Run(output, source));
        var sourceListPath = Path.Combine(output, "TlGenCompile.sources");
        var hostileSourceList = "../../Other.cs\n";
        File.WriteAllText(sourceListPath, hostileSourceList);
        var manifestPath = Path.Combine(output, "TlGenCompile.manifest.json");
        var manifest = File.ReadAllText(manifestPath);
        var sourceListHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(hostileSourceList)));
        var hashProperty = "\"sourceListHash\": \"";
        var hashStart = manifest.IndexOf(hashProperty, StringComparison.Ordinal) + hashProperty.Length;
        manifest = manifest[..hashStart] + sourceListHash + manifest[(hashStart + 64)..];
        File.WriteAllText(manifestPath, manifest);

        Assert.Equal(0, Run(output, source));
        Assert.Equal(
            ["CompiledAlpha.g.cs", KernelEmitter.SharedFileName],
            File.ReadAllLines(sourceListPath));
    }

    [Fact]
    public void MsBuildWithoutTimelineItemsPrunesPreviouslyOwnedOutputs()
    {
        var (projectDirectory, project, _) = CreateConsumerProject();

        Build(project, true);
        var manifest = Assert.Single(Directory.GetFiles(
            Path.Combine(projectDirectory, "obj"),
            "TlGenCompile.manifest.json",
            SearchOption.AllDirectories));
        var output = Path.GetDirectoryName(manifest)!;
        Assert.True(File.Exists(Path.Combine(output, "CompiledAlpha.g.cs")));
        var unowned = Path.Combine(output, "Keep.g.cs");
        File.WriteAllText(unowned, "namespace Keep; internal static class Value { }");

        Build(project, false);

        Assert.False(File.Exists(Path.Combine(output, "CompiledAlpha.g.cs")));
        Assert.False(File.Exists(Path.Combine(output, KernelEmitter.SharedFileName)));
        Assert.True(File.Exists(unowned));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MsBuildIgnoresUnownedGeneratedSourceWhenManifestCannotEstablishOwnership(bool corruptManifest)
    {
        var (projectDirectory, project, timeline) = CreateConsumerProject();
        Build(project, true);
        var manifest = Assert.Single(Directory.GetFiles(
            Path.Combine(projectDirectory, "obj"),
            "TlGenCompile.manifest.json",
            SearchOption.AllDirectories));
        var output = Path.GetDirectoryName(manifest)!;
        var stale = Path.Combine(output, "CompiledOld.g.cs");
        File.WriteAllText(stale, "this is invalid C#");
        if (corruptManifest)
            File.WriteAllText(manifest, "{");
        else
            File.Delete(manifest);
        File.WriteAllText(timeline, Declaration("Alpha", 11));

        Build(project, true);

        Assert.True(File.Exists(stale));
        var sourceList = File.ReadAllLines(Path.Combine(output, "TlGenCompile.sources"));
        Assert.Contains("CompiledAlpha.g.cs", sourceList);
        Assert.Contains(KernelEmitter.SharedFileName, sourceList);
        Assert.DoesNotContain("CompiledOld.g.cs", sourceList);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private string WriteSource(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static int Run(string output, params string[] sources)
        => RunWithDefines(output, "", sources);

    private static int RunWithDefines(string output, string defines, params string[] sources)
    {
        var args = new List<string> { "--compile", "--output", output, "--define", defines };
        foreach (var source in sources)
        {
            args.Add("--source");
            args.Add(source);
        }

        return GeneratorCli.Main(args.ToArray());
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "tl.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException();
    }

    private (string Directory, string Project, string Timeline) CreateConsumerProject()
    {
        var repository = FindRepositoryRoot();
        var projectDirectory = Path.Combine(_root, "consumer");
        Directory.CreateDirectory(projectDirectory);
        var timeline = Path.Combine(projectDirectory, "Timeline.cs");
        File.WriteAllText(timeline, Declaration("Alpha", 10));

        var generator = System.Security.SecurityElement.Escape(typeof(GeneratorCli).Assembly.Location)!;
        var coreProject = System.Security.SecurityElement.Escape(Path.Combine(repository, "src", "Tl.Core", "Tl.Core.csproj"))!;
        var targets = System.Security.SecurityElement.Escape(Path.Combine(repository, "src", "Tl.Gen", "build", "Tl.Gen.targets"))!;
        var project = Path.Combine(projectDirectory, "Consumer.csproj");
        File.WriteAllText(project, $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                <TlGenAssembly>{{generator}}</TlGenAssembly>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{{coreProject}}" />
                <Compile Include="Timeline.cs" Condition="'$(IncludeTimeline)' == 'true'" />
                <TlCompileTimeline Include="Timeline.cs" Condition="'$(IncludeTimeline)' == 'true'" />
              </ItemGroup>
              <Import Project="{{targets}}" />
            </Project>
            """);
        return (projectDirectory, project, timeline);
    }

    private static void Build(string project, bool includeTimeline)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        process.StartInfo.ArgumentList.Add("build");
        process.StartInfo.ArgumentList.Add(project);
        process.StartInfo.ArgumentList.Add("-m:1");
        process.StartInfo.ArgumentList.Add("-p:UseSharedCompilation=false");
        process.StartInfo.ArgumentList.Add("-p:NuGetAudit=false");
        process.StartInfo.ArgumentList.Add($"-p:IncludeTimeline={includeTimeline.ToString().ToLowerInvariant()}");
        process.StartInfo.ArgumentList.Add("--nologo");
        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, standardOutput + Environment.NewLine + standardError);
    }

    private static string Declaration(string name, uint end) => $$"""
        using Tl;
        using Tl.Compiled;

        namespace Fix;

        public readonly record struct FClip(float Amount);

        public readonly struct FTrack : IBlend<FClip>
        {
            public void Blend(in FClip first, in FClip second, float t, out FClip result)
                => result = first;
        }

        public static class Sites
        {
            internal static readonly CompiledTimelineInfo {{name}} = Timeline<FTrack, FClip>.Build(static b =>
            {
                var track = b.Track(new FTrack());
                b.Clip(in track, new FClip(1f), start: 0, end: {{end}}u);
            }).Compile();
        }
        """;

    private static string PartialDeclaration(string name, uint end) => $$"""
        using Tl;

        namespace Fix;

        public readonly record struct FClip(float Amount);

        public readonly struct FTrack : IBlend<FClip>
        {
            public void Blend(in FClip first, in FClip second, float t, out FClip result)
                => result = first;
        }

        public readonly partial struct {{name}} : ITimeline<FTrack, FClip>
        {
            public static void Define(scoped TimelineBuilder<FTrack, FClip> timeline)
            {
                var track = timeline.Track(new FTrack());
                timeline.Clip(in track, new FClip(1f), 0u, {{end}}u);
            }
        }
        """;
}
