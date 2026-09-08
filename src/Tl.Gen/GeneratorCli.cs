using System.Globalization;
using Tl.Gen.Analysis;
using Tl.Gen.CSharp;
using Tl.Gen.Model;

namespace Tl.Gen;

public static class GeneratorCli
{
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--compile")
            return RunCompileMode(args);

        string? inputFile = null;
        string? outputDir = null;
        string ns = "Tl.Generated";
        string name = "GeneratedTimelineDef";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--input" && i + 1 < args.Length)
                inputFile = args[++i];
            else if (args[i] == "--output" && i + 1 < args.Length)
                outputDir = args[++i];
            else if (args[i] == "--namespace" && i + 1 < args.Length)
                ns = args[++i];
            else if (args[i] == "--name" && i + 1 < args.Length)
                name = args[++i];
        }

        if (string.IsNullOrEmpty(outputDir))
        {
            Console.Error.WriteLine("Error: --output directory is required.");
            return 1;
        }

        Directory.CreateDirectory(outputDir);

        TimelineDefinition def;
        if (!string.IsNullOrEmpty(inputFile) && File.Exists(inputFile))
        {
            def = ParseDefinitionFile(inputFile, name, ns);
        }
        else
        {
            // Default built-in smoke definition
            def = new TimelineDefinition
            {
                Name = name,
                Namespace = ns,
                TrackTypeName = "AotTrack",
                ClipTypeName = "AotClip",
                BlendMethodBody = "result = new AotClip(first.Amount * (1f - t) + second.Amount * t);",
                Tracks =
                [
                    new TrackDefinition { Index = 0, TrackExpression = "new AotTrack(1)" },
                    new TrackDefinition { Index = 1, TrackExpression = "new AotTrack(2)" }
                ],
                Clips =
                [
                    new ClipDefinition { TrackIndex = 0, Start = 0, End = 10, PayloadExpression = "new AotClip(10f)" },
                    new ClipDefinition { TrackIndex = 0, Start = 5, End = 15, PayloadExpression = "new AotClip(20f)" },
                    new ClipDefinition { TrackIndex = 1, Start = 2, End = 8, PayloadExpression = "new AotClip(5f)" },
                    new ClipDefinition { TrackIndex = 1, Start = 10, End = 18, PayloadExpression = "new AotClip(15f)" },
                ]
            };
        }

        var plan = RegionAnalyzer.Analyze(def);
        var files = CSharpAdapter.Default.Emit(plan);

        foreach (var file in files)
        {
            var targetPath = Path.Combine(outputDir, file.RelativePath);
            File.WriteAllText(targetPath, file.Content);
            Console.WriteLine($"Generated {targetPath}");
        }

        return 0;
    }

    private static TimelineDefinition ParseDefinitionFile(string path, string name, string ns)
    {
        var tracks = new Dictionary<ushort, TrackDefinition>();
        var clips = new List<ClipDefinition>();
        string trackType = "AotTrack";
        string clipType = "AotClip";
        string? blendBody = null;
        bool loops = false;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var parts = trimmed.Split(':');
            var key = parts[0].Trim();
            if (key == "track_type" && parts.Length > 1)
            {
                trackType = parts[1].Trim();
            }
            else if (key == "clip_type" && parts.Length > 1)
            {
                clipType = parts[1].Trim();
            }
            else if (key == "blend" && parts.Length > 1)
            {
                blendBody = string.Join(':', parts.Skip(1)).Trim();
            }
            else if (key == "loops" && parts.Length > 1)
            {
                loops = bool.Parse(parts[1].Trim());
            }
            else if (key == "track")
            {
                var idx = ushort.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                var expr = parts.Length > 2 ? parts[2].Trim() : $"new {trackType}({idx})";
                tracks[idx] = new TrackDefinition
                {
                    Index = idx,
                    TrackExpression = expr
                };
            }
            else if (key == "clip")
            {
                var tIdx = ushort.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                var start = uint.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                var end = uint.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
                var val = float.Parse(parts[4].Trim(), CultureInfo.InvariantCulture);
                var expr = parts.Length > 5 ? parts[5].Trim() : $"new {clipType}({val.ToString(CultureInfo.InvariantCulture)}f)";
                clips.Add(new ClipDefinition
                {
                    TrackIndex = tIdx,
                    Start = start,
                    End = end,
                    PayloadExpression = expr,
                    NumericValue = val
                });
            }
        }

        blendBody ??= $"result = new {clipType}(first.Amount * (1f - t) + second.Amount * t);";

        return new TimelineDefinition
        {
            Name = name,
            Namespace = ns,
            TrackTypeName = trackType,
            ClipTypeName = clipType,
            BlendMethodBody = blendBody,
            Loops = loops,
            Tracks = tracks.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList(),
            Clips = clips
        };
    }

    // ---- .Compile() declaration scanning ----
    // Reads the compiling project's sources, interprets every
    // Timeline<TTrack,TClip>.Build(author).Compile() declaration, and emits
    // the shared compiled runtime plus one specialized kernel per
    // declaration. Non-eligible sites surface as TLGENxx errors naming the
    // constraint and pointing at the in-memory interpreter path.
    private static int RunCompileMode(string[] args)
    {
        string? outputDir = null;
        var sources = new List<string>();

        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
                outputDir = args[++i];
            else if (args[i] == "--source" && i + 1 < args.Length)
                sources.Add(args[++i]);
        }

        if (string.IsNullOrEmpty(outputDir))
        {
            Console.Error.WriteLine("Error: --output directory is required.");
            return 1;
        }

        Directory.CreateDirectory(outputDir);

        var compileSources = sources
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(static path => new CompileSource(path, File.ReadAllText(path)))
            .ToList();
        var cacheKey = CompileGenerationCache.GetKey(compileSources);
        var previous = CompileGenerationCache.Load(outputDir);
        if (CompileGenerationCache.IsHit(outputDir, cacheKey, previous))
        {
            Console.WriteLine($"TlGenCompile: cache hit -> {outputDir}");
            return 0;
        }

        var (declarations, diagnostics) = DeclarationReader.Read(
            compileSources.Select(static source => (source.Path, source.Content)).ToList());

        foreach (var diagnostic in diagnostics)
            Console.Error.WriteLine(diagnostic.ToString());

        if (diagnostics.Count > 0)
            return 2;

        if (declarations.Count == 0)
        {
            CompileGenerationCache.Synchronize(outputDir, cacheKey, [], previous);
            Console.WriteLine("TlGenCompile: no .Compile() declarations found.");
            return 0;
        }

        var artifacts = new List<CompileArtifact>(declarations.Count + 1);
        var emitted = 0;
        foreach (var declaration in declarations)
        {
            var plan = RegionAnalyzer.Analyze(declaration.Definition);
            var slots = WorkSlotMaterializer.ForRegions(plan);
            var kernel = KernelEmitter.EmitKernel(plan, slots, declaration.KernelName, declaration.KernelName, 0);
            artifacts.Add(new CompileArtifact(
                $"{declaration.KernelName}.g.cs",
                CompileGenerationCache.NormalizeSource(kernel)));
            Console.WriteLine(
                $"TlGenCompile: {declaration.KernelName} <- {Path.GetFileName(declaration.File)}:{declaration.Line} " +
                $"({declaration.TrackCount} tracks, {declaration.ClipCount} clips, duration {plan.Duration}, loops {plan.Definition.Loops}).");
            emitted++;
        }

        artifacts.Add(new CompileArtifact(
            KernelEmitter.SharedFileName,
            CompileGenerationCache.NormalizeSource(KernelEmitter.EmitSharedRuntime())));
        CompileGenerationCache.Synchronize(outputDir, cacheKey, artifacts, previous);
        Console.WriteLine($"TlGenCompile: {emitted} kernel(s) + shared runtime -> {outputDir}");
        return 0;
    }
}
