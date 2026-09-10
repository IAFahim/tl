using System.Globalization;
using System.Text;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

internal static class UnityJobEmitter
{
    internal static IReadOnlyList<CompileArtifact> Emit(JobReadResult model)
    {
        var timelines = model.Timelines.OrderBy(Qualified, StringComparer.Ordinal).ToArray();
        var plans = timelines.ToDictionary(Qualified, JobTimelinePlanAdapter.Create, StringComparer.Ordinal);
        Validate(model, timelines, plans);
        var files = timelines.Select((timeline, index) => new CompileArtifact(
            $"TlUnityJob{index}.g.cs",
            Timeline(timeline, plans[Qualified(timeline)]))).ToList();
        files.AddRange(model.Catalogs.OrderBy(static catalog => catalog.Namespace + "." + catalog.Name, StringComparer.Ordinal)
            .Select((catalog, index) => new CompileArtifact(
                $"TlUnityCatalog{index}.g.cs",
                Catalog(catalog, timelines, plans))));
        return files;
    }

    private static void Validate(
        JobReadResult model,
        IReadOnlyList<JobTimeline> timelines,
        IReadOnlyDictionary<string, BoundOrderedTimelinePlan> plans)
    {
        if (model.Catalogs.Count == 0)
            throw new InvalidOperationException("The Unity Entities backend requires an explicit timeline catalog.");
        foreach (var timeline in timelines)
        {
            var bound = plans[Qualified(timeline)];
            if (timeline.Tracks.Count != 1 || timeline.Clips.Count != 1 || timeline.Before.Count != 0 || timeline.After.Count != 0
                || bound.OperationBindings.Length != 1 || bound.Plan.Regions.Any(static region => region.OccurrenceCount > 1))
                throw new InvalidOperationException($"Timeline '{Qualified(timeline)}' is outside the first Unity vertical slice: one track, one clip, one operation, and no hooks or blends are required.");
        }
        foreach (var catalog in model.Catalogs)
        foreach (var schema in catalog.Schemas)
            if (schema.Assets.Count != 1)
                throw new InvalidOperationException($"Unity schema '{catalog.Namespace}.{catalog.Name}.{schema.Name}' must contain exactly one asset in the first vertical slice.");
    }

    private static string Timeline(JobTimeline timeline, BoundOrderedTimelinePlan bound)
    {
        var writer = Header(timeline.Namespace);
        var track = timeline.Tracks[0];
        var clip = timeline.Clips[0];
        var operation = bound.OperationBindings[0];
        Line(writer, $"public readonly struct {timeline.Name}");
        Line(writer, "{");
        Line(writer, $"public const uint Duration = {U(timeline.Duration)};");
        Line(writer, $"public const bool Loops = {Bool(timeline.Loops)};");
        Line(writer, $"public static int StaticDataBytes => global::Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<{track.TypeName}>() + global::Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<{clip.TypeName}>();");
        Line(writer, $"private static readonly {track.TypeName} __tlTrack = {track.Expression};");
        Line(writer, $"private static readonly {clip.TypeName} __tlClip = {clip.Expression};");
        Line(writer, "internal static bool Select(in global::Tl.TimelineState state, bool reverse, out global::Tl.TimelineState next, out uint tick, out long cycle, out global::Tl.FrameFlags flags)");
        Line(writer, "{");
        Line(writer, "return global::Tl.TimelineMovement.Select(in state, Duration, Loops, reverse, out next, out tick, out cycle, out flags);");
        Line(writer, "}");
        Line(writer, "internal static long FrameCycle(in global::Tl.TimelineState state, global::Tl.FrameFlags flags)");
        Line(writer, "{");
        Line(writer, timeline.Loops
            ? "return (flags & global::Tl.FrameFlags.Reverse) != 0 && state.Position == 0 ? unchecked(state.Cycle - 1) : state.Cycle;"
            : "return 0L;");
        Line(writer, "}");
        Line(writer, "internal static global::Tl.TimelineState Commit(in global::Tl.TimelineState state, uint tick, global::Tl.FrameFlags flags)");
        Line(writer, "{");
        if (timeline.Loops)
        {
            Line(writer, "if ((flags & global::Tl.FrameFlags.Reverse) != 0) return new global::Tl.TimelineState(state.Asset, tick, FrameCycle(in state, flags));");
            Line(writer, "if ((flags & global::Tl.FrameFlags.TimelineEnd) != 0) return new global::Tl.TimelineState(state.Asset, 0u, unchecked(state.Cycle + 1));");
            Line(writer, "return new global::Tl.TimelineState(state.Asset, tick + 1u, state.Cycle);");
        }
        else
            Line(writer, "return new global::Tl.TimelineState(state.Asset, (flags & global::Tl.FrameFlags.Reverse) != 0 ? tick : tick + 1u);");
        Line(writer, "}");
        Line(writer, $"internal static void Execute(ushort stage, uint tick, uint gameTick, long cycle, global::Tl.FrameFlags flags, bool reverse{Parameters(operation.Slots)})");
        Line(writer, "{");
        Line(writer, $"if (stage != 0 || tick < {U(clip.Start)} || tick >= {U(clip.End)}) return;");
        Line(writer, "var workFlags = flags;");
        Line(writer, $"if (tick == {U(clip.Start)}) workFlags |= global::Tl.FrameFlags.ClipStart;");
        Line(writer, $"if (tick == {U(clip.End - 1)}) workFlags |= global::Tl.FrameFlags.ClipEnd;");
        Line(writer, $"var frame = new global::Tl.Frame<{track.TypeName}, {track.ClipTypeName}>(in __tlTrack, in __tlClip, gameTick, tick, cycle, {track.Index.ToString(CultureInfo.InvariantCulture)}, workFlags);");
        Line(writer, $"{track.Job.TypeName}.Execute(in frame{Arguments(operation.Slots)});");
        Line(writer, "}");
        Line(writer, "}");
        Footer(writer, timeline.Namespace);
        return writer.ToString();
    }

    private static string Catalog(
        JobCatalog catalog,
        IReadOnlyList<JobTimeline> timelines,
        IReadOnlyDictionary<string, BoundOrderedTimelinePlan> plans)
    {
        var byName = timelines.ToDictionary(Qualified, StringComparer.Ordinal);
        var assets = catalog.Schemas.SelectMany(static schema => schema.Assets).Distinct(StringComparer.Ordinal)
            .OrderBy(static asset => asset, StringComparer.Ordinal).Select(asset => byName[asset]).ToArray();
        var ids = assets.Select((asset, index) => (Name: Qualified(asset), Id: index + 1))
            .ToDictionary(static pair => pair.Name, static pair => pair.Id, StringComparer.Ordinal);
        var writer = Header(catalog.Namespace);
        Line(writer, $"public readonly partial struct {catalog.Name}");
        Line(writer, "{");
        Line(writer, "public enum Asset : uint");
        Line(writer, "{");
        Line(writer, "None = 0,");
        foreach (var asset in assets)
            Line(writer, $"{asset.Name} = {ids[Qualified(asset)].ToString(CultureInfo.InvariantCulture)},");
        Line(writer, "}");
        Line(writer, "public struct State");
        Line(writer, "{");
        Line(writer, "internal global::Tl.TimelineState Value;");
        Line(writer, "internal uint PendingTick;");
        Line(writer, "internal global::Tl.FrameFlags PendingFlags;");
        Line(writer, "internal bool Pending;");
        Line(writer, "public State(Asset asset, uint position = 0, long cycle = 0)");
        Line(writer, "{");
        Line(writer, "Value = new global::Tl.TimelineState((uint)asset, position, cycle);");
        Line(writer, "PendingTick = 0u;");
        Line(writer, "PendingFlags = global::Tl.FrameFlags.None;");
        Line(writer, "Pending = false;");
        Line(writer, "}");
        Line(writer, "public Asset Timeline { get { return (Asset)Value.Asset; } }");
        Line(writer, "public uint Position { get { return Value.Position; } }");
        Line(writer, "public long Cycle { get { return Value.Cycle; } }");
        Line(writer, "}");
        Line(writer, "public static int StateBytes => global::Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<State>();");
        Line(writer, $"public static int StaticDataBytes => {string.Join(" + ", assets.Select(asset => asset.Name + ".StaticDataBytes").DefaultIfEmpty("0"))};");
        Line(writer, "public struct TimelineComponent : global::Unity.Entities.IComponentData");
        Line(writer, "{");
        Line(writer, "public State Value;");
        Line(writer, "}");
        foreach (var schema in catalog.Schemas)
            Line(writer, $"public struct {schema.Name} : global::Unity.Entities.IComponentData, global::Unity.Entities.IEnableableComponent {{ }}");
        Line(writer, "public struct Stage0 : global::Unity.Entities.IComponentData, global::Unity.Entities.IEnableableComponent { }");
        Line(writer, "public static void Tick(ref global::Unity.Entities.SystemState state, uint gameTick, int delta = 1)");
        Line(writer, "{");
        Line(writer, "if (delta == 0) return;");
        Line(writer, "bool reverse = delta < 0;");
        Line(writer, "long remaining = reverse ? -(long)delta : delta;");
        Line(writer, $"var clearData = new __Tl{catalog.Name}ClearJob.InternalCompilerQueryAndHandleData();");
        Line(writer, "clearData.Init(ref state, true);");
        foreach (var schema in catalog.Schemas)
        {
            Line(writer, $"var {Lower(schema.Name)}SelectData = new __Tl{catalog.Name}{schema.Name}SelectJob.InternalCompilerQueryAndHandleData();");
            Line(writer, $"{Lower(schema.Name)}SelectData.Init(ref state, true);");
        }
        Line(writer, $"var stage0Data = new __Tl{catalog.Name}Stage0Job.InternalCompilerQueryAndHandleData();");
        Line(writer, "stage0Data.Init(ref state, true);");
        Line(writer, $"var commitData = new __Tl{catalog.Name}CommitJob.InternalCompilerQueryAndHandleData();");
        Line(writer, "commitData.Init(ref state, true);");
        Line(writer, "var dependency = state.Dependency;");
        Line(writer, "while (remaining-- != 0)");
        Line(writer, "{");
        Line(writer, "if (reverse) gameTick = unchecked(gameTick - 1u);");
        Line(writer, $"var clearJob = new __Tl{catalog.Name}ClearJob();");
        Line(writer, "dependency = clearData.ScheduleParallel(ref clearJob, clearData.DefaultQuery, dependency);");
        foreach (var schema in catalog.Schemas)
        {
            Line(writer, $"var {Lower(schema.Name)}SelectJob = new __Tl{catalog.Name}{schema.Name}SelectJob {{ Reverse = reverse }};");
            Line(writer, $"dependency = {Lower(schema.Name)}SelectData.ScheduleParallel(ref {Lower(schema.Name)}SelectJob, {Lower(schema.Name)}SelectData.DefaultQuery, dependency);");
        }
        Line(writer, $"var stage0Job = new __Tl{catalog.Name}Stage0Job {{ GameTick = gameTick, Reverse = reverse }};");
        Line(writer, "dependency = stage0Data.ScheduleParallel(ref stage0Job, stage0Data.DefaultQuery, dependency);");
        Line(writer, $"var commitJob = new __Tl{catalog.Name}CommitJob();");
        Line(writer, "dependency = commitData.ScheduleParallel(ref commitJob, commitData.DefaultQuery, dependency);");
        Line(writer, "if (!reverse) gameTick = unchecked(gameTick + 1u);");
        Line(writer, "}");
        Line(writer, "state.Dependency = dependency;");
        Line(writer, "}");
        Line(writer, "}");
        EmitJobs(writer, catalog, assets, ids, byName, plans);
        Footer(writer, catalog.Namespace);
        return writer.ToString();
    }

    private static void EmitJobs(
        StringBuilder writer,
        JobCatalog catalog,
        IReadOnlyList<JobTimeline> assets,
        IReadOnlyDictionary<string, int> ids,
        IReadOnlyDictionary<string, JobTimeline> byName,
        IReadOnlyDictionary<string, BoundOrderedTimelinePlan> plans)
    {
        Line(writer, "[global::Unity.Burst.BurstCompile(CompileSynchronously = true)]");
        Line(writer, "[WithOptions(global::Unity.Entities.EntityQueryOptions.IgnoreComponentEnabledState)]");
        Line(writer, $"internal partial struct __Tl{catalog.Name}ClearJob : IJobEntity");
        Line(writer, "{");
        Line(writer, $"private void Execute(ref {catalog.Name}.TimelineComponent timeline, global::Unity.Entities.EnabledRefRW<{catalog.Name}.Stage0> stage)");
        Line(writer, "{");
        Line(writer, "timeline.Value.Pending = false;");
        Line(writer, "stage.ValueRW = false;");
        Line(writer, "}");
        Line(writer, "}");

        foreach (var schema in catalog.Schemas)
        {
            var asset = byName[schema.Assets[0]];
            var slots = JobEmitter.Slots([asset]);
            Line(writer, "[global::Unity.Burst.BurstCompile(CompileSynchronously = true)]");
            Line(writer, "[WithOptions(global::Unity.Entities.EntityQueryOptions.IgnoreComponentEnabledState)]");
            Line(writer, $"[WithAll(typeof({catalog.Name}.{schema.Name}){TypeArguments(slots)})]");
            Line(writer, $"internal partial struct __Tl{catalog.Name}{schema.Name}SelectJob : IJobEntity");
            Line(writer, "{");
            Line(writer, "public bool Reverse;");
            Line(writer, $"private void Execute(ref {catalog.Name}.TimelineComponent timeline, global::Unity.Entities.EnabledRefRW<{catalog.Name}.Stage0> stage)");
            Line(writer, "{");
            Line(writer, $"if (timeline.Value.Value.Asset != {ids[Qualified(asset)].ToString(CultureInfo.InvariantCulture)}u) return;");
            Line(writer, $"if (!{asset.Name}.Select(in timeline.Value.Value, Reverse, out _, out timeline.Value.PendingTick, out _, out timeline.Value.PendingFlags)) return;");
            Line(writer, "timeline.Value.Pending = true;");
            Line(writer, "stage.ValueRW = true;");
            Line(writer, "}");
            Line(writer, "}");
        }

        var operationAssets = assets.GroupBy(asset => plans[Qualified(asset)].OperationBindings[0].TypeName, StringComparer.Ordinal).ToArray();
        if (operationAssets.Length != 1)
            throw new InvalidOperationException($"Catalog '{catalog.Namespace}.{catalog.Name}' must use one operation kind in the first Unity vertical slice.");
        var operation = plans[Qualified(operationAssets[0].First())].OperationBindings[0];
        Line(writer, "[global::Unity.Burst.BurstCompile(CompileSynchronously = true)]");
        Line(writer, $"[WithAll(typeof({catalog.Name}.Stage0))]");
        Line(writer, $"internal partial struct __Tl{catalog.Name}Stage0Job : IJobEntity");
        Line(writer, "{");
        Line(writer, "public uint GameTick;");
        Line(writer, "public bool Reverse;");
        Line(writer, $"private void Execute(in {catalog.Name}.TimelineComponent timeline{Parameters(operation.Slots)})");
        Line(writer, "{");
        Line(writer, "if (!timeline.Value.Pending) return;");
        Line(writer, "switch (timeline.Value.Value.Asset)");
        Line(writer, "{");
        foreach (var asset in assets)
        {
            Line(writer, $"case {ids[Qualified(asset)].ToString(CultureInfo.InvariantCulture)}u:");
            Line(writer, $"{asset.Name}.Execute(0, timeline.Value.PendingTick, GameTick, {asset.Name}.FrameCycle(in timeline.Value.Value, timeline.Value.PendingFlags), timeline.Value.PendingFlags, Reverse{Arguments(operation.Slots)});");
            Line(writer, "break;");
        }
        Line(writer, "}");
        Line(writer, "}");
        Line(writer, "}");

        Line(writer, "[global::Unity.Burst.BurstCompile(CompileSynchronously = true)]");
        Line(writer, "[WithOptions(global::Unity.Entities.EntityQueryOptions.IgnoreComponentEnabledState)]");
        Line(writer, $"internal partial struct __Tl{catalog.Name}CommitJob : IJobEntity");
        Line(writer, "{");
        Line(writer, $"private void Execute(ref {catalog.Name}.TimelineComponent timeline, global::Unity.Entities.EnabledRefRW<{catalog.Name}.Stage0> stage)");
        Line(writer, "{");
        Line(writer, "if (timeline.Value.Pending)");
        Line(writer, "{");
        Line(writer, "switch (timeline.Value.Value.Asset)");
        Line(writer, "{");
        foreach (var asset in assets)
        {
            Line(writer, $"case {ids[Qualified(asset)].ToString(CultureInfo.InvariantCulture)}u:");
            Line(writer, $"timeline.Value.Value = {asset.Name}.Commit(in timeline.Value.Value, timeline.Value.PendingTick, timeline.Value.PendingFlags);");
            Line(writer, "break;");
        }
        Line(writer, "}");
        Line(writer, "}");
        Line(writer, "timeline.Value.Pending = false;");
        Line(writer, "stage.ValueRW = false;");
        Line(writer, "}");
        Line(writer, "}");
    }

    private static string Parameters(IEnumerable<TimelineSlot> slots)
        => string.Concat(slots.Select(static slot => $", {(slot.Mode == SlotMode.Input ? "in" : "ref")} {slot.TypeName} @{slot.Name}"));

    private static string Arguments(IEnumerable<TimelineSlot> slots)
        => string.Concat(slots.Select(static slot => $", {(slot.Mode == SlotMode.Input ? "in" : "ref")} @{slot.Name}"));

    private static string TypeArguments(IEnumerable<TimelineSlot> slots)
        => string.Concat(slots.Select(static slot => $", typeof({slot.TypeName})"));

    private static string Qualified(JobTimeline timeline)
        => "global::" + (timeline.Namespace.Length == 0 ? "" : timeline.Namespace + ".") + timeline.Name;

    private static string U(uint value) => value.ToString(CultureInfo.InvariantCulture) + "u";
    private static string Bool(bool value) => value ? "true" : "false";
    private static string Lower(string value) => char.ToLowerInvariant(value[0]) + value.Substring(1);
    private static void Line(StringBuilder writer, string value) => writer.Append(value).Append('\n');

    private static StringBuilder Header(string nameSpace)
    {
        var writer = new StringBuilder();
        if (nameSpace.Length != 0)
        {
            Line(writer, $"namespace {nameSpace}");
            Line(writer, "{");
        }
        Line(writer, "using Unity.Entities;");
        return writer;
    }

    private static void Footer(StringBuilder writer, string nameSpace)
    {
        if (nameSpace.Length != 0)
            Line(writer, "}");
    }
}
