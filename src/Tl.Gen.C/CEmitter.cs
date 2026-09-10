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
        uint EnterForward,
        uint EnterBackward,
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
                1,
                plan.Tracks.Length,
                plan.Clips.Length,
                regions.Length,
                plan.Duration,
                plan.Loops,
                artifacts.Length,
                artifacts.Sum(static artifact => artifact.Utf8Bytes),
                0,
                12,
                4,
                24,
                4,
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
            ValidateCallbackIdentifier(operation.ForwardSymbol, reservedSymbols, nameof(binding));
            ValidateCallbackIdentifier(operation.BackwardSymbol, reservedSymbols, nameof(binding));
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
        Line(writer, "#include <stddef.h>");
        Line(writer, "#include <stdint.h>");
        Line(writer);
        Line(writer, "#ifndef TL_C_ABI_VERSION");
        Line(writer, "#define TL_C_ABI_VERSION 1");
        Line(writer, "#elif TL_C_ABI_VERSION != 1");
        Line(writer, "#error \"incompatible tl C ABI version\"");
        Line(writer, "#endif");
        Line(writer, "#ifndef TL_C_ABI_V1_TYPES");
        Line(writer, "#define TL_C_ABI_V1_TYPES 1");
        Line(writer, "typedef uint16_t tl_playback_flags;");
        Line(writer, "typedef uint8_t tl_clip_state;");
        Line(writer, "enum { TL_PLAYBACK_STARTED = 1u, TL_PLAYBACK_STOPPED = 2u, TL_PLAYBACK_LAST_LOOP_FRAME = 4u, TL_PLAYBACK_COMPLETED = 8u };");
        Line(writer, "enum { TL_CLIP_ENTER = 0u, TL_CLIP_STAY = 1u, TL_CLIP_EXIT = 2u };");
        Line(writer, "typedef struct { uint32_t tick; uint16_t cycles; uint16_t owner; tl_playback_flags flags; uint16_t reserved; } tl_playback;");
        Line(writer, "typedef struct { uint32_t tick; uint32_t track_payload; uint32_t first_payload; uint32_t second_payload; float factor; uint16_t track_index; tl_clip_state state; uint8_t payload_count; } tl_frame;");
        Line(writer, "_Static_assert(sizeof(float) == 4, \"tl requires 32-bit float\");");
        Line(writer, "_Static_assert(sizeof(tl_playback) == 12, \"tl_playback ABI mismatch\");");
        Line(writer, "_Static_assert(_Alignof(tl_playback) == 4, \"tl_playback alignment mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_playback, flags) == 8, \"tl_playback ABI mismatch\");");
        Line(writer, "_Static_assert(sizeof(tl_frame) == 24, \"tl_frame ABI mismatch\");");
        Line(writer, "_Static_assert(_Alignof(tl_frame) == 4, \"tl_frame alignment mismatch\");");
        Line(writer, "_Static_assert(offsetof(tl_frame, track_index) == 20, \"tl_frame ABI mismatch\");");
        Line(writer, "#endif");
        Line(writer);
        foreach (var symbol in plan.Tracks
            .Select(track => operations[track.Operation])
            .SelectMany(static operation => new[] { operation.ForwardSymbol, operation.BackwardSymbol })
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
            Line(writer, $"void {symbol}(void *context, const tl_frame *frame);");
        if (plan.Tracks.Length != 0)
            Line(writer);
        Line(writer, $"uint16_t {prefix}_id(void);");
        Line(writer, $"uint32_t {prefix}_duration(void);");
        Line(writer, $"bool {prefix}_is_looping(void);");
        Line(writer, $"bool {prefix}_try_start(uint16_t id, uint32_t at, tl_playback *playback);");
        Line(writer, $"bool {prefix}_try_stop(uint16_t id, const tl_playback *playback, tl_playback *stopped);");
        Line(writer, $"bool {prefix}_try_forward(uint16_t id, const tl_playback *playback, uint32_t tick, void *context, tl_playback *next);");
        Line(writer, $"bool {prefix}_try_backward(uint16_t id, const tl_playback *playback, uint32_t tick, void *context, tl_playback *next);");
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
        Line(writer, $"static bool {prefix}_can_run(uint16_t id, const tl_playback *playback, tl_playback *next)");
        Line(writer, "{");
        Line(writer, $"    return playback != NULL && next != NULL && id == (uint16_t){I(plan.RuntimeId)}u && playback->owner == id && (playback->flags & TL_PLAYBACK_STARTED) != 0u && (playback->flags & TL_PLAYBACK_STOPPED) == 0u;");
        Line(writer, "}");
        Line(writer);
        EmitPosition(writer, plan, prefix, false);
        Line(writer);
        EmitPosition(writer, plan, prefix, true);
        Line(writer);
        EmitApply(writer, plan, prefix, operations, regions, false);
        Line(writer);
        EmitApply(writer, plan, prefix, operations, regions, true);
        Line(writer);
        Line(writer, $"bool {prefix}_try_start(uint16_t id, uint32_t at, tl_playback *playback)");
        Line(writer, "{");
        Line(writer, "    if (playback == NULL) return false;");
        Line(writer, "    *playback = (tl_playback){ 0u, 0u, 0u, 0u, 0u };");
        Line(writer, $"    if (id != (uint16_t){I(plan.RuntimeId)}u) return false;");
        Line(writer, "    *playback = (tl_playback){ at, 0u, id, TL_PLAYBACK_STARTED, 0u };");
        Line(writer, "    return true;");
        Line(writer, "}");
        Line(writer);
        Line(writer, $"bool {prefix}_try_stop(uint16_t id, const tl_playback *playback, tl_playback *stopped)");
        Line(writer, "{");
        Line(writer, "    if (playback == NULL || stopped == NULL) return false;");
        Line(writer, "    *stopped = *playback;");
        Line(writer, $"    if (id != (uint16_t){I(plan.RuntimeId)}u || playback->owner != id || (playback->flags & TL_PLAYBACK_STARTED) == 0u) return false;");
        Line(writer, "    stopped->flags = (tl_playback_flags)(stopped->flags | TL_PLAYBACK_STOPPED);");
        Line(writer, "    return true;");
        Line(writer, "}");
        Line(writer);
        EmitAdvance(writer, plan, prefix, false);
        Line(writer);
        EmitAdvance(writer, plan, prefix, true);
        return writer.ToString();
    }

    private static void EmitPosition(StringBuilder writer, TimelinePlan plan, string prefix, bool backward)
    {
        var direction = backward ? "backward" : "forward";
        Line(writer, $"static bool {prefix}_position_{direction}(const tl_playback *from, uint32_t tick, uint32_t *effective, uint32_t *previous_effective, uint32_t *crossed_cycles, uint16_t *cycles)");
        Line(writer, "{");
        if (!plan.Loops || plan.Duration == 0)
        {
            Line(writer, "    *effective = tick;");
            Line(writer, "    *previous_effective = from->tick;");
            Line(writer, "    *crossed_cycles = 0u;");
            Line(writer, "    *cycles = from->cycles;");
            Line(writer, "    return true;");
            Line(writer, "}");
            return;
        }

        Line(writer, $"    uint32_t previous_quotient = from->tick / {U(plan.Duration)};");
        Line(writer, $"    *previous_effective = from->tick - previous_quotient * {U(plan.Duration)};");
        Line(writer, $"    uint32_t quotient = tick / {U(plan.Duration)};");
        Line(writer, $"    *effective = tick - quotient * {U(plan.Duration)};");
        if (!backward)
        {
            Line(writer, "    *crossed_cycles = tick >= from->tick ? quotient - previous_quotient : (*effective < *previous_effective ? 1u : 0u);");
            Line(writer, "    if (*crossed_cycles > (uint32_t)UINT16_MAX - from->cycles) { *cycles = from->cycles; return false; }");
            Line(writer, "    *cycles = (uint16_t)(from->cycles + *crossed_cycles);");
        }
        else
        {
            Line(writer, "    *crossed_cycles = tick <= from->tick ? previous_quotient - quotient : (*effective > *previous_effective ? 1u : 0u);");
            Line(writer, "    *cycles = *crossed_cycles >= from->cycles ? 0u : (uint16_t)(from->cycles - *crossed_cycles);");
        }
        Line(writer, "    return true;");
        Line(writer, "}");
    }

    private static void EmitApply(
        StringBuilder writer,
        TimelinePlan plan,
        string prefix,
        IReadOnlyDictionary<OperationId, COperationBinding> operations,
        ImmutableArray<Region> regions,
        bool backward)
    {
        var direction = backward ? "backward" : "forward";
        Line(writer, $"static bool {prefix}_apply_{direction}(uint32_t effective, uint32_t previous_effective, uint32_t crossed_cycles, void *context)");
        Line(writer, "{");
        Line(writer, "    (void)context;");
        Line(writer, "    (void)effective;");
        Line(writer, "    (void)previous_effective;");
        Line(writer, "    (void)crossed_cycles;");
        for (var regionIndex = 0; regionIndex < regions.Length; regionIndex++)
        {
            var region = regions[regionIndex];
            var branch = regionIndex == 0 ? "if" : "else if";
            var condition = region.Start == 0u
                ? $"effective < {U(region.End)}"
                : $"effective >= {U(region.Start)} && effective < {U(region.End)}";
            Line(writer, $"    {branch} ({condition})");
            Line(writer, "    {");
            if (!region.Works.IsEmpty)
                Line(writer, "        if (context == NULL) return false;");
            for (var workIndex = 0; workIndex < region.Works.Length; workIndex++)
                EmitWork(writer, plan, operations, region.Works[workIndex], backward);
            Line(writer, "    }");
        }
        Line(writer, "    return true;");
        Line(writer, "}");
    }

    private static void EmitWork(
        StringBuilder writer,
        TimelinePlan plan,
        IReadOnlyDictionary<OperationId, COperationBinding> operations,
        Work work,
        bool backward)
    {
        var exit = backward ? work.EnterForward : work.EnterBackward - 1u;
        var crossed = backward
            ? $"previous_effective >= {U(work.EnterBackward)}"
            : $"previous_effective < {U(work.EnterForward)}";
        var enterPossible = backward
            ? !plan.Loops || work.EnterBackward < plan.Duration
            : work.EnterForward != 0;
        var enter = plan.Loops
            ? enterPossible ? $"crossed_cycles != 0u || {crossed}" : "crossed_cycles != 0u"
            : enterPossible ? crossed : "false";
        var state = enter == "false"
            ? $"effective == {U(exit)} ? TL_CLIP_EXIT : TL_CLIP_STAY"
            : $"effective == {U(exit)} ? TL_CLIP_EXIT : ({enter} ? TL_CLIP_ENTER : TL_CLIP_STAY)";
        var second = work.Second;
        var factor = second is null
            ? "0.0f"
            : work.FactorLength <= 1
                ? "0.5f"
                : $"(float)(effective - {U(work.FactorStart)}) / {I(work.FactorLength - 1)}.0f";
        var secondPayload = second?.Payload ?? 0u;
        var payloadCount = second is null ? 1 : 2;
        Line(writer, "        {");
        Line(writer, $"            tl_frame frame = {{ effective, {U(work.Track.Payload)}, {U(work.First.Payload)}, {U(secondPayload)}, {factor}, (uint16_t){I(work.Track.Index)}u, (tl_clip_state)({state}), (uint8_t){I(payloadCount)}u }};");
        var operation = operations[work.Track.Operation];
        Line(writer, $"            {(backward ? operation.BackwardSymbol : operation.ForwardSymbol)}(context, &frame);");
        Line(writer, "        }");
    }

    private static void EmitAdvance(StringBuilder writer, TimelinePlan plan, string prefix, bool backward)
    {
        var direction = backward ? "backward" : "forward";
        Line(writer, $"bool {prefix}_try_{direction}(uint16_t id, const tl_playback *playback, uint32_t tick, void *context, tl_playback *next)");
        Line(writer, "{");
        Line(writer, $"    if (!{prefix}_can_run(id, playback, next))");
        Line(writer, "    {");
        Line(writer, "        if (playback != NULL && next != NULL) *next = *playback;");
        Line(writer, "        return false;");
        Line(writer, "    }");
        Line(writer, "    uint32_t effective;");
        Line(writer, "    uint32_t previous_effective;");
        Line(writer, "    uint32_t crossed_cycles;");
        Line(writer, "    uint16_t cycles;");
        Line(writer, $"    if (!{prefix}_position_{direction}(playback, tick, &effective, &previous_effective, &crossed_cycles, &cycles)) {{ *next = *playback; return false; }}");
        Line(writer, $"    if (!{prefix}_apply_{direction}(effective, previous_effective, crossed_cycles, context)) {{ *next = *playback; return false; }}");
        Line(writer, "    tl_playback_flags flags = TL_PLAYBACK_STARTED;");
        if (plan.Loops && plan.Duration != 0)
            Line(writer, $"    if (effective == {U(plan.Duration - 1u)}) flags = (tl_playback_flags)(flags | TL_PLAYBACK_LAST_LOOP_FRAME);");
        else if (plan.Duration == 0)
            Line(writer, "    flags = (tl_playback_flags)(flags | TL_PLAYBACK_COMPLETED);");
        else if (backward)
            Line(writer, "    if (effective == 0u) flags = (tl_playback_flags)(flags | TL_PLAYBACK_COMPLETED);");
        else
            Line(writer, $"    if (effective >= {U(plan.Duration - 1u)}) flags = (tl_playback_flags)(flags | TL_PLAYBACK_COMPLETED);");
        Line(writer, "    *next = (tl_playback){ tick, cycles, id, flags, 0u };");
        Line(writer, "    return true;");
        Line(writer, "}");
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
                    works.Add(new Work(track, clip, null, clip.Start, clip.End, 0u, 0u));
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
            "NULL",
            "offsetof",
            "ptrdiff_t",
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
            "tl_clip_state",
            "tl_frame",
            "tl_playback",
            "tl_playback_flags",
            "TL_C_ABI_V1_TYPES",
            "TL_C_ABI_VERSION",
            "TL_CLIP_ENTER",
            "TL_CLIP_EXIT",
            "TL_CLIP_STAY",
            "TL_PLAYBACK_COMPLETED",
            "TL_PLAYBACK_LAST_LOOP_FRAME",
            "TL_PLAYBACK_STARTED",
            "TL_PLAYBACK_STOPPED",
            "TL_" + prefix + "_H",
            prefix + "_apply_backward",
            prefix + "_apply_forward",
            prefix + "_can_run",
            prefix + "_duration",
            prefix + "_id",
            prefix + "_is_looping",
            prefix + "_position_backward",
            prefix + "_position_forward",
            prefix + "_try_backward",
            prefix + "_try_forward",
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
