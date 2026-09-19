using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class ResolverTests
{
    [Fact]
    public void FromAssembliesDeduplicatesAndResolvesFromExactlyTheGivenAssemblies()
    {
        var fixtures = typeof(Tlb.AlphaTrack).Assembly;
        var resolver = BakerAssemblyResolver.FromAssemblies([fixtures, fixtures]);

        var resolved = resolver.ResolveType("Tlb", "AlphaTrack", "Tl.Bake.Tests", "ResolverTests");
        Assert.Equal(typeof(Tlb.AlphaTrack), resolved);
        Assert.Equal(fixtures, Assert.Single(resolver.ReferencedAssemblies));
    }

    [Fact]
    public void ResolutionNeverLeavesTheGivenAssemblies()
    {
        var resolver = BakerAssemblyResolver.FromAssemblies([typeof(TimelineBaker).Assembly]);

        var ex = Assert.Throws<BakeDiagnosticException>(() => resolver.ResolveType("Tlb", "AlphaTrack", null, "ResolverTests"));
        Assert.Contains("no loaded type named (Tlb, AlphaTrack) for ResolverTests", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssemblyMismatchReportsTheLoadedAssembliesDiagnostic()
    {
        var resolver = BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]);

        var ex = Assert.Throws<BakeDiagnosticException>(() => resolver.ResolveType("Tlb", "AlphaTrack", "Tl.Gen.Tlb", "ResolverTests"));
        Assert.Contains("assembly 'Tl.Gen.Tlb' does not contain (Tlb, AlphaTrack) for ResolverTests", ex.Message, StringComparison.Ordinal);
        Assert.Contains("matching types exist in assembly(es): Tl.Bake.Tests", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AbsentTypeReportsTheUnknownTypeDiagnostic()
    {
        var resolver = BakerAssemblyResolver.FromAssemblies([typeof(Tlb.AlphaTrack).Assembly]);

        var ex = Assert.Throws<BakeDiagnosticException>(() => resolver.ResolveType("Tlb", "NoSuchTrack", "Tl.Bake.Tests", "track 0"));
        Assert.Contains("unknown/unresolvable type: no loaded type named (Tlb, NoSuchTrack) for track 0", ex.Message, StringComparison.Ordinal);
    }
}
