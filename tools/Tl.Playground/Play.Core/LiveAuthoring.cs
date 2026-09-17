using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl;
using Tl.Gen.Tlb;

namespace Play;

public sealed record LiveCompile(Assembly? Assembly, string Error, long CompileMs)
{
    public bool Ok => Assembly is not null;
}

public sealed class LiveRun
{
    public ushort[] Positions = [];
    public float[] Effects = [];
    public int Ticks;
    public int Moved;
    public int Skipped;
    public ulong Checksum;
}

public static class LiveAuthoring
{
    static readonly List<Assembly> KeepAlive = [];
    static int _compiles;

    public const string DefaultSource = """
using System.Threading;
using Tl;

namespace Live;

public readonly record struct AmountClip(float Amount);

public readonly record struct ScaleTrack(float Scale) : IBlend<AmountClip>
{
    public void Blend(in AmountClip first, in AmountClip second, float factor, out AmountClip result)
        => result = new AmountClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public static unsafe class Setup
{
    static int _installed;

    public static void Run()
    {
        if (Interlocked.Exchange(ref _installed, 1) == 1) return;
        PairRuntime<ScaleTrack, AmountClip>.Consume(&ExecuteScale, &BindFloat);
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

    static void ExecuteScale(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        AmountClip scratch = default;
        var frame = TickFrame.ToFrame<ScaleTrack, AmountClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }
}
""";

    public const string DefaultTimelineJson = """
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
            var compilation = CSharpCompilation.Create("Live" + _compiles++, [CSharpSyntaxTree.ParseText(source, parse)], references, options);
            using var pe = new MemoryStream();
            var emit = compilation.Emit(pe);
            if (!emit.Success)
            {
                var errors = new StringBuilder();
                foreach (var diagnostic in emit.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning).Take(20))
                    errors.AppendLine(diagnostic.ToString());
                return new LiveCompile(null, errors.ToString(), watch.ElapsedMilliseconds);
            }
            pe.Position = 0;
            var context = new AssemblyLoadContext("live" + _compiles, isCollectible: false);
            var assembly = context.LoadFromStream(pe);
            KeepAlive.Add(assembly);
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
            var resolver = new BakerAssemblyResolver();
            resolver.AddAssembly(assembly);
            return (TimelineBaker.BakeJson(timelineJson, resolver), "");
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    public static string BakeStats(byte[] tlb)
    {
        var view = TlbMetadata.Read(tlb);
        return $"bytes={tlb.Length} types={view.Types.Count} pairs={view.PairTypes.Count} labels={view.Labels.Count}";
    }

    public static (Type Track, Type Clip) FindPair(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
            foreach (var face in type.GetInterfaces())
                if (face.IsGenericType && face.GetGenericTypeDefinition() == typeof(IBlend<>))
                    return (type, face.GetGenericArguments()[0]);
        throw new InvalidOperationException("live run needs a track type implementing IBlend<TClip>; the compiled code has none");
    }

    public static LiveRun Run(Assembly assembly, byte[] tlb, int duration, bool looping, int rows, int ticks)
    {
        var run = new LiveRun { Positions = new ushort[rows], Effects = new float[rows], Ticks = ticks };
        InvokeSetup(assembly);
        var (track, clip) = FindPair(assembly);
        var lane = typeof(BakedLane<,>).MakeGenericType(track, clip);
        var bind = lane.GetMethod("Bind", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException("BakedLane.Bind not found");
        var effect = lane.GetMethod("Effect", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException("BakedLane.Effect not found");
        var inverse = lane.GetMethod("InverseEffect", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException("BakedLane.InverseEffect not found");
        var select = typeof(TimelineMovement).GetMethod("Select", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException("TimelineMovement.Select not found");
        var durationTick = (ushort)duration;
        using var asset = TimelineAsset.Load(tlb);
        bind.Invoke(null, [asset]);
        for (var t = 0; t < ticks; t++)
        {
            var forward = t < (ticks + 1) / 2;
            for (var i = 0; i < rows; i++)
            {
                var args = new object?[] { new TimelineState(1, run.Positions[i]), durationTick, looping, !forward, null, (ushort)0, FrameFlags.None };
                if ((bool)select.Invoke(null, args)! is false)
                {
                    run.Skipped++;
                    continue;
                }
                var next = (TimelineState)args[4]!;
                var tick = (ushort)args[5]!;
                var delta = (float)(forward ? effect : inverse).Invoke(null, [tick])!;
                run.Effects[i] += delta;
                run.Positions[i] = next.Position;
                run.Moved++;
            }
        }
        var hash = 14695981039346656037ul;
        for (var i = 0; i < rows; i++)
        {
            hash = unchecked((hash ^ run.Positions[i]) * 1099511628211ul);
            hash = unchecked((hash ^ (uint)BitConverter.SingleToInt32Bits(run.Effects[i])) * 1099511628211ul);
        }
        run.Checksum = hash;
        return run;
    }

    static void InvokeSetup(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var run = type.GetMethod("Run", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (run is null) continue;
            run.Invoke(null, null);
            return;
        }
    }

    public static string Receipt(int rows = 8, int ticks = 41)
    {
        var report = new StringBuilder();
        var compile = Compile(DefaultSource);
        if (!compile.Ok)
            throw new InvalidOperationException($"live receipt failed to compile: {compile.Error}");
        var json = TypeJson(compile.Assembly!);
        if (!json.Contains("\"ScaleTrack\"", StringComparison.Ordinal) || !json.Contains("\"blendable\": true", StringComparison.Ordinal))
            throw new InvalidOperationException($"live receipt type json missing the sample pair: {json}");
        var (bytes, bakeError) = Bake(compile.Assembly!, DefaultTimelineJson);
        if (bytes.Length == 0)
            throw new InvalidOperationException($"live receipt bake failed: {bakeError}");
        var run = Run(compile.Assembly!, bytes, duration: 64, looping: true, rows, ticks);
        if (run.Moved == 0)
            throw new InvalidOperationException("live receipt moved zero rows");
        report.AppendLine($"LIVE PASS compileMs={compile.CompileMs.ToString(CultureInfo.InvariantCulture)} bytes={bytes.Length} moved={run.Moved} skipped={run.Skipped} checksum={run.Checksum.ToString(CultureInfo.InvariantCulture)}");
        return report.ToString();
    }
}
