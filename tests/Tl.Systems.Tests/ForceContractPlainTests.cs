using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class ForceContractPlainTests
{
    static readonly string[] Files = ["ForceBaking", "ForceClip", "ForceData", "ForceKind", "ForceSystem", "UnityStubs"];

    static CSharpCompilation Compile(IEnumerable<SyntaxTree> extra)
    {
        var options = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = Files.Select(name => CSharpSyntaxTree.ParseText(EmissionTests.ReadResource("Tl.Systems.Tests.ForceContract." + name + ".cs"), options, name + ".cs")).Concat(extra).ToList();
        return CSharpCompilation.Create("Tl.Unity.ForceContract", trees, EmissionTests.References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
    }

    [Fact]
    public void ThePlainGeneratorCompilesTheFiveForceContractFilesUnchanged()
    {
        var emitted = SystemGeneration.Emit(Compile([]), "Force");
        Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static d => d.Code + ": " + d.Message)));
        var all = string.Join("\n", emitted.Artifacts.Select(static a => a.Content));
        Assert.Contains("public ref partial struct TlAuto_Force", all);
        Assert.Contains("global::System.Span<global::Force> C0_Force;", all);
        Assert.Contains("global::ForceKind.Combine(ref __total, in __v);", all);
        Assert.Contains("global::ForceKind.Apply(in __total, ref __c0[__e]);", all);
        Assert.Contains("world.Tick(__direction);", all);
        Assert.Contains("global::Unity.Mathematics.float3 __v = default;", all);
        Assert.Contains("new global::Tl.TlFrame<global::ForceClip>", all);
        Assert.Contains("global::ForceTrack.Evaluate", all);
        var options = new CSharpParseOptions(LanguageVersion.Latest);
        var compiled = Compile(emitted.Artifacts.Select(a => CSharpSyntaxTree.ParseText(a.Content, options, a.RelativePath)));
        var errors = compiled.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
