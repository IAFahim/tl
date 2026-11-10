using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Gen.CSharp.Tests;

public sealed class NewStyleAnalyzerTests
{
    const string Source = """
        using Tl;
        namespace Portable;
        [TlState] public partial struct Push { public float X; }
        [TlClip("Push")] public struct PushClip { public float X; }
        [TlKind] public struct PushKind
        {
            public static void Combine(ref float total, in float next) => total += next;
            public static void Apply(in float total, ref Push push) => push.X += total;
        }
        public struct PushTrack : ITlTrack<PushClip>
        {
            [Feeds(typeof(PushKind))]
            public static float Evaluate(in TlFrame<PushClip> frame) => frame.Clip.X;
        }
        """;

    [Fact]
    public void OrdinaryAnalyzerEmitsTheSharedFrontEndSystem()
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create("Portable.Pack", [CSharpSyntaxTree.ParseText(Source, parse, "Push.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: parse);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));
        Assert.Contains("TlAuto_Portable_Pack", generated);
        Assert.Contains("global::Portable.PushKind.Apply", generated);
        Assert.Contains("world.Tick(__direction);", generated);
        Assert.Contains("float __v = __acc[__e];", generated);
        Assert.Contains("global::Portable.PushTrack.Evaluate", generated);
        Assert.NotNull(output.GetTypeByMetadataName("Tl.Generated.TlAuto_Portable_Pack"));
        using var bytes = new MemoryStream();
        Assert.True(output.Emit(bytes).Success);
    }

    const string EventSource = """
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
        """;

    [Fact]
    public void TrackClipEventsLandInTheAutoSystem()
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create("Portable.Pack", [CSharpSyntaxTree.ParseText(EventSource, parse, "Push.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: parse);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics.Where(d => d.Severity != DiagnosticSeverity.Hidden));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var auto = output.GetTypeByMetadataName("Tl.Generated.TlAuto_Portable_Pack");
        Assert.NotNull(auto);
        var fields = auto!.GetMembers().OfType<IFieldSymbol>().Select(field => field.Type.ToDisplayString()).ToArray();
        Assert.Contains("Tl.Events<Portable.Strike>", fields);
        Assert.Contains("Tl.Events<Portable.Ring>", fields);
        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));
        Assert.Contains("world.Tick(__direction);", generated);
        Assert.Contains("global::Portable.PushTrack.OnClipStart", generated);
        Assert.Contains("global::Portable.BellTrack.OnClipEnd", generated);
        using var bytes = new MemoryStream();
        Assert.True(output.Emit(bytes).Success);
    }

    [Fact]
    public void RepeatedAnalyzerRunsKeepTheGeneratedSourceIdentical()
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create("Portable.Pack", [CSharpSyntaxTree.ParseText(Source, parse, "Push.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: parse);
        driver = driver.RunGenerators(compilation);
        var first = driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()).ToArray();
        driver = driver.RunGenerators(compilation);
        Assert.Equal(first, driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));
    }

    static IEnumerable<MetadataReference> References()
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return paths.Append(typeof(ITrack<,>).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path));
    }
}
