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
var outputPath = Path.GetFullPath(options.Output ?? Path.Combine(repository, "benchmarks", "ConsumerFusion", "Generated", "FusedPulse.g.cs"));
var source = File.ReadAllText(sourcePath);

if (options.SelfTest)
{
    RunSelfTests(sourcePath, source);
    return;
}

var definition = ReadDefinition(sourcePath, source);
var operands = ValidateGrammar(definition, source);
var plan = RegionAnalyzer.Analyze(definition);
var regions = WorkSlotMaterializer.ForRegions(plan);
var generated = Emit(plan, regions, operands, options.InlineBatch);
WriteIfChanged(outputPath, generated);

static TimelineDefinition ReadDefinition(string path, string source)
{
    var read = DeclarationReader.Read([(path, source)]);
    if (read.Diagnostics.Count != 0)
        throw new InvalidDataException(string.Join(Environment.NewLine, read.Diagnostics));
    if (read.Declarations.Count != 1)
        throw new InvalidDataException($"Expected one compiled timeline declaration, found {read.Declarations.Count}.");
    return read.Declarations[0].Definition;
}

static Operands ValidateGrammar(TimelineDefinition definition, string source)
{
    if (definition.Name != "CompiledPulse" || definition.Namespace != "Pulse"
        || definition.TrackTypeName != "PulseTrack" || definition.ClipTypeName != "PulseClip")
        throw new NotSupportedException("ConsumerGenerate supports only Pulse.Decls.Pulse over Timeline<PulseTrack, PulseClip>.");
    if (!definition.Loops)
        throw new NotSupportedException("ConsumerGenerate requires the looping Pulse declaration.");
    if (definition.Tracks.Count == 0 || definition.Clips.Count == 0)
        throw new NotSupportedException("ConsumerGenerate requires active Pulse tracks and clips.");

    var tree = SyntaxFactory.ParseSyntaxTree(source);
    var errors = tree.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
    if (errors.Length != 0)
        throw new InvalidDataException(string.Join(Environment.NewLine, errors));
    var root = tree.GetRoot();
    ValidateClipDeclaration(root);
    ValidateTrackDeclaration(root);

    var offsets = new string[definition.Tracks.Count];
    for (var i = 0; i < definition.Tracks.Count; i++)
    {
        var track = definition.Tracks[i];
        if (track.Index != i)
            throw new NotSupportedException("ConsumerGenerate requires contiguous authored Pulse track indices.");
        offsets[i] = ReadOperand(track.TrackExpression, "PulseTrack", "int");
    }

    var amounts = new string[definition.Clips.Count];
    for (var i = 0; i < definition.Clips.Count; i++)
    {
        if (definition.Clips[i].TrackIndex >= definition.Tracks.Count)
            throw new NotSupportedException("ConsumerGenerate found a Pulse clip with an invalid track index.");
        amounts[i] = ReadOperand(definition.Clips[i].PayloadExpression, "PulseClip", "float");
    }

    return new Operands(offsets, amounts);
}

static void ValidateClipDeclaration(SyntaxNode root)
{
    var declarations = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
        .Where(static declaration => declaration.Identifier.ValueText == "PulseClip")
        .ToArray();
    if (declarations.Length != 1 || declarations[0] is not RecordDeclarationSyntax declaration)
        throw new NotSupportedException("ConsumerGenerate requires one PulseClip declaration.");
    if (!declaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
        || !HasExactModifiers(declaration.Modifiers, SyntaxKind.PublicKeyword, SyntaxKind.ReadOnlyKeyword)
        || declaration.AttributeLists.Count != 0
        || declaration.BaseList != null
        || declaration.Members.Count != 0
        || !declaration.SemicolonToken.IsKind(SyntaxKind.SemicolonToken)
        || declaration.ParameterList?.Parameters.Count != 1
        || declaration.ParameterList.Parameters[0].Type?.ToString() != "float"
        || declaration.ParameterList.Parameters[0].Identifier.ValueText != "Amount"
        || !IsPlainParameter(declaration.ParameterList.Parameters[0]))
        throw new NotSupportedException("ConsumerGenerate requires exactly public readonly record struct PulseClip(float Amount).");
}

static void ValidateTrackDeclaration(SyntaxNode root)
{
    var declarations = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
        .Where(static declaration => declaration.Identifier.ValueText == "PulseTrack")
        .ToArray();
    if (declarations.Length != 1 || declarations[0] is not StructDeclarationSyntax declaration)
        throw new NotSupportedException("ConsumerGenerate requires one PulseTrack declaration.");
    var parameters = declaration.ParameterList?.Parameters;
    var baseTypes = declaration.BaseList?.Types;
    if (!HasExactModifiers(declaration.Modifiers, SyntaxKind.PublicKeyword, SyntaxKind.ReadOnlyKeyword)
        || declaration.AttributeLists.Count != 0
        || parameters?.Count != 1
        || parameters.Value[0].Type?.ToString() != "int"
        || parameters.Value[0].Identifier.ValueText != "offset"
        || !IsPlainParameter(parameters.Value[0])
        || baseTypes?.Count != 1
        || baseTypes.Value[0].Type.ToString() != "IBlend<PulseClip>"
        || declaration.Members.Count != 2)
        throw new NotSupportedException("ConsumerGenerate requires the exact PulseTrack(int offset) declaration shape.");

    var fields = declaration.Members.OfType<FieldDeclarationSyntax>().ToArray();
    var methods = declaration.Members.OfType<MethodDeclarationSyntax>().ToArray();
    if (fields.Length != 1 || methods.Length != 1 || !IsSupportedOffset(fields[0]) || !IsSupportedBlend(methods[0]))
        throw new NotSupportedException("ConsumerGenerate requires the exact PulseTrack offset and blend grammar.");
}

static bool HasExactModifiers(SyntaxTokenList modifiers, params SyntaxKind[] expected)
{
    if (modifiers.Count != expected.Length)
        return false;
    return expected.All(kind => modifiers.Any(token => token.IsKind(kind)));
}

static bool IsPlainParameter(ParameterSyntax parameter) =>
    parameter.AttributeLists.Count == 0
    && parameter.Modifiers.Count == 0
    && parameter.Default == null;

static bool IsSupportedOffset(FieldDeclarationSyntax field)
{
    if (field.AttributeLists.Count != 0
        || !HasExactModifiers(field.Modifiers, SyntaxKind.PublicKeyword, SyntaxKind.ReadOnlyKeyword)
        || field.Declaration.Type.ToString() != "int"
        || field.Declaration.Variables.Count != 1)
        return false;
    var variable = field.Declaration.Variables[0];
    return variable.Identifier.ValueText == "Offset"
        && variable.Initializer?.Value is IdentifierNameSyntax { Identifier.ValueText: "offset" };
}

static bool IsSupportedBlend(MethodDeclarationSyntax method)
{
    var parameters = method.ParameterList.Parameters;
    if (method.AttributeLists.Count != 0
        || !HasExactModifiers(method.Modifiers, SyntaxKind.PublicKeyword)
        || method.ReturnType.ToString() != "void"
        || method.Body != null
        || method.ExpressionBody == null
        || method.ExplicitInterfaceSpecifier != null
        || method.TypeParameterList != null
        || method.ConstraintClauses.Count != 0
        || parameters.Count != 4
        || parameters[0].Type?.ToString() != "PulseClip"
        || parameters[0].Identifier.ValueText != "first"
        || !HasExactModifiers(parameters[0].Modifiers, SyntaxKind.InKeyword)
        || parameters[0].AttributeLists.Count != 0
        || parameters[0].Default != null
        || parameters[1].Type?.ToString() != "PulseClip"
        || parameters[1].Identifier.ValueText != "second"
        || !HasExactModifiers(parameters[1].Modifiers, SyntaxKind.InKeyword)
        || parameters[1].AttributeLists.Count != 0
        || parameters[1].Default != null
        || parameters[2].Type?.ToString() != "float"
        || parameters[2].Identifier.ValueText != "t"
        || parameters[2].Modifiers.Count != 0
        || parameters[2].AttributeLists.Count != 0
        || parameters[2].Default != null
        || parameters[3].Type?.ToString() != "PulseClip"
        || parameters[3].Identifier.ValueText != "result"
        || !HasExactModifiers(parameters[3].Modifiers, SyntaxKind.OutKeyword)
        || parameters[3].AttributeLists.Count != 0
        || parameters[3].Default != null)
        return false;
    const string expected = "result = new PulseClip(first.Amount + (second.Amount - first.Amount) * t)";
    return method.ExpressionBody.Expression.NormalizeWhitespace().ToFullString() == expected;
}

static string ReadOperand(string expressionText, string typeName, string primitive)
{
    var expression = SyntaxFactory.ParseExpression(expressionText);
    if (expression.ContainsDiagnostics
        || expression is not ObjectCreationExpressionSyntax creation
        || creation.Type.ToString() != typeName
        || creation.Initializer != null
        || creation.ArgumentList?.Arguments.Count != 1
        || creation.ArgumentList.Arguments[0].NameColon != null
        || !creation.ArgumentList.Arguments[0].RefKindKeyword.IsKind(SyntaxKind.None))
        throw new NotSupportedException($"ConsumerGenerate accepts only {typeName}({primitive}-literal) operands; found '{expressionText}'.");
    var operand = creation.ArgumentList.Arguments[0].Expression;
    var supported = primitive == "float" ? IsFloatLiteral(operand) : IsIntLiteral(operand);
    if (!supported)
        throw new NotSupportedException($"ConsumerGenerate accepts only {typeName}({primitive}-literal) operands; found '{expressionText}'.");
    return operand.ToString();
}

static bool IsFloatLiteral(ExpressionSyntax expression) => expression switch
{
    LiteralExpressionSyntax literal when literal.Token.Value is float => true,
    PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryMinusExpression) || unary.IsKind(SyntaxKind.UnaryPlusExpression)
        => unary.Operand is LiteralExpressionSyntax literal && literal.Token.Value is float,
    _ => false,
};

static bool IsIntLiteral(ExpressionSyntax expression) => expression switch
{
    LiteralExpressionSyntax literal when literal.Token.Value is int => true,
    PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryMinusExpression) || unary.IsKind(SyntaxKind.UnaryPlusExpression)
        => unary.Operand is LiteralExpressionSyntax literal && literal.Token.Value is int,
    _ => false,
};

static string Emit(TimelinePlan plan, EmittedWorkSlot[][] regions, Operands operands, bool inlineBatch)
{
    if (plan.Duration == 0 || regions.Length < 2 || plan.RegionStarts[^1] != plan.Duration)
        throw new NotSupportedException("ConsumerGenerate requires a non-empty looping region plan.");
    var writer = new StringBuilder();
    Line(writer, "#nullable enable");
    Line(writer, "using System;");
    Line(writer, "using System.Runtime.CompilerServices;");
    Line(writer, "using Tl;");
    Line(writer);
    Line(writer, "namespace Tl.ConsumerFusion;");
    Line(writer);
    Line(writer, "public static class FusedPulse");
    Line(writer, "{");
    Line(writer, $"    public const uint Duration = {U(plan.Duration)};");
    Line(writer, "    public const bool Loops = true;");
    Line(writer);
    Line(writer, "    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    Line(writer, "    public static Playback Start(uint tick = 0)");
    Line(writer, "        => Mint(tick, 0, PlaybackFlags.Started);");
    Line(writer);
    Line(writer, "    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    Line(writer, "    public static Playback Stop(in Playback playback)");
    Line(writer, "    {");
    Line(writer, "        if (!playback.Has(PlaybackFlags.Started))");
    Line(writer, "            throw new InvalidOperationException(\"Cannot stop a playback that was never started.\");");
    Line(writer, "        return Mint(playback.Tick, playback.Cycles, playback.Flags | PlaybackFlags.Stopped);");
    Line(writer, "    }");
    Line(writer);
    EmitScalar(writer, plan, regions, operands, false);
    Line(writer);
    EmitBatch(writer, plan, regions, operands, false, inlineBatch);
    Line(writer);
    EmitScalar(writer, plan, regions, operands, true);
    Line(writer);
    EmitBatch(writer, plan, regions, operands, true, inlineBatch);
    Line(writer);
    Line(writer, "    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    Line(writer, "    private static Playback Mint(uint tick, ushort cycles, PlaybackFlags flags)");
    Line(writer, "        => Unsafe.BitCast<ulong, Playback>(tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);");
    Line(writer, "}");
    return writer.ToString();
}

static void EmitScalar(StringBuilder writer, TimelinePlan plan, EmittedWorkSlot[][] regions, Operands operands, bool backward)
{
    var name = backward ? "Backward" : "Forward";
    Line(writer, "    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    Line(writer, $"    public static Playback {name}<TInput, TResult>(in Playback from, in TInput input, ref TResult result, uint tick)");
    EmitConstraints(writer);
    Line(writer, "    {");
    EmitValidation(writer, 2);
    Line(writer, "        var previousQuotient = from.Tick / Duration;");
    Line(writer, "        var previousEffective = from.Tick - previousQuotient * Duration;");
    Line(writer, "        var quotient = tick / Duration;");
    Line(writer, "        var effective = tick - quotient * Duration;");
    EmitCycleUpdate(writer, backward, "from.Tick", "from.Cycles", "var newCycles", 2);
    Line(writer, "        var flags = PlaybackFlags.Started;");
    Line(writer, "        if (effective == Duration - 1u)");
    Line(writer, "            flags |= PlaybackFlags.LastLoopFrame;");
    EmitTree(writer, plan.RegionStarts, regions, operands, 0, regions.Length - 2, 2, backward);
    Line(writer, "        return Mint(tick, newCycles, flags);");
    Line(writer, "    }");
}

static void EmitBatch(
    StringBuilder writer, TimelinePlan plan, EmittedWorkSlot[][] regions, Operands operands,
    bool backward, bool inlineBatch)
{
    var name = backward ? "Backward" : "Forward";
    if (inlineBatch)
        Line(writer, "    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
    Line(writer, $"    public static Playback {name}<TInput, TResult>(in Playback from, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)");
    EmitConstraints(writer);
    Line(writer, "    {");
    EmitValidation(writer, 2);
    Line(writer, "        if (ticks.IsEmpty)");
    Line(writer, "            return from;");
    Line(writer, "        var stateTick = from.Tick;");
    Line(writer, "        var stateCycles = from.Cycles;");
    Line(writer, "        var previousQuotient = stateTick / Duration;");
    Line(writer, "        var previousEffective = stateTick - previousQuotient * Duration;");
    Line(writer, "        foreach (var tick in ticks)");
    Line(writer, "        {");
    Line(writer, "            var quotient = tick / Duration;");
    Line(writer, "            var effective = tick - quotient * Duration;");
    EmitCycleUpdate(writer, backward, "stateTick", "stateCycles", "stateCycles", 3);
    EmitTree(writer, plan.RegionStarts, regions, operands, 0, regions.Length - 2, 3, backward);
    Line(writer, "            stateTick = tick;");
    Line(writer, "            previousEffective = effective;");
    Line(writer, "            previousQuotient = quotient;");
    Line(writer, "        }");
    Line(writer, "        var flags = PlaybackFlags.Started;");
    Line(writer, "        if (previousEffective == Duration - 1u)");
    Line(writer, "            flags |= PlaybackFlags.LastLoopFrame;");
    Line(writer, "        return Mint(stateTick, stateCycles, flags);");
    Line(writer, "    }");
}

static void EmitConstraints(StringBuilder writer)
{
    Line(writer, "        where TInput : unmanaged");
    Line(writer, "        where TResult : unmanaged, IWorkOperation<TInput, TResult>");
}

static void EmitValidation(StringBuilder writer, int depth)
{
    var indent = new string(' ', depth * 4);
    Line(writer, $"{indent}if (!from.Has(PlaybackFlags.Started))");
    Line(writer, $"{indent}    throw new InvalidOperationException(\"Playback was never started; mint one with FusedPulse.Start.\");");
    Line(writer, $"{indent}if (from.Has(PlaybackFlags.Stopped))");
    Line(writer, $"{indent}    throw new InvalidOperationException(\"Playback is stopped.\");");
}

static void EmitCycleUpdate(
    StringBuilder writer, bool backward, string stateTick, string stateCycles, string destination, int depth)
{
    var indent = new string(' ', depth * 4);
    Line(writer, $"{indent}uint cycles;");
    if (!backward)
    {
        Line(writer, $"{indent}if (tick >= {stateTick})");
        Line(writer, $"{indent}    cycles = quotient - previousQuotient;");
        Line(writer, $"{indent}else");
        Line(writer, $"{indent}    cycles = effective < previousEffective ? 1u : 0u;");
        Line(writer, $"{indent}if (cycles > ushort.MaxValue - {stateCycles})");
        Line(writer, $"{indent}    throw new ArgumentOutOfRangeException(\"ticks\", \"Playback cycle capacity exceeded.\");");
        Line(writer, $"{indent}{destination} = (ushort)({stateCycles} + cycles);");
    }
    else
    {
        Line(writer, $"{indent}if (tick <= {stateTick})");
        Line(writer, $"{indent}    cycles = previousQuotient - quotient;");
        Line(writer, $"{indent}else");
        Line(writer, $"{indent}    cycles = effective > previousEffective ? 1u : 0u;");
        Line(writer, $"{indent}{destination} = (ushort)({stateCycles} - Math.Min({stateCycles}, cycles));");
    }
}

static void EmitTree(
    StringBuilder writer, uint[] starts, EmittedWorkSlot[][] regions, Operands operands,
    int lo, int hi, int depth, bool backward)
{
    var indent = new string(' ', depth * 4);
    if (lo == hi)
    {
        EmitRegion(writer, starts, regions[lo], operands, lo, indent, backward);
        return;
    }
    var mid = (lo + hi + 1) / 2;
    Line(writer, $"{indent}if (effective < {U(starts[mid])})");
    Line(writer, $"{indent}{{");
    EmitTree(writer, starts, regions, operands, lo, mid - 1, depth + 1, backward);
    Line(writer, $"{indent}}}");
    Line(writer, $"{indent}else");
    Line(writer, $"{indent}{{");
    EmitTree(writer, starts, regions, operands, mid, hi, depth + 1, backward);
    Line(writer, $"{indent}}}");
}

static void EmitRegion(
    StringBuilder writer, uint[] starts, EmittedWorkSlot[] works, Operands operands,
    int region, string indent, bool backward)
{
    var method = backward ? "Backward" : "Forward";
    var states = new Dictionary<(uint EnterF, uint EnterB), string>();
    for (var i = 0; i < works.Length; i++)
    {
        var work = works[i];
        var suffix = region.ToString(CultureInfo.InvariantCulture) + "_" + i.ToString(CultureInfo.InvariantCulture);
        if (!states.TryGetValue((work.EnterF, work.EnterB), out var state))
        {
            state = $"state_{suffix}";
            states.Add((work.EnterF, work.EnterB), state);
            EmitState(writer, starts, work, region, indent, state, backward);
        }
        string amount;
        if (work.Second == EmittedWorkSlot.Single)
        {
            amount = operands.Amounts[work.First];
        }
        else
        {
            Line(writer, $"{indent}var factor_{suffix} = {Factor(work)};");
            Line(writer, $"{indent}var difference_{suffix} = {operands.Amounts[work.Second]} - {operands.Amounts[work.First]};");
            Line(writer, $"{indent}var amount_{suffix} = {operands.Amounts[work.First]} + difference_{suffix} * factor_{suffix};");
            amount = $"amount_{suffix}";
        }
        Line(writer, $"{indent}TResult.{method}({work.Index.ToString(CultureInfo.InvariantCulture)}, {operands.Offsets[work.Index]}, {amount}, {state}, effective, in input, ref result);");
    }
}

static void EmitState(
    StringBuilder writer, uint[] starts, EmittedWorkSlot work, int region,
    string indent, string state, bool backward)
{
    var lower = starts[region];
    var upper = starts[region + 1];
    var exitTick = backward ? work.EnterF : work.EnterB - 1u;
    var exitPossible = exitTick >= lower && exitTick < upper;
    var enterPossible = backward ? work.EnterB < starts[^1] : work.EnterF != 0;
    var enter = backward
        ? $"previousEffective >= {U(work.EnterB)}"
        : $"previousEffective < {U(work.EnterF)}";
    if (exitPossible)
    {
        Line(writer, $"{indent}var {state} = effective == {U(exitTick)}");
        Line(writer, $"{indent}    ? ClipState.Exit");
        Line(writer, enterPossible
            ? $"{indent}    : cycles != 0 || {enter} ? ClipState.Enter : ClipState.Stay;"
            : $"{indent}    : cycles != 0 ? ClipState.Enter : ClipState.Stay;");
        return;
    }
    Line(writer, enterPossible
        ? $"{indent}var {state} = cycles != 0 || {enter} ? ClipState.Enter : ClipState.Stay;"
        : $"{indent}var {state} = cycles != 0 ? ClipState.Enter : ClipState.Stay;");
}

static string Factor(EmittedWorkSlot work) => work.FactorLength <= 1
    ? "0.5f"
    : $"(effective - {U(work.FactorStart)}) / {(work.FactorLength - 1).ToString(CultureInfo.InvariantCulture)}f";

static string U(uint value) => value.ToString(CultureInfo.InvariantCulture) + "u";

static void Line(StringBuilder writer, string value = "")
{
    writer.Append(value);
    writer.Append('\n');
}

static void WriteIfChanged(string outputPath, string generated)
{
    var encoding = new UTF8Encoding(false, true);
    var bytes = encoding.GetBytes(generated);
    if (File.Exists(outputPath) && File.ReadAllBytes(outputPath).AsSpan().SequenceEqual(bytes))
    {
        Console.WriteLine($"Unchanged {outputPath} ({bytes.Length.ToString(CultureInfo.InvariantCulture)} bytes).");
        return;
    }
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllBytes(outputPath, bytes);
    Console.WriteLine($"Generated {outputPath} ({bytes.Length.ToString(CultureInfo.InvariantCulture)} bytes).");
}

static void RunSelfTests(string path, string source)
{
    var definition = ReadDefinition(path, source);
    var operands = ValidateGrammar(definition, source);
    var plan = RegionAnalyzer.Analyze(definition);
    _ = Emit(plan, WorkSlotMaterializer.ForRegions(plan), operands, false);
    RequireRejected(path, ReplaceRequired(source,
        "first.Amount + (second.Amount - first.Amount) * t",
        "second.Amount + (first.Amount - second.Amount) * t"));
    RequireRejected(path, ReplaceRequired(source, "PulseClip(float Amount)", "PulseClip(float Value)"));
    RequireRejected(path, ReplaceRequired(source,
        "public readonly record struct PulseClip(float Amount);",
        "public readonly record struct PulseClip(float Amount) { public float Amount => 7f; }"));
    RequireRejected(path, ReplaceRequired(source,
        "public readonly record struct PulseClip(float Amount);",
        "public readonly record struct PulseClip(float Amount) { static PulseClip() { } }"));
    RequireRejected(path, ReplaceRequired(source, "PulseTrack(int offset)", "PulseTrack(long offset)"));
    RequireRejected(path, ReplaceRequired(source, "new PulseTrack(1)", "new PulseTrack((int)1)"));
    RequireRejected(path, ReplaceRequired(source, "new PulseClip(1f)", "new PulseClip((float)1)"));
    RequireRejected(path, "namespace Unsupported; public static class Source { }");
    Console.WriteLine("ConsumerGenerate self-test passed 8 rejection probes.");
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
        ValidateGrammar(ReadDefinition(path, source), source);
    }
    catch (Exception exception) when (exception is InvalidDataException or NotSupportedException)
    {
        return;
    }
    throw new InvalidOperationException("Unsupported consumer-fusion grammar was accepted.");
}

static string FindRepositoryRoot(string start)
{
    for (var current = new DirectoryInfo(start); current != null; current = current.Parent)
        if (File.Exists(Path.Combine(current.FullName, "samples", "Compiled", "Timeline.cs")))
            return current.FullName;
    throw new DirectoryNotFoundException("Could not find the repository root from the current directory.");
}

sealed record Operands(string[] Offsets, string[] Amounts);

sealed record Options(string? Source, string? Output, bool SelfTest, bool InlineBatch)
{
    public static Options Parse(string[] args)
    {
        string? source = null;
        string? output = null;
        var selfTest = false;
        var inlineBatch = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--source" when i + 1 < args.Length:
                    source = args[++i];
                    break;
                case "--output" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--self-test":
                    selfTest = true;
                    break;
                case "--inline-batch":
                    inlineBatch = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown or incomplete argument '{args[i]}'.");
            }
        }
        return new Options(source, output, selfTest, inlineBatch);
    }
}
