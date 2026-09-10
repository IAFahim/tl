using System.Globalization;
using System.Text;
using Tl.Gen.Model;

namespace Tl.Gen.CSharp;

internal static class HeterogeneousEmitter
{
    private sealed record Schema(
        int Index,
        HeterogeneousTimeline Representative,
        IReadOnlyList<TimelineSlot> Inputs,
        IReadOnlyList<TimelineSlot> Outputs);

    private sealed record RoutedTimeline(
        HeterogeneousTimeline Timeline,
        int Index,
        int Module,
        byte Ordinal,
        Schema Schema);

    private sealed record Compilation(
        IReadOnlyList<RoutedTimeline> Timelines,
        IReadOnlyList<Schema> Schemas,
        int ModuleCount);

    private sealed record Work(
        HeterogeneousTrack Track,
        HeterogeneousClip First,
        HeterogeneousClip? Second,
        uint EnterForward,
        uint EnterBackward,
        uint FactorStart,
        uint FactorLength);

    private sealed record Region(uint Start, uint End, IReadOnlyList<Work> Works);

    internal static string Emit(HeterogeneousTimeline timeline)
    {
        var compilation = Compile([timeline]);
        return EmitTimeline(compilation, compilation.Timelines[0]);
    }

    internal static IReadOnlyList<CompileArtifact> EmitCompilation(IReadOnlyList<HeterogeneousTimeline> timelines)
    {
        var compilation = Compile(timelines);
        var artifacts = compilation.Timelines
            .Select(timeline => new CompileArtifact($"Tl{timeline.Index}.g.cs", EmitTimeline(compilation, timeline)))
            .ToList();
        if (compilation.Timelines.Count == 0)
            return artifacts;
        artifacts.Add(new CompileArtifact("TlModules.g.cs", EmitModules(compilation.ModuleCount)));
        artifacts.AddRange(compilation.Schemas.Select(schema =>
            new CompileArtifact($"TlSchema{schema.Index}.g.cs", EmitSchema(compilation, schema))));
        return artifacts;
    }

    internal static int RegionCount(HeterogeneousTimeline timeline) => Regions(timeline).Count;

    private static string EmitTimeline(Compilation compilation, RoutedTimeline routed)
    {
        var timeline = routed.Timeline;
        var schema = Qualified(routed.Schema);
        var regions = Regions(timeline);
        var writer = new StringBuilder();
        Line(writer, "#nullable enable");
        foreach (var directive in timeline.Usings)
            Line(writer, directive);
        if (timeline.Usings.Count != 0)
            Line(writer);
        if (timeline.Namespace.Length != 0)
        {
            Line(writer, $"namespace {timeline.Namespace};");
            Line(writer);
        }
        Line(writer, $"public readonly partial struct {timeline.Name}");
        Line(writer, "{");
        Line(writer, $"    public const uint Duration = {U(timeline.Duration)};");
        Line(writer, $"    public const bool Loops = {Bool(timeline.Loops)};");
        Line(writer, $"    public const int TrackCount = {I(timeline.Tracks.Count)};");
        Line(writer, $"    public const int ClipCount = {I(timeline.Clips.Count)};");
        Line(writer, $"    public const int RegionCount = {I(regions.Count)};");
        Line(writer, "    public const int MaxBatchLength = 256;");
        Line(writer, $"    public static nuint StaticDataBytes => {StaticDataBytes(timeline)};");
        EmitData(writer, timeline);
        Line(writer, "    private static class Dynamic");
        Line(writer, "    {");
        Line(writer, $"        internal static readonly ushort Id = global::Tl.Timeline.RegisterCompiled(Duration, Loops, new global::Tl.CompiledRoute(global::__TlGeneratedModules.Module{I(routed.Module)}, {I(routed.Ordinal)}));");
        Line(writer, "    }");
        Line(writer, "    public static ushort Id => Dynamic.Id;");
        Line(writer);
        EmitFacade(writer, timeline);
        Line(writer);
        EmitInput(writer, routed, schema, HasCompatibleAlternate(compilation, routed));
        Line(writer);
        EmitOutput(writer, timeline, schema);
        Line(writer);
        EmitValidation(writer, timeline, schema);
        Line(writer);
        EmitPosition(writer, timeline, false);
        Line(writer);
        EmitPosition(writer, timeline, true);
        Line(writer);
        EmitScalar(writer, timeline, schema, false);
        Line(writer);
        EmitScalar(writer, timeline, schema, true);
        Line(writer);
        EmitBatch(writer, timeline, schema, false);
        Line(writer);
        EmitBatch(writer, timeline, schema, true);
        Line(writer);
        EmitApply(writer, timeline, schema, regions, false);
        Line(writer);
        EmitApply(writer, timeline, schema, regions, true);
        Line(writer, "}");
        return writer.ToString();
    }

    private static Compilation Compile(IReadOnlyList<HeterogeneousTimeline> timelines)
    {
        if (timelines.Count > ushort.MaxValue + 1)
            throw new InvalidOperationException("A compilation may contain at most 65536 timelines.");

        var schemas = new List<Schema>();
        var byKey = new Dictionary<string, Schema>(StringComparer.Ordinal);
        var routed = new List<RoutedTimeline>(timelines.Count);
        for (var index = 0; index < timelines.Count; index++)
        {
            var timeline = timelines[index];
            var key = SchemaKey(timeline);
            if (!byKey.TryGetValue(key, out var schema))
            {
                schema = new Schema(schemas.Count, timeline, timeline.Inputs, timeline.Outputs);
                schemas.Add(schema);
                byKey.Add(key, schema);
            }
            routed.Add(new RoutedTimeline(timeline, index, index >> 8, (byte)index, schema));
        }
        return new Compilation(routed, schemas, (timelines.Count + byte.MaxValue) / 256);
    }

    private static string EmitModules(int count)
    {
        var writer = new StringBuilder();
        Line(writer, "#nullable enable");
        Line(writer);
        Line(writer, "internal static class __TlGeneratedModules");
        Line(writer, "{");
        for (var module = 0; module < count; module++)
            Line(writer, $"    internal static readonly byte Module{I(module)} = global::Tl.Timeline.RegisterModule();");
        Line(writer, "}");
        return writer.ToString();
    }

    private static string EmitSchema(Compilation compilation, Schema schema)
    {
        var writer = new StringBuilder();
        Line(writer, "#nullable enable");
        foreach (var directive in schema.Representative.Usings)
            Line(writer, directive);
        if (schema.Representative.Usings.Count != 0)
            Line(writer);
        if (schema.Representative.Namespace.Length != 0)
        {
            Line(writer, $"namespace {schema.Representative.Namespace};");
            Line(writer);
        }
        Line(writer, $"internal static class __TlGeneratedSchema{I(schema.Index)}");
        Line(writer, "{");
        EmitCanonicalInput(writer, schema);
        Line(writer);
        EmitCanonicalOutput(writer, schema);
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.InlineArray(256)]");
        Line(writer, "    private struct ModuleMap");
        Line(writer, "    {");
        Line(writer, "        private ushort _element0;");
        Line(writer, "    }");
        Line(writer);
        Line(writer, "    private static ModuleMap s_modules = CreateModules();");
        Line(writer);
        Line(writer, "    private static ModuleMap CreateModules()");
        Line(writer, "    {");
        Line(writer, "        var modules = default(ModuleMap);");
        foreach (var module in compilation.Timelines
            .Where(target => Compatible(schema, target.Schema))
            .Select(static target => target.Module)
            .Distinct()
            .Order())
            Line(writer, $"        modules[global::__TlGeneratedModules.Module{I(module)}] = {I(module + 1)};");
        Line(writer, "        return modules;");
        Line(writer, "    }");
        foreach (var backward in new[] { false, true })
        {
            Line(writer);
            EmitRouter(writer, compilation, schema, backward, false);
            Line(writer);
            EmitRouter(writer, compilation, schema, backward, true);
        }
        Line(writer, "}");
        return writer.ToString();
    }

    private static void EmitCanonicalInput(StringBuilder writer, Schema schema)
    {
        Line(writer, "    internal readonly ref struct Input");
        Line(writer, "    {");
        foreach (var slot in schema.Inputs)
            Line(writer, $"        internal readonly ref readonly {slot.TypeName} _{slot.Name};");
        if (schema.Inputs.Count != 0)
        {
            Line(writer, "        internal readonly bool IsValid;");
            Line(writer);
            Line(writer, "        internal Input(");
            for (var index = 0; index < schema.Inputs.Count; index++)
            {
                var slot = schema.Inputs[index];
                var suffix = index + 1 == schema.Inputs.Count ? ")" : ",";
                Line(writer, $"            ref readonly {slot.TypeName} {Escape(slot.Name)}{suffix}");
            }
            Line(writer, "        {");
            foreach (var slot in schema.Inputs)
                Line(writer, $"            _{slot.Name} = ref {Escape(slot.Name)};");
            Line(writer, $"            IsValid = {string.Join(" && ", schema.Inputs.Select(slot => $"!global::System.Runtime.CompilerServices.Unsafe.IsNullRef(ref global::System.Runtime.CompilerServices.Unsafe.AsRef(in {Escape(slot.Name)}))"))};");
            Line(writer, "        }");
        }
        Line(writer, "    }");
    }

    private static void EmitCanonicalOutput(StringBuilder writer, Schema schema)
    {
        Line(writer, "    internal ref struct Output");
        Line(writer, "    {");
        foreach (var slot in schema.Outputs)
            Line(writer, $"        internal ref {slot.TypeName} _{slot.Name};");
        if (schema.Outputs.Count != 0)
        {
            Line(writer, "        internal readonly bool IsValid;");
            Line(writer);
            Line(writer, "        internal Output(");
            for (var index = 0; index < schema.Outputs.Count; index++)
            {
                var slot = schema.Outputs[index];
                var suffix = index + 1 == schema.Outputs.Count ? ")" : ",";
                Line(writer, $"            ref {slot.TypeName} {Escape(slot.Name)}{suffix}");
            }
            Line(writer, "        {");
            foreach (var slot in schema.Outputs)
                Line(writer, $"            _{slot.Name} = ref {Escape(slot.Name)};");
            Line(writer, $"            IsValid = {string.Join(" && ", schema.Outputs.Select(slot => $"!global::System.Runtime.CompilerServices.Unsafe.IsNullRef(ref {Escape(slot.Name)})"))};");
            Line(writer, "        }");
        }
        Line(writer, "    }");
    }

    private static void EmitRouter(
        StringBuilder writer,
        Compilation compilation,
        Schema caller,
        bool backward,
        bool batch)
    {
        var direction = backward ? "Backward" : "Forward";
        var tickType = batch ? "global::System.ReadOnlySpan<uint> ticks" : "uint tick";
        var tickValue = batch ? "ticks" : "tick";
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]");
        Line(writer, $"    internal static bool Try{direction}(");
        Line(writer, "        ushort id,");
        Line(writer, "        in global::Tl.Playback playback,");
        Line(writer, $"        {tickType},");
        Line(writer, "        in Input input,");
        Line(writer, "        ref Output output,");
        Line(writer, "        out global::Tl.Playback next)");
        Line(writer, "    {");
        Line(writer, "        if (!global::Tl.Timeline.TryGetCompiledRoute(id, out var route))");
        Line(writer, "        {");
        Line(writer, "            next = playback;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, "        switch (s_modules[route.Module])");
        Line(writer, "        {");
        foreach (var module in compilation.Timelines
            .Where(target => Compatible(caller, target.Schema))
            .GroupBy(static target => target.Module)
            .OrderBy(static group => group.Key))
        {
            Line(writer, $"            case {I(module.Key + 1)}:");
            Line(writer, "                switch (route.Ordinal)");
            Line(writer, "                {");
            foreach (var target in module.OrderBy(static item => item.Ordinal))
                EmitRouteCase(writer, caller, target, direction, tickValue);
            Line(writer, "                }");
            Line(writer, "                break;");
        }
        Line(writer, "        }");
        Line(writer, "        next = playback;");
        Line(writer, "        return false;");
        Line(writer, "    }");
    }

    private static void EmitRouteCase(
        StringBuilder writer,
        Schema caller,
        RoutedTimeline target,
        string direction,
        string ticks)
    {
        var targetType = Qualified(target.Timeline);
        var targetSchema = Qualified(target.Schema);
        Line(writer, $"                    case {I(target.Ordinal)}:");
        Line(writer, "                    {");
        if (caller.Index == target.Schema.Index)
            Line(writer, $"                        return {targetType}.{direction}Kernel(id, in playback, {ticks}, in input, ref output, out next);");
        else
        {
            Line(writer, target.Schema.Inputs.Count == 0
                ? $"                        var targetInput = default({targetSchema}.Input);"
                : $"                        var targetInput = new {targetSchema}.Input({string.Join(", ", target.Schema.Inputs.Select(slot => $"in input._{slot.Name}"))});");
            Line(writer, target.Schema.Outputs.Count == 0
                ? $"                        var targetOutput = default({targetSchema}.Output);"
                : $"                        var targetOutput = new {targetSchema}.Output({string.Join(", ", target.Schema.Outputs.Select(slot => $"ref output._{slot.Name}"))});");
            Line(writer, $"                        return {targetType}.{direction}Kernel(id, in playback, {ticks}, in targetInput, ref targetOutput, out next);");
        }
        Line(writer, "                    }");
    }

    private static bool Compatible(Schema caller, Schema target)
        => target.Inputs.All(required => caller.Inputs.Any(candidate => SameSlot(candidate, required)))
            && target.Outputs.All(required => caller.Outputs.Any(candidate => SameSlot(candidate, required)));

    private static bool HasCompatibleAlternate(Compilation compilation, RoutedTimeline caller)
        => compilation.Timelines.Any(target => target.Index != caller.Index && Compatible(caller.Schema, target.Schema));

    private static bool SameSlot(TimelineSlot left, TimelineSlot right)
        => left.Name == right.Name && TypeKey(left.TypeName) == TypeKey(right.TypeName) && left.Mode == right.Mode;

    private static string SchemaKey(HeterogeneousTimeline timeline)
        => timeline.Namespace + "\n"
            + string.Join("\n", timeline.Inputs.Concat(timeline.Outputs)
                .Select(slot => $"{(byte)slot.Mode}:{slot.Name}:{TypeKey(slot.TypeName)}"));

    private static string TypeKey(string type)
        => type.StartsWith("global::", StringComparison.Ordinal) ? type[8..] : type;

    private static string Qualified(Schema schema)
        => Qualified(schema.Representative.Namespace, $"__TlGeneratedSchema{I(schema.Index)}");

    private static string Qualified(HeterogeneousTimeline timeline)
        => Qualified(timeline.Namespace, timeline.Name);

    private static string TypedPlayback(HeterogeneousTimeline timeline)
        => $"global::Tl.Playback<{Qualified(timeline)}>";

    private static string Qualified(string ns, string name)
        => ns.Length == 0 ? $"global::{name}" : $"global::{ns}.{name}";

    private static void EmitData(StringBuilder writer, HeterogeneousTimeline timeline)
    {
        foreach (var track in timeline.Tracks)
            Line(writer, $"    private static readonly {track.TypeName} s_track{I(track.Index)} = {track.Expression};");
        for (var index = 0; index < timeline.Clips.Count; index++)
        {
            var clip = timeline.Clips[index];
            Line(writer, $"    private static readonly {clip.TypeName} s_clip{I(index)} = {clip.Expression};");
        }
    }

    private static string StaticDataBytes(HeterogeneousTimeline timeline)
    {
        var values = timeline.Tracks.Select(static track => track.TypeName)
            .Concat(timeline.Clips.Select(static clip => clip.TypeName))
            .GroupBy(static type => type, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(static group => $"(nuint)global::System.Runtime.CompilerServices.Unsafe.SizeOf<{group.Key}>() * (nuint){I(group.Count())}")
            .ToArray();
        return values.Length == 0 ? "0u" : string.Join(" + ", values);
    }

    private static void EmitFacade(StringBuilder writer, HeterogeneousTimeline timeline)
    {
        var playback = TypedPlayback(timeline);
        Line(writer, $"    public static {playback} Start(uint at = 0u)");
        Line(writer, $"        => global::Tl.Timeline.CreateTypedPlayback<{Qualified(timeline)}>(at, 0, global::Tl.PlaybackFlags.Started);");
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, "    public static bool TryStop(");
        Line(writer, $"        in {playback} playback,");
        Line(writer, $"        out {playback} stopped)");
        Line(writer, "    {");
        Line(writer, "        if (!playback.Has(global::Tl.PlaybackFlags.Started))");
        Line(writer, "        {");
        Line(writer, "            stopped = playback;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, "        stopped = playback.Has(global::Tl.PlaybackFlags.Stopped)");
        Line(writer, "            ? playback");
        Line(writer, $"            : global::Tl.Timeline.CreateTypedPlayback<{Qualified(timeline)}>(playback.Tick, playback.Cycles, playback.Flags | global::Tl.PlaybackFlags.Stopped);");
        Line(writer, "        return true;");
        Line(writer, "    }");
    }

    private static void EmitInput(
        StringBuilder writer,
        RoutedTimeline routed,
        string schema,
        bool hasCompatibleAlternate)
    {
        var timeline = routed.Timeline;
        Line(writer, "    public readonly ref struct Input : global::Tl.ITimelineInput<Input, Output>");
        Line(writer, "    {");
        Line(writer, $"        internal readonly {schema}.Input _context;");
        if (timeline.Inputs.Count != 0)
        {
            Line(writer);
            Line(writer, "        public Input(");
            for (var index = 0; index < timeline.Inputs.Count; index++)
            {
                var slot = timeline.Inputs[index];
                var suffix = index + 1 == timeline.Inputs.Count ? ")" : ",";
                Line(writer, $"            ref readonly {slot.TypeName} {Escape(slot.Name)}{suffix}");
            }
            Line(writer, "        {");
            Line(writer, $"            _context = new {schema}.Input({string.Join(", ", timeline.Inputs.Select(slot => $"in {Escape(slot.Name)}"))});");
            Line(writer, "        }");
            Line(writer);
        }
        else
        {
            Line(writer, "        public Input()");
            Line(writer, "        {");
            Line(writer, "            _context = default;");
            Line(writer, "        }");
            Line(writer);
        }
        EmitInputBridge(writer, routed, "Forward", schema, false, hasCompatibleAlternate);
        Line(writer);
        EmitInputBridge(writer, routed, "Backward", schema, false, hasCompatibleAlternate);
        Line(writer);
        EmitInputBridge(writer, routed, "Forward", schema, true, hasCompatibleAlternate);
        Line(writer);
        EmitInputBridge(writer, routed, "Backward", schema, true, hasCompatibleAlternate);
        Line(writer, "    }");
    }

    private static void EmitInputBridge(
        StringBuilder writer,
        RoutedTimeline routed,
        string name,
        string schema,
        bool batch,
        bool hasCompatibleAlternate)
    {
        var ticks = batch ? "global::System.ReadOnlySpan<uint> ticks" : "uint tick";
        var value = batch ? "ticks" : "tick";
        Line(writer, "        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"        public static bool Try{name}(");
        Line(writer, "            ushort id,");
        Line(writer, "            in global::Tl.Playback playback,");
        Line(writer, $"            {ticks},");
        Line(writer, "            scoped in Input input,");
        Line(writer, "            scoped ref Output output,");
        Line(writer, "            out global::Tl.Playback next)");
        Line(writer, "        {");
        if (hasCompatibleAlternate)
        {
            Line(writer, "            if (!global::Tl.Timeline.TryGetCompiledRoute(id, out var route))");
            Line(writer, "            {");
            Line(writer, "                next = playback;");
            Line(writer, "                return false;");
            Line(writer, "            }");
            Line(writer, $"            if (route.Module != global::__TlGeneratedModules.Module{I(routed.Module)} || route.Ordinal != {I(routed.Ordinal)})");
            Line(writer, $"                return {schema}.Try{name}(id, in playback, {value}, in input._context, ref output._context, out next);");
        }
        else
        {
            Line(writer, "            if (id != Id)");
            Line(writer, "            {");
            Line(writer, "                next = playback;");
            Line(writer, "                return false;");
            Line(writer, "            }");
        }
        Line(writer, $"            return {name}Kernel(id, in playback, {value}, in input._context, ref output._context, out next);");
        Line(writer, "        }");
    }

    private static void EmitOutput(StringBuilder writer, HeterogeneousTimeline timeline, string schema)
    {
        Line(writer, "    public ref struct Output");
        Line(writer, "    {");
        Line(writer, $"        internal {schema}.Output _context;");
        if (timeline.Outputs.Count != 0)
        {
            Line(writer);
            Line(writer, "        public Output(");
            for (var index = 0; index < timeline.Outputs.Count; index++)
            {
                var slot = timeline.Outputs[index];
                var suffix = index + 1 == timeline.Outputs.Count ? ")" : ",";
                Line(writer, $"            ref {slot.TypeName} {Escape(slot.Name)}{suffix}");
            }
            Line(writer, "        {");
            Line(writer, $"            _context = new {schema}.Output({string.Join(", ", timeline.Outputs.Select(slot => $"ref {Escape(slot.Name)}"))});");
            Line(writer, "        }");
        }
        else
        {
            Line(writer);
            Line(writer, "        public Output()");
            Line(writer, "        {");
            Line(writer, "            _context = default;");
            Line(writer, "        }");
        }
        Line(writer, "    }");
    }

    private static void EmitValidation(StringBuilder writer, HeterogeneousTimeline timeline, string schema)
    {
        var playback = TypedPlayback(timeline);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    private static bool CanRun(in {playback} playback, in {schema}.Input input, ref {schema}.Output output)");
        Line(writer, "    {");
        Line(writer, "        if ((playback.Flags & (global::Tl.PlaybackFlags.Started | global::Tl.PlaybackFlags.Stopped)) != global::Tl.PlaybackFlags.Started)");
        Line(writer, "            return false;");
        if (timeline.Inputs.Count != 0)
            Line(writer, "        if (!input.IsValid) return false;");
        if (timeline.Outputs.Count != 0)
            Line(writer, "        if (!output.IsValid) return false;");
        Line(writer, "        return true;");
        Line(writer, "    }");
    }

    private static void EmitPosition(StringBuilder writer, HeterogeneousTimeline timeline, bool backward)
    {
        var direction = backward ? "Backward" : "Forward";
        var playback = TypedPlayback(timeline);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    private static bool Position{direction}(");
        Line(writer, $"        in {playback} from,");
        Line(writer, "        uint tick,");
        Line(writer, "        out uint effective,");
        Line(writer, "        out uint previousEffective,");
        Line(writer, "        out uint crossedCycles,");
        Line(writer, "        out ushort cycles)");
        Line(writer, "    {");
        if (!timeline.Loops || timeline.Duration == 0)
        {
            Line(writer, "        effective = tick;");
            Line(writer, "        previousEffective = from.Tick;");
            Line(writer, "        crossedCycles = 0;");
            Line(writer, "        cycles = from.Cycles;");
            Line(writer, "        return true;");
            Line(writer, "    }");
            return;
        }
        Line(writer, "        var previousQuotient = from.Tick / Duration;");
        Line(writer, "        previousEffective = from.Tick - previousQuotient * Duration;");
        Line(writer, "        var quotient = tick / Duration;");
        Line(writer, "        effective = tick - quotient * Duration;");
        if (!backward)
        {
            Line(writer, "        crossedCycles = tick >= from.Tick");
            Line(writer, "            ? quotient - previousQuotient");
            Line(writer, "            : effective < previousEffective ? 1u : 0u;");
            Line(writer, "        if (crossedCycles > ushort.MaxValue - from.Cycles)");
            Line(writer, "        {");
            Line(writer, "            cycles = from.Cycles;");
            Line(writer, "            return false;");
            Line(writer, "        }");
            Line(writer, "        cycles = (ushort)(from.Cycles + crossedCycles);");
        }
        else
        {
            Line(writer, "        crossedCycles = tick <= from.Tick");
            Line(writer, "            ? previousQuotient - quotient");
            Line(writer, "            : effective > previousEffective ? 1u : 0u;");
            Line(writer, "        cycles = (ushort)(from.Cycles - global::System.Math.Min(from.Cycles, crossedCycles));");
        }
        Line(writer, "        return true;");
        Line(writer, "    }");
    }

    private static void EmitScalar(StringBuilder writer, HeterogeneousTimeline timeline, string schema, bool backward)
    {
        var direction = backward ? "Backward" : "Forward";
        var playback = TypedPlayback(timeline);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    public static bool Try{direction}(");
        Line(writer, $"        in {playback} playback,");
        Line(writer, "        uint tick,");
        Line(writer, "        scoped in Input input,");
        Line(writer, "        scoped ref Output output,");
        Line(writer, $"        out {playback} next)");
        Line(writer, $"        => {direction}TypedKernel(in playback, tick, in input._context, ref output._context, out next);");
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    private static bool {direction}TypedKernel(");
        Line(writer, $"        in {playback} playback,");
        Line(writer, "        uint tick,");
        Line(writer, $"        in {schema}.Input input,");
        Line(writer, $"        ref {schema}.Output output,");
        Line(writer, $"        out {playback} next)");
        Line(writer, "    {");
        Line(writer, $"        if (!CanRun(in playback, in input, ref output) || !Position{direction}(in playback, tick, out var effective, out var previousEffective, out var crossedCycles, out var cycles))");
        Line(writer, "        {");
        Line(writer, "            next = playback;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, $"        Apply{direction}(effective, previousEffective, crossedCycles, in input, ref output);");
        EmitNext(writer, timeline, backward, "tick", "effective", "cycles", 2);
        Line(writer, "        return true;");
        Line(writer, "    }");
        Line(writer);
        EmitDynamicKernel(writer, timeline, schema, backward, false);
    }

    private static void EmitBatch(StringBuilder writer, HeterogeneousTimeline timeline, string schema, bool backward)
    {
        var direction = backward ? "Backward" : "Forward";
        var playback = TypedPlayback(timeline);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    public static bool Try{direction}(");
        Line(writer, $"        in {playback} playback,");
        Line(writer, "        global::System.ReadOnlySpan<uint> ticks,");
        Line(writer, "        scoped in Input input,");
        Line(writer, "        scoped ref Output output,");
        Line(writer, $"        out {playback} next)");
        Line(writer, $"        => {direction}TypedKernel(in playback, ticks, in input._context, ref output._context, out next);");
        Line(writer);
        Line(writer, $"    private static bool {direction}TypedKernel(");
        Line(writer, $"        in {playback} playback,");
        Line(writer, "        global::System.ReadOnlySpan<uint> ticks,");
        Line(writer, $"        in {schema}.Input input,");
        Line(writer, $"        ref {schema}.Output output,");
        Line(writer, $"        out {playback} next)");
        Line(writer, "    {");
        Line(writer, "        if (!CanRun(in playback, in input, ref output))");
        Line(writer, "        {");
        Line(writer, "            next = playback;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, "        if (ticks.Length > MaxBatchLength)");
        Line(writer, "        {");
        Line(writer, "            next = playback;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, "        global::System.Span<uint> stableTicks = stackalloc uint[ticks.Length];");
        Line(writer, "        ticks.CopyTo(stableTicks);");
        if (!timeline.Loops || timeline.Duration == 0)
        {
            Line(writer, "        if (stableTicks.IsEmpty)");
            Line(writer, "        {");
            Line(writer, "            next = playback;");
            Line(writer, "            return true;");
            Line(writer, "        }");
            Line(writer, "        var effective = playback.Tick;");
            Line(writer, "        var cycles = playback.Cycles;");
            Line(writer, "        foreach (var tick in stableTicks)");
            Line(writer, "        {");
            Line(writer, "            var previousEffective = effective;");
            Line(writer, "            effective = tick;");
            Line(writer, $"            Apply{direction}(effective, previousEffective, 0u, in input, ref output);");
            Line(writer, "        }");
            EmitNext(writer, timeline, backward, "effective", "effective", "cycles", 2);
            Line(writer, "        return true;");
            Line(writer, "    }");
            Line(writer);
            EmitDynamicKernel(writer, timeline, schema, backward, true);
            return;
        }
        if (timeline.Loops && timeline.Duration != 0)
        {
            Line(writer, "        var validated = playback;");
            Line(writer, "        foreach (var tick in stableTicks)");
            Line(writer, "        {");
            Line(writer, $"            if (!Position{direction}(in validated, tick, out var effective, out _, out _, out var cycles))");
            Line(writer, "            {");
            Line(writer, "                next = playback;");
            Line(writer, "                return false;");
            Line(writer, "            }");
            EmitNext(writer, timeline, backward, "tick", "effective", "cycles", 3, "validated");
            Line(writer, "        }");
        }
        Line(writer, "        var state = playback;");
        Line(writer, "        foreach (var tick in stableTicks)");
        Line(writer, "        {");
        Line(writer, $"            if (!Position{direction}(in state, tick, out var effective, out var previousEffective, out var crossedCycles, out var cycles))");
        Line(writer, "            {");
        Line(writer, "                next = playback;");
        Line(writer, "                return false;");
        Line(writer, "            }");
        Line(writer, $"            Apply{direction}(effective, previousEffective, crossedCycles, in input, ref output);");
        EmitNext(writer, timeline, backward, "tick", "effective", "cycles", 3, "state");
        Line(writer, "        }");
        Line(writer, "        next = state;");
        Line(writer, "        return true;");
        Line(writer, "    }");
        Line(writer);
        EmitDynamicKernel(writer, timeline, schema, backward, true);
    }

    private static void EmitDynamicKernel(
        StringBuilder writer,
        HeterogeneousTimeline timeline,
        string schema,
        bool backward,
        bool batch)
    {
        var direction = backward ? "Backward" : "Forward";
        var tickType = batch ? "global::System.ReadOnlySpan<uint> ticks" : "uint tick";
        var tickValue = batch ? "ticks" : "tick";
        Line(writer, $"    internal static bool {direction}Kernel(");
        Line(writer, "        ushort id,");
        Line(writer, "        in global::Tl.Playback playback,");
        Line(writer, $"        {tickType},");
        Line(writer, $"        in {schema}.Input input,");
        Line(writer, $"        ref {schema}.Output output,");
        Line(writer, "        out global::Tl.Playback next)");
        Line(writer, "    {");
        Line(writer, "        if (id != Id || playback.Owner != id || (playback.Flags & (global::Tl.PlaybackFlags.Started | global::Tl.PlaybackFlags.Stopped)) != global::Tl.PlaybackFlags.Started)");
        Line(writer, "        {");
        Line(writer, "            next = playback;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, $"        var typed = global::Tl.Timeline.CreateTypedPlayback<{Qualified(timeline)}>(playback.Tick, playback.Cycles, playback.Flags);");
        Line(writer, $"        if (!{direction}TypedKernel(in typed, {tickValue}, in input, ref output, out var typedNext))");
        Line(writer, "        {");
        Line(writer, "            next = playback;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, "        next = global::Tl.Timeline.CreateCompiledPlayback(id, typedNext.Tick, typedNext.Cycles, typedNext.Flags);");
        Line(writer, "        return true;");
        Line(writer, "    }");
    }

    private static void EmitNext(
        StringBuilder writer,
        HeterogeneousTimeline timeline,
        bool backward,
        string tick,
        string effective,
        string cycles,
        int depth,
        string destination = "next")
    {
        var indent = new string(' ', depth * 4);
        Line(writer, $"{indent}var flags = global::Tl.PlaybackFlags.Started;");
        if (timeline.Loops && timeline.Duration != 0)
        {
            Line(writer, $"{indent}if ({effective} == Duration - 1u)");
            Line(writer, $"{indent}    flags |= global::Tl.PlaybackFlags.LastLoopFrame;");
        }
        else if (timeline.Duration == 0)
            Line(writer, $"{indent}flags |= global::Tl.PlaybackFlags.Completed;");
        else
        {
            var completed = backward ? $"{effective} == 0u" : $"{effective} >= Duration - 1u";
            Line(writer, $"{indent}if ({completed})");
            Line(writer, $"{indent}    flags |= global::Tl.PlaybackFlags.Completed;");
        }
        Line(writer, $"{indent}{destination} = global::Tl.Timeline.CreateTypedPlayback<{Qualified(timeline)}>({tick}, {cycles}, flags);");
    }

    private static void EmitApply(
        StringBuilder writer,
        HeterogeneousTimeline timeline,
        string schema,
        IReadOnlyList<Region> regions,
        bool backward)
    {
        var direction = backward ? "Backward" : "Forward";
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    private static void Apply{direction}(uint effective, uint previousEffective, uint crossedCycles, in {schema}.Input input, ref {schema}.Output output)");
        Line(writer, "    {");
        foreach (var hook in timeline.BeforeHooks)
            EmitHook(writer, hook, backward, 2);
        for (var regionIndex = 0; regionIndex < regions.Count; regionIndex++)
        {
            var region = regions[regionIndex];
            var branch = regionIndex == 0 ? "if" : "else if";
            var condition = region.Start == 0
                ? $"effective < {U(region.End)}"
                : $"effective >= {U(region.Start)} && effective < {U(region.End)}";
            Line(writer, $"        {branch} ({condition})");
            Line(writer, "        {");
            for (var workIndex = 0; workIndex < region.Works.Count; workIndex++)
                EmitWork(writer, timeline, region.Works[workIndex], regionIndex, workIndex, backward);
            Line(writer, "        }");
        }
        foreach (var hook in timeline.AfterHooks)
            EmitHook(writer, hook, backward, 2);
        Line(writer, "    }");
    }

    private static void EmitHook(StringBuilder writer, TimelineHook hook, bool backward, int depth)
    {
        var slots = backward ? hook.BackwardSlots : hook.ForwardSlots;
        var arguments = slots.Select(slot => slot.Mode switch
        {
            SlotMode.Input => $"in input._{slot.Name}",
            SlotMode.Reference => $"ref output._{slot.Name}",
            SlotMode.Output => $"out output._{slot.Name}",
            _ => throw new InvalidOperationException(),
        });
        Line(writer, $"{new string(' ', depth * 4)}{hook.TypeName}.{(backward ? "Backward" : "Forward")}({string.Join(", ", arguments)});");
    }

    private static void EmitWork(
        StringBuilder writer,
        HeterogeneousTimeline timeline,
        Work work,
        int region,
        int ordinal,
        bool backward)
    {
        var suffix = I(region) + "_" + I(ordinal);
        var exit = backward ? work.EnterForward : work.EnterBackward - 1u;
        var crossed = backward
            ? $"previousEffective >= {U(work.EnterBackward)}"
            : $"previousEffective < {U(work.EnterForward)}";
        var enterPossible = backward
            ? !timeline.Loops || work.EnterBackward < timeline.Duration
            : work.EnterForward != 0;
        var enter = timeline.Loops
            ? enterPossible ? $"crossedCycles != 0u || {crossed}" : "crossedCycles != 0u"
            : enterPossible ? crossed : "false";
        Line(writer, $"            var state_{suffix} = effective == {U(exit)}");
        Line(writer, "                ? global::Tl.ClipState.Exit");
        Line(writer, enter == "false"
            ? "                : global::Tl.ClipState.Stay;"
            : $"                : {enter} ? global::Tl.ClipState.Enter : global::Tl.ClipState.Stay;");

        var firstIndex = ClipIndex(timeline.Clips, work.First);
        var clip = $"s_clip{I(firstIndex)}";
        if (work.Second != null)
        {
            var secondIndex = ClipIndex(timeline.Clips, work.Second);
            var factor = work.FactorLength <= 1
                ? "0.5f"
                : $"(effective - {U(work.FactorStart)}) / {I(work.FactorLength - 1)}f";
            Line(writer, $"            var factor_{suffix} = {factor};");
            Line(writer, $"            s_track{I(work.Track.Index)}.Blend(in s_clip{I(firstIndex)}, in s_clip{I(secondIndex)}, factor_{suffix}, out var clip_{suffix});");
            clip = $"clip_{suffix}";
        }
        Line(writer, $"            var frame_{suffix} = new global::Tl.Frame<{work.Track.TypeName}, {work.Track.ClipTypeName}>(in s_track{I(work.Track.Index)}, in {clip}, effective, state_{suffix}, {I(work.Track.Index)});");
        var slots = backward ? work.Track.BackwardSlots : work.Track.ForwardSlots;
        var arguments = new List<string> { $"in frame_{suffix}" };
        foreach (var slot in slots)
        {
            arguments.Add(slot.Mode switch
            {
                SlotMode.Input => $"in input._{slot.Name}",
                SlotMode.Reference => $"ref output._{slot.Name}",
                SlotMode.Output => $"out output._{slot.Name}",
                _ => throw new InvalidOperationException(),
            });
        }
        Line(writer, $"            {work.Track.TypeName}.{(backward ? "Backward" : "Forward")}({string.Join(", ", arguments)});");
    }

    private static IReadOnlyList<Region> Regions(HeterogeneousTimeline timeline)
    {
        if (timeline.Duration == 0)
            return [];
        var cuts = timeline.Clips
            .SelectMany(static clip => new[] { clip.Start, clip.End })
            .Append(0u)
            .Append(timeline.Duration)
            .Distinct()
            .Order()
            .ToArray();
        var regions = new List<Region>();
        for (var index = 0; index + 1 < cuts.Length; index++)
        {
            var start = cuts[index];
            var end = cuts[index + 1];
            var works = new List<Work>();
            foreach (var track in timeline.Tracks)
            {
                var active = timeline.Clips
                    .Where(clip => clip.TrackIndex == track.Index && clip.Start <= start && start < clip.End)
                    .OrderBy(static clip => clip.Start)
                    .ToArray();
                if (active.Length == 1)
                {
                    var clip = active[0];
                    works.Add(new Work(track, clip, null, clip.Start, clip.End, 0, 0));
                }
                else if (active.Length == 2)
                {
                    var first = active[0];
                    var second = active[1];
                    var factorStart = Math.Max(first.Start, second.Start);
                    var factorEnd = Math.Min(first.End, second.End);
                    works.Add(new Work(
                        track,
                        first,
                        second,
                        Math.Min(first.Start, second.Start),
                        Math.Max(first.End, second.End),
                        factorStart,
                        factorEnd - factorStart));
                }
            }
            regions.Add(new Region(start, end, works));
        }
        return regions;
    }

    private static string Escape(string value) => "@" + value;
    private static int ClipIndex(IReadOnlyList<HeterogeneousClip> clips, HeterogeneousClip clip)
    {
        for (var index = 0; index < clips.Count; index++)
            if (ReferenceEquals(clips[index], clip))
                return index;
        throw new InvalidOperationException();
    }

    private static string Bool(bool value) => value ? "true" : "false";
    private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string I(uint value) => value.ToString(CultureInfo.InvariantCulture);
    private static string U(uint value) => I(value) + "u";

    private static void Line(StringBuilder writer, string value = "")
    {
        writer.Append(value);
        writer.Append('\n');
    }
}
