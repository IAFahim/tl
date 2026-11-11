using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class IncrementalParityTests
{
    internal static void AssertParity(CSharpCompilation compilation)
    {
        var id = string.Concat(compilation.AssemblyName!.Select(static c => char.IsLetterOrDigit(c) ? c : '_'));
        var whole = SystemGeneration.Emit(compilation, id);
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Latest));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var found);
        Assert.Equal(whole.Diagnostics.Select(static d => d.Code + ": " + d.Message), found.Where(static d => d.Id.StartsWith("TLGEN", StringComparison.Ordinal)).Select(static d => d.Id + ": " + d.GetMessage()));
        var generated = driver.GetRunResult().GeneratedTrees.Where(static tree => Path.GetFileName(tree.FilePath).StartsWith("TlSystems", StringComparison.Ordinal)).ToDictionary(static tree => Path.GetFileName(tree.FilePath), static tree => tree.ToString());
        Assert.Equal(whole.Artifacts.OrderBy(static a => a.RelativePath, StringComparer.Ordinal).Select(static a => a.RelativePath), generated.Keys.OrderBy(static k => k, StringComparer.Ordinal));
        foreach (var artifact in whole.Artifacts)
            Assert.True(JobEmitter.Normalize(artifact.Content) == generated[artifact.RelativePath], $"{artifact.RelativePath} differs between the whole-compilation reader and the incremental models.");
    }

    [Fact]
    public void KindDomainMatches() => AssertParity(KindEmissionTests.Compile(KindEmissionTests.KindSource, "ParityKinds"));

    [Fact]
    public void IdleDomainMatches() => AssertParity(KindEmissionTests.Compile(EmissionTests.ReadResource("Tl.Systems.Tests.IdleDomain.IdleDomain.cs"), "ParityIdle"));

    [Fact]
    public void KindBenchDomainMatches() => AssertParity(KindEmissionTests.Compile(EmissionTests.ReadResource("Tl.Systems.Tests.KindBenchDomain.Domain.cs"), "ParityKindBench"));

    [Fact]
    public void EventsAutoSystemMatches() => AssertParity(KindEmissionTests.Compile("""
        using Tl;
        namespace Portable;
        [TlState] public partial struct Push { public float X; }
        [TlClip("Push")] public struct PushClip { public float X; public int Amount; }
        public struct Strike { public int Amount; }
        public struct Ring { public int Count; }
        [TlKind] public struct PushKind
        {
            public static void Combine(ref float total, in float next) => total += next;
            public static void Apply(in float total, ref Push push) => push.X += total;
        }
        public struct PushTrack : ITlTrack<PushClip>
        {
            [Feeds(typeof(PushKind))]
            public static float Evaluate(in TlFrame<PushClip> frame) => frame.Clip.X;
            public static void OnClipStart(in TlFrame<PushClip> frame, ref Events<Strike> strikes) => strikes.Raise(new Strike { Amount = frame.Clip.Amount });
        }
        public struct BellTrack : ITlTrack<PushClip>
        {
            public static void OnClipEnd(in TlFrame<PushClip> frame, ref Events<Ring> rings) => rings.Raise(new Ring { Count = 1 });
        }
        """, "ParityEvents"));
}
