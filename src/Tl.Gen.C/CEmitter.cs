using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Tl.Compiler;

namespace Tl.Gen.C;

public static class CEmitter
{
    private sealed record Work(
        TrackPlan Track,
        ClipPlan First,
        ClipPlan? Second,
        uint FactorStart,
        uint FactorLength);

    private sealed record Region(uint Start, uint End, ImmutableArray<Work> Works);

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else", "enum",
        "extern", "float", "for", "goto", "if", "inline", "int", "long", "register", "restrict", "return",
        "short", "signed", "sizeof", "static", "struct", "switch", "typedef", "union", "unsigned", "void",
        "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex", "_Generic", "_Imaginary",
        "_Noreturn", "_Static_assert", "_Thread_local"
    };

    public static ImmutableArray<CArtifact> Emit(TimelinePlan plan, CBinding binding)
        => Generate(plan, binding).Artifacts;

    public static CEmission Generate(TimelinePlan plan, CBinding binding)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(binding);
        var operations = Validate(plan, binding);
        var regions = Regions(plan);
        ImmutableArray<CArtifact> artifacts =
        [
            new CArtifact(binding.HeaderFileName, EmitHeader(plan, binding, operations)),
            new CArtifact(binding.SymbolPrefix + ".c", EmitSource(plan, binding, operations, regions))
        ];
        return new(
            artifacts,
            new(
                TimelinePlan.FormatVersion,
                2,
                plan.Tracks.Length,
                plan.Clips.Length,
                regions.Length,
                plan.Duration,
                plan.Loops,
                artifacts.Length,
                artifacts.Sum(static artifact => artifact.Utf8Bytes),
                0,
                16,
                8,
                40,
                8,
                0));
    }

    private static IReadOnlyDictionary<OperationId, COperationBinding> Validate(TimelinePlan plan, CBinding binding)
    {
        if (string.IsNullOrWhiteSpace(plan.Identity))
            throw new ArgumentException("Timeline identity cannot be empty.", nameof(plan));
        ValidateIdentifier(binding.SymbolPrefix, nameof(binding));
        ValidateFileName(binding.HeaderFileName, nameof(binding));
        if (plan.Tracks.Length > ushort.MaxValue + 1)
            throw new ArgumentException("A timeline may contain at most 65,536 tracks.", nameof(plan));

        var tracks = new HashSet<ushort>();
        foreach (var track in plan.Tracks)
        {
            if (!tracks.Add(track.Index))
                throw new ArgumentException($"Track index {track.Index} is duplicated.", nameof(plan));
            if (string.IsNullOrWhiteSpace(track.Operation.Value))
                throw new ArgumentException($"Track index {track.Index} has no operation.", nameof(plan));
        }

        foreach (var clip in plan.Clips)
        {
            if (!tracks.Contains(clip.TrackIndex))
                throw new ArgumentException($"Clip track index {clip.TrackIndex} does not exist.", nameof(plan));
            if (clip.Start >= clip.End)
                throw new ArgumentException($"Clip [{clip.Start}, {clip.End}) is empty or reversed.", nameof(plan));
        }

        foreach (var track in plan.Tracks)
        {
            var events = plan.Clips
                .Where(clip => clip.TrackIndex == track.Index)
                .SelectMany(static clip => new[] { (Tick: clip.Start, Delta: 1), (Tick: clip.End, Delta: -1) })
                .GroupBy(static item => item.Tick)
                .OrderBy(static group => group.Key);
            var active = 0;
            foreach (var group in events)
            {
                active += group.Where(static item => item.Delta < 0).Sum(static item => item.Delta);
                active += group.Where(static item => item.Delta > 0).Sum(static item => item.Delta);
                if (active > 2)
                    throw new ArgumentException($"Track index {track.Index} has more than two overlapping clips.", nameof(plan));
            }
        }

        var reservedSymbols = ReservedSymbols(binding.SymbolPrefix);
        var operations = new Dictionary<OperationId, COperationBinding>();
        foreach (var operation in binding.Operations)
        {
            if (string.IsNullOrWhiteSpace(operation.Operation.Value))
                throw new ArgumentException("An operation binding has no operation ID.", nameof(binding));
            ValidateCallbackIdentifier(operation.SeekSymbol, reservedSymbols, nameof(binding));
            if (!operations.TryAdd(operation.Operation, operation))
                throw new ArgumentException($"Operation '{operation.Operation.Value}' is bound more than once.", nameof(binding));
        }

        foreach (var operation in plan.Tracks.Select(static track => track.Operation).Distinct())
            if (!operations.ContainsKey(operation))
                throw new ArgumentException($"Operation '{operation.Value}' is not bound.", nameof(binding));
        return operations;
    }

    private static string EmitHeader(
        TimelinePlan plan,
        CBinding binding,
        IReadOnlyDictionary<OperationId, COperationBinding> operations)
    {
        var prefix = binding.SymbolPrefix;
        var guard = "TL_" + prefix + "_H";
        var writer = new StringBuilder();
        Line(writer, $"#ifndef {guard}");
        Line(writer, $"#define {guard}");
        Line(writer);
        Line(writer, "#include <stdbool.h>");
        Line(writer, "#include <float.h>");
        Line(writer, "#include <limits.h>");
        Line(writer, "#include <stddef.h>");
        Line(writer, "#include <stdint.h>");
        Line(writer);
        Line(writer, "#ifndef TL_C_ABI_VERSION");
        Line(writer, "#define TL_C_ABI_VERSION 2");
        Line(writer, "#elif TL_C_ABI_VERSION != 2");
        Line(writer, "#error \"incompatible tl C ABI version\"");
        Line(writer, "#endif");
        Line(writer, "#ifndef TL_C_ABI_V2_TYPES");
        Line(writer, "#define TL_C_ABI_V2_TYPES 1");
        Line(writer, "#define TL_C_ABI_ENDIANNESS_NATIVE 1");
        Line(writer, "#define TL_C_ABI_FLOAT_EVALUATION_NATIVE 1");
        Line(writer, "#define TL_C_ABI_FLOAT_ROUNDING_NATIVE 1");
        Line(writer, "typedef uint8_t tl_playback_flags;");
        Line(writer, "typedef uint8_t tl_frame_flags;");
        Line(writer, "enum { TL_PLAYBACK_STARTED = 1u, TL_PLAYBACK_STOPPED = 2u };");
        Line(writer, "enum { TL_FRAME_CLIP_START = 1u, TL_FRAME_CLIP_END = 2u, TL_FRAME_TIMELINE_START = 4u, TL_FRAME_TIMELINE_END = 8u, TL_FRAME_COMPLETED_BEFORE = 16u, TL_FRAME_COMPLETED_AFTER = 32u, TL_FRAME_LOOPING = 64u, TL_FRAME_REVERSE = 128u };");
        Line(writer, "typedef struct { _Alignas(8) int64_t position; uint32_t game_tick; uint16_t owner; tl_playback_flags flags; uint8_t reserved; } tl_playback;");
        Line(writer, "typedef struct { _Alignas(8) int64_t cycle; uint32_t game_tick; uint32_t timeline_tick; uint32_t track_payload; uint32_t first_payload; uint32_t second_payload; float factor; uint16_t track_index; tl_frame_flags flags; uint8_t payload_count; uint32_t reserved; } tl_frame;");
        Line(writer, "_Static_assert(CHAR_BIT == 8, \"tl requires 8-bit bytes\");");
        Line(writer, "_Static_assert(sizeof(float) == 4, \"tl requires 32-bit float\");");
        Line(writer, "_Static_assert(FLT_RADIX == 2, \"tl requires binary float\");");
        Line(writer, "_Static_assert(FLT_MANT_DIG == 24, \"tl requires binary32 precision\");");
        Line(writer, "_Static_assert(FLT_MIN_EXP == -125, \"tl requires binary32 minimum exponent\");");
        Line(writer, "_Static_assert(FLT_MAX_EXP == 128, \"tl requires binary32 maximum exponent\");");
        Line(writer, "_Static_assert(sizeof(tl_playback) == 16, \"tl_playback ABI mismatch\");");
        Line(writer, "_Static_assert(_Alignof(tl_playback) == 8, \"tl_playback alignment mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_playback, position) == 0, \"tl_playback ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_playback, game_tick) == 8, \"tl_playback ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_playback, owner) == 12, \"tl_playback ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_playback, flags) == 14, \"tl_playback ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_playback, reserved) == 15, \"tl_playback ABI mismatch\");");
        Line(writer, "_Static_assert(sizeof(tl_frame) == 40, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(_Alignof(tl_frame) == 8, \"tl_frame alignment mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, cycle) == 0, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, game_tick) == 8, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, timeline_tick) == 12, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, track_payload) == 16, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, first_payload) == 20, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, second_payload) == 24, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, factor) == 28, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, track_index) == 32, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, flags) == 34, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, payload_count) == 35, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, reserved) == 36, \"tl_frame ABI mismatch\");");
        Line(writer, "#endif");
        Line(writer);
        foreach (var symbol in plan.Tracks
            .Select(track => operations[track.Operation])
            .Select(static operation => operation.SeekSymbol)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
            Line(writer, $"void {symbol}(void *context, const tl_frame *frame);");
        if (plan.Tracks.Length != 0)
            Line(writer);
        Line(writer, $"uint16_t {prefix}_id(void);");
        Line(writer, $"uint32_t {prefix}_duration(void);");
        Line(writer, $"bool {prefix}_is_looping(void);");
        Line(writer, $"bool {prefix}_try_start(uint16_t id, uint32_t game_tick, tl_playback *playback);");
        Line(writer, $"bool {prefix}_try_stop(uint16_t id, const tl_playback *playback, tl_playback *stopped);");
        Line(writer, $"bool {prefix}_try_seek(uint16_t id, const tl_playback *playback, int32_t delta, void *context, tl_playback *next);");
        Line(writer);
        Line(writer, "#endif");
        return writer.ToString();
    }

    private static string EmitSource(
        TimelinePlan plan,
        CBinding binding,
        IReadOnlyDictionary<OperationId, COperationBinding> operations,
        ImmutableArray<Region> regions)
    {
        var prefix = binding.SymbolPrefix;
        var writer = new StringBuilder();
        Line(writer, $"#include \"{binding.HeaderFileName}\"");
        Line(writer);
        Line(writer, $"uint16_t {prefix}_id(void) {{ return (uint16_t){I(plan.RuntimeId)}u; }}");
        Line(writer, $"uint32_t {prefix}_duration(void) {{ return (uint32_t){U(plan.Duration)}; }}");
        Line(writer, $"bool {prefix}_is_looping(void) {{ return {Bool(plan.Loops)}; }}");
        Line(writer);
        Line(writer, $"static bool {prefix}_can_run(uint16_t id, const tl_playback *playback, tl_playback *before)");
        Line(writer, "{");
        Line(writer, "    if (playback == NULL) return false;");
        Line(writer, "    *before = *playback;");
        Line(writer, $"    return id == (uint16_t){I(plan.RuntimeId)}u && before->owner == id && (before->flags & (TL_PLAYBACK_STARTED | TL_PLAYBACK_STOPPED)) == TL_PLAYBACK_STARTED;");
        Line(writer, "}");
        Line(writer);
        if (plan.Duration != 0)
        {
            EmitApply(writer, prefix, operations, regions, false);
            Line(writer);
            EmitApply(writer, prefix, operations, regions, true);
            Line(writer);
        }
        Line(writer, $"bool {prefix}_try_start(uint16_t id, uint32_t game_tick, tl_playback *playback)");
        Line(writer, "{");
        Line(writer, "    if (playback == NULL) return false;");
        Line(writer, "    *playback = (tl_playback){ 0u, 0u, 0u, 0u, 0u };");
        Line(writer, $"    if (id != (uint16_t){I(plan.RuntimeId)}u) return false;");
        Line(writer, "    *playback = (tl_playback){ INT64_C(0), game_tick, id, TL_PLAYBACK_STARTED, 0u };");
        Line(writer, "    return true;");
        Line(writer, "}");
        Line(writer);
        Line(writer, $"bool {prefix}_try_stop(uint16_t id, const tl_playback *playback, tl_playback *stopped)");
        Line(writer, "{");
        Line(writer, "    if (playback == NULL || stopped == NULL) return false;");
        Line(writer, "    tl_playback before = *playback;");
        Line(writer, "    *stopped = before;");
        Line(writer, $"    if (id != (uint16_t){I(plan.RuntimeId)}u || before.owner != id || (before.flags & TL_PLAYBACK_STARTED) == 0u) return false;");
        Line(writer, "    stopped->flags = (tl_playback_flags)(stopped->flags | TL_PLAYBACK_STOPPED);");
        Line(writer, "    return true;");
        Line(writer, "}");
        Line(writer);
        EmitSeek(writer, plan, prefix);
        return writer.ToString();
    }

    private static void EmitApply(
        StringBuilder writer,
        string prefix,
        IReadOnlyDictionary<OperationId, COperationBinding> operations,
        ImmutableArray<Region> regions,
        bool backward)
    {
        var direction = backward ? "backward" : "forward";
        Line(writer, $"static void {prefix}_apply_{direction}(uint32_t local, uint32_t game_tick, int64_t cycle, tl_frame_flags frame_flags, void *context)");
        Line(writer, "{");
        Line(writer, "    (void)context;");
        Line(writer, "    (void)local;");
        Line(writer, "    (void)game_tick;");
        Line(writer, "    (void)cycle;");
        Line(writer, "    (void)frame_flags;");
        for (var regionIndex = 0; regionIndex < regions.Length; regionIndex++)
        {
            var region = regions[regionIndex];
            var branch = regionIndex == 0 ? "if" : "else if";
            var condition = region.Start == 0u
                ? $"local < {U(region.End)}"
                : $"local >= {U(region.Start)} && local < {U(region.End)}";
            Line(writer, $"    {branch} ({condition})");
            Line(writer, "    {");
            var works = backward ? region.Works.Reverse() : region.Works;
            foreach (var work in works)
                EmitWork(writer, operations, work);
            Line(writer, "    }");
        }
        Line(writer, "}");
    }

    private static void EmitWork(
        StringBuilder writer,
        IReadOnlyDictionary<OperationId, COperationBinding> operations,
        Work work)
    {
        var second = work.Second;
        var factor = second is null
            ? "0.0f"
            : work.FactorLength <= 1
                ? "0.5f"
                : $"(float)(local - {U(work.FactorStart)}) / {I(work.FactorLength - 1)}.0f";
        var secondPayload = second?.Payload ?? 0u;
        var payloadCount = second is null ? 1 : 2;
        Line(writer, "        {");
        Line(writer, "            tl_frame_flags work_flags = frame_flags;");
        var starts = second is null
            ? $"local == {U(work.First.Start)}"
            : $"local == {U(work.First.Start)} || local == {U(second.Value.Start)}";
        var ends = second is null
            ? $"local == {U(work.First.End - 1u)}"
            : $"local == {U(work.First.End - 1u)} || local == {U(second.Value.End - 1u)}";
        Line(writer, $"            if ({starts}) work_flags = (tl_frame_flags)(work_flags | TL_FRAME_CLIP_START);");
        Line(writer, $"            if ({ends}) work_flags = (tl_frame_flags)(work_flags | TL_FRAME_CLIP_END);");
        Line(writer, $"            tl_frame frame = {{ cycle, game_tick, local, {U(work.Track.Payload)}, {U(work.First.Payload)}, {U(secondPayload)}, {factor}, (uint16_t){I(work.Track.Index)}u, work_flags, (uint8_t){I(payloadCount)}u, 0u }};");
        var operation = operations[work.Track.Operation];
        Line(writer, $"            {operation.SeekSymbol}(context, &frame);");
        Line(writer, "        }");
    }

    private static void EmitSeek(StringBuilder writer, TimelinePlan plan, string prefix)
    {
        Line(writer, $"bool {prefix}_try_seek(uint16_t id, const tl_playback *playback, int32_t delta, void *context, tl_playback *next)");
        Line(writer, "{");
        if (plan.Duration == 0)
            Line(writer, "    (void)context;");
        Line(writer, "    if (next == NULL) return false;");
        Line(writer, "    tl_playback before;");
        Line(writer, $"    if (!{prefix}_can_run(id, playback, &before))");
        Line(writer, "    {");
        Line(writer, "        if (playback != NULL) *next = *playback;");
        Line(writer, "        return false;");
        Line(writer, "    }");
        Line(writer, "    int64_t distance = (int64_t)delta;");
        var finite = !plan.Loops || plan.Duration == 0;
        if (finite)
            Line(writer, $"    if (before.position < INT64_C(0) || before.position > INT64_C({I(plan.Duration)})) {{ *next = before; return false; }}");
        Line(writer, "    if ((distance > INT64_C(0) && before.position > INT64_MAX - distance) || (distance < INT64_C(0) && before.position < INT64_MIN - distance)) { *next = before; return false; }");
        Line(writer, "    int64_t target_position = before.position + distance;");
        if (finite)
            Line(writer, $"    if (target_position < INT64_C(0) || target_position > INT64_C({I(plan.Duration)})) {{ *next = before; return false; }}");
        Line(writer, "    if (delta == 0) { *next = before; return true; }");
        if (plan.Duration == 0)
        {
            Line(writer, "    *next = before;");
            Line(writer, "    return false;");
            Line(writer, "}");
            return;
        }
        if (!plan.Clips.IsEmpty)
            Line(writer, "    if (context == NULL) { *next = before; return false; }");
        Line(writer, "    uint32_t target_game_tick = before.game_tick + (uint32_t)delta;");
        EmitInitialPosition(writer, plan);
        Line(writer, "    if (delta == 1)");
        Line(writer, "    {");
        EmitFrameFlags(writer, plan, false, "local", "before.position", 2);
        Line(writer, $"        {prefix}_apply_forward(local, before.game_tick, cycle, frame_flags, context);");
        Line(writer, "    }");
        Line(writer, "    else if (delta == -1)");
        Line(writer, "    {");
        Line(writer, "        uint32_t game_tick = before.game_tick;");
        EmitReverseStep(writer, plan, 2);
        EmitFrameFlags(writer, plan, true, "local", "target_position", 2);
        Line(writer, $"        {prefix}_apply_backward(local, game_tick, cycle, frame_flags, context);");
        Line(writer, "    }");
        Line(writer, "    else if (delta > 1)");
        Line(writer, "    {");
        Line(writer, "        int64_t position = before.position;");
        Line(writer, "        uint32_t game_tick = before.game_tick;");
        Line(writer, "        while (position < target_position)");
        Line(writer, "        {");
        EmitFrameFlags(writer, plan, false, "local", "position", 3);
        Line(writer, $"            {prefix}_apply_forward(local, game_tick, cycle, frame_flags, context);");
        Line(writer, "            position++;");
        Line(writer, "            game_tick++;");
        EmitForwardStep(writer, plan, 3);
        Line(writer, "        }");
        Line(writer, "    }");
        Line(writer, "    else");
        Line(writer, "    {");
        Line(writer, "        int64_t position = before.position;");
        Line(writer, "        uint32_t game_tick = before.game_tick;");
        Line(writer, "        while (position > target_position)");
        Line(writer, "        {");
        EmitReverseStep(writer, plan, 3);
        Line(writer, "            position--;");
        EmitFrameFlags(writer, plan, true, "local", "position", 3);
        Line(writer, $"            {prefix}_apply_backward(local, game_tick, cycle, frame_flags, context);");
        Line(writer, "        }");
        Line(writer, "    }");
        Line(writer, "    *next = (tl_playback){ target_position, target_game_tick, id, before.flags, 0u };");
        Line(writer, "    return true;");
        Line(writer, "}");
    }

    private static void EmitInitialPosition(StringBuilder writer, TimelinePlan plan)
    {
        if (plan.Loops)
        {
            Line(writer, $"    int64_t cycle = before.position / INT64_C({I(plan.Duration)});");
            Line(writer, $"    int64_t remainder = before.position - cycle * INT64_C({I(plan.Duration)});");
            Line(writer, "    if (remainder < INT64_C(0))");
            Line(writer, "    {");
            Line(writer, $"        remainder += INT64_C({I(plan.Duration)});");
            Line(writer, "        cycle--;");
            Line(writer, "    }");
            Line(writer, "    uint32_t local = (uint32_t)remainder;");
        }
        else
        {
            Line(writer, "    int64_t cycle = INT64_C(0);");
            Line(writer, "    uint32_t local = (uint32_t)before.position;");
        }
    }

    private static void EmitForwardStep(StringBuilder writer, TimelinePlan plan, int depth)
    {
        var indent = new string(' ', depth * 4);
        if (plan.Loops)
        {
            Line(writer, $"{indent}if (local == {U(plan.Duration - 1u)})");
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

    private static void EmitReverseStep(StringBuilder writer, TimelinePlan plan, int depth)
    {
        var indent = new string(' ', depth * 4);
        Line(writer, $"{indent}game_tick--;");
        if (plan.Loops)
        {
            Line(writer, $"{indent}if (local == 0u)");
            Line(writer, $"{indent}{{");
            Line(writer, $"{indent}    local = {U(plan.Duration - 1u)};");
            Line(writer, $"{indent}    cycle--;");
            Line(writer, $"{indent}}}");
            Line(writer, $"{indent}else");
            Line(writer, $"{indent}    local--;");
        }
        else
            Line(writer, $"{indent}local--;");
    }

    private static void EmitFrameFlags(StringBuilder writer, TimelinePlan plan, bool reverse, string local, string position, int depth)
    {
        var indent = new string(' ', depth * 4);
        var initial = new List<string>();
        if (plan.Loops)
            initial.Add("TL_FRAME_LOOPING");
        if (reverse)
            initial.Add("TL_FRAME_REVERSE");
        Line(writer, $"{indent}tl_frame_flags frame_flags = (tl_frame_flags){(initial.Count == 0 ? "0u" : $"({string.Join(" | ", initial)})")};");
        Line(writer, $"{indent}if ({local} == 0u) frame_flags = (tl_frame_flags)(frame_flags | TL_FRAME_TIMELINE_START);");
        Line(writer, $"{indent}if ({local} == {U(plan.Duration - 1u)}) frame_flags = (tl_frame_flags)(frame_flags | TL_FRAME_TIMELINE_END);");
        if (!plan.Loops)
            Line(writer, $"{indent}if ({position} == INT64_C({I(plan.Duration - 1u)})) frame_flags = (tl_frame_flags)(frame_flags | {(reverse ? "TL_FRAME_COMPLETED_BEFORE" : "TL_FRAME_COMPLETED_AFTER")});");
    }

    private static ImmutableArray<Region> Regions(TimelinePlan plan)
    {
        if (plan.Duration == 0)
            return [];
        var cuts = plan.Clips
            .SelectMany(static clip => new[] { clip.Start, clip.End })
            .Append(0u)
            .Append(plan.Duration)
            .Distinct()
            .Order()
            .ToArray();
        var regions = ImmutableArray.CreateBuilder<Region>(cuts.Length - 1);
        for (var index = 0; index + 1 < cuts.Length; index++)
        {
            var start = cuts[index];
            var end = cuts[index + 1];
            var works = ImmutableArray.CreateBuilder<Work>();
            foreach (var track in plan.Tracks)
            {
                var active = plan.Clips
                    .Where(clip => clip.TrackIndex == track.Index && clip.Start <= start && start < clip.End)
                    .Select(static (clip, authored) => (Clip: clip, Authored: authored))
                    .OrderBy(static item => item.Clip.Start)
                    .ThenBy(static item => item.Authored)
                    .Select(static item => item.Clip)
                    .ToArray();
                if (active.Length == 1)
                {
                    var clip = active[0];
                    works.Add(new Work(track, clip, null, 0u, 0u));
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
                        factorStart,
                        factorEnd - factorStart));
                }
            }
            regions.Add(new Region(start, end, works.ToImmutable()));
        }
        return regions.MoveToImmutable();
    }

    private static void ValidateIdentifier(string value, string parameter)
    {
        if (string.IsNullOrEmpty(value) || Keywords.Contains(value) || !IsIdentifierStart(value[0]) || value[0] == '_' || value.Skip(1).Any(static character => !IsIdentifierPart(character)))
            throw new ArgumentException($"'{value}' is not a portable C identifier.", parameter);
    }

    private static void ValidateCallbackIdentifier(string value, HashSet<string> reservedSymbols, string parameter)
    {
        ValidateIdentifier(value, parameter);
        if (reservedSymbols.Contains(value))
            throw new ArgumentException($"'{value}' conflicts with an identifier used by the generated C ABI.", parameter);
    }

    private static HashSet<string> ReservedSymbols(string prefix)
        => new(StringComparer.Ordinal)
        {
            "bool",
            "false",
            "true",
            "__bool_true_false_are_defined",
            "NULL",
            "CHAR_BIT",
            "SCHAR_MIN",
            "SCHAR_MAX",
            "UCHAR_MAX",
            "CHAR_MIN",
            "CHAR_MAX",
            "MB_LEN_MAX",
            "SHRT_MIN",
            "SHRT_MAX",
            "USHRT_MAX",
            "INT_MIN",
            "INT_MAX",
            "UINT_MAX",
            "LONG_MIN",
            "LONG_MAX",
            "ULONG_MAX",
            "LLONG_MIN",
            "LLONG_MAX",
            "ULLONG_MAX",
            "FLT_RADIX",
            "FLT_ROUNDS",
            "FLT_EVAL_METHOD",
            "DECIMAL_DIG",
            "FLT_DECIMAL_DIG",
            "DBL_DECIMAL_DIG",
            "LDBL_DECIMAL_DIG",
            "FLT_MANT_DIG",
            "DBL_MANT_DIG",
            "LDBL_MANT_DIG",
            "FLT_DIG",
            "DBL_DIG",
            "LDBL_DIG",
            "FLT_MIN_EXP",
            "DBL_MIN_EXP",
            "LDBL_MIN_EXP",
            "FLT_MIN_10_EXP",
            "DBL_MIN_10_EXP",
            "LDBL_MIN_10_EXP",
            "FLT_MAX_EXP",
            "DBL_MAX_EXP",
            "LDBL_MAX_EXP",
            "FLT_MAX_10_EXP",
            "DBL_MAX_10_EXP",
            "LDBL_MAX_10_EXP",
            "FLT_MAX",
            "DBL_MAX",
            "LDBL_MAX",
            "FLT_EPSILON",
            "DBL_EPSILON",
            "LDBL_EPSILON",
            "FLT_MIN",
            "DBL_MIN",
            "LDBL_MIN",
            "FLT_TRUE_MIN",
            "DBL_TRUE_MIN",
            "LDBL_TRUE_MIN",
            "FLT_HAS_SUBNORM",
            "DBL_HAS_SUBNORM",
            "LDBL_HAS_SUBNORM",
            "offsetof",
            "ptrdiff_t",
            "rsize_t",
            "size_t",
            "max_align_t",
            "wchar_t",
            "int8_t",
            "int16_t",
            "int32_t",
            "int64_t",
            "uint8_t",
            "uint16_t",
            "uint32_t",
            "uint64_t",
            "int_least8_t",
            "int_least16_t",
            "int_least32_t",
            "int_least64_t",
            "uint_least8_t",
            "uint_least16_t",
            "uint_least32_t",
            "uint_least64_t",
            "int_fast8_t",
            "int_fast16_t",
            "int_fast32_t",
            "int_fast64_t",
            "uint_fast8_t",
            "uint_fast16_t",
            "uint_fast32_t",
            "uint_fast64_t",
            "intptr_t",
            "uintptr_t",
            "intmax_t",
            "uintmax_t",
            "INT8_MIN",
            "INT16_MIN",
            "INT32_MIN",
            "INT64_MIN",
            "INT8_MAX",
            "INT16_MAX",
            "INT32_MAX",
            "INT64_MAX",
            "UINT8_MAX",
            "UINT16_MAX",
            "UINT32_MAX",
            "UINT64_MAX",
            "INT_LEAST8_MIN",
            "INT_LEAST16_MIN",
            "INT_LEAST32_MIN",
            "INT_LEAST64_MIN",
            "INT_LEAST8_MAX",
            "INT_LEAST16_MAX",
            "INT_LEAST32_MAX",
            "INT_LEAST64_MAX",
            "UINT_LEAST8_MAX",
            "UINT_LEAST16_MAX",
            "UINT_LEAST32_MAX",
            "UINT_LEAST64_MAX",
            "INT_FAST8_MIN",
            "INT_FAST16_MIN",
            "INT_FAST32_MIN",
            "INT_FAST64_MIN",
            "INT_FAST8_MAX",
            "INT_FAST16_MAX",
            "INT_FAST32_MAX",
            "INT_FAST64_MAX",
            "UINT_FAST8_MAX",
            "UINT_FAST16_MAX",
            "UINT_FAST32_MAX",
            "UINT_FAST64_MAX",
            "INTPTR_MIN",
            "INTPTR_MAX",
            "UINTPTR_MAX",
            "INTMAX_MIN",
            "INTMAX_MAX",
            "UINTMAX_MAX",
            "PTRDIFF_MIN",
            "PTRDIFF_MAX",
            "SIG_ATOMIC_MIN",
            "SIG_ATOMIC_MAX",
            "SIZE_MAX",
            "RSIZE_MAX",
            "WCHAR_MIN",
            "WCHAR_MAX",
            "WINT_MIN",
            "WINT_MAX",
            "INT8_C",
            "INT16_C",
            "INT32_C",
            "INT64_C",
            "UINT8_C",
            "UINT16_C",
            "UINT32_C",
            "UINT64_C",
            "INTMAX_C",
            "UINTMAX_C",
            "tl_frame_flags",
            "tl_frame",
            "tl_playback",
            "tl_playback_flags",
            "TL_C_ABI_V2_TYPES",
            "TL_C_ABI_VERSION",
            "TL_C_ABI_ENDIANNESS_NATIVE",
            "TL_C_ABI_FLOAT_EVALUATION_NATIVE",
            "TL_C_ABI_FLOAT_ROUNDING_NATIVE",
            "TL_FRAME_CLIP_END",
            "TL_FRAME_CLIP_START",
            "TL_FRAME_COMPLETED_AFTER",
            "TL_FRAME_COMPLETED_BEFORE",
            "TL_FRAME_LOOPING",
            "TL_FRAME_REVERSE",
            "TL_FRAME_TIMELINE_END",
            "TL_FRAME_TIMELINE_START",
            "TL_PLAYBACK_STARTED",
            "TL_PLAYBACK_STOPPED",
            "TL_" + prefix + "_H",
            prefix + "_apply_backward",
            prefix + "_apply_forward",
            prefix + "_can_run",
            prefix + "_duration",
            prefix + "_id",
            prefix + "_is_looping",
            prefix + "_try_seek",
            prefix + "_try_start",
            prefix + "_try_stop"
        };

    private static void ValidateFileName(string value, string parameter)
    {
        if (string.IsNullOrEmpty(value) || !value.EndsWith(".h", StringComparison.Ordinal) || value.Any(static character => !(char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.')))
            throw new ArgumentException($"'{value}' is not a portable header file name.", parameter);
    }

    private static bool IsIdentifierStart(char value) => char.IsAsciiLetter(value) || value == '_';
    private static bool IsIdentifierPart(char value) => char.IsAsciiLetterOrDigit(value) || value == '_';
    private static string Bool(bool value) => value ? "true" : "false";
    private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string I(ushort value) => value.ToString(CultureInfo.InvariantCulture);
    private static string I(uint value) => value.ToString(CultureInfo.InvariantCulture);
    private static string U(uint value) => I(value) + "u";

    private static void Line(StringBuilder writer, string value = "")
    {
        writer.Append(value);
        writer.Append('\n');
    }
}
