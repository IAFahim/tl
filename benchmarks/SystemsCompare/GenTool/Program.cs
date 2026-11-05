// Provenance tool: runs the same SystemGeneration.Emit the sanctioned
// BenchGenTests path calls (same compilation shape, same id scheme) and writes
// the committed snapshot. SystemGeneration is internal, so this reaches it via
// reflection; emitted content is byte-identical to the test-path output.
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

if (args.Length < 4)
{
    Console.Error.WriteLine("usage: GenTool <assemblyName> <id> <outDir> <sources...>");
    return 2;
}

var assemblyName = args[0];
var id = args[1];
var outDir = args[2];
var trees = args[3..]
    .Select(static path => CSharpSyntaxTree.ParseText(
        File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.Latest), Path.GetFileName(path)))
    .ToArray();

var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
{
    if (loaded.IsDynamic) continue;
    try { if (loaded.Location.Length > 0) locations.Add(loaded.Location); }
    catch (NotSupportedException) { }
}
locations.Add(typeof(Tl.ITrack<,>).Assembly.Location);
locations.Add(typeof(object).Assembly.Location);
var references = locations.Select(static path => MetadataReference.CreateFromFile(path));

var compilation = CSharpCompilation.Create(
    assemblyName, trees, references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

// Public anchor type lives in the generator assembly; Emit itself is internal.
var generatorAssembly = typeof(Tl.Gen.CSharp.TimelineIncrementalGenerator).Assembly;
var systemGeneration = generatorAssembly.GetType("Tl.Gen.CSharp.SystemGeneration")
    ?? throw new InvalidOperationException("SystemGeneration type not found.");
var emit = systemGeneration.GetMethod("Emit", BindingFlags.Public | BindingFlags.Static)
    ?? throw new InvalidOperationException("Emit method not found.");
var result = emit.Invoke(null, [compilation, id])
    ?? throw new InvalidOperationException("Emit returned null.");
var resultType = result.GetType();
var diagnostics = (System.Collections.IEnumerable?)resultType.GetField("Item2")?.GetValue(result);
var failures = 0;
if (diagnostics is not null)
    foreach (var diagnostic in diagnostics)
    {
        Console.Error.WriteLine("GEN " + diagnostic);
        failures++;
    }
if (failures > 0) return 2;

var artifacts = (System.Collections.IEnumerable?)resultType.GetField("Item1")?.GetValue(result);
if (artifacts is null) throw new InvalidOperationException("Emit returned no artifacts.");
Directory.CreateDirectory(outDir);
foreach (var artifact in artifacts)
{
    var artifactType = artifact.GetType();
    var relativePath = (string?)artifactType.GetProperty("RelativePath")?.GetValue(artifact);
    var content = (string?)artifactType.GetProperty("Content")?.GetValue(artifact);
    if (relativePath is null || content is null) throw new InvalidOperationException("Artifact shape mismatch.");
    var fileName = Path.GetFileName(relativePath);
    File.WriteAllText(Path.Combine(outDir, fileName), content);
    Console.WriteLine("GEN wrote " + fileName);
}
return 0;
