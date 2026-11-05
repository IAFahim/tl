using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class KindBenchGenTests
{
    [Fact]
    public void KindBenchEmissionMatchesCommitted()
    {
        var source = EmissionTests.ReadResource("Tl.Systems.Tests.KindBenchDomain.Domain.cs");
        var emitted = SystemGeneration.Emit(KindEmissionTests.Compile(source, "KindSystems"), "KindBench");
        Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var actual = Assert.Single(emitted.Artifacts).Content.Replace("\r\n", "\n");
        var root = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(root, "tl.slnx")); i++) root = Path.GetDirectoryName(root)!;
        var path = Path.Combine(root, "benchmarks", "KindSystems", "Generated", "TlSystemsKindBench.g.cs");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_BENCH") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
        }
        Assert.True(File.Exists(path), "Missing benchmarks/KindSystems/Generated/TlSystemsKindBench.g.cs; regenerate with TL_UPDATE_BENCH=1.");
        var expected = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.True(actual == expected, $"Snapshot differs at first offset {EmissionTests.FirstDifference(actual, expected)}.");
    }
}
