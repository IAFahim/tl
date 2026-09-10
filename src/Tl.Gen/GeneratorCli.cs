using Tl.Gen.Analysis;
using Tl.Gen.CSharp;
using System.Security.Cryptography;

namespace Tl.Gen;

public static class GeneratorCli
{
    public static int Main(string[] args)
    {
        if (args is not ["--compile", ..])
        {
            Console.Error.WriteLine("TLGEN00: --compile is required.");
            return 1;
        }

        string? output = null;
        var paths = new List<string>();
        var references = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        var symbols = new SortedSet<string>(StringComparer.Ordinal);
        for (var index = 1; index < args.Length; index++)
        {
            if (args[index] == "--output" && index + 1 < args.Length)
                output = args[++index];
            else if (args[index] == "--source" && index + 1 < args.Length)
                paths.Add(args[++index]);
            else if (args[index] == "--source-list" && index + 1 < args.Length)
                paths.AddRange(File.ReadAllLines(args[++index]));
            else if (args[index] == "--reference-list" && index + 1 < args.Length)
                references.AddRange(File.ReadAllLines(args[++index]));
            else if (args[index] == "--option-list" && index + 1 < args.Length)
                ReadOptions(args[++index], options);
            else if (args[index] == "--define" && index + 1 < args.Length)
                AddSymbols(args[++index], symbols);
            else
            {
                Console.Error.WriteLine($"TLGEN00: invalid argument '{args[index]}'.");
                return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            Console.Error.WriteLine("TLGEN00: --output is required.");
            return 1;
        }

        var sources = paths
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(static path => new CompileSource(path, File.ReadAllText(path)))
            .ToArray();
        var defines = symbols.ToArray();
        var semanticInputs = references
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(static path => path + "=" + ReferenceIdentity(path))
            .Concat(options.OrderBy(static pair => pair.Key, StringComparer.Ordinal).Select(static pair => pair.Key + "=" + pair.Value))
            .ToArray();
        var key = CompileGenerationCache.GetKey(sources, defines, semanticInputs);
        var previous = CompileGenerationCache.Load(output);
        if (CompileGenerationCache.IsHit(output, key, previous))
        {
            Console.WriteLine($"TlGenCompile: cache hit; report {Path.Combine(output, CompileGenerationCache.ReportFileName)}");
            return 0;
        }

        var settings = new HeterogeneousCompilationSettings
        {
            ReferencePaths = references,
            LanguageVersion = options.GetValueOrDefault("language-version", "preview"),
            Nullable = options.GetValueOrDefault("nullable", "enable"),
            AllowUnsafe = Boolean(options.GetValueOrDefault("allow-unsafe")),
            CheckOverflow = Boolean(options.GetValueOrDefault("check-overflow")),
        };
        var (timelines, diagnostics) = HeterogeneousReader.Read(
            sources.Select(static source => (source.Path, source.Content)).ToArray(),
            defines,
            settings);
        foreach (var diagnostic in diagnostics)
            Console.Error.WriteLine(diagnostic);
        if (diagnostics.Count != 0)
            return 2;

        var ordered = timelines
            .OrderBy(static timeline => timeline.Namespace, StringComparer.Ordinal)
            .ThenBy(static timeline => timeline.Name, StringComparer.Ordinal)
            .ToArray();
        if (ordered.Length > ushort.MaxValue + 1)
        {
            Console.Error.WriteLine("TLGEN49: a compilation may contain at most 65536 timelines.");
            return 2;
        }
        var artifacts = HeterogeneousEmitter.EmitCompilation(ordered)
            .Select(static artifact => artifact with
            {
                Content = CompileGenerationCache.NormalizeSource(artifact.Content),
            })
            .ToArray();
        var report = Report(ordered, artifacts);
        CompileGenerationCache.Synchronize(output, key, artifacts, report, previous);
        var bytes = artifacts.Sum(static artifact => System.Text.Encoding.UTF8.GetByteCount(artifact.Content));
        Console.WriteLine($"TlGenCompile: {ordered.Length} timeline(s), {artifacts.Length} source file(s), {bytes:N0} UTF-8 B; report {Path.Combine(output, CompileGenerationCache.ReportFileName)}");
        return 0;
    }

    private static string Report(
        IReadOnlyList<Tl.Gen.Model.HeterogeneousTimeline> timelines,
        IReadOnlyList<CompileArtifact> artifacts)
    {
        var writer = new System.Text.StringBuilder();
        var sourceBytes = artifacts.Sum(static artifact => System.Text.Encoding.UTF8.GetByteCount(artifact.Content));
        var schemas = artifacts.Count(static artifact => artifact.RelativePath.StartsWith("TlSchema", StringComparison.Ordinal));
        var modules = timelines.Count == 0 ? 0 : (timelines.Count + byte.MaxValue) / 256;
        writer.AppendLine("format\t1");
        writer.AppendLine("backend\tcsharp");
        writer.AppendLine($"timelines\t{timelines.Count}");
        writer.AppendLine($"generated-source-files\t{artifacts.Count}");
        writer.AppendLine($"generated-source-utf8-bytes\t{sourceBytes}");
        writer.AppendLine($"declared-shared-dispatch-value-bytes\t{schemas * 512 + modules}");
        writer.AppendLine("runtime-registry-bytes\tglobal::Tl.Timeline.RegistryRetainedBytes");
        for (var index = 0; index < timelines.Count; index++)
        {
            var timeline = timelines[index];
            var qualified = timeline.Namespace.Length == 0 ? timeline.Name : timeline.Namespace + "." + timeline.Name;
            writer.Append("timeline\t").Append(qualified)
                .Append("\ttracks=").Append(timeline.Tracks.Count)
                .Append("\tclips=").Append(timeline.Clips.Count)
                .Append("\tregions=").Append(HeterogeneousEmitter.RegionCount(timeline))
                .Append("\tduration=").Append(timeline.Duration)
                .Append("\tloops=").Append(timeline.Loops ? "true" : "false")
                .Append("\tstatic-data-bytes=").Append(qualified).AppendLine(".StaticDataBytes");
        }
        foreach (var artifact in artifacts.OrderBy(static artifact => artifact.RelativePath, StringComparer.Ordinal))
            writer.Append("artifact\t").Append(artifact.RelativePath).Append("\tutf8-bytes=")
                .AppendLine(System.Text.Encoding.UTF8.GetByteCount(artifact.Content).ToString(System.Globalization.CultureInfo.InvariantCulture));
        return writer.ToString();
    }

    private static void AddSymbols(string value, ISet<string> symbols)
    {
        foreach (var symbol in value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            symbols.Add(symbol);
    }

    private static void ReadOptions(string path, IDictionary<string, string> options)
    {
        foreach (var line in File.ReadAllLines(path))
        {
            var separator = line.IndexOf('=');
            if (separator > 0)
                options[line[..separator]] = line[(separator + 1)..];
        }
    }

    private static bool Boolean(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static string ReferenceIdentity(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return exception.GetType().Name;
        }
    }
}
