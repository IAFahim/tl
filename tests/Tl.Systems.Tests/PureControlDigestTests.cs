using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class PureControlDigestTests
{
    static readonly Lazy<Assembly> Host = new(Build);

    static byte[] Compile(string name, IEnumerable<string> sources, IEnumerable<MetadataReference> references, bool generate)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(name, sources.Select((s, i) => CSharpSyntaxTree.ParseText(s, parse, name + i + ".cs")), references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release, nullableContextOptions: NullableContextOptions.Enable));
        if (generate)
        {
            var emitted = SystemGeneration.Emit(compilation, "KindShape");
            Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static d => d.Code + ": " + d.Message)));
            foreach (var artifact in emitted.Artifacts) compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(artifact.Content, parse, artifact.RelativePath));
        }
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return stream.ToArray();
    }

    static Assembly Build()
    {
        var bake = typeof(LibraryBake).Assembly.Location;
        var references = EmissionTests.References().Where(r => r.Display != bake).Append(MetadataReference.CreateFromFile(bake)).ToList();
        var domain = Compile("KindShape", [EmissionTests.ReadResource("Tl.Systems.Tests.KindShapeBench.PureDomain.cs")], references, true);
        var host = Compile("PureControlHost", [EmissionTests.ReadResource("Tl.Systems.Tests.PureControls.PureControls.cs"), EmissionTests.ReadResource("Tl.Systems.Tests.PureControls.PureControlHost.cs")], references.Append(MetadataReference.CreateFromImage(domain)), false);
        var context = new AssemblyLoadContext("PureControls");
        context.LoadFromStream(new MemoryStream(domain));
        return context.LoadFromStream(new MemoryStream(host));
    }

    [Theory]
    [InlineData(0, 0x82D306A901444EE5UL)]
    [InlineData(1, 0x21169E0A2D902970UL)]
    [InlineData(2, 0xCE0C9000F350DCEAUL)]
    [InlineData(3, 0x6E94BB8ED2D6D448UL)]
    [InlineData(4, 0x353C6F3E47477A92UL)]
    [InlineData(5, 0xCCDA47EB590B8F13UL)]
    [InlineData(6, 0x9E4C223A745CCF3BUL)]
    [InlineData(7, 0x877438935C504D2DUL)]
    [InlineData(8, 0xC5E32D459217B66AUL)]
    [InlineData(9, 0x23D8BA11791E0662UL)]
    public void EachUnityControlScriptHashesLikeThePinnedDotNetDigest(int control, ulong pinned)
    {
        var controls = Host.Value.GetType("KindPure.PureControls", true)!;
        var first = (byte[])controls.GetMethod("Library")!.Invoke(null, [false])!;
        var second = (byte[])controls.GetMethod("Library")!.Invoke(null, [true])!;
        var digest = (ulong)Host.Value.GetType("KindPure.PureControlHost", true)!.GetMethod("Digest")!.Invoke(null, [control, first, second])!;
        Assert.True(pinned == digest, $"{Enum.GetName(controls.Assembly.GetType("KindPure.PureControl", true)!, control)} hashes {digest:X16}, pinned {pinned:X16}.");
    }
}
