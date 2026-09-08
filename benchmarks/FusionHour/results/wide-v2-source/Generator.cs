using System.Globalization;
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
var read = DeclarationReader.Read([(sourcePath, source)]);

if (read.Diagnostics.Count != 0)
    throw new InvalidDataException(string.Join(Environment.NewLine, read.Diagnostics));
if (read.Declarations.Count != 1)
    throw new InvalidDataException($"Expected one compiled timeline declaration, found {read.Declarations.Count}.");

var definition = read.Declarations[0].Definition;
ValidateGrammar(definition, source);
var plan = RegionAnalyzer.Analyze(definition);
var slots = WorkSlotMaterializer.ForRegions(plan);
var amounts = definition.Clips.Select(ReadAmount).ToArray();
var generated = Emit(plan, slots, amounts);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, generated);
Console.WriteLine($"Generated {outputPath} ({generated.Length.ToString(CultureInfo.InvariantCulture)} characters).");

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

static bool IsFloatLiteral(ExpressionSyntax expression) => expression switch
{
    LiteralExpressionSyntax literal when literal.Token.Value is float => true,
    PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryMinusExpression) || unary.IsKind(SyntaxKind.UnaryPlusExpression)
        => unary.Operand is LiteralExpressionSyntax literal && literal.Token.Value is float,
    _ => false,
};

static string Emit(TimelinePlan plan, EmittedWorkSlot[][] regions, string[] amounts)
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
    EmitBatch(writer, plan, regions, amounts, backward: false);
    writer.AppendLine();
    writer.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    writer.AppendLine("    public static Playback Backward(in Playback from, ref float sum, uint tick)");
    writer.AppendLine("    {");
    EmitValidation(writer, "from", 2);
    writer.AppendLine("        return BackwardOne(in from, ref sum, tick);");
    writer.AppendLine("    }");
    writer.AppendLine();
    EmitBatch(writer, plan, regions, amounts, backward: true);
    writer.AppendLine();
    EmitOne(writer, plan, regions, amounts, backward: false);
    writer.AppendLine();
    EmitOne(writer, plan, regions, amounts, backward: true);
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

static void EmitOne(StringBuilder writer, TimelinePlan plan, EmittedWorkSlot[][] regions, string[] amounts, bool backward)
{
    var name = backward ? "BackwardOne" : "ForwardOne";
    writer.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    writer.AppendLine($"    private static Playback {name}(in Playback from, ref float sum, uint tick)");
    writer.AppendLine("    {");
    writer.AppendLine("        var previousEffective = from.Tick % Duration;");
    writer.AppendLine("        var effective = tick % Duration;");
    writer.AppendLine("        uint cycles;");
    if (!backward)
    {
        writer.AppendLine("        if (tick >= from.Tick)");
        writer.AppendLine("            cycles = tick / Duration - from.Tick / Duration;");
        writer.AppendLine("        else");
        writer.AppendLine("            cycles = effective < previousEffective ? 1u : 0u;");
        writer.AppendLine("        if (cycles > ushort.MaxValue - from.Cycles)");
        writer.AppendLine("            throw new ArgumentOutOfRangeException(\"ticks\", \"Playback cycle capacity exceeded.\");");
        writer.AppendLine("        var newCycles = (ushort)(from.Cycles + cycles);");
    }
    else
    {
        writer.AppendLine("        if (tick <= from.Tick)");
        writer.AppendLine("            cycles = from.Tick / Duration - tick / Duration;");
        writer.AppendLine("        else");
        writer.AppendLine("            cycles = effective > previousEffective ? 1u : 0u;");
        writer.AppendLine("        var newCycles = (ushort)(from.Cycles - Math.Min(from.Cycles, cycles));");
    }
    writer.AppendLine("        var flags = PlaybackFlags.Started;");
    writer.AppendLine("        if (effective == Duration - 1u)");
    writer.AppendLine("            flags |= PlaybackFlags.LastLoopFrame;");
    EmitTree(writer, plan.RegionStarts, regions, amounts, 0, regions.Length - 1, 2, backward);
    writer.AppendLine("        return Mint(tick, newCycles, flags);");
    writer.AppendLine("    }");
}

static void EmitBatch(StringBuilder writer, TimelinePlan plan, EmittedWorkSlot[][] regions, string[] amounts, bool backward)
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
    writer.AppendLine("        foreach (var tick in ticks)");
    writer.AppendLine("        {");
    writer.AppendLine("            var effective = tick % Duration;");
    writer.AppendLine("            var quotient = tick / Duration;");
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
    EmitTree(writer, plan.RegionStarts, regions, amounts, 0, regions.Length - 1, 3, backward);
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

readonly record struct Options(string? Source, string? Output)
{
    public static Options Parse(string[] arguments)
    {
        string? source = null;
        string? output = null;
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
                default:
                    throw new ArgumentException($"Unknown or incomplete argument '{arguments[i]}'. Use --source PATH --output PATH.");
            }
        }

        return new Options(source, output);
    }
}
