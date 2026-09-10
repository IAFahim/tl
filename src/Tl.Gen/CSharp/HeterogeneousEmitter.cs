using System.Globalization;
using System.Text;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

internal static class HeterogeneousEmitter
{
    private sealed record Schema(int Index, HeterogeneousTimeline Representative, IReadOnlyList<TimelineSlot> ReadOnlySlots, IReadOnlyList<TimelineSlot> WritableSlots);
    private sealed record RoutedTimeline(HeterogeneousTimeline Timeline, int Index, int Module, byte Ordinal, Schema Schema);
    private sealed record Compilation(IReadOnlyList<RoutedTimeline> Timelines, IReadOnlyList<Schema> Schemas, int ModuleCount);
    private sealed record Work(HeterogeneousTrack Track, HeterogeneousClip First, HeterogeneousClip? Second, uint FactorStart, uint FactorLength);
    private sealed record Region(uint Start, uint End, IReadOnlyList<Work> Works);

    internal static string Emit(HeterogeneousTimeline timeline)
    {
        var compilation = Compile([timeline]);
        return EmitTimeline(compilation.Timelines[0]);
    }

    internal static IReadOnlyList<CompileArtifact> EmitCompilation(IReadOnlyList<HeterogeneousTimeline> timelines)
    {
        var compilation = Compile(timelines);
        var artifacts = compilation.Timelines.Select(timeline => new CompileArtifact($"Tl{timeline.Index}.g.cs", EmitTimeline(timeline))).ToList();
        if (compilation.Timelines.Count == 0)
            return artifacts;
        artifacts.Add(new CompileArtifact("TlModules.g.cs", EmitModules(compilation.ModuleCount)));
        artifacts.AddRange(compilation.Schemas.Select(schema => new CompileArtifact($"TlSchema{schema.Index}.g.cs", EmitSchema(compilation, schema))));
        return artifacts;
    }

    internal static int RegionCount(HeterogeneousTimeline timeline) => Regions(timeline).Count;

    private static string EmitTimeline(RoutedTimeline routed)
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
        Line(writer, $"    public static nuint StaticDataBytes => {StaticDataBytes(timeline)};");
        EmitImmutableData(writer, timeline);
        Line(writer, "    private static class Dynamic");
        Line(writer, "    {");
        Line(writer, $"        internal static readonly ushort Id = global::Tl.Timeline.RegisterCompiled(Duration, Loops, new global::Tl.CompiledRoute(global::__TlGeneratedModules.Module{I(routed.Module)}, {I(routed.Ordinal)}));");
        Line(writer, "    }");
        Line(writer, "    public static ushort Id => Dynamic.Id;");
        Line(writer);
        EmitFacade(writer, timeline);
        Line(writer);
        EmitTypedData(writer, timeline, schema);
        Line(writer);
        EmitDynamicData(writer, timeline, schema);
        Line(writer);
        EmitCore(writer, timeline, schema);
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
                schema = new Schema(schemas.Count, timeline, timeline.ReadOnlySlots, timeline.WritableSlots);
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
        EmitCanonicalData(writer, schema);
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
        foreach (var module in compilation.Timelines.Where(target => Compatible(schema, target.Schema)).Select(static target => target.Module).Distinct().Order())
            Line(writer, $"        modules[global::__TlGeneratedModules.Module{I(module)}] = {I(module + 1)};");
        Line(writer, "        return modules;");
        Line(writer, "    }");
        Line(writer);
        EmitRouter(writer, compilation, schema);
        Line(writer, "}");
        return writer.ToString();
    }

    private static void EmitCanonicalData(StringBuilder writer, Schema schema)
    {
        Line(writer, "    internal ref struct Data");
        Line(writer, "    {");
        foreach (var slot in schema.ReadOnlySlots)
            Line(writer, $"        internal readonly ref readonly {slot.TypeName} _{slot.Name};");
        foreach (var slot in schema.WritableSlots)
            Line(writer, $"        internal readonly ref {slot.TypeName} _{slot.Name};");
        if (schema.ReadOnlySlots.Count + schema.WritableSlots.Count != 0)
        {
            Line(writer);
            Line(writer, "        internal Data(");
            EmitParameters(writer, schema.ReadOnlySlots, schema.WritableSlots, 3);
            Line(writer, "        {");
            foreach (var slot in schema.ReadOnlySlots)
                Line(writer, $"            _{slot.Name} = ref {Escape(slot.Name)};");
            foreach (var slot in schema.WritableSlots)
                Line(writer, $"            _{slot.Name} = ref {Escape(slot.Name)};");
            Line(writer, "        }");
        }
        Line(writer, "    }");
    }

    private static void EmitRouter(StringBuilder writer, Compilation compilation, Schema caller)
    {
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]");
        Line(writer, "    internal static bool TrySeek(");
        Line(writer, "        ushort id,");
        Line(writer, "        scoped ref global::Tl.Playback playback,");
        Line(writer, "        int delta,");
        Line(writer, "        in Data data)");
        Line(writer, "    {");
        Line(writer, "        var before = playback;");
        Line(writer, "        if (before.Owner != id || (before.Flags & (global::Tl.PlaybackFlags.Started | global::Tl.PlaybackFlags.Stopped)) != global::Tl.PlaybackFlags.Started)");
        Line(writer, "            return false;");
        Line(writer, "        if (!global::Tl.Timeline.TryGetCompiledRoute(id, out var route))");
        Line(writer, "            return false;");
        Line(writer, "        switch (s_modules[route.Module])");
        Line(writer, "        {");
        foreach (var module in compilation.Timelines.Where(target => Compatible(caller, target.Schema)).GroupBy(static target => target.Module).OrderBy(static group => group.Key))
        {
            Line(writer, $"            case {I(module.Key + 1)}:");
            Line(writer, "                switch (route.Ordinal)");
            Line(writer, "                {");
            foreach (var target in module.OrderBy(static item => item.Ordinal))
                EmitRouteCase(writer, caller, target);
            Line(writer, "                }");
            Line(writer, "                break;");
        }
        Line(writer, "        }");
        Line(writer, "        return false;");
        Line(writer, "    }");
    }

    private static void EmitRouteCase(StringBuilder writer, Schema caller, RoutedTimeline target)
    {
        var targetType = Qualified(target.Timeline);
        var targetSchema = Qualified(target.Schema);
        Line(writer, $"                    case {I(target.Ordinal)}:");
        if (caller.Index == target.Schema.Index)
            Line(writer, $"                        return {targetType}.DynamicSeekKernel(id, ref playback, delta, in data);");
        else
        {
            Line(writer, "                    {");
            Line(writer, $"                        if (id != {targetType}.Id)");
            Line(writer, "                            return false;");
            if (target.Schema.ReadOnlySlots.Count + target.Schema.WritableSlots.Count == 0)
                Line(writer, $"                        var targetData = default({targetSchema}.Data);");
            else
                Line(writer, $"                        var targetData = new {targetSchema}.Data({DataArguments(target.Schema, "data")});");
            Line(writer, $"                        return {targetType}.DynamicSeekKernel(id, ref playback, delta, in targetData);");
            Line(writer, "                    }");
        }
    }

    private static bool Compatible(Schema caller, Schema target)
        => target.ReadOnlySlots.All(required => caller.ReadOnlySlots.Any(candidate => SameSlot(candidate, required)))
            && target.WritableSlots.All(required => caller.WritableSlots.Any(candidate => SameSlot(candidate, required)));

    private static bool SameSlot(TimelineSlot left, TimelineSlot right)
        => left.Name == right.Name && TypeKey(left.TypeName) == TypeKey(right.TypeName);

    private static string SchemaKey(HeterogeneousTimeline timeline)
        => string.Join("\n", timeline.ReadOnlySlots.Select(slot => $"r:{slot.Name}:{TypeKey(slot.TypeName)}").Concat(timeline.WritableSlots.Select(slot => $"w:{slot.Name}:{TypeKey(slot.TypeName)}")));

    private static string TypeKey(string type)
        => type.StartsWith("global::", StringComparison.Ordinal) ? type[8..] : type;

    private static string Qualified(Schema schema) => Qualified(schema.Representative.Namespace, $"__TlGeneratedSchema{I(schema.Index)}");
    private static string Qualified(HeterogeneousTimeline timeline) => Qualified(timeline.Namespace, timeline.Name);
    private static string TypedPlayback(HeterogeneousTimeline timeline) => $"global::Tl.Playback<{Qualified(timeline)}>";
    private static string Qualified(string ns, string name) => ns.Length == 0 ? $"global::{name}" : $"global::{ns}.{name}";

    private static void EmitImmutableData(StringBuilder writer, HeterogeneousTimeline timeline)
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
        var values = timeline.Tracks.Select(static track => track.TypeName).Concat(timeline.Clips.Select(static clip => clip.TypeName)).GroupBy(static type => type, StringComparer.Ordinal).OrderBy(static group => group.Key, StringComparer.Ordinal).Select(static group => $"(nuint)global::System.Runtime.CompilerServices.Unsafe.SizeOf<{group.Key}>() * (nuint){I(group.Count())}").ToArray();
        return values.Length == 0 ? "0u" : string.Join(" + ", values);
    }

    private static void EmitFacade(StringBuilder writer, HeterogeneousTimeline timeline)
    {
        var playback = TypedPlayback(timeline);
        Line(writer, $"    public static {playback} Start(uint gameTick)");
        Line(writer, $"        => global::Tl.Timeline.CreateTypedPlayback<{Qualified(timeline)}>(0L, gameTick, global::Tl.PlaybackFlags.Started);");
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, "    public static bool TryStop(");
        Line(writer, $"        in {playback} playback,");
        Line(writer, $"        out {playback} stopped)");
        Line(writer, "    {");
        Line(writer, "        if ((playback.Flags & global::Tl.PlaybackFlags.Started) == 0)");
        Line(writer, "        {");
        Line(writer, "            stopped = playback;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, "        stopped = (playback.Flags & global::Tl.PlaybackFlags.Stopped) != 0");
        Line(writer, "            ? playback");
        Line(writer, $"            : global::Tl.Timeline.CreateTypedPlayback<{Qualified(timeline)}>(playback.Position, playback.GameTick, playback.Flags | global::Tl.PlaybackFlags.Stopped);");
        Line(writer, "        return true;");
        Line(writer, "    }");
    }

    private static void EmitTypedData(StringBuilder writer, HeterogeneousTimeline timeline, string schema)
    {
        var playback = TypedPlayback(timeline);
        Line(writer, "    public ref struct Data");
        Line(writer, "    {");
        Line(writer, $"        internal readonly ref {playback} _playback;");
        Line(writer, $"        internal readonly {schema}.Data _context;");
        Line(writer);
        Line(writer, "        public Data(");
        Line(writer, $"            ref {playback} playback{ConstructorSuffix(timeline)}");
        EmitFollowingParameters(writer, timeline.ReadOnlySlots, timeline.WritableSlots, 3);
        Line(writer, "        {");
        Line(writer, "            _playback = ref playback;");
        EmitContextConstruction(writer, timeline, schema, 3);
        Line(writer, "        }");
        Line(writer, "    }");
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, "    public static bool TrySeek(scoped ref Data data, int delta)");
        Line(writer, "    {");
        Line(writer, "        if (global::System.Runtime.CompilerServices.Unsafe.IsNullRef(ref data._playback))");
        Line(writer, "            return false;");
        Line(writer, "        var before = data._playback;");
        Line(writer, "        if (!SeekCore(before.Position, before.GameTick, before.Flags, delta, in data._context, out var position, out var gameTick))");
        Line(writer, "            return false;");
        Line(writer, "        if (delta != 0)");
        Line(writer, $"            data._playback = global::Tl.Timeline.CreateTypedPlayback<{Qualified(timeline)}>(position, gameTick, before.Flags);");
        Line(writer, "        return true;");
        Line(writer, "    }");
    }

    private static void EmitDynamicData(StringBuilder writer, HeterogeneousTimeline timeline, string schema)
    {
        Line(writer, "    public ref struct DynamicData : global::Tl.ITimelineData<DynamicData>");
        Line(writer, "    {");
        Line(writer, "        internal readonly ref global::Tl.Playback _playback;");
        Line(writer, $"        internal readonly {schema}.Data _context;");
        Line(writer);
        Line(writer, "        public DynamicData(");
        Line(writer, $"            ref global::Tl.Playback playback{ConstructorSuffix(timeline)}");
        EmitFollowingParameters(writer, timeline.ReadOnlySlots, timeline.WritableSlots, 3);
        Line(writer, "        {");
        Line(writer, "            _playback = ref playback;");
        EmitContextConstruction(writer, timeline, schema, 3);
        Line(writer, "        }");
        Line(writer);
        Line(writer, "        static bool global::Tl.ITimelineData<DynamicData>.TrySeek(ushort id, scoped ref DynamicData data, int delta)");
        Line(writer, "        {");
        Line(writer, "            if (global::System.Runtime.CompilerServices.Unsafe.IsNullRef(ref data._playback))");
        Line(writer, "                return false;");
        Line(writer, $"            return {schema}.TrySeek(id, ref data._playback, delta, in data._context);");
        Line(writer, "        }");
        Line(writer, "    }");
    }

    private static string ConstructorSuffix(HeterogeneousTimeline timeline) => timeline.ReadOnlySlots.Count + timeline.WritableSlots.Count == 0 ? ")" : ",";

    private static void EmitFollowingParameters(StringBuilder writer, IReadOnlyList<TimelineSlot> readOnlySlots, IReadOnlyList<TimelineSlot> writableSlots, int depth)
    {
        var slots = readOnlySlots.Select(slot => (Slot: slot, ReadOnly: true)).Concat(writableSlots.Select(slot => (Slot: slot, ReadOnly: false))).ToArray();
        for (var index = 0; index < slots.Length; index++)
        {
            var (slot, readOnly) = slots[index];
            var suffix = index + 1 == slots.Length ? ")" : ",";
            Line(writer, $"{new string(' ', depth * 4)}{(readOnly ? "ref readonly" : "ref")} {slot.TypeName} {Escape(slot.Name)}{suffix}");
        }
    }

    private static void EmitParameters(StringBuilder writer, IReadOnlyList<TimelineSlot> readOnlySlots, IReadOnlyList<TimelineSlot> writableSlots, int depth)
        => EmitFollowingParameters(writer, readOnlySlots, writableSlots, depth);

    private static void EmitContextConstruction(StringBuilder writer, HeterogeneousTimeline timeline, string schema, int depth)
    {
        var indent = new string(' ', depth * 4);
        if (timeline.ReadOnlySlots.Count + timeline.WritableSlots.Count == 0)
            Line(writer, $"{indent}_context = default;");
        else
            Line(writer, $"{indent}_context = new {schema}.Data({DataArguments(timeline.ReadOnlySlots, timeline.WritableSlots)});");
    }

    private static string DataArguments(Schema schema, string source)
        => string.Join(", ", schema.ReadOnlySlots.Select(slot => $"in {source}._{slot.Name}").Concat(schema.WritableSlots.Select(slot => $"ref {source}._{slot.Name}")));

    private static string DataArguments(IReadOnlyList<TimelineSlot> readOnlySlots, IReadOnlyList<TimelineSlot> writableSlots)
        => string.Join(", ", readOnlySlots.Select(slot => $"in {Escape(slot.Name)}").Concat(writableSlots.Select(slot => $"ref {Escape(slot.Name)}")));

    private static void EmitCore(StringBuilder writer, HeterogeneousTimeline timeline, string schema)
    {
        Line(writer, "    internal static bool DynamicSeekKernel(");
        Line(writer, "        ushort id,");
        Line(writer, "        scoped ref global::Tl.Playback playback,");
        Line(writer, "        int delta,");
        Line(writer, $"        in {schema}.Data data)");
        Line(writer, "    {");
        Line(writer, "        var before = playback;");
        Line(writer, "        if (id != Id || before.Owner != id");
        Line(writer, "            || !SeekCore(before.Position, before.GameTick, before.Flags, delta, in data, out var position, out var gameTick))");
        Line(writer, "            return false;");
        Line(writer, "        if (delta != 0)");
        Line(writer, "            playback = global::Tl.Timeline.CreateCompiledPlayback(id, position, gameTick, before.Flags);");
        Line(writer, "        return true;");
        Line(writer, "    }");
        Line(writer);
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, "    private static bool SeekCore(");
        Line(writer, "        long beforePosition,");
        Line(writer, "        uint beforeGameTick,");
        Line(writer, "        global::Tl.PlaybackFlags playbackFlags,");
        Line(writer, "        int delta,");
        Line(writer, $"        in {schema}.Data data,");
        Line(writer, "        out long targetPosition,");
        Line(writer, "        out uint targetGameTick)");
        Line(writer, "    {");
        Line(writer, "        var distance = (long)delta;");
        Line(writer, "        if ((playbackFlags & (global::Tl.PlaybackFlags.Started | global::Tl.PlaybackFlags.Stopped)) != global::Tl.PlaybackFlags.Started");
        if (!timeline.Loops || timeline.Duration == 0)
            Line(writer, "            || beforePosition < 0L || beforePosition > Duration");
        Line(writer, "            || distance > 0L && beforePosition > long.MaxValue - distance");
        Line(writer, "            || distance < 0L && beforePosition < long.MinValue - distance)");
        Line(writer, "        {");
        Line(writer, "            targetPosition = beforePosition;");
        Line(writer, "            targetGameTick = beforeGameTick;");
        Line(writer, "            return false;");
        Line(writer, "        }");
        Line(writer, "        targetPosition = beforePosition + distance;");
        Line(writer, "        targetGameTick = unchecked(beforeGameTick + (uint)delta);");
        if (!timeline.Loops || timeline.Duration == 0)
        {
            Line(writer, "        if (targetPosition < 0L || targetPosition > Duration)");
            Line(writer, "            return false;");
        }
        Line(writer, "        if (delta == 0)");
        Line(writer, "            return true;");
        if (timeline.Duration == 0)
        {
            Line(writer, "        return false;");
            Line(writer, "    }");
            return;
        }
        EmitInitialPosition(writer, timeline);
        Line(writer, "        if (delta == 1)");
        Line(writer, "        {");
        EmitFrameFlags(writer, timeline, false, "local", "beforePosition", 3);
        Line(writer, "            ApplyForward(local, beforeGameTick, cycle, frameFlags, in data);");
        Line(writer, "            return true;");
        Line(writer, "        }");
        Line(writer, "        if (delta == -1)");
        Line(writer, "        {");
        Line(writer, "            var gameTick = beforeGameTick;");
        EmitReverseStep(writer, timeline, 3);
        EmitFrameFlags(writer, timeline, true, "local", "targetPosition", 3);
        Line(writer, "            ApplyReverse(local, gameTick, cycle, frameFlags, in data);");
        Line(writer, "            return true;");
        Line(writer, "        }");
        Line(writer, "        if (delta > 1)");
        Line(writer, "        {");
        Line(writer, "            var position = beforePosition;");
        Line(writer, "            var gameTick = beforeGameTick;");
        Line(writer, "            while (position < targetPosition)");
        Line(writer, "            {");
        EmitFrameFlags(writer, timeline, false, "local", "position", 4);
        Line(writer, "                ApplyForward(local, gameTick, cycle, frameFlags, in data);");
        Line(writer, "                position++;");
        Line(writer, "                gameTick = unchecked(gameTick + 1u);");
        EmitForwardLocalStep(writer, timeline, 4);
        Line(writer, "            }");
        Line(writer, "            return true;");
        Line(writer, "        }");
        Line(writer, "        {");
        Line(writer, "            var position = beforePosition;");
        Line(writer, "            var gameTick = beforeGameTick;");
        Line(writer, "            while (position > targetPosition)");
        Line(writer, "            {");
        EmitReverseStep(writer, timeline, 4);
        Line(writer, "                position--;");
        EmitFrameFlags(writer, timeline, true, "local", "position", 4);
        Line(writer, "                ApplyReverse(local, gameTick, cycle, frameFlags, in data);");
        Line(writer, "            }");
        Line(writer, "            return true;");
        Line(writer, "        }");
        Line(writer, "    }");
    }

    private static void EmitInitialPosition(StringBuilder writer, HeterogeneousTimeline timeline)
    {
        if (timeline.Loops)
        {
            Line(writer, "        var cycle = beforePosition / (long)Duration;");
            Line(writer, "        var remainder = beforePosition - cycle * (long)Duration;");
            Line(writer, "        if (remainder < 0L)");
            Line(writer, "        {");
            Line(writer, "            remainder += Duration;");
            Line(writer, "            cycle--;");
            Line(writer, "        }");
            Line(writer, "        var local = (uint)remainder;");
        }
        else
        {
            Line(writer, "        var cycle = 0L;");
            Line(writer, "        var local = (uint)beforePosition;");
        }
    }

    private static void EmitForwardLocalStep(StringBuilder writer, HeterogeneousTimeline timeline, int depth)
    {
        var indent = new string(' ', depth * 4);
        if (timeline.Loops)
        {
            Line(writer, $"{indent}if (local == Duration - 1u)");
            Line(writer, $"{indent}{{");
            Line(writer, $"{indent}    local = 0u;");
            Line(writer, $"{indent}    cycle++;");
            Line(writer, $"{indent}}}");
            Line(writer, $"{indent}else");
            Line(writer, $"{indent}    local++;");
        }
        else
            Line(writer, $"{indent}local++;");
    }

    private static void EmitReverseStep(StringBuilder writer, HeterogeneousTimeline timeline, int depth)
    {
        var indent = new string(' ', depth * 4);
        Line(writer, $"{indent}gameTick = unchecked(gameTick - 1u);");
        if (timeline.Loops)
        {
            Line(writer, $"{indent}if (local == 0u)");
            Line(writer, $"{indent}{{");
            Line(writer, $"{indent}    local = Duration - 1u;");
            Line(writer, $"{indent}    cycle--;");
            Line(writer, $"{indent}}}");
            Line(writer, $"{indent}else");
            Line(writer, $"{indent}    local--;");
        }
        else
            Line(writer, $"{indent}local--;");
    }

    private static void EmitFrameFlags(StringBuilder writer, HeterogeneousTimeline timeline, bool reverse, string local, string position, int depth)
    {
        var indent = new string(' ', depth * 4);
        var initial = new List<string>();
        if (timeline.Loops)
            initial.Add("global::Tl.FrameFlags.Looping");
        if (reverse)
            initial.Add("global::Tl.FrameFlags.Reverse");
        Line(writer, $"{indent}var frameFlags = {(initial.Count == 0 ? "global::Tl.FrameFlags.None" : string.Join(" | ", initial))};");
        Line(writer, $"{indent}if ({local} == 0u)");
        Line(writer, $"{indent}    frameFlags |= global::Tl.FrameFlags.TimelineStart;");
        Line(writer, $"{indent}if ({local} == Duration - 1u)");
        Line(writer, $"{indent}    frameFlags |= global::Tl.FrameFlags.TimelineEnd;");
        if (!timeline.Loops)
        {
            Line(writer, $"{indent}if ({position} == (long)Duration - 1L)");
            Line(writer, $"{indent}    frameFlags |= global::Tl.FrameFlags.{(reverse ? "CompletedBefore" : "CompletedAfter")};");
        }
    }

    private static void EmitApply(StringBuilder writer, HeterogeneousTimeline timeline, string schema, IReadOnlyList<Region> regions, bool reverse)
    {
        var name = reverse ? "Reverse" : "Forward";
        Line(writer, "    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        Line(writer, $"    private static void Apply{name}(uint local, uint gameTick, long cycle, global::Tl.FrameFlags frameFlags, in {schema}.Data data)");
        Line(writer, "    {");
        var leadingHooks = reverse ? timeline.AfterHooks.Reverse() : timeline.BeforeHooks;
        var trailingHooks = reverse ? timeline.BeforeHooks.Reverse() : timeline.AfterHooks;
        foreach (var hook in leadingHooks)
            EmitHook(writer, hook, reverse, 2);
        for (var regionIndex = 0; regionIndex < regions.Count; regionIndex++)
        {
            var region = regions[regionIndex];
            var branch = regionIndex == 0 ? "if" : "else if";
            var condition = region.Start == 0 ? $"local < {U(region.End)}" : $"local >= {U(region.Start)} && local < {U(region.End)}";
            Line(writer, $"        {branch} ({condition})");
            Line(writer, "        {");
            var works = reverse ? region.Works.Reverse() : region.Works;
            var workIndex = 0;
            foreach (var work in works)
                EmitWork(writer, timeline, work, regionIndex, workIndex++, 3);
            Line(writer, "        }");
        }
        foreach (var hook in trailingHooks)
            EmitHook(writer, hook, reverse, 2);
        Line(writer, "    }");
    }

    private static void EmitHook(StringBuilder writer, TimelineHook hook, bool reverse, int depth)
    {
        var slots = reverse ? hook.BackwardSlots : hook.ForwardSlots;
        var arguments = slots.Select(slot => SlotArgument(slot, "data"));
        Line(writer, $"{new string(' ', depth * 4)}{hook.TypeName}.{(reverse ? "Backward" : "Forward")}({string.Join(", ", arguments)});");
    }

    private static void EmitWork(StringBuilder writer, HeterogeneousTimeline timeline, Work work, int region, int ordinal, int depth)
    {
        var indent = new string(' ', depth * 4);
        var suffix = I(region) + "_" + I(ordinal);
        var firstIndex = ClipIndex(timeline.Clips, work.First);
        var clip = $"s_clip{I(firstIndex)}";
        Line(writer, $"{indent}var workFlags_{suffix} = frameFlags;");
        var starts = work.Second is null ? $"local == {U(work.First.Start)}" : $"local == {U(work.First.Start)} || local == {U(work.Second.Start)}";
        var ends = work.Second is null ? $"local == {U(work.First.End - 1u)}" : $"local == {U(work.First.End - 1u)} || local == {U(work.Second.End - 1u)}";
        Line(writer, $"{indent}if ({starts})");
        Line(writer, $"{indent}    workFlags_{suffix} |= global::Tl.FrameFlags.ClipStart;");
        Line(writer, $"{indent}if ({ends})");
        Line(writer, $"{indent}    workFlags_{suffix} |= global::Tl.FrameFlags.ClipEnd;");
        if (work.Second != null)
        {
            var secondIndex = ClipIndex(timeline.Clips, work.Second);
            var factor = work.FactorLength <= 1 ? "0.5f" : $"(local - {U(work.FactorStart)}) / {I(work.FactorLength - 1)}f";
            Line(writer, $"{indent}var factor_{suffix} = {factor};");
            Line(writer, $"{indent}s_track{I(work.Track.Index)}.Blend(in s_clip{I(firstIndex)}, in s_clip{I(secondIndex)}, factor_{suffix}, out var clip_{suffix});");
            clip = $"clip_{suffix}";
        }
        Line(writer, $"{indent}var frame_{suffix} = new global::Tl.Frame<{work.Track.TypeName}, {work.Track.ClipTypeName}>(in s_track{I(work.Track.Index)}, in {clip}, gameTick, local, cycle, {I(work.Track.Index)}, workFlags_{suffix});");
        var arguments = new List<string> { $"in frame_{suffix}" };
        arguments.AddRange(work.Track.SeekSlots.Select(slot => SlotArgument(slot, "data")));
        Line(writer, $"{indent}{work.Track.TypeName}.Seek({string.Join(", ", arguments)});");
    }

    private static string SlotArgument(TimelineSlot slot, string source)
        => slot.Mode switch
        {
            SlotMode.Input => $"in {source}._{slot.Name}",
            SlotMode.Reference => $"ref {source}._{slot.Name}",
            SlotMode.Output => $"out {source}._{slot.Name}",
            _ => throw new InvalidOperationException(),
        };

    private static IReadOnlyList<Region> Regions(HeterogeneousTimeline timeline)
    {
        if (timeline.Duration == 0)
            return [];
        var cuts = timeline.Clips.SelectMany(static clip => new[] { clip.Start, clip.End }).Append(0u).Append(timeline.Duration).Distinct().Order().ToArray();
        var regions = new List<Region>();
        for (var index = 0; index + 1 < cuts.Length; index++)
        {
            var start = cuts[index];
            var end = cuts[index + 1];
            var works = new List<Work>();
            foreach (var track in timeline.Tracks)
            {
                var active = timeline.Clips.Where(clip => clip.TrackIndex == track.Index && clip.Start <= start && start < clip.End).OrderBy(static clip => clip.Start).ToArray();
                if (active.Length == 1)
                    works.Add(new Work(track, active[0], null, 0u, 0u));
                else if (active.Length == 2)
                {
                    var first = active[0];
                    var second = active[1];
                    var factorStart = Math.Max(first.Start, second.Start);
                    var factorEnd = Math.Min(first.End, second.End);
                    works.Add(new Work(track, first, second, factorStart, factorEnd - factorStart));
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
