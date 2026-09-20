using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;

namespace Play;

public sealed record LiveCompile(Assembly? Assembly, string Error, long CompileMs)
{
    public bool Ok => Assembly is not null;
}

public sealed class LiveRun
{
    public string[] Console = [];
    public ulong Checksum;
}

public static class LiveAuthoring
{
    static readonly List<Assembly> KeepAlive = [];
    static readonly Dictionary<string, Assembly> Compiled = new(StringComparer.Ordinal);
    static int _compiles;

    public const string DefaultSource = """
using Tl;

namespace Live;

public readonly record struct JumpClip(float Velocity);

public readonly record struct JumpTrack(float Scale) : IBlend<JumpClip>
{
    public void Blend(in JumpClip first, in JumpClip second, float factor, out JumpClip result)
        => result = new JumpClip(first.Velocity + (second.Velocity - first.Velocity) * factor);
}

public readonly struct MoveY : ITrack<JumpTrack, JumpClip>
{
    public static void OnActive(in Frame<JumpTrack, JumpClip> frame, ref float y)
        => y += frame.Direction * frame.Clip.Velocity * frame.Track.Scale;
}

public static class Play
{
    public static void Run(byte[] jumpTlb)
    {
        ushort jumpTimeline = TimelineAsset.Load(jumpTlb);
        var ids = new ushort[] { jumpTimeline, jumpTimeline, jumpTimeline, jumpTimeline };
        var tick = new ushort[4];
        var y = new float[4];

        Console.WriteLine("four characters jump, one call per frame:");
        for (var frame = 1; frame <= 30; frame++)
        {
            Timeline<JumpTrack, JumpClip>.Apply(ids, tick, true, y); Timeline.Advance(ids, tick, true);
            if (frame % 3 == 0)
                Console.WriteLine($"  tick {frame,2}   y = {y[0],4:0.0} m   {new string('#', Math.Max(0, (int)Math.Round(y[0] / 3)))}");
        }

        Console.WriteLine();
        Console.WriteLine("rewind walks the arc back exactly:");
        for (var frame = 0; frame < 30; frame++)
            { Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, tick, false, y); Timeline.Advance(jumpTimeline, tick, false); }
        Console.WriteLine($"  after 30 back ticks: y = {y[0]:0.0} m, tick = {tick[0]}");
    }
}
""";

    public const string DefaultTimelineJson = """
{
  "name": "jump", "duration": 30, "loop": true,
  "tracks": [
    {
      "name": "arc", "namespace": "Live", "type": "JumpTrack", "data": { "Scale": 1.0 },
      "clips": [
        { "name": "rise", "namespace": "Live", "type": "JumpClip", "start": 0, "end": 15, "data": { "Velocity": 2.0 } },
        { "name": "fall", "namespace": "Live", "type": "JumpClip", "start": 15, "end": 30, "data": { "Velocity": -2.0 } }
      ]
    }
  ]
}
""";

    public const string ReproducerTimelineJson = """
{ "name": "jump", "duration": 30, "loop": true,
  "tracks": [ { "name": "arc", "namespace": "Live", "type": "JumpTrack", "data": { "Scale": 1.0 },
    "clips": [
      { "name": "rise", "namespace": "Live", "type": "JumpClip", "start": 0, "end": 15, "data": { "Velocity": 2.0 } },
      { "name": "fall", "namespace": "Live", "type": "JumpClip", "start": 15, "end": 30, "data": { "Velocity": -3.0 } } ] } ] }
""";

    public const string RawBindingSource = """
using System.Threading;
using Tl;

namespace Live;

public readonly record struct AmountClip(float Amount);

public readonly record struct ScaleTrack(float Scale) : IBlend<AmountClip>
{
    public void Blend(in AmountClip first, in AmountClip second, float factor, out AmountClip result)
        => result = new AmountClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public static unsafe class Play
{
    static int _installed;

    public static void Run(byte[] scaleTlb)
    {
        if (Interlocked.Exchange(ref _installed, 1) == 0)
            PairRuntime<ScaleTrack, AmountClip>.Consume(&ExecuteAmount, &BindFloat);
        ushort timeline = TimelineAsset.Load(scaleTlb);
        var ids = new ushort[] { timeline, timeline, timeline, timeline };
        var tick = new ushort[4];
        var amount = new float[4];

        Console.WriteLine("raw function-pointer consumer, no ITrack, no generator binding:");
        for (var frame = 1; frame <= 24; frame++)
        {
            Timeline<ScaleTrack, AmountClip>.Apply(ids, tick, true, amount); Timeline.Advance(ids, tick, true);
            if (frame % 4 == 0)
                Console.WriteLine($"  tick {frame,2}   amount = {amount[0],6:0.0}");
        }
    }

    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    static void ExecuteAmount(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        AmountClip scratch = default;
        var frame = TickFrame.ToFrame<ScaleTrack, AmountClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
    }
}
""";

    public const string RawBindingTimelineJson = """
{
  "duration": 64,
  "loop": true,
  "tracks": [
    {
      "namespace": "Live",
      "type": "ScaleTrack",
      "data": { "Scale": 2 },
      "clips": [
        { "namespace": "Live", "type": "AmountClip", "data": { "Amount": 1.5 }, "start": 0, "end": 32 },
        { "namespace": "Live", "type": "AmountClip", "data": { "Amount": -2.5 }, "start": 40, "end": 64 }
      ]
    }
  ]
}
""";

    public static LiveCompile Compile(string source)
    {
        if (Compiled.TryGetValue(source, out var cached))
            return new LiveCompile(cached, "", 0);
        var watch = Stopwatch.StartNew();
        try
        {
            var references = new List<MetadataReference>(Basic.Reference.Assemblies.Net100.References.All);
            var embedded = EmbeddedTlCore();
            if (embedded is null)
                return new LiveCompile(null, "the host app does not embed the Tl.Core reference assembly (LiveRef.Tl.Core.dll)", watch.ElapsedMilliseconds);
            references.Add(MetadataReference.CreateFromStream(embedded, filePath: "Tl.Core.dll"));
            var parse = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release)
                .WithConcurrentBuild(false);
            var compilationName = "Live" + _compiles;
            _compiles++;
            var compilation = CSharpCompilation.Create(compilationName, [CSharpSyntaxTree.ParseText(source, parse), CSharpSyntaxTree.ParseText("global using System;", parse)], references, options);
            var driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator()]);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var generatorDiagnostics);
            var errors = new StringBuilder();
            foreach (var diagnostic in generatorDiagnostics.Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning).Take(20))
                errors.AppendLine(diagnostic.ToString());
            using var pe = new MemoryStream();
            var emit = generated.Emit(pe);
            if (errors.Length == 0 && !emit.Success)
                foreach (var diagnostic in emit.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning).Take(20))
                    errors.AppendLine(diagnostic.ToString());
            if (errors.Length > 0)
                return new LiveCompile(null, errors.ToString(), watch.ElapsedMilliseconds);
            pe.Position = 0;
            var context = new AssemblyLoadContext(compilationName, isCollectible: false);
            var assembly = context.LoadFromStream(pe);
            KeepAlive.Add(assembly);
            Compiled[source] = assembly;
            return new LiveCompile(assembly, "", watch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new LiveCompile(null, ex.Message, watch.ElapsedMilliseconds);
        }
    }

    static Stream? EmbeddedTlCore()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            try
            {
                var stream = asm.GetManifestResourceStream("LiveRef.Tl.Core.dll");
                if (stream is not null) return stream;
            }
            catch
            {
            }
        }
        return null;
    }

    public static string TypeJson(Assembly assembly) => TlbIntrospection.Introspect([assembly]);

    public static (byte[] Bytes, string Error) Bake(Assembly assembly, string timelineJson)
    {
        try
        {
            var resolver = BakerAssemblyResolver.FromAssemblies([assembly]);
            return (TimelineBaker.BakeJson(timelineJson, resolver), "");
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    public static (byte[][] Packages, string Error) BakeAll(Assembly assembly, string timelineJson)
    {
        try
        {
            using var document = JsonDocument.Parse(timelineJson);
            if (document.RootElement.ValueKind is not JsonValueKind.Array)
            {
                var (bytes, error) = Bake(assembly, timelineJson);
                if (bytes.Length == 0)
                    return ([], error);
                return ([bytes], "");
            }
            var resolver = BakerAssemblyResolver.FromAssemblies([assembly]);
            var packages = new List<byte[]>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                byte[] baked;
                try
                {
                    baked = TimelineBaker.BakeJson(element.GetRawText(), resolver);
                }
                catch (Exception ex)
                {
                    return ([], ex.Message);
                }
                if (baked.Length == 0)
                    return ([], "a timeline entry baked to zero bytes");
                packages.Add(baked);
            }
            return ([.. packages], "");
        }
        catch (JsonException ex)
        {
            return ([], ex.Message);
        }
    }

    public static string BakeStats(byte[] tlb)
    {
        var view = TlbMetadata.Read(tlb);
        return $"bytes={tlb.Length} types={view.Types.Count} pairs={view.PairTypes.Count} labels={view.Labels.Count}";
    }

    public static string BakeStats(byte[][] packages)
    {
        if (packages.Length == 1)
            return BakeStats(packages[0]);
        var total = 0;
        foreach (var package in packages)
            total += package.Length;
        return $"packages={packages.Length} bytes={total}";
    }

    public static LiveRun Run(Assembly assembly, byte[] tlb) => Run(assembly, [tlb]);

    public static LiveRun Run(Assembly assembly, byte[][] packages)
    {
        var run = new LiveRun();
        var console = new StringBuilder();
        var priorOut = Console.Out;
        var priorCulture = CultureInfo.CurrentCulture;
        var priorUICulture = CultureInfo.CurrentUICulture;
        Console.SetOut(new StringWriter(console, CultureInfo.InvariantCulture));
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            InvokeRun(assembly, packages);
        }
        finally
        {
            Console.SetOut(priorOut);
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUICulture;
        }
        run.Console = console.ToString().Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var hash = 14695981039346656037ul;
        foreach (var b in Encoding.UTF8.GetBytes(console.ToString()))
            hash = unchecked((hash ^ b) * 1099511628211ul);
        run.Checksum = hash;
        return run;
    }

    static void InvokeRun(Assembly assembly, byte[][] packages)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = [.. ex.Types.Where(t => t is not null)!];
        }
        MethodInfo? single = null;
        MethodInfo? batch = null;
        foreach (var type in types)
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "Run") continue;
                var parameters = method.GetParameters();
                if (parameters.Length != 1) continue;
                if (parameters[0].ParameterType == typeof(byte[])) single = method;
                else if (parameters[0].ParameterType == typeof(byte[][])) batch = method;
            }
        var entries = new List<MethodInfo>();
        if (single is not null) entries.Add(single);
        if (batch is not null) entries.Add(batch);
        if (entries.Count == 0)
            throw new InvalidOperationException("the compiled code has no public static Run(byte[] tlb) or Run(byte[][] tlbs) entry; add a playback entry that takes the baked timelines");
        if (entries.Count > 1)
        {
            var names = entries.Select(e => $"{e.DeclaringType?.FullName}.{e.Name}").OrderBy(n => n, StringComparer.Ordinal).Distinct();
            throw new InvalidOperationException($"ambiguous Run entry: {entries.Count} public static Run(byte[]) / Run(byte[][]) methods compiled ({string.Join(", ", names)}); keep exactly one playback entry");
        }
        var entryMethod = entries[0];
        if (entryMethod.GetParameters()[0].ParameterType == typeof(byte[][]))
        {
            InvokeEntry(entryMethod, packages);
            return;
        }
        if (packages.Length != 1)
            throw new InvalidOperationException($"the data pane holds {packages.Length} baked timelines but Run takes one byte[]; use Run(byte[][] tlbs) and load each package with TimelineAsset.Load");
        InvokeEntry(entryMethod, packages[0]);
    }

    static void InvokeEntry(MethodInfo entry, object argument)
    {
        try
        {
            entry.Invoke(null, [argument]);
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException(ex.InnerException?.Message ?? ex.Message, ex.InnerException);
        }
    }

    public static string Receipt()
    {
        var report = new StringBuilder();
        var compile = Compile(DefaultSource);
        if (!compile.Ok)
            throw new InvalidOperationException($"live receipt failed to compile: {compile.Error}");
        var json = TypeJson(compile.Assembly!);
        if (!json.Contains("\"Live.MoveY\"", StringComparison.Ordinal) || !json.Contains("\"consumers\"", StringComparison.Ordinal))
            throw new InvalidOperationException($"live receipt introspection missing the MoveY consumer: {json}");
        var (bytes, bakeError) = Bake(compile.Assembly!, DefaultTimelineJson);
        if (bytes.Length == 0)
            throw new InvalidOperationException($"live receipt bake failed: {bakeError}");
        var run = Run(compile.Assembly!, bytes);
        Require(run.Console.Length == 13, $"arc run printed {run.Console.Length} lines, expected 13");
        Require(run.Console[0] == "four characters jump, one call per frame:", $"arc header: {run.Console[0]}");
        Require(run.Console.Contains("  tick  3   y =  6.0 m   ##"), "arc rise line missing");
        Require(run.Console.Any(line => line.StartsWith("  tick 15   y = 30.0 m   ##########", StringComparison.Ordinal)), "arc apex line missing");
        Require(run.Console.Any(line => line.StartsWith("  tick 30   y =  0.0 m", StringComparison.Ordinal)), "arc landing line missing");
        Require(run.Console.Contains("rewind walks the arc back exactly:"), "rewind header missing");
        Require(run.Console.Contains("  after 30 back ticks: y = 0.0 m, tick = 0"), "rewind line missing");

        var replay = Compile(DefaultSource + "\n// replay");
        if (!replay.Ok)
            throw new InvalidOperationException($"live receipt recompile failed: {replay.Error}");
        var (replayBytes, replayBakeError) = Bake(replay.Assembly!, DefaultTimelineJson);
        if (replayBytes.Length == 0)
            throw new InvalidOperationException($"live receipt recompile bake failed: {replayBakeError}");
        var replayRun = Run(replay.Assembly!, replayBytes);
        Require(replayRun.Console.Contains("  tick  3   y =  6.0 m   ##"), "recompiled source must bind and apply the consumer exactly once");

        var (fallBytes, fallBakeError) = Bake(compile.Assembly!, ReproducerTimelineJson);
        if (fallBytes.Length == 0)
            throw new InvalidOperationException($"live receipt reproducer bake failed: {fallBakeError}");
        var fallRun = Run(compile.Assembly!, fallBytes);
        Require(fallRun.Console.Length == 13, $"reproducer arc run printed {fallRun.Console.Length} lines, expected 13");
        Require(fallRun.Console.Any(line => line.StartsWith("  tick 27   y = -6.0 m", StringComparison.Ordinal)), "reproducer negative-bar frame missing");
        Require(fallRun.Console.Any(line => line.StartsWith("  tick 30   y = -15.0 m", StringComparison.Ordinal)), "reproducer landing line missing");
        Require(fallRun.Console[^1] == "  after 30 back ticks: y = 0.0 m, tick = 0", $"reproducer rewind line: {fallRun.Console[^1]}");

        var shapes = new (string Name, string Json)[]
        {
            ("end == duration", ReproducerTimelineJson),
            ("end == duration - 1", ShapeJson(30, true, (0, 15, 2f), (15, 29, -3f))),
            ("duration == 1", ShapeJson(1, true, (0, 1, 2f))),
            ("adjacent shared tick", ShapeJson(30, true, (0, 10, 2f), (10, 20, -3f), (20, 30, -1f))),
            ("loop=false end == duration", ShapeJson(30, false, (0, 15, 2f), (15, 30, -3f))),
        };
        foreach (var (name, shapeJson) in shapes)
        {
            var (shapeBytes, shapeError) = Bake(compile.Assembly!, shapeJson);
            if (shapeBytes.Length == 0)
                throw new InvalidOperationException($"live receipt matrix bake failed for {name}: {shapeError}");
            var shapeRun = Run(compile.Assembly!, shapeBytes);
            Require(shapeRun.Console.Length == 13, $"matrix {name} printed {shapeRun.Console.Length} lines, expected 13");
            Require(shapeRun.Console[^1] == "  after 30 back ticks: y = 0.0 m, tick = 0", $"matrix {name} rewind line: {shapeRun.Console[^1]}");
        }

        var (emptyBytes, emptyError) = Bake(compile.Assembly!, ShapeJson(30, true, (0, 15, 2f), (15, 15, -3f)));
        Require(emptyBytes.Length == 0 && emptyError.Contains("start >= end", StringComparison.Ordinal), $"matrix start == end must reject with a located diagnostic, got: {emptyError}");
        var (outsideBytes, outsideError) = Bake(compile.Assembly!, ShapeJson(30, true, (0, 35, 2f)));
        Require(outsideBytes.Length == 0 && outsideError.Contains("clips outside [0, duration]", StringComparison.Ordinal), $"matrix end > duration must reject with a located diagnostic, got: {outsideError}");

        var raw = Compile(RawBindingSource);
        if (!raw.Ok)
            throw new InvalidOperationException($"live receipt raw preset failed to compile: {raw.Error}");
        var rawJson = TypeJson(raw.Assembly!);
        if (!rawJson.Contains("\"consumers\": []", StringComparison.Ordinal))
            throw new InvalidOperationException($"raw preset must register no consumers: {rawJson}");
        var (rawBytes, rawBakeError) = Bake(raw.Assembly!, RawBindingTimelineJson);
        if (rawBytes.Length == 0)
            throw new InvalidOperationException($"live receipt raw bake failed: {rawBakeError}");
        var rawRun = Run(raw.Assembly!, rawBytes);
        Require(rawRun.Console.Contains("  tick  4   amount =   12.0"), "raw preset single-registration line missing");
        report.AppendLine($"LIVE PASS compileMs={compile.CompileMs.ToString(CultureInfo.InvariantCulture)} bytes={bytes.Length} lines={run.Console.Length} checksum={run.Checksum.ToString(CultureInfo.InvariantCulture)}");
        report.AppendLine($"MATRIX PASS shapes={shapes.Length} checksum={fallRun.Checksum.ToString(CultureInfo.InvariantCulture)}");
        return report.ToString();
    }

    static string ShapeJson(int duration, bool loop, params (int Start, int End, float Velocity)[] clips)
    {
        var rows = new StringBuilder();
        foreach (var (start, end, velocity) in clips)
        {
            if (rows.Length > 0)
                rows.Append(", ");
            rows.Append(CultureInfo.InvariantCulture, $"{{ \"namespace\": \"Live\", \"type\": \"JumpClip\", \"start\": {start}, \"end\": {end}, \"data\": {{ \"Velocity\": {velocity.ToString("0.0", CultureInfo.InvariantCulture)} }} }}");
        }
        return string.Create(CultureInfo.InvariantCulture, $"{{ \"duration\": {duration}, \"loop\": {(loop ? "true" : "false")}, \"tracks\": [ {{ \"namespace\": \"Live\", \"type\": \"JumpTrack\", \"data\": {{ \"Scale\": 1.0 }}, \"clips\": [ {rows} ] }} ] }}");
    }

    static void Require(bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException($"live receipt failed: {detail}");
    }
}
