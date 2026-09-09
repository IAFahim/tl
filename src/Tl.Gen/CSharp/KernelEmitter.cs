using System.Globalization;
using System.Text;
using Tl.Gen.Analysis;
using Tl.Gen.Model;

namespace Tl.Gen.CSharp;

public static class KernelEmitter
{
    public const string SharedFileName = "CompiledRuntime.g.cs";
    private const int FullyInlineWorkSiteLimit = 64;
    private readonly record struct CompactSegment(uint Start, uint End, EmittedWorkSlot Work);

    public static string EmitSharedRuntime() => """
        #nullable enable
        using Tl;

        namespace Tl.Compiled;

        public readonly record struct CompiledTimelineInfo(string Kernel);

        public static class TimelineCompileExtensions
        {
            public static CompiledTimelineInfo Compile<TTrack, TClip>(this TimelineAuthoring<TTrack, TClip> authoring)
                where TTrack : unmanaged, IBlend<TClip>
                where TClip : unmanaged
                => new("<compiled: call the generated kernel class for this declaration>");
        }
        """ + "\n";

    public static string EmitKernel(
        TimelinePlan plan,
        EmittedWorkSlot[][] regions,
        string kernelName,
        string sourceFile,
        int sourceLine)
        => EmitKernel(plan, regions, kernelName, sourceFile, sourceLine, CompiledDeclarationKind.LegacyCompile);

    public static string EmitKernel(
        TimelinePlan plan,
        EmittedWorkSlot[][] regions,
        string kernelName,
        string sourceFile,
        int sourceLine,
        CompiledDeclarationKind kind)
    {
        var definition = plan.Definition;
        var writer = new StringBuilder();
        Line(writer, "#nullable enable");
        if (definition.Namespace.Length != 0)
        {
            Line(writer, $"namespace {definition.Namespace};");
            Line(writer);
        }
        foreach (var sourceUsing in definition.SourceUsings)
            Line(writer, sourceUsing);
        if (definition.SourceUsings.Count != 0)
            Line(writer);
        var declaration = kind == CompiledDeclarationKind.PartialTimeline
            ? $"public readonly partial struct {kernelName}"
            : $"public static class {kernelName}";
        Line(writer, declaration);
        Line(writer, "{");
        Line(writer, $"    public const uint Duration = {U(plan.Duration)};");
        Line(writer, $"    public const bool Loops = {Boolean(definition.Loops)};");
        EmitData(writer, definition);
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, "    public static global::Tl.Playback Start(uint at = 0)");
        Line(writer, "        => Mint(at, 0, global::Tl.PlaybackFlags.Started);");
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, "    public static global::Tl.Playback Stop(in global::Tl.Playback playback)");
        Line(writer, "    {");
        Line(writer, "        if (!playback.Has(global::Tl.PlaybackFlags.Started))");
        Line(writer, "            throw new global::System.InvalidOperationException(\"Cannot stop a playback that was never started.\");");
        Line(writer, "        return Mint(playback.Tick, playback.Cycles, playback.Flags | global::Tl.PlaybackFlags.Stopped);");
        Line(writer, "    }");
        Line(writer);
        var compactRegionProgram = regions.Sum(static region => region.Length) > FullyInlineWorkSiteLimit;
        var compactTracks = CompactTracks(plan, regions);
        EmitScalar(writer, plan, regions, backward: false, kernelName, compactRegionProgram);
        Line(writer);
        EmitBatch(writer, plan, regions, backward: false, kernelName, compactRegionProgram);
        Line(writer);
        EmitScalar(writer, plan, regions, backward: true, kernelName, compactRegionProgram);
        Line(writer);
        EmitBatch(writer, plan, regions, backward: true, kernelName, compactRegionProgram);
        if (compactRegionProgram)
        {
            Line(writer);
            EmitCompactRegionProgram(writer, plan, compactTracks, backward: false);
            Line(writer);
            EmitCompactRegionProgram(writer, plan, compactTracks, backward: true);
        }
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, "    private static global::Tl.Playback Mint(uint tick, ushort cycles, global::Tl.PlaybackFlags flags)");
        Line(writer, "        => global::System.Runtime.CompilerServices.Unsafe.BitCast<ulong, global::Tl.Playback>(tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);");
        Line(writer, "}");
        return writer.ToString();
    }

    private static void EmitData(StringBuilder writer, TimelineDefinition definition)
    {
        for (var index = 0; index < definition.Tracks.Count; index++)
            Line(writer, $"    private static readonly {definition.TrackTypeName} s_track{I(index)} = {definition.Tracks[index].TrackExpression};");
        for (var index = 0; index < definition.Clips.Count; index++)
            Line(writer, $"    private static readonly {definition.ClipTypeName} s_clip{I(index)} = {definition.Clips[index].PayloadExpression};");
    }

    private static void EmitScalar(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot[][] regions,
        bool backward,
        string kernelName,
        bool sharedRegionProgram)
    {
        var method = backward ? "Backward" : "Forward";
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    public static global::Tl.Playback {method}<TInput, TResult>(in global::Tl.Playback from, in TInput input, ref TResult result, uint tick)");
        EmitConstraints(writer, plan.Definition);
        Line(writer, "    {");
        EmitValidation(writer, kernelName, 2);
        if (plan.MaxActiveTracks == 0)
        {
            EmitFlags(writer, plan, backward, "tick", 2);
            Line(writer, "        return Mint(tick, from.Cycles, flags);");
            Line(writer, "    }");
            return;
        }
        EmitInitialPosition(writer, plan, "from.Tick", 2);
        EmitCycleUpdate(writer, plan, backward, "from.Tick", "from.Cycles", "var newCycles", 2);
        EmitRegionProgram(writer, plan, regions, backward, sharedRegionProgram, 2);
        EmitFlags(writer, plan, backward, "effective", 2);
        Line(writer, "        return Mint(tick, newCycles, flags);");
        Line(writer, "    }");
    }

    private static void EmitBatch(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot[][] regions,
        bool backward,
        string kernelName,
        bool sharedRegionProgram)
    {
        var method = backward ? "Backward" : "Forward";
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    public static global::Tl.Playback {method}<TInput, TResult>(in global::Tl.Playback from, in TInput input, ref TResult result, params global::System.ReadOnlySpan<uint> ticks)");
        EmitConstraints(writer, plan.Definition);
        Line(writer, "    {");
        EmitValidation(writer, kernelName, 2);
        Line(writer, "        if (ticks.IsEmpty)");
        Line(writer, "            return from;");
        if (plan.MaxActiveTracks == 0)
        {
            Line(writer, "        var tick = ticks[^1];");
            EmitFlags(writer, plan, backward, "tick", 2);
            Line(writer, "        return Mint(tick, from.Cycles, flags);");
            Line(writer, "    }");
            return;
        }
        Line(writer, "        var stateTick = from.Tick;");
        Line(writer, "        var stateCycles = from.Cycles;");
        EmitPreviousPosition(writer, plan, "stateTick", 2);
        Line(writer, "        foreach (var tick in ticks)");
        Line(writer, "        {");
        EmitEffectivePosition(writer, plan, 3);
        EmitCycleUpdate(writer, plan, backward, "stateTick", "stateCycles", "stateCycles", 3);
        EmitRegionProgram(writer, plan, regions, backward, sharedRegionProgram, 3);
        Line(writer, "            stateTick = tick;");
        Line(writer, "            previousEffective = effective;");
        if (IsLooping(plan))
            Line(writer, "            previousQuotient = quotient;");
        Line(writer, "        }");
        EmitFlags(writer, plan, backward, "previousEffective", 2);
        Line(writer, "        return Mint(stateTick, stateCycles, flags);");
        Line(writer, "    }");
    }

    private static void EmitRegionProgram(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot[][] regions,
        bool backward,
        bool shared,
        int depth)
    {
        if (!shared)
        {
            EmitRegions(writer, plan, regions, backward, depth);
            return;
        }

        var method = backward ? "ApplyBackward" : "ApplyForward";
        var cycles = IsLooping(plan) ? "cycles" : "0u";
        Line(writer, $"{Indent(depth)}{method}(effective, previousEffective, {cycles}, in input, ref result);");
    }

    private static void EmitCompactRegionProgram(
        StringBuilder writer,
        TimelinePlan plan,
        IReadOnlyList<CompactSegment>[] tracks,
        bool backward)
    {
        var method = backward ? "ApplyBackward" : "ApplyForward";
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining | global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]");
        Line(writer, $"    private static void {method}<TInput, TResult>(uint effective, uint previousEffective, uint cycles, in TInput input, ref TResult result)");
        EmitConstraints(writer, plan.Definition);
        Line(writer, "    {");
        Line(writer, "        var count = 0;");
        foreach (var track in tracks)
            if (track.Count != 0)
            {
                Line(writer, $"        if ({ActiveCondition(track)})");
                Line(writer, "            count++;");
            }
        Line(writer, "        var ordinal = 0;");
        for (var trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
            EmitCompactTrack(writer, plan, tracks[trackIndex], trackIndex, backward);
        Line(writer, "    }");
    }

    private static void EmitCompactTrack(
        StringBuilder writer,
        TimelinePlan plan,
        IReadOnlyList<CompactSegment> segments,
        int trackIndex,
        bool backward)
    {
        for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
        {
            var segment = segments[segmentIndex];
            var branch = segmentIndex == 0 ? "if" : "else if";
            Line(writer, $"        {branch} ({IntervalCondition(segment.Start, segment.End)})");
            Line(writer, "        {");
            EmitCompactWork(writer, plan, segment.Work, trackIndex, segmentIndex, backward);
            Line(writer, "            ordinal++;");
            Line(writer, "        }");
        }
    }

    private static void EmitCompactWork(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot work,
        int trackIndex,
        int segmentIndex,
        bool backward)
    {
        var suffix = I(trackIndex) + "_" + I(segmentIndex);
        var state = "state_" + suffix;
        EmitCompactState(writer, plan, work, "            ", state, backward);
        var clip = $"s_clip{I(work.First)}";
        if (work.Second != EmittedWorkSlot.Single)
        {
            Line(writer, $"            var factor_{suffix} = {Factor(work)};");
            Line(writer, $"            s_track{I(work.Index)}.Blend(in s_clip{I(work.First)}, in s_clip{I(work.Second)}, factor_{suffix}, out var clip_{suffix});");
            clip = "clip_" + suffix;
        }
        var method = backward ? "Backward" : "Forward";
        Line(writer,
            $"            TResult.{method}(ordinal, count, {I(work.Index)}, in s_track{I(work.Index)}, in {clip}, {state}, effective, in input, ref result);");
    }

    private static void EmitCompactState(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot work,
        string indent,
        string state,
        bool backward)
    {
        var exitTick = backward ? work.EnterF : work.EnterB - 1u;
        var crossed = backward
            ? $"previousEffective >= {U(work.EnterB)}"
            : $"previousEffective < {U(work.EnterF)}";
        var enterPossible = backward
            ? !IsLooping(plan) || work.EnterB < plan.Duration
            : work.EnterF != 0u;
        var enter = IsLooping(plan)
            ? enterPossible ? $"cycles != 0u || {crossed}" : "cycles != 0u"
            : enterPossible ? crossed : "false";
        Line(writer, $"{indent}var {state} = effective == {U(exitTick)}");
        Line(writer, $"{indent}    ? global::Tl.ClipState.Exit");
        Line(writer, enter == "false"
            ? $"{indent}    : global::Tl.ClipState.Stay;"
            : $"{indent}    : {enter} ? global::Tl.ClipState.Enter : global::Tl.ClipState.Stay;");
    }

    private static IReadOnlyList<CompactSegment>[] CompactTracks(
        TimelinePlan plan,
        EmittedWorkSlot[][] regions)
    {
        var tracks = Enumerable.Range(0, plan.Definition.Tracks.Count)
            .Select(static _ => new List<CompactSegment>())
            .ToArray();
        for (var region = 0; region + 1 < plan.RegionStarts.Length; region++)
        {
            var start = plan.RegionStarts[region];
            var end = plan.RegionStarts[region + 1];
            foreach (var work in regions[region])
            {
                var segments = tracks[work.Index];
                if (segments.Count != 0)
                {
                    var previous = segments[^1];
                    if (previous.End == start && previous.Work == work)
                    {
                        segments[^1] = previous with { End = end };
                        continue;
                    }
                }
                segments.Add(new CompactSegment(start, end, work));
            }
        }
        return tracks;
    }

    private static string ActiveCondition(IReadOnlyList<CompactSegment> segments)
    {
        var spans = new List<(uint Start, uint End)>();
        foreach (var segment in segments)
        {
            if (spans.Count != 0 && spans[^1].End == segment.Start)
            {
                spans[^1] = (spans[^1].Start, segment.End);
                continue;
            }
            spans.Add((segment.Start, segment.End));
        }
        return string.Join(" || ", spans.Select(static span => $"({IntervalCondition(span.Start, span.End)})"));
    }

    private static string IntervalCondition(uint start, uint end) => start == 0u
        ? $"effective < {U(end)}"
        : $"effective >= {U(start)} && effective < {U(end)}";

    private static void EmitInitialPosition(StringBuilder writer, TimelinePlan plan, string stateTick, int depth)
    {
        EmitPreviousPosition(writer, plan, stateTick, depth);
        EmitEffectivePosition(writer, plan, depth);
    }

    private static void EmitPreviousPosition(StringBuilder writer, TimelinePlan plan, string stateTick, int depth)
    {
        var indent = Indent(depth);
        if (IsLooping(plan))
        {
            Line(writer, $"{indent}var previousQuotient = {stateTick} / Duration;");
            Line(writer, $"{indent}var previousEffective = {stateTick} - previousQuotient * Duration;");
            return;
        }
        Line(writer, $"{indent}var previousEffective = {stateTick};");
    }

    private static void EmitEffectivePosition(StringBuilder writer, TimelinePlan plan, int depth)
    {
        var indent = Indent(depth);
        if (IsLooping(plan))
        {
            Line(writer, $"{indent}var quotient = tick / Duration;");
            Line(writer, $"{indent}var effective = tick - quotient * Duration;");
            return;
        }
        Line(writer, $"{indent}var effective = tick;");
    }

    private static void EmitCycleUpdate(
        StringBuilder writer,
        TimelinePlan plan,
        bool backward,
        string stateTick,
        string stateCycles,
        string destination,
        int depth)
    {
        var indent = Indent(depth);
        if (!IsLooping(plan))
        {
            if (destination.StartsWith("var ", StringComparison.Ordinal))
                Line(writer, $"{indent}{destination} = {stateCycles};");
            return;
        }
        Line(writer, $"{indent}uint cycles;");
        if (!backward)
        {
            Line(writer, $"{indent}if (tick >= {stateTick})");
            Line(writer, $"{indent}    cycles = quotient - previousQuotient;");
            Line(writer, $"{indent}else");
            Line(writer, $"{indent}    cycles = effective < previousEffective ? 1u : 0u;");
            Line(writer, $"{indent}if (cycles > ushort.MaxValue - {stateCycles})");
            Line(writer, $"{indent}    throw new global::System.ArgumentOutOfRangeException(\"ticks\", \"Playback cycle capacity exceeded.\");");
            Line(writer, $"{indent}{destination} = (ushort)({stateCycles} + cycles);");
            return;
        }
        Line(writer, $"{indent}if (tick <= {stateTick})");
        Line(writer, $"{indent}    cycles = previousQuotient - quotient;");
        Line(writer, $"{indent}else");
        Line(writer, $"{indent}    cycles = effective > previousEffective ? 1u : 0u;");
        Line(writer, $"{indent}{destination} = (ushort)({stateCycles} - global::System.Math.Min({stateCycles}, cycles));");
    }

    private static void EmitRegions(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot[][] regions,
        bool backward,
        int depth)
    {
        var upper = IsLooping(plan) ? regions.Length - 2 : regions.Length - 1;
        EmitRegionTree(writer, plan, regions, 0, Math.Max(0, upper), depth, backward);
    }

    private static void EmitRegionTree(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot[][] regions,
        int lower,
        int upper,
        int depth,
        bool backward)
    {
        var indent = Indent(depth);
        if (lower == upper)
        {
            EmitRegion(writer, plan, regions[lower], lower, indent, backward);
            return;
        }
        var middle = (lower + upper + 1) / 2;
        Line(writer, $"{indent}if (effective < {U(plan.RegionStarts[middle])})");
        Line(writer, $"{indent}{{");
        EmitRegionTree(writer, plan, regions, lower, middle - 1, depth + 1, backward);
        Line(writer, $"{indent}}}");
        Line(writer, $"{indent}else");
        Line(writer, $"{indent}{{");
        EmitRegionTree(writer, plan, regions, middle, upper, depth + 1, backward);
        Line(writer, $"{indent}}}");
    }

    private static void EmitRegion(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot[] works,
        int region,
        string indent,
        bool backward)
    {
        var method = backward ? "Backward" : "Forward";
        var states = new Dictionary<(uint EnterF, uint EnterB), string>();
        for (var ordinal = 0; ordinal < works.Length; ordinal++)
        {
            var work = works[ordinal];
            var suffix = I(region) + "_" + I(ordinal);
            if (!states.TryGetValue((work.EnterF, work.EnterB), out var state))
            {
                state = "state_" + suffix;
                states.Add((work.EnterF, work.EnterB), state);
                EmitState(writer, plan, work, region, indent, state, backward);
            }
            var clip = $"s_clip{I(work.First)}";
            if (work.Second != EmittedWorkSlot.Single)
            {
                Line(writer, $"{indent}var factor_{suffix} = {Factor(work)};");
                Line(writer, $"{indent}s_track{I(work.Index)}.Blend(in s_clip{I(work.First)}, in s_clip{I(work.Second)}, factor_{suffix}, out var clip_{suffix});");
                clip = "clip_" + suffix;
            }
            Line(writer,
                $"{indent}TResult.{method}({I(ordinal)}, {I(works.Length)}, {I(work.Index)}, in s_track{I(work.Index)}, in {clip}, {state}, effective, in input, ref result);");
        }
    }

    private static void EmitState(
        StringBuilder writer,
        TimelinePlan plan,
        EmittedWorkSlot work,
        int region,
        string indent,
        string state,
        bool backward)
    {
        var lower = plan.RegionStarts[region];
        var upper = region + 1 < plan.RegionStarts.Length ? plan.RegionStarts[region + 1] : uint.MaxValue;
        var exitTick = backward ? work.EnterF : work.EnterB - 1u;
        var exitPossible = exitTick >= lower && exitTick < upper;
        var enterPossible = backward
            ? !IsLooping(plan) || work.EnterB < plan.Duration
            : work.EnterF != 0u;
        var crossed = backward
            ? $"previousEffective >= {U(work.EnterB)}"
            : $"previousEffective < {U(work.EnterF)}";
        var enter = IsLooping(plan)
            ? enterPossible ? $"cycles != 0u || {crossed}" : "cycles != 0u"
            : enterPossible ? crossed : "false";
        if (exitPossible)
        {
            Line(writer, $"{indent}var {state} = effective == {U(exitTick)}");
            Line(writer, $"{indent}    ? global::Tl.ClipState.Exit");
            Line(writer, $"{indent}    : {enter} ? global::Tl.ClipState.Enter : global::Tl.ClipState.Stay;");
            return;
        }
        Line(writer, enter == "false"
            ? $"{indent}const global::Tl.ClipState {state} = global::Tl.ClipState.Stay;"
            : $"{indent}var {state} = {enter} ? global::Tl.ClipState.Enter : global::Tl.ClipState.Stay;");
    }

    private static void EmitFlags(
        StringBuilder writer,
        TimelinePlan plan,
        bool backward,
        string effective,
        int depth)
    {
        var indent = Indent(depth);
        Line(writer, $"{indent}var flags = global::Tl.PlaybackFlags.Started;");
        if (plan.Definition.Loops)
        {
            if (plan.Duration != 0u)
            {
                Line(writer, $"{indent}if ({effective} == Duration - 1u)");
                Line(writer, $"{indent}    flags |= global::Tl.PlaybackFlags.LastLoopFrame;");
            }
            return;
        }
        if (plan.Duration == 0u)
        {
            Line(writer, $"{indent}flags |= global::Tl.PlaybackFlags.Completed;");
            return;
        }
        var completion = backward ? $"{effective} == 0u" : $"{effective} >= Duration - 1u";
        Line(writer, $"{indent}if ({completion})");
        Line(writer, $"{indent}    flags |= global::Tl.PlaybackFlags.Completed;");
    }

    private static void EmitValidation(StringBuilder writer, string kernelName, int depth)
    {
        var indent = Indent(depth);
        Line(writer, $"{indent}if (!from.Has(global::Tl.PlaybackFlags.Started))");
        Line(writer, $"{indent}    throw new global::System.InvalidOperationException(\"Playback was never started; mint one with {kernelName}.Start.\");");
        Line(writer, $"{indent}if (from.Has(global::Tl.PlaybackFlags.Stopped))");
        Line(writer, $"{indent}    throw new global::System.InvalidOperationException(\"Playback is stopped.\");");
    }

    private static void EmitConstraints(StringBuilder writer, TimelineDefinition definition)
    {
        Line(writer, "        where TInput : struct");
        Line(writer,
            $"        where TResult : struct, global::Tl.ITrack<{definition.TrackTypeName}, {definition.ClipTypeName}, TInput, TResult>");
    }

    private static string Factor(EmittedWorkSlot work) => work.FactorLength <= 1u
        ? "0.5f"
        : $"(effective - {U(work.FactorStart)}) / {I(work.FactorLength - 1u)}f";

    private static bool IsLooping(TimelinePlan plan) => plan.Definition.Loops && plan.Duration != 0u;

    private static string Boolean(bool value) => value ? "true" : "false";

    private static string Indent(int depth) => new(' ', depth * 4);

    private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string I(uint value) => value.ToString(CultureInfo.InvariantCulture);

    private static string U(uint value) => I(value) + "u";

    private static void Line(StringBuilder writer, string value = "")
    {
        writer.Append(value);
        writer.Append('\n');
    }
}
