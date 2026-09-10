using System.Globalization;
using System.Text;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

internal static class JobEmitter
{
    internal static string Normalize(string content) => content.Replace("\r\n", "\n").Replace('\r', '\n');
    internal static IReadOnlyList<CompileArtifact> Emit(JobReadResult model)
    {
        var timelines = model.Timelines.OrderBy(Qualified, StringComparer.Ordinal).ToArray();
        var files = timelines.Select((timeline, index) => new CompileArtifact($"TlJob{index}.g.cs", Timeline(timeline))).ToList();
        files.AddRange(model.Catalogs.OrderBy(static catalog => catalog.Namespace + "." + catalog.Name, StringComparer.Ordinal)
            .Select((catalog, index) => new CompileArtifact($"TlCatalog{index}.g.cs", Catalog(catalog, timelines))));
        return files;
    }

    private static string Timeline(JobTimeline timeline)
    {
        var writer = Header(timeline.Namespace, timeline.Usings);
        var slots = Slots([timeline]);
        Line(writer, $"public readonly partial struct {timeline.Name}");
        Line(writer, "{");
        Line(writer, $"public const uint Duration = {U(timeline.Duration)};");
        Line(writer, $"public const bool Loops = {Bool(timeline.Loops)};");
        Line(writer, $"public const int TrackCount = {timeline.Tracks.Count};");
        Line(writer, $"public const int ClipCount = {timeline.Clips.Count};");
        foreach (var track in timeline.Tracks)
            Line(writer, $"private static readonly {track.TypeName} __tlTrack{track.Index} = {track.Expression};");
        for (var index = 0; index < timeline.Clips.Count; index++)
            Line(writer, $"private static readonly {timeline.Clips[index].TypeName} __tlClip{index} = {timeline.Clips[index].Expression};");
        var sizes = timeline.Tracks.Select(static track => $"global::System.Runtime.CompilerServices.Unsafe.SizeOf<{track.TypeName}>()")
            .Concat(timeline.Clips.Select(static clip => $"global::System.Runtime.CompilerServices.Unsafe.SizeOf<{clip.TypeName}>()"));
        Line(writer, $"public static int StaticDataBytes => {string.Join(" + ", sizes.DefaultIfEmpty("0"))};");
        Line(writer, "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        Line(writer, "public static bool Select(in global::Tl.TimelineState state, bool reverse, out global::Tl.TimelineState next, out uint tick, out long cycle, out global::Tl.FrameFlags flags)");
        Line(writer, "=> global::Tl.TimelineMovement.Select(in state, Duration, Loops, reverse, out next, out tick, out cycle, out flags);");
        Line(writer, "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        Line(writer, $"public static void ExecuteFrame(uint __tlTick, uint __tlGameTick, long __tlCycle, global::Tl.FrameFlags __tlFlags{Parameters(slots)})");
        Line(writer, "{");
        Line(writer, "if ((__tlFlags & global::Tl.FrameFlags.Reverse) != 0)");
        Line(writer, "{");
        Execute(writer, timeline, true);
        Line(writer, "}");
        Line(writer, "else");
        Line(writer, "{");
        Execute(writer, timeline, false);
        Line(writer, "}");
        Line(writer, "}");
        Line(writer, "}");
        return writer.ToString();
    }

    private static void Execute(StringBuilder writer, JobTimeline timeline, bool reverse)
    {
        var leading = reverse ? timeline.After.Reverse() : timeline.Before;
        var trailing = reverse ? timeline.Before.Reverse() : timeline.After;
        if (timeline.Before.Count + timeline.After.Count != 0)
            Line(writer, "var __tlFrame = new global::Tl.TimelineFrame(__tlGameTick, __tlTick, __tlCycle, __tlFlags);");
        foreach (var hook in leading)
            Line(writer, $"{hook.TypeName}.Execute(in __tlFrame{Arguments(hook.Slots)});");
        var cuts = timeline.Clips.SelectMany(static clip => new[] { clip.Start, clip.End })
            .Append(0u).Append(timeline.Duration).Distinct().OrderBy(static cut => cut).ToArray();
        var tracks = reverse ? timeline.Tracks.Reverse() : timeline.Tracks;
        for (var region = 0; region + 1 < cuts.Length; region++)
        {
            var start = cuts[region];
            var end = cuts[region + 1];
            Line(writer, $"if (__tlTick >= {U(start)} && __tlTick < {U(end)})");
            Line(writer, "{");
            foreach (var track in tracks)
            {
                var clips = timeline.Clips.Select((clip, index) => (Clip: clip, Index: index))
                    .Where(pair => pair.Clip.TrackIndex == track.Index && pair.Clip.Start <= start && pair.Clip.End > start)
                    .OrderBy(static pair => pair.Clip.Start).ThenBy(static pair => pair.Index).ToArray();
                if (clips.Length == 0)
                    continue;
                Line(writer, "{");
                var first = clips[0];
                var windowStart = first.Clip.Start;
                var windowEnd = first.Clip.End;
                var clipExpression = $"__tlClip{first.Index}";
                if (clips.Length == 2)
                {
                    var second = clips[1];
                    windowStart = Math.Min(windowStart, second.Clip.Start);
                    windowEnd = Math.Max(windowEnd, second.Clip.End);
                    var factorStart = Math.Max(first.Clip.Start, second.Clip.Start);
                    var factorLength = Math.Min(first.Clip.End, second.Clip.End) - factorStart;
                    var factor = factorLength <= 1 ? "0.5f" : $"(__tlTick - {U(factorStart)}) / (float){U(factorLength - 1)}";
                    Line(writer, $"__tlTrack{track.Index}.Blend(in __tlClip{first.Index}, in __tlClip{second.Index}, {factor}, out var __tlResolved);");
                    clipExpression = "__tlResolved";
                }
                Line(writer, "var __tlWorkFlags = __tlFlags;");
                Line(writer, $"if (__tlTick == {U(windowStart)}) __tlWorkFlags |= global::Tl.FrameFlags.ClipStart;");
                Line(writer, $"if (__tlTick == {U(windowEnd - 1)}) __tlWorkFlags |= global::Tl.FrameFlags.ClipEnd;");
                Line(writer, $"var __tlWork = new global::Tl.Frame<{track.TypeName}, {track.ClipTypeName}>(in __tlTrack{track.Index}, in {clipExpression}, __tlGameTick, __tlTick, __tlCycle, {track.Index}, __tlWorkFlags);");
                Line(writer, $"{track.Job.TypeName}.Execute(in __tlWork{Arguments(track.Job.Slots)});");
                Line(writer, "}");
            }
            Line(writer, "}");
        }
        foreach (var hook in trailing)
            Line(writer, $"{hook.TypeName}.Execute(in __tlFrame{Arguments(hook.Slots)});");
    }

    private static string Catalog(JobCatalog catalog, IReadOnlyList<JobTimeline> timelines)
    {
        var byName = timelines.ToDictionary(Qualified, StringComparer.Ordinal);
        var assets = catalog.Schemas.SelectMany(static schema => schema.Assets).Distinct(StringComparer.Ordinal)
            .OrderBy(static asset => asset, StringComparer.Ordinal).Select(asset => byName[asset]).ToArray();
        var ids = assets.Select((asset, index) => (Name: Qualified(asset), Id: index + 1))
            .ToDictionary(static pair => pair.Name, static pair => pair.Id, StringComparer.Ordinal);
        var writer = Header(catalog.Namespace, []);
        Line(writer, $"public readonly partial struct {catalog.Name}");
        Line(writer, "{");
        Line(writer, "public enum Asset : uint");
        Line(writer, "{");
        Line(writer, "None = 0,");
        foreach (var asset in assets)
            Line(writer, $"{asset.Name} = {ids[Qualified(asset)]},");
        Line(writer, "}");
        Line(writer, "public readonly struct State");
        Line(writer, "{");
        Line(writer, "internal readonly global::Tl.TimelineState Value;");
        Line(writer, "public State(Asset asset, uint position = 0, long cycle = 0) => Value = new global::Tl.TimelineState((uint)asset, position, cycle);");
        Line(writer, "internal State(global::Tl.TimelineState value) => Value = value;");
        Line(writer, "public Asset Timeline => (Asset)Value.Asset;");
        Line(writer, "public uint Position => Value.Position;");
        Line(writer, "public long Cycle => Value.Cycle;");
        Line(writer, "}");
        Line(writer, "public readonly struct Query");
        Line(writer, "{");
        foreach (var schema in catalog.Schemas)
        {
            var members = schema.Assets.Select(asset => byName[asset]).ToArray();
            var slots = Slots(members);
            Line(writer, $"public {schema.Name}Query {schema.Name}(global::System.Span<State> __tlStates{Columns(slots)}) => new(__tlStates{string.Concat(slots.Select(static slot => ", @" + slot.Name))});");
        }
        Line(writer, "}");
        foreach (var schema in catalog.Schemas)
            Query(writer, schema, schema.Assets.Select(asset => byName[asset]).ToArray(), ids);
        Line(writer, "}");
        return writer.ToString();
    }

    private static void Query(StringBuilder writer, JobSchema schema, IReadOnlyList<JobTimeline> assets, IReadOnlyDictionary<string, int> ids)
    {
        var slots = Slots(assets);
        Line(writer, $"public readonly ref struct {schema.Name}Query");
        Line(writer, "{");
        Line(writer, "private readonly global::System.Span<State> __tlStates;");
        foreach (var slot in slots)
            Line(writer, $"private readonly {ColumnType(slot)} @{slot.Name};");
        Line(writer, $"internal {schema.Name}Query(global::System.Span<State> __tlStates{Columns(slots)})");
        Line(writer, "{");
        Line(writer, "this.__tlStates = __tlStates;");
        foreach (var slot in slots)
        {
            Line(writer, $"if (@{slot.Name}.Length != __tlStates.Length) throw new global::System.ArgumentException(\"Column length must equal timeline row count.\", nameof(@{slot.Name}));");
            Line(writer, $"this.@{slot.Name} = @{slot.Name};");
        }
        var columns = new[] { (Name: "__tlStates", Writable: true) }.Concat(slots.Select(static slot => (Name: "@" + slot.Name, Writable: slot.Mode != SlotMode.Input))).ToArray();
        for (var first = 0; first < columns.Length; first++)
            for (var second = first + 1; second < columns.Length; second++)
                if (columns[first].Writable || columns[second].Writable)
                    Line(writer, $"if (global::System.MemoryExtensions.Overlaps((global::System.ReadOnlySpan<byte>)global::System.Runtime.InteropServices.MemoryMarshal.AsBytes({columns[first].Name}), global::System.Runtime.InteropServices.MemoryMarshal.AsBytes({columns[second].Name}))) throw new global::System.ArgumentException(\"Writable component columns must not overlap other columns.\");");
        Line(writer, "Validate();");
        Line(writer, "}");
        Line(writer, "private void Validate()");
        Line(writer, "{");
        Line(writer, "foreach (ref readonly var __tlState in __tlStates)");
        Line(writer, "switch (__tlState.Value.Asset)");
        Line(writer, "{");
        Line(writer, "case 0:");
        foreach (var asset in assets)
            Line(writer, $"case {ids[Qualified(asset)]}:");
        Line(writer, "break;");
        Line(writer, "default: throw new global::System.ArgumentException(\"Timeline does not belong to this query schema.\");");
        Line(writer, "}");
        Line(writer, "}");
        Line(writer, "public void Tick(uint gameTick, int delta = 1)");
        Line(writer, "{");
        Line(writer, "if (delta == 0 || __tlStates.IsEmpty) return;");
        Line(writer, "Validate();");
        Line(writer, "bool __tlReverse = delta < 0;");
        Line(writer, "long __tlRemaining = __tlReverse ? -(long)delta : delta;");
        Line(writer, "while (__tlRemaining-- != 0)");
        Line(writer, "{");
        Line(writer, "bool __tlMoved = false;");
        Line(writer, "if (__tlReverse) gameTick = unchecked(gameTick - 1);");
        Line(writer, "for (int __tlRow = 0; __tlRow < __tlStates.Length; __tlRow++)");
        Line(writer, "{");
        Line(writer, "ref var __tlState = ref __tlStates[__tlRow];");
        Line(writer, "switch (__tlState.Value.Asset)");
        Line(writer, "{");
        foreach (var asset in assets)
        {
            Line(writer, $"case {ids[Qualified(asset)]}:");
            Line(writer, "{");
            var name = Qualified(asset);
            Line(writer, $"if (!{name}.Select(in __tlState.Value, __tlReverse, out var __tlNext, out var __tlTick, out var __tlCycle, out var __tlFlags)) break;");
            Line(writer, $"{name}.ExecuteFrame(__tlTick, gameTick, __tlCycle, __tlFlags{Arguments(Slots([asset]), "[__tlRow]")});");
            Line(writer, "__tlState = new State(__tlNext);");
            Line(writer, "__tlMoved = true;");
            Line(writer, "break;");
            Line(writer, "}");
        }
        Line(writer, "}");
        Line(writer, "}");
        Line(writer, "if (!__tlMoved) break;");
        Line(writer, "if (!__tlReverse) gameTick = unchecked(gameTick + 1);");
        Line(writer, "}");
        Line(writer, "}");
        Line(writer, "}");
    }

    internal static IReadOnlyList<TimelineSlot> Slots(IEnumerable<JobTimeline> timelines)
        => timelines.SelectMany(static timeline => timeline.Before.Concat(timeline.Tracks.Select(static track => track.Job)).Concat(timeline.After))
            .SelectMany(static job => job.Slots).GroupBy(static slot => slot.Name + "\0" + slot.TypeName, StringComparer.Ordinal)
            .Select(static group => new TimelineSlot(group.First().Name, group.First().TypeName,
                group.Any(static slot => slot.Mode != SlotMode.Input) ? SlotMode.Reference : SlotMode.Input)).ToArray();

    private static string Parameters(IEnumerable<TimelineSlot> slots)
        => string.Concat(slots.Select(static slot => $", {Mode(slot)} {slot.TypeName} @{slot.Name}"));

    private static string Arguments(IEnumerable<TimelineSlot> slots, string suffix = "")
        => string.Concat(slots.Select(slot => $", {Mode(slot)} @{slot.Name}{suffix}"));

    private static string Columns(IEnumerable<TimelineSlot> slots)
        => string.Concat(slots.Select(static slot => $", {ColumnType(slot)} @{slot.Name}"));

    private static string ColumnType(TimelineSlot slot)
        => $"global::System.{(slot.Mode == SlotMode.Input ? "ReadOnlySpan" : "Span")}<{slot.TypeName}>";

    private static string Mode(TimelineSlot slot) => slot.Mode == SlotMode.Input ? "in" : "ref";
    private static string Qualified(JobTimeline timeline) => "global::" + (timeline.Namespace.Length == 0 ? "" : timeline.Namespace + ".") + timeline.Name;
    private static string U(uint value) => value.ToString(CultureInfo.InvariantCulture) + "u";
    private static string Bool(bool value) => value ? "true" : "false";
    private static void Line(StringBuilder writer, string text) => writer.Append(text).Append('\n');

    private static StringBuilder Header(string nameSpace, IReadOnlyList<string> usings)
    {
        var writer = new StringBuilder();
        Line(writer, "#nullable enable");
        foreach (var directive in usings)
            Line(writer, directive);
        if (nameSpace.Length != 0)
            Line(writer, $"namespace {nameSpace};");
        return writer;
    }
}
