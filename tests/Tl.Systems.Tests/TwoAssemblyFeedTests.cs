using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Tl.Systems.Tests;

public sealed partial class TwoAssemblyTests
{
    static Assembly HostFeedingTimes(int times)
    {
        var built = Assemblies.Value;
        const string feed = "new Tl.Generated.TlAuto_Tl_Unity_PushGame { C0_Wind = wind }.Run(world, players, forward);";
        var source = HostSource.Replace("if (feedFirst) " + feed, "if (feedFirst) { " + string.Concat(Enumerable.Repeat(feed + " ", times)) + "}");
        Assert.NotEqual(HostSource, source);
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var references = EmissionTests.References().Append(MetadataReference.CreateFromImage(built.PackImage)).Append(MetadataReference.CreateFromImage(built.GameImage));
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release);
        var compilation = CSharpCompilation.Create("PushHost" + times, [CSharpSyntaxTree.ParseText(source, parse, "PushHost" + times + ".cs")], references, options);
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return AssemblyLoadContext.GetLoadContext(built.Pack)!.LoadFromStream(new MemoryStream(stream.ToArray()));
    }

    [Fact]
    public void AForeignFeederThatRunsSeveralTimesBeforeItsOwnerCountsEachSpanOnce()
    {
        var built = Assemblies.Value;
        var library = Library(built.Pack, built.Game);
        var once = Play(built, library, true);
        Assert.Equal(once, Play(HostFeedingTimes(2), library, true));
        Assert.Equal(once, Play(HostFeedingTimes(9), library, true));
    }
}
