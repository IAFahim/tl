using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.CSharp.Analysis;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class EmissionTests
{
    internal static readonly string ToolboxSource = ReadResource("Tl.Systems.Tests.Toolbox.Toolbox.cs");
    internal static readonly string ExpectedSnapshot = ReadResource("Tl.Systems.Tests.Generated.TlSystemsSnapshot.g.cs").Replace("\r\n", "\n");

    internal static string ReadResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal static CSharpCompilation ToolboxCompilation(string assemblyName = "Tl.Systems.Tests")
    {
        var tree = CSharpSyntaxTree.ParseText(ToolboxSource, new CSharpParseOptions(LanguageVersion.Latest), "Toolbox.cs");
        return CSharpCompilation.Create(assemblyName, [tree], References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
    }

    internal static IEnumerable<MetadataReference> References()
    {
        var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;
            try
            {
                if (assembly.Location.Length > 0) locations.Add(assembly.Location);
            }
            catch (NotSupportedException) { }
        }
        locations.Add(typeof(ITrack<,>).Assembly.Location);
        locations.Add(typeof(object).Assembly.Location);
        return locations.Select(static path => MetadataReference.CreateFromFile(path));
    }

    [Fact]
    public void ToolboxProducesZeroDiagnostics()
    {
        var compilation = ToolboxCompilation();
        var read = SystemReader.Read(compilation);
        Assert.True(read.Diagnostics.Count == 0, string.Join("\n", read.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
    }

    [Fact]
    public void EmittedSourceParsesAsCSharp9()
    {
        var compilation = ToolboxCompilation();
        var emitted = SystemGeneration.Emit(compilation, "Test");
        Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var artifact = Assert.Single(emitted.Artifacts);
        var parsed = CSharpSyntaxTree.ParseText(artifact.Content, new CSharpParseOptions(LanguageVersion.CSharp9), artifact.RelativePath);
        var errors = parsed.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors.Select(static diagnostic => diagnostic.ToString())));
    }

    [Fact]
    public void EmittedSourceMatchesSnapshot()
    {
        var compilation = ToolboxCompilation();
        var emitted = SystemGeneration.Emit(compilation, "Test");
        Assert.True(emitted.Diagnostics.Count == 0, string.Join("\n", emitted.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var artifact = Assert.Single(emitted.Artifacts);
        var actual = artifact.Content.Replace("\r\n", "\n");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1")
        {
            var root = AppContext.BaseDirectory;
            for (var i = 0; i < 8 && !File.Exists(Path.Combine(root, "Tl.Systems.Tests.csproj")); i++) root = Path.GetDirectoryName(root)!;
            File.WriteAllText(Path.Combine(root, "Generated", "TlSystemsSnapshot.g.cs"), actual);
        }
        Assert.True(actual == ExpectedSnapshot, $"Snapshot differs at first offset {FirstDifference(actual, ExpectedSnapshot)}.");
    }

    internal static int FirstDifference(string left, string right)
    {
        var bound = Math.Min(left.Length, right.Length);
        for (var i = 0; i < bound; i++) if (left[i] != right[i]) return i;
        return bound;
    }
}
