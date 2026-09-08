using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.Analysis;
using Tl.Gen.Model;

var options = Options.Parse(args);
var repository = FindRepositoryRoot(Directory.GetCurrentDirectory());
var sourcePath = Path.GetFullPath(options.Source ?? Path.Combine(repository, "samples", "Compiled", "Timeline.cs"));
var outputPath = Path.GetFullPath(options.Output ?? Path.Combine(repository, "benchmarks", "FusionHour", "Generated", "FusedPulse.g.cs"));
var source = File.ReadAllText(sourcePath);

if (options.SelfTest)
{
    RunSelfTests(sourcePath, source);
    return;
}

var definition = ReadDefinition(sourcePath, source);
ValidateGrammar(definition, source);
var plan = RegionAnalyzer.Analyze(definition);
var slots = WorkSlotMaterializer.ForRegions(plan);
var amounts = definition.Clips.Select(ReadAmount).ToArray();
var dense = options.Backend == BackendMode.Dense
    ? BuildDense(plan, slots, definition.Clips.Select(ReadAmountValue).ToArray())
    : null;
if (dense != null && options.Batch == BatchMode.Runs)
    throw new NotSupportedException("The dense backend supports scalar and carry batch modes; region runs apply only to the tree backend.");
var generated = Emit(plan, slots, amounts, options.Batch, dense);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, generated);
Console.WriteLine($"Generated {outputPath} ({generated.Length.ToString(CultureInfo.InvariantCulture)} characters).");

static TimelineDefinition ReadDefinition(string path, string source)
{
    var read = DeclarationReader.Read([(path, source)]);
    if (read.Diagnostics.Count != 0)
        throw new InvalidDataException(string.Join(Environment.NewLine, read.Diagnostics));
    if (read.Declarations.Count != 1)
        throw new InvalidDataException($"Expected one compiled timeline declaration, found {read.Declarations.Count}.");
    return read.Declarations[0].Definition;
}

static void RunSelfTests(string path, string source)
{
    ValidateGrammar(ReadDefinition(path, source), source);
    RequireRejected(
        path,
        ReplaceRequired(source, "first.Amount + (second.Amount - first.Amount) * t", "second.Amount + (first.Amount - second.Amount) * t"));
    RequireRejected(path, ReplaceRequired(source, "PulseClip(float Amount)", "PulseClip(float Value)"));
    RequireRejected(path, "namespace Unsupported; public static class Source { }");
    Console.WriteLine("FusionGenerate self-test passed 3 rejection probes.");
}

static string ReplaceRequired(string source, string oldValue, string newValue)
{
    var changed = source.Replace(oldValue, newValue, StringComparison.Ordinal);
    if (changed == source)
        throw new InvalidDataException($"Self-test fixture did not contain '{oldValue}'.");
    return changed;
}

static void RequireRejected(string path, string source)
{
    try
    {
        var definition = ReadDefinition(path, source);
        ValidateGrammar(definition, source);
    }
    catch (Exception exception) when (exception is InvalidDataException or NotSupportedException)
    {
        return;
    }

    throw new InvalidOperationException("Unsupported fusion grammar was accepted.");
}

static string FindRepositoryRoot(string start)
{
    for (var current = new DirectoryInfo(start); current != null; current = current.Parent)
        if (File.Exists(Path.Combine(current.FullName, "samples", "Compiled", "Timeline.cs")))
            return current.FullName;

    throw new DirectoryNotFoundException("Could not find the repository root from the current directory.");
}

static void ValidateGrammar(TimelineDefinition definition, string source)
{
    if (definition.Name != "CompiledPulse" || definition.Namespace != "Pulse"
        || definition.TrackTypeName != "PulseTrack" || definition.ClipTypeName != "PulseClip")
        throw new NotSupportedException("FusionGenerate supports only Pulse.Decls.Pulse over Timeline<PulseTrack, PulseClip>.");
    if (!definition.Loops)
        throw new NotSupportedException("FusionGenerate currently supports only the looping Pulse fixture.");
    if (definition.Tracks.Count == 0 || definition.Clips.Count == 0)
        throw new NotSupportedException("FusionGenerate requires active Pulse tracks and clips.");

    var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
    var clipTypes = root.DescendantNodes().OfType<RecordDeclarationSyntax>()
        .Where(static declaration => declaration.Identifier.ValueText == "PulseClip")
        .ToArray();
    if (clipTypes.Length != 1
        || !clipTypes[0].ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
        || !clipTypes[0].Modifiers.Any(static token => token.IsKind(SyntaxKind.ReadOnlyKeyword))
        || clipTypes[0].ParameterList?.Parameters.Count != 1
        || clipTypes[0].ParameterList!.Parameters[0].Type?.ToString() != "float"
        || clipTypes[0].ParameterList!.Parameters[0].Identifier.ValueText != "Amount")
        throw new NotSupportedException("FusionGenerate requires the exact readonly record struct PulseClip(float Amount) payload shape.");

    var blendMethods = root.DescendantNodes().OfType<StructDeclarationSyntax>()
        .Where(static declaration => declaration.Identifier.ValueText == "PulseTrack")
        .SelectMany(static declaration => declaration.Members.OfType<MethodDeclarationSyntax>())
        .Where(static method => method.Identifier.ValueText == "Blend")
        .ToArray();
    if (blendMethods.Length != 1 || !IsSupportedBlend(blendMethods[0]))
        throw new NotSupportedException("FusionGenerate requires PulseTrack.Blend to be exactly first.Amount + (second.Amount - first.Amount) * t.");
}

static bool IsSupportedBlend(MethodDeclarationSyntax method)
{
    var parameters = method.ParameterList.Parameters;
    if (method.ReturnType.ToString() != "void"
        || method.Body != null
        || method.ExpressionBody == null
        || parameters.Count != 4
        || parameters[0].Type?.ToString() != "PulseClip"
        || parameters[0].Identifier.ValueText != "first"
        || !parameters[0].Modifiers.Any(static token => token.IsKind(SyntaxKind.InKeyword))
        || parameters[1].Type?.ToString() != "PulseClip"
        || parameters[1].Identifier.ValueText != "second"
        || !parameters[1].Modifiers.Any(static token => token.IsKind(SyntaxKind.InKeyword))
        || parameters[2].Type?.ToString() != "float"
        || parameters[2].Identifier.ValueText != "t"
        || parameters[2].Modifiers.Count != 0
        || parameters[3].Type?.ToString() != "PulseClip"
        || parameters[3].Identifier.ValueText != "result"
        || !parameters[3].Modifiers.Any(static token => token.IsKind(SyntaxKind.OutKeyword)))
        return false;

    var actual = method.ExpressionBody.Expression.NormalizeWhitespace().ToFullString();
    const string expected = "result = new PulseClip(first.Amount + (second.Amount - first.Amount) * t)";
    return actual == expected;
}

static string ReadAmount(ClipDefinition clip)
{
    var expression = SyntaxFactory.ParseExpression(clip.PayloadExpression);
    if (expression is not ObjectCreationExpressionSyntax creation
        || creation.Type.ToString() != "PulseClip"
        || creation.Initializer != null
        || creation.ArgumentList?.Arguments.Count != 1)
        throw new NotSupportedException($"FusionGenerate accepts only PulseClip(float-literal) payloads; found '{clip.PayloadExpression}'.");

    var amount = creation.ArgumentList.Arguments[0].Expression;
    if (!IsFloatLiteral(amount))
        throw new NotSupportedException($"FusionGenerate accepts only PulseClip(float-literal) payloads; found '{clip.PayloadExpression}'.");

    return amount.ToString();
}

static float ReadAmountValue(ClipDefinition clip)
{
    var expression = SyntaxFactory.ParseExpression(clip.PayloadExpression);
    var amount = ((ObjectCreationExpressionSyntax)expression).ArgumentList!.Arguments[0].Expression;
    return amount switch
    {
        LiteralExpressionSyntax literal => (float)literal.Token.Value!,
        PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryMinusExpression)
            => -(float)((LiteralExpressionSyntax)unary.Operand).Token.Value!,
        PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryPlusExpression)
            => (float)((LiteralExpressionSyntax)unary.Operand).Token.Value!,
        _ => throw new InvalidOperationException("Validated float literal could not be read."),
    };
}

static bool IsFloatLiteral(ExpressionSyntax expression) => expression switch
{
    LiteralExpressionSyntax literal when literal.Token.Value is float => true,
    PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryMinusExpression) || unary.IsKind(SyntaxKind.UnaryPlusExpression)
        => unary.Operand is LiteralExpressionSyntax literal && literal.Token.Value is float,
    _ => false,
};

static DenseData BuildDense(TimelinePlan plan, EmittedWorkSlot[][] regions, float[] amounts)
{
    if (plan.Duration > 4096u)
        throw new NotSupportedException("The dense backend supports durations up to 4096 ticks.");
    if (plan.MaxActiveTracks > 3)
        throw new NotSupportedException("The dense backend supports at most 3 active works per tick.");

    var width = Math.Max(1, plan.MaxActiveTracks);
    var duration = checked((int)plan.Duration);
    var counts = new byte[duration];
    var values = new float[checked(duration * width)];
    var region = 0;
    for (var tick = 0; tick < duration; tick++)
    {
        while (region + 1 < plan.RegionStarts.Length && plan.RegionStarts[region + 1] <= (uint)tick)
            region++;

        var works = regions[region];
        counts[tick] = checked((byte)works.Length);
        for (var i = 0; i < works.Length; i++)
        {
            var work = works[i];
            var value = amounts[work.First];
            if (work.Second != EmittedWorkSlot.Single)
            {
                var factor = work.FactorLength <= 1
                    ? 0.5f
                    : DivideExact((uint)tick - work.FactorStart, work.FactorLength - 1u);
                var difference = SubtractExact(amounts[work.Second], amounts[work.First]);
                value = AddExact(amounts[work.First], MultiplyExact(difference, factor));
            }
            values[tick * width + i] = value;
        }
    }

    return new DenseData(counts, values, width);
}

[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
static float DivideExact(uint numerator, uint denominator) => numerator / (float)denominator;

[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
static float SubtractExact(float left, float right) => left - right;

[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
static float MultiplyExact(float left, float right) => left * right;

[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
static float AddExact(float left, float right) => left + right;

static string FloatLiteral(float value)
{
    var bits = BitConverter.SingleToInt32Bits(value);
    if (bits == int.MinValue)
        return "-0f";
    if (!float.IsFinite(value))
        throw new NotSupportedException("The dense backend requires finite materialized float operands.");
    return value.ToString("R", CultureInfo.InvariantCulture) + "f";
}

static string Emit(TimelinePlan plan, EmittedWorkSlot[][] regions, string[] amounts, BatchMode batchMode, DenseData? dense)
{
    var writer = new StringBuilder();
    writer.AppendLine("#nullable enable");
    writer.AppendLine("using System;");
    writer.AppendLine("using System.Runtime.CompilerServices;");
    writer.AppendLine("using Tl;");
    writer.AppendLine();
    writer.AppendLine("namespace Tl.FusionExperiment;");
    writer.AppendLine();
    writer.AppendLine("public static class FusedPulse");
    writer.AppendLine("{");
    writer.AppendLine($"    public const uint Duration = {U(plan.Duration)};");
    writer.AppendLine("    public const bool Loops = true;");
    if (dense != null)
    {
        writer.AppendLine();
        writer.AppendLine($"    private static ReadOnlySpan<byte> Counts => [{string.Join(", ", dense.Counts)}];");
        writer.AppendLine($"    private static ReadOnlySpan<float> Amounts => [{string.Join(", ", dense.Amounts.Select(FloatLiteral))}];");
    }
    writer.AppendLine();
    writer.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    writer.AppendLine("    public static Playback Start(uint tick = 0)");
    writer.AppendLine("        => Mint(tick, 0, PlaybackFlags.Started);");
    writer.AppendLine();
    writer.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    writer.AppendLine("    public static Playback Stop(in Playback playback)");
    writer.AppendLine("    {");
    writer.AppendLine("        if (!playback.Has(PlaybackFlags.Started))");
    writer.AppendLine("            throw new InvalidOperationException(\"Cannot stop a playback that was never started.\");");
    writer.AppendLine("        return Mint(playback.Tick, playback.Cycles, playback.Flags | PlaybackFlags.Stopped);");
    writer.AppendLine("    }");
    writer.AppendLine();
    writer.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    writer.AppendLine("    public static Playback Forward(in Playback from, ref float sum, uint tick)");
    writer.AppendLine("    {");
    EmitValidation(writer, "from", 2);
    writer.AppendLine("        return ForwardOne(in from, ref sum, tick);");
    writer.AppendLine("    }");
    writer.AppendLine();
    EmitBatch(writer, plan, regions, amounts, backward: false, batchMode, dense);
    writer.AppendLine();
    writer.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    writer.AppendLine("    public static Playback Backward(in Playback from, ref float sum, uint tick)");
    writer.AppendLine("    {");
    EmitValidation(writer, "from", 2);
    writer.AppendLine("        return BackwardOne(in from, ref sum, tick);");
    writer.AppendLine("    }");
    writer.AppendLine();
    EmitBatch(writer, plan, regions, amounts, backward: true, batchMode, dense);
    writer.AppendLine();
    EmitOne(writer, plan, regions, amounts, backward: false, dense);
    writer.AppendLine();
    EmitOne(writer, plan, regions, amounts, backward: true, dense);
    writer.AppendLine();
    writer.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    writer.AppendLine("    private static Playback Mint(uint tick, ushort cycles, PlaybackFlags flags)");
    writer.AppendLine("        => Unsafe.BitCast<ulong, Playback>(tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);");
    writer.AppendLine("}");
    return writer.ToString();
}

static void EmitValidation(StringBuilder writer, string playback, int depth)
{
    var indent = new string(' ', depth * 4);
    writer.AppendLine($"{indent}if (!{playback}.Has(PlaybackFlags.Started))");
    writer.AppendLine($"{indent}    throw new InvalidOperationException(\"Playback was never started; mint one with FusedPulse.Start.\");");
    writer.AppendLine($"{indent}if ({playback}.Has(PlaybackFlags.Stopped))");
    writer.AppendLine($"{indent}    throw new InvalidOperationException(\"Playback is stopped.\");");
}

static void EmitOne(
    StringBuilder writer,
    TimelinePlan plan,
    EmittedWorkSlot[][] regions,
    string[] amounts,
    bool backward,
    DenseData? dense)
{
    var name = backward ? "BackwardOne" : "ForwardOne";
    writer.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    writer.AppendLine($"    private static Playback {name}(in Playback from, ref float sum, uint tick)");
    writer.AppendLine("    {");
    if (dense == null)
    {
        writer.AppendLine("        var previousEffective = from.Tick % Duration;");
        writer.AppendLine("        var effective = tick % Duration;");
    }
    else
    {
        writer.AppendLine("        var previousQuotient = from.Tick / Duration;");
        writer.AppendLine("        var quotient = tick / Duration;");
        writer.AppendLine("        var effective = tick - quotient * Duration;");
    }
    writer.AppendLine("        uint cycles;");
    if (!backward)
    {
        writer.AppendLine("        if (tick >= from.Tick)");
        writer.AppendLine(dense == null
            ? "            cycles = tick / Duration - from.Tick / Duration;"
            : "            cycles = quotient - previousQuotient;");
        writer.AppendLine("        else");
        if (dense == null)
        {
            writer.AppendLine("            cycles = effective < previousEffective ? 1u : 0u;");
        }
        else
        {
            writer.AppendLine("        {");
            writer.AppendLine("            var previousEffective = from.Tick - previousQuotient * Duration;");
            writer.AppendLine("            cycles = effective < previousEffective ? 1u : 0u;");
            writer.AppendLine("        }");
        }
        writer.AppendLine("        if (cycles > ushort.MaxValue - from.Cycles)");
        writer.AppendLine("            throw new ArgumentOutOfRangeException(\"ticks\", \"Playback cycle capacity exceeded.\");");
        writer.AppendLine("        var newCycles = (ushort)(from.Cycles + cycles);");
    }
    else
    {
        writer.AppendLine("        if (tick <= from.Tick)");
        writer.AppendLine(dense == null
            ? "            cycles = from.Tick / Duration - tick / Duration;"
            : "            cycles = previousQuotient - quotient;");
        writer.AppendLine("        else");
        if (dense == null)
        {
            writer.AppendLine("            cycles = effective > previousEffective ? 1u : 0u;");
        }
        else
        {
            writer.AppendLine("        {");
            writer.AppendLine("            var previousEffective = from.Tick - previousQuotient * Duration;");
            writer.AppendLine("            cycles = effective > previousEffective ? 1u : 0u;");
            writer.AppendLine("        }");
        }
        writer.AppendLine("        var newCycles = (ushort)(from.Cycles - Math.Min(from.Cycles, cycles));");
    }
    writer.AppendLine("        var flags = PlaybackFlags.Started;");
    writer.AppendLine("        if (effective == Duration - 1u)");
    writer.AppendLine("            flags |= PlaybackFlags.LastLoopFrame;");
    if (dense == null)
        EmitTree(writer, plan.RegionStarts, regions, amounts, 0, regions.Length - 1, 2, backward);
    else
        EmitDenseEffects(writer, dense.Width, 2, backward);
    writer.AppendLine("        return Mint(tick, newCycles, flags);");
    writer.AppendLine("    }");
}

static void EmitBatch(
    StringBuilder writer,
    TimelinePlan plan,
    EmittedWorkSlot[][] regions,
    string[] amounts,
    bool backward,
    BatchMode batchMode,
    DenseData? dense)
{
    switch (batchMode)
    {
        case BatchMode.Scalar:
            EmitScalarBatch(writer, backward);
            break;
        case BatchMode.Carry:
            EmitCarryBatch(writer, plan, regions, amounts, backward, dense);
            break;
        case BatchMode.Runs:
            EmitRunsBatch(writer, plan, regions, amounts, backward);
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(batchMode));
    }
}

static void EmitScalarBatch(StringBuilder writer, bool backward)
{
    var name = backward ? "Backward" : "Forward";
    var one = backward ? "BackwardOne" : "ForwardOne";
    writer.AppendLine($"    public static Playback {name}(in Playback from, ref float sum, ReadOnlySpan<uint> ticks)");
    writer.AppendLine("    {");
    EmitValidation(writer, "from", 2);
    writer.AppendLine("        var playback = from;");
    writer.AppendLine("        foreach (var tick in ticks)");
    writer.AppendLine($"            playback = {one}(in playback, ref sum, tick);");
    writer.AppendLine("        return playback;");
    writer.AppendLine("    }");
}

static void EmitCarryBatch(
    StringBuilder writer,
    TimelinePlan plan,
    EmittedWorkSlot[][] regions,
    string[] amounts,
    bool backward,
    DenseData? dense)
{
    var name = backward ? "Backward" : "Forward";
    writer.AppendLine($"    public static Playback {name}(in Playback from, ref float sum, ReadOnlySpan<uint> ticks)");
    writer.AppendLine("    {");
    EmitValidation(writer, "from", 2);
    writer.AppendLine("        if (ticks.IsEmpty)");
    writer.AppendLine("            return from;");
    writer.AppendLine("        var stateTick = from.Tick;");
    writer.AppendLine("        var stateCycles = from.Cycles;");
    if (dense == null)
    {
        writer.AppendLine("        var previousEffective = stateTick % Duration;");
        writer.AppendLine("        var previousQuotient = stateTick / Duration;");
    }
    else
    {
        writer.AppendLine("        var previousQuotient = stateTick / Duration;");
        writer.AppendLine("        var previousEffective = stateTick - previousQuotient * Duration;");
    }
    writer.AppendLine("        foreach (var tick in ticks)");
    writer.AppendLine("        {");
    if (dense == null)
    {
        writer.AppendLine("            var effective = tick % Duration;");
        writer.AppendLine("            var quotient = tick / Duration;");
    }
    else
    {
        writer.AppendLine("            var quotient = tick / Duration;");
        writer.AppendLine("            var effective = tick - quotient * Duration;");
    }
    writer.AppendLine("            uint cycles;");
    if (!backward)
    {
        writer.AppendLine("            if (tick >= stateTick)");
        writer.AppendLine("                cycles = quotient - previousQuotient;");
        writer.AppendLine("            else");
        writer.AppendLine("                cycles = effective < previousEffective ? 1u : 0u;");
        writer.AppendLine("            if (cycles > ushort.MaxValue - stateCycles)");
        writer.AppendLine("                throw new ArgumentOutOfRangeException(\"ticks\", \"Playback cycle capacity exceeded.\");");
        writer.AppendLine("            stateCycles = (ushort)(stateCycles + cycles);");
    }
    else
    {
        writer.AppendLine("            if (tick <= stateTick)");
        writer.AppendLine("                cycles = previousQuotient - quotient;");
        writer.AppendLine("            else");
        writer.AppendLine("                cycles = effective > previousEffective ? 1u : 0u;");
        writer.AppendLine("            stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));");
    }
    if (dense == null)
        EmitTree(writer, plan.RegionStarts, regions, amounts, 0, regions.Length - 1, 3, backward);
    else
        EmitDenseEffects(writer, dense.Width, 3, backward);
    writer.AppendLine("            stateTick = tick;");
    writer.AppendLine("            previousEffective = effective;");
    writer.AppendLine("            previousQuotient = quotient;");
    writer.AppendLine("        }");
    writer.AppendLine("        var flags = PlaybackFlags.Started;");
    writer.AppendLine("        if (previousEffective == Duration - 1u)");
    writer.AppendLine("            flags |= PlaybackFlags.LastLoopFrame;");
    writer.AppendLine("        return Mint(stateTick, stateCycles, flags);");
    writer.AppendLine("    }");
}

static void EmitRunsBatch(StringBuilder writer, TimelinePlan plan, EmittedWorkSlot[][] regions, string[] amounts, bool backward)
{
    var name = backward ? "Backward" : "Forward";
    writer.AppendLine($"    public static Playback {name}(in Playback from, ref float sum, ReadOnlySpan<uint> ticks)");
    writer.AppendLine("    {");
    EmitValidation(writer, "from", 2);
    writer.AppendLine("        if (ticks.IsEmpty)");
    writer.AppendLine("            return from;");
    writer.AppendLine("        var stateTick = from.Tick;");
    writer.AppendLine("        var stateCycles = from.Cycles;");
    writer.AppendLine("        var previousEffective = stateTick % Duration;");
    writer.AppendLine("        var previousQuotient = stateTick / Duration;");
    writer.AppendLine("        var index = 0;");
    writer.AppendLine("        var effective = ticks[0] % Duration;");
    writer.AppendLine("        while ((uint)index < (uint)ticks.Length)");
    writer.AppendLine("        {");
    EmitRunTree(writer, plan, regions, amounts, 0, regions.Length - 2, 3, backward);
    writer.AppendLine("        }");
    writer.AppendLine("        var flags = PlaybackFlags.Started;");
    writer.AppendLine("        if (previousEffective == Duration - 1u)");
    writer.AppendLine("            flags |= PlaybackFlags.LastLoopFrame;");
    writer.AppendLine("        return Mint(stateTick, stateCycles, flags);");
    writer.AppendLine("    }");
}

static void EmitRunTree(
    StringBuilder writer,
    TimelinePlan plan,
    EmittedWorkSlot[][] regions,
    string[] amounts,
    int lo,
    int hi,
    int depth,
    bool backward)
{
    var indent = new string(' ', depth * 4);
    if (lo == hi)
    {
        var lower = plan.RegionStarts[lo];
        var upper = plan.RegionStarts[lo + 1];
        writer.AppendLine($"{indent}while ((uint)index < (uint)ticks.Length && effective >= {U(lower)} && effective < {U(upper)})");
        writer.AppendLine($"{indent}{{");
        writer.AppendLine($"{indent}    var tick = ticks[index];");
        writer.AppendLine($"{indent}    var quotient = tick / Duration;");
        writer.AppendLine($"{indent}    uint cycles;");
        if (!backward)
        {
            writer.AppendLine($"{indent}    if (tick >= stateTick)");
            writer.AppendLine($"{indent}        cycles = quotient - previousQuotient;");
            writer.AppendLine($"{indent}    else");
            writer.AppendLine($"{indent}        cycles = effective < previousEffective ? 1u : 0u;");
            writer.AppendLine($"{indent}    if (cycles > ushort.MaxValue - stateCycles)");
            writer.AppendLine($"{indent}        throw new ArgumentOutOfRangeException(\"ticks\", \"Playback cycle capacity exceeded.\");");
            writer.AppendLine($"{indent}    stateCycles = (ushort)(stateCycles + cycles);");
        }
        else
        {
            writer.AppendLine($"{indent}    if (tick <= stateTick)");
            writer.AppendLine($"{indent}        cycles = previousQuotient - quotient;");
            writer.AppendLine($"{indent}    else");
            writer.AppendLine($"{indent}        cycles = effective > previousEffective ? 1u : 0u;");
            writer.AppendLine($"{indent}    stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));");
        }
        EmitRegion(writer, regions[lo], amounts, lo, indent + "    ", backward);
        writer.AppendLine($"{indent}    stateTick = tick;");
        writer.AppendLine($"{indent}    previousEffective = effective;");
        writer.AppendLine($"{indent}    previousQuotient = quotient;");
        writer.AppendLine($"{indent}    index++;");
        writer.AppendLine($"{indent}    if ((uint)index < (uint)ticks.Length)");
        writer.AppendLine($"{indent}        effective = ticks[index] % Duration;");
        writer.AppendLine($"{indent}}}");
        return;
    }

    var mid = (lo + hi + 1) / 2;
    writer.AppendLine($"{indent}if (effective < {U(plan.RegionStarts[mid])})");
    writer.AppendLine($"{indent}{{");
    EmitRunTree(writer, plan, regions, amounts, lo, mid - 1, depth + 1, backward);
    writer.AppendLine($"{indent}}}");
    writer.AppendLine($"{indent}else");
    writer.AppendLine($"{indent}{{");
    EmitRunTree(writer, plan, regions, amounts, mid, hi, depth + 1, backward);
    writer.AppendLine($"{indent}}}");
}

static void EmitDenseEffects(StringBuilder writer, int width, int depth, bool backward)
{
    var indent = new string(' ', depth * 4);
    var operation = backward ? "-=" : "+=";
    writer.AppendLine($"{indent}var denseIndex = (int)effective;");
    writer.AppendLine($"{indent}var denseOffset = denseIndex * {width.ToString(CultureInfo.InvariantCulture)};");
    writer.AppendLine($"{indent}var denseCount = Counts[denseIndex];");
    writer.AppendLine($"{indent}var denseAmounts = Amounts;");
    writer.AppendLine($"{indent}if (denseCount != 0)");
    writer.AppendLine($"{indent}{{");
    writer.AppendLine($"{indent}    sum {operation} denseAmounts[denseOffset];");
    for (var i = 1; i < width; i++)
    {
        writer.AppendLine($"{indent}    if (denseCount > {i.ToString(CultureInfo.InvariantCulture)})");
        writer.AppendLine($"{indent}        sum {operation} denseAmounts[denseOffset + {i.ToString(CultureInfo.InvariantCulture)}];");
    }
    writer.AppendLine($"{indent}}}");
}

static void EmitTree(
    StringBuilder writer,
    uint[] starts,
    EmittedWorkSlot[][] regions,
    string[] amounts,
    int lo,
    int hi,
    int depth,
    bool backward)
{
    var indent = new string(' ', depth * 4);
    if (lo == hi)
    {
        EmitRegion(writer, regions[lo], amounts, lo, indent, backward);
        return;
    }

    var mid = (lo + hi + 1) / 2;
    writer.AppendLine($"{indent}if (effective < {U(starts[mid])})");
    writer.AppendLine($"{indent}{{");
    EmitTree(writer, starts, regions, amounts, lo, mid - 1, depth + 1, backward);
    writer.AppendLine($"{indent}}}");
    writer.AppendLine($"{indent}else");
    writer.AppendLine($"{indent}{{");
    EmitTree(writer, starts, regions, amounts, mid, hi, depth + 1, backward);
    writer.AppendLine($"{indent}}}");
}

static void EmitRegion(StringBuilder writer, EmittedWorkSlot[] works, string[] amounts, int region, string indent, bool backward)
{
    var operation = backward ? "-=" : "+=";
    for (var i = 0; i < works.Length; i++)
    {
        var work = works[i];
        if (work.Second == EmittedWorkSlot.Single)
        {
            writer.AppendLine($"{indent}sum {operation} {amounts[work.First]};");
            continue;
        }

        var suffix = region.ToString(CultureInfo.InvariantCulture) + "_" + i.ToString(CultureInfo.InvariantCulture);
        writer.AppendLine($"{indent}var factor_{suffix} = {Factor(work)};");
        writer.AppendLine($"{indent}var difference_{suffix} = {amounts[work.Second]} - {amounts[work.First]};");
        writer.AppendLine($"{indent}var blended_{suffix} = {amounts[work.First]} + difference_{suffix} * factor_{suffix};");
        writer.AppendLine($"{indent}sum {operation} blended_{suffix};");
    }
}

static string Factor(EmittedWorkSlot work) => work.FactorLength <= 1
    ? "0.5f"
    : $"(effective - {U(work.FactorStart)}) / {(work.FactorLength - 1).ToString(CultureInfo.InvariantCulture)}f";

static string U(uint value) => value.ToString(CultureInfo.InvariantCulture) + "u";

enum BatchMode { Scalar, Carry, Runs }

enum BackendMode { Tree, Dense }

sealed record DenseData(byte[] Counts, float[] Amounts, int Width);

readonly record struct Options(string? Source, string? Output, BatchMode Batch, BackendMode Backend, bool SelfTest)
{
    public static Options Parse(string[] arguments)
    {
        string? source = null;
        string? output = null;
        var batch = BatchMode.Carry;
        var backend = BackendMode.Tree;
        var selfTest = false;
        for (var i = 0; i < arguments.Length; i++)
        {
            switch (arguments[i])
            {
                case "--source" when i + 1 < arguments.Length:
                    source = arguments[++i];
                    break;
                case "--output" when i + 1 < arguments.Length:
                    output = arguments[++i];
                    break;
                case "--batch" when i + 1 < arguments.Length:
                    batch = arguments[++i] switch
                    {
                        "scalar" => BatchMode.Scalar,
                        "carry" => BatchMode.Carry,
                        "runs" => BatchMode.Runs,
                        var value => throw new ArgumentException($"Unknown batch mode '{value}'. Use scalar, carry, or runs."),
                    };
                    break;
                case "--self-test":
                    selfTest = true;
                    break;
                case "--backend" when i + 1 < arguments.Length:
                    backend = arguments[++i] switch
                    {
                        "tree" => BackendMode.Tree,
                        "dense" => BackendMode.Dense,
                        var value => throw new ArgumentException($"Unknown backend '{value}'. Use tree or dense."),
                    };
                    break;
                default:
                    throw new ArgumentException($"Unknown or incomplete argument '{arguments[i]}'. Use --source PATH --output PATH.");
            }
        }

        return new Options(source, output, batch, backend, selfTest);
    }
}
