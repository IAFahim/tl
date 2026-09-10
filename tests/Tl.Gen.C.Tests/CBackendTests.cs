using System.Diagnostics;
using Tl.Compiler;
using Xunit;

namespace Tl.Gen.C.Tests;

public sealed class CBackendTests
{
    private static readonly OperationId Combat = new("combat");
    private static readonly OperationId Audit = new("audit");

    [Fact]
    public void OutputIsDeterministicAndFreezesTheSupportedC11Abi()
    {
        var plan = CreatePlan("battle", 42, true);
        var binding = CreateBinding("battle");
        var first = CEmitter.Emit(plan, binding);
        var second = CEmitter.Emit(plan, binding);
        var emission = CEmitter.Generate(plan, binding);
        var output = string.Concat(first.Select(static artifact => artifact.Content));

        Assert.True(first.SequenceEqual(second));
        Assert.Equal(["battle.h", "battle.c"], first.Select(static artifact => artifact.RelativePath));
        Assert.Contains("#define TL_C_ABI_VERSION 2", first[0].Content);
        Assert.Contains("#include <float.h>", first[0].Content);
        Assert.Contains("#define TL_C_ABI_ENDIANNESS_NATIVE 1", first[0].Content);
        Assert.Contains("#define TL_C_ABI_FLOAT_EVALUATION_NATIVE 1", first[0].Content);
        Assert.Contains("#define TL_C_ABI_FLOAT_ROUNDING_NATIVE 1", first[0].Content);
        Assert.Contains("_Static_assert(CHAR_BIT == 8", first[0].Content);
        Assert.Contains("_Static_assert(FLT_RADIX == 2", first[0].Content);
        Assert.Contains("_Static_assert(FLT_MANT_DIG == 24", first[0].Content);
        Assert.Contains("_Static_assert(FLT_MIN_EXP == -125", first[0].Content);
        Assert.Contains("_Static_assert(FLT_MAX_EXP == 128", first[0].Content);
        Assert.Contains("_Alignas(8) int64_t position", first[0].Content);
        Assert.Contains("_Alignas(8) int64_t cycle", first[0].Content);
        Assert.Contains("_Static_assert(sizeof(tl_playback) == 16", first[0].Content);
        Assert.Contains("_Static_assert(sizeof(tl_frame) == 40", first[0].Content);
        Assert.Contains("_Static_assert(offsetof(tl_playback, flags) == 14", first[0].Content);
        Assert.Contains("_Static_assert(offsetof(tl_frame, flags) == 34", first[0].Content);
        Assert.Contains("local >= 4u && local < 5u", first[1].Content);
        Assert.Contains("combat_seek(context, &frame);", first[1].Content);
        Assert.Contains("bool battle_try_seek(uint16_t id, const tl_playback *playback, int32_t delta, void *context, tl_playback *next)", output);
        Assert.DoesNotContain("restrict context", output);
        Assert.DoesNotContain("try_forward", output);
        Assert.DoesNotContain("try_backward", output);
        Assert.DoesNotContain("tl_clip_state", output);
        Assert.DoesNotContain("TL_CLIP_", output);
        Assert.DoesNotContain("global::", output);
        Assert.Equal(1, emission.Report.PlanFormatVersion);
        Assert.Equal(2, emission.Report.AbiVersion);
        Assert.Equal(2, emission.Report.TrackCount);
        Assert.Equal(3, emission.Report.ClipCount);
        Assert.Equal(6, emission.Report.RegionCount);
        Assert.Equal(8u, emission.Report.Duration);
        Assert.True(emission.Report.Loops);
        Assert.Equal(emission.Artifacts.Sum(static artifact => artifact.Utf8Bytes), emission.Report.SourceUtf8Bytes);
        Assert.Equal(14_627, emission.Report.SourceUtf8Bytes);
        Assert.Equal(0, emission.Report.StaticDataBytes);
        Assert.Equal(16, emission.Report.PlaybackBytes);
        Assert.Equal(8, emission.Report.PlaybackAlignment);
        Assert.Equal(40, emission.Report.FrameBytes);
        Assert.Equal(8, emission.Report.FrameAlignment);
        Assert.Equal(0, emission.Report.RuntimeHeapBytes);
    }

    [Fact]
    public void InvalidPlansAndBindingsAreRejectedBeforeEmission()
    {
        var tripleOverlap = new TimelinePlan(
            "invalid",
            1,
            false,
            [new TrackPlan(0, 0, Combat)],
            [new ClipPlan(0, 1, 0, 8), new ClipPlan(0, 2, 1, 7), new ClipPlan(0, 3, 2, 6)]);

        Assert.Throws<ArgumentException>(() => CEmitter.Emit(tripleOverlap, CreateBinding("invalid")));
        Assert.Throws<ArgumentException>(() => CEmitter.Emit(CreatePlan("battle", 42, true), new CBinding("while", "battle.h", [])));
        Assert.Throws<ArgumentException>(() => CEmitter.Emit(CreatePlan("battle", 42, true), new CBinding("_battle", "battle.h", [])));
    }

    [Theory]
    [InlineData("reserved_try_seek")]
    [InlineData("reserved_apply_backward")]
    [InlineData("TL_reserved_H")]
    [InlineData("tl_frame")]
    [InlineData("tl_frame_flags")]
    [InlineData("TL_FRAME_REVERSE")]
    [InlineData("bool")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("NULL")]
    [InlineData("offsetof")]
    [InlineData("size_t")]
    [InlineData("ptrdiff_t")]
    [InlineData("max_align_t")]
    [InlineData("uint8_t")]
    [InlineData("uint16_t")]
    [InlineData("uint32_t")]
    [InlineData("int32_t")]
    [InlineData("int64_t")]
    [InlineData("int_least32_t")]
    [InlineData("uint_fast64_t")]
    [InlineData("intptr_t")]
    [InlineData("uintptr_t")]
    [InlineData("intmax_t")]
    [InlineData("uintmax_t")]
    [InlineData("CHAR_BIT")]
    [InlineData("INT_MAX")]
    [InlineData("ULLONG_MAX")]
    [InlineData("FLT_RADIX")]
    [InlineData("FLT_EPSILON")]
    [InlineData("FLT_ROUNDS")]
    [InlineData("FLT_EVAL_METHOD")]
    [InlineData("DECIMAL_DIG")]
    [InlineData("FLT_TRUE_MIN")]
    [InlineData("FLT_MANT_DIG")]
    [InlineData("FLT_MIN_EXP")]
    [InlineData("FLT_MAX_EXP")]
    [InlineData("INT64_C")]
    [InlineData("INT64_MAX")]
    [InlineData("INT64_MIN")]
    [InlineData("UINT32_C")]
    [InlineData("_reserved")]
    [InlineData("__reserved")]
    public void CallbackSymbolsCannotCollideWithTheGeneratedCTranslationUnit(string symbol)
    {
        var plan = new TimelinePlan(
            "reserved",
            1,
            false,
            [new TrackPlan(0, 0, Combat)],
            [new ClipPlan(0, 0, 0, 1)]);

        Assert.Throws<ArgumentException>(() => CEmitter.Emit(
            plan,
            new CBinding("reserved", "reserved.h", [new COperationBinding(Combat, symbol)])));
    }

    [Fact]
    public void FullUShortTrackIndexAndCountRangeIsRepresentable()
    {
        var tracks = Enumerable.Range(0, ushort.MaxValue + 1)
            .Select(static index => new TrackPlan((ushort)index, (uint)index, Combat))
            .ToArray();
        var plan = new TimelinePlan("capacity", 1, false, tracks, []);
        var binding = new CBinding(
            "capacity",
            "capacity.h",
            [new COperationBinding(Combat, "capacity_operation_seek")]);
        var emission = CEmitter.Generate(plan, binding);

        Assert.Equal(ushort.MaxValue + 1, emission.Report.TrackCount);
        Assert.Equal((ushort)255, plan.Tracks[255].Index);
        Assert.Equal((ushort)256, plan.Tracks[256].Index);
        Assert.Equal(ushort.MaxValue, plan.Tracks[ushort.MaxValue].Index);
        Assert.InRange(emission.Report.SourceUtf8Bytes, 1, 10_000);
        var tooMany = new TimelinePlan("capacity-overflow", 1, false, tracks.Append(tracks[0]), []);
        Assert.Throws<ArgumentException>(() => CEmitter.Emit(tooMany, binding));
    }

    [Fact]
    public void GeneratedC11MatchesSignedBoundaryAndReplaySemantics()
    {
        var compilers = AvailableCompilers();
        if (compilers.Count == 0)
            return;

        var root = Path.Combine(Path.GetTempPath(), "tl-c-backend-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Write(root, CEmitter.Emit(CreatePlan("battle", 42, true), CreateBinding("battle")));
            Write(root, CEmitter.Emit(CreatePlan("finite", 43, false), CreateBinding("finite")));
            Write(root, CEmitter.Emit(new TimelinePlan("empty", 45, false, [], []), new CBinding("empty", "empty.h", [])));
            Write(root, CEmitter.Emit(
                new TimelinePlan(
                    "one",
                    46,
                    false,
                    [new TrackPlan(0, 13, Combat)],
                    [new ClipPlan(0, 17, 0, 1)]),
                new CBinding("one", "one.h", [new COperationBinding(Combat, "one_seek")])));
            File.WriteAllText(Path.Combine(root, "consumer.c"), Consumer);

            foreach (var compiler in compilers)
            {
                var executable = Path.Combine(root, "receipt-" + Path.GetFileName(compiler));
                var compilation = Run(root, compiler, "-std=c11", "-O2", "-Wall", "-Wextra", "-Werror", "battle.c", "finite.c", "empty.c", "one.c", "consumer.c", "-o", executable);
                Assert.True(compilation.ExitCode == 0, $"{compiler}: {compilation.Output}");
                var receipt = Run(root, executable);
                Assert.True(receipt.ExitCode == 0, $"{compiler}: exit {receipt.ExitCode}: {receipt.Output}");
                Assert.Equal("c11 signed seek parity: 16 mirrored frames; lifecycle exact\n", receipt.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void GeneratedC11PreservesFullWidthTrackIndicesAndReverseOrder()
    {
        if (!CanRun("cc", "--version"))
            return;

        var plan = new TimelinePlan(
            "wide",
            44,
            false,
            [new TrackPlan(255, 2_550, Combat), new TrackPlan(256, 2_560, Combat), new TrackPlan(ushort.MaxValue, 65_535, Combat)],
            [new ClipPlan(255, 2_551, 1, 4), new ClipPlan(256, 2_561, 1, 4), new ClipPlan(ushort.MaxValue, 65_534, 1, 4)]);
        var binding = new CBinding("wide", "wide.h", [new COperationBinding(Combat, "wide_operation_seek")]);
        var root = Path.Combine(Path.GetTempPath(), "tl-c-wide-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Write(root, CEmitter.Emit(plan, binding));
            File.WriteAllText(Path.Combine(root, "consumer.c"), WideConsumer);
            var executable = Path.Combine(root, "receipt");
            var compilation = Run(root, "cc", "-std=c11", "-O2", "-Wall", "-Wextra", "-Werror", "wide.c", "consumer.c", "-o", executable);
            Assert.True(compilation.ExitCode == 0, compilation.Output);
            var receipt = Run(root, executable);
            Assert.True(receipt.ExitCode == 0, $"exit {receipt.ExitCode}: {receipt.Output}");
            Assert.Equal("c11 wide reverse order: 18 frames; balance 0\n", receipt.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static TimelinePlan CreatePlan(string identity, ushort runtimeId, bool loops)
        => new(
            identity,
            runtimeId,
            loops,
            [new TrackPlan(0, 9, Combat), new TrackPlan(1, 11, Audit)],
            [new ClipPlan(0, 10, 2, 5), new ClipPlan(0, 20, 4, 8), new ClipPlan(1, 7, 1, 3)]);

    private static CBinding CreateBinding(string prefix)
        => new(
            prefix,
            prefix + ".h",
            [new COperationBinding(Combat, "combat_seek"), new COperationBinding(Audit, "audit_seek")]);

    private static void Write(string root, IEnumerable<CArtifact> artifacts)
    {
        foreach (var artifact in artifacts)
            File.WriteAllText(Path.Combine(root, artifact.RelativePath), artifact.Content);
    }

    private static IReadOnlyList<string> AvailableCompilers()
        => new[] { "cc", "gcc", "clang" }.Where(static compiler => CanRun(compiler, "--version")).ToArray();

    private static bool CanRun(string fileName, params string[] arguments)
    {
        try
        {
            return Run(Path.GetTempPath(), fileName, arguments).ExitCode == 0;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return false;
        }
    }

    private static (int ExitCode, string Output) Run(string directory, string fileName, params string[] arguments)
    {
        var start = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start '{fileName}'.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output + error);
    }

    private const string Consumer = """
        #include "battle.h"
        #include "empty.h"
        #include "finite.h"
        #include "one.h"
        #include <math.h>
        #include <stdio.h>
        #include <string.h>

        typedef struct { tl_playback playback; tl_frame frames[64]; uint32_t count; int64_t balance; } receipt;

        static void record_frame(void *context, const tl_frame *frame)
        {
            receipt *value = (receipt *)context;
            int64_t amount = (int64_t)frame->track_payload + (int64_t)frame->first_payload + (int64_t)frame->second_payload + INT64_C(1);
            value->frames[value->count] = *frame;
            value->count++;
            value->balance += (frame->flags & TL_FRAME_REVERSE) != 0u ? -amount : amount;
        }

        void combat_seek(void *context, const tl_frame *frame) { record_frame(context, frame); }
        void audit_seek(void *context, const tl_frame *frame) { record_frame(context, frame); }
        void one_seek(void *context, const tl_frame *frame) { record_frame(context, frame); }

        static int playback_is(const tl_playback *first, const tl_playback *second)
        {
            return first->position == second->position && first->game_tick == second->game_tick && first->owner == second->owner && first->flags == second->flags && first->reserved == second->reserved;
        }

        static int receipt_is(const receipt *first, const receipt *second)
        {
            return first->count == second->count && first->balance == second->balance && memcmp(first->frames, second->frames, first->count * sizeof(tl_frame)) == 0;
        }

        static int identity_is(const tl_frame *forward, const tl_frame *reverse)
        {
            tl_frame_flags reverse_intrinsic = (tl_frame_flags)(reverse->flags & (tl_frame_flags)~TL_FRAME_REVERSE);
            return forward->cycle == reverse->cycle && forward->game_tick == reverse->game_tick && forward->timeline_tick == reverse->timeline_tick && forward->track_payload == reverse->track_payload && forward->first_payload == reverse->first_payload && forward->second_payload == reverse->second_payload && fabsf(forward->factor - reverse->factor) < 0.0001f && forward->track_index == reverse->track_index && forward->flags == reverse_intrinsic && forward->payload_count == reverse->payload_count;
        }

        int main(void)
        {
            receipt multi = { 0 };
            receipt scalar = { 0 };
            tl_playback multi_playback;
            tl_playback scalar_playback;
            tl_playback next;
            if (battle_id() != 42u || battle_duration() != 8u || !battle_is_looping()) return 1;
            if (!battle_try_start(42u, 200000u, &multi_playback) || !battle_try_start(42u, 200000u, &scalar_playback)) return 2;
            tl_playback started = multi_playback;
            if (started.position != INT64_C(0) || started.game_tick != 200000u || started.flags != TL_PLAYBACK_STARTED) return 3;
            if (!battle_try_seek(42u, &multi_playback, 0, NULL, &next) || !playback_is(&next, &multi_playback) || multi.count != 0u) return 4;
            if (!battle_try_seek(42u, &multi_playback, 5, &multi, &next)) return 5;
            multi_playback = next;
            for (int32_t step = 0; step < 5; step++)
            {
                if (!battle_try_seek(42u, &scalar_playback, 1, &scalar, &next)) return 6;
                scalar_playback = next;
            }
            if (!playback_is(&multi_playback, &scalar_playback) || !receipt_is(&multi, &scalar) || multi.count != 5u) return 7;
            if (multi.frames[0].timeline_tick != 1u || multi.frames[0].flags != (TL_FRAME_LOOPING | TL_FRAME_CLIP_START)) return 8;
            if (multi.frames[4].timeline_tick != 4u || (multi.frames[4].flags & (TL_FRAME_CLIP_START | TL_FRAME_CLIP_END)) != (TL_FRAME_CLIP_START | TL_FRAME_CLIP_END) || multi.frames[4].payload_count != 2u) return 9;

            multi = (receipt){ 0 };
            scalar = (receipt){ 0 };
            if (!battle_try_seek(42u, &multi_playback, -5, &multi, &next)) return 10;
            multi_playback = next;
            for (int32_t step = 0; step < 5; step++)
            {
                if (!battle_try_seek(42u, &scalar_playback, -1, &scalar, &next)) return 11;
                scalar_playback = next;
            }
            if (!playback_is(&multi_playback, &started) || !playback_is(&multi_playback, &scalar_playback) || !receipt_is(&multi, &scalar) || multi.count != 5u) return 12;

            receipt mirror = { 0 };
            tl_playback mirror_playback;
            if (!battle_try_start(42u, 200000u, &mirror_playback)) return 13;
            tl_playback mirror_start = mirror_playback;
            if (!battle_try_seek(42u, &mirror_playback, 8, &mirror, &next)) return 14;
            mirror_playback = next;
            if (mirror.count != 8u || mirror_playback.position != INT64_C(8) || mirror_playback.game_tick != 200008u) return 15;
            if (!battle_try_seek(42u, &mirror_playback, -8, &mirror, &next)) return 16;
            mirror_playback = next;
            if (mirror.count != 16u || mirror.balance != INT64_C(0) || !playback_is(&mirror_playback, &mirror_start)) return 17;
            for (uint32_t index = 0u; index < 8u; index++)
                if (!identity_is(&mirror.frames[index], &mirror.frames[15u - index])) return 18;
            if ((mirror.frames[7].flags & (TL_FRAME_LOOPING | TL_FRAME_TIMELINE_END | TL_FRAME_CLIP_END)) != (TL_FRAME_LOOPING | TL_FRAME_TIMELINE_END | TL_FRAME_CLIP_END)) return 19;

            receipt negative = { 0 };
            tl_playback negative_playback;
            if (!battle_try_start(42u, 0u, &negative_playback)) return 20;
            tl_playback negative_start = negative_playback;
            if (!battle_try_seek(42u, &negative_playback, -1, &negative, &next)) return 21;
            negative_playback = next;
            if (negative.count != 1u || negative_playback.position != INT64_C(-1) || negative_playback.game_tick != UINT32_MAX || negative.frames[0].cycle != INT64_C(-1) || negative.frames[0].timeline_tick != 7u || negative.frames[0].game_tick != UINT32_MAX || (negative.frames[0].flags & (TL_FRAME_LOOPING | TL_FRAME_REVERSE | TL_FRAME_TIMELINE_END | TL_FRAME_CLIP_END)) != (TL_FRAME_LOOPING | TL_FRAME_REVERSE | TL_FRAME_TIMELINE_END | TL_FRAME_CLIP_END)) return 22;
            if (!battle_try_seek(42u, &negative_playback, 1, &negative, &next)) return 23;
            negative_playback = next;
            if (!playback_is(&negative_playback, &negative_start) || negative.balance != INT64_C(0)) return 24;

            receipt wrapping = { 0 };
            tl_playback wrapping_playback;
            if (!battle_try_start(42u, UINT32_MAX, &wrapping_playback)) return 25;
            if (!battle_try_seek(42u, &wrapping_playback, 2, &wrapping, &next)) return 26;
            wrapping_playback = next;
            if (wrapping_playback.game_tick != 1u || wrapping.count != 1u || wrapping.frames[0].game_tick != 0u || wrapping.frames[0].timeline_tick != 1u) return 27;

            receipt failed = { 0 };
            tl_playback forged = { INT64_MIN + INT64_C(7), 99u, 42u, TL_PLAYBACK_STARTED, 0u };
            next = (tl_playback){ INT64_C(77), 77u, 77u, 77u, 77u };
            if (battle_try_seek(42u, &forged, INT32_MIN, &failed, &next) || !playback_is(&next, &forged) || failed.count != 0u) return 28;
            forged = (tl_playback){ INT64_MAX, 99u, 42u, TL_PLAYBACK_STARTED, 0u };
            if (battle_try_seek(42u, &forged, 1, &failed, &next) || !playback_is(&next, &forged) || failed.count != 0u) return 29;
            if (!battle_try_start(42u, 3u, &forged)) return 30;
            if (battle_try_seek(42u, &forged, 2, NULL, &next) || !playback_is(&next, &forged) || failed.count != 0u) return 31;
            forged.owner = 41u;
            if (battle_try_seek(42u, &forged, 1, &failed, &next) || !playback_is(&next, &forged)) return 32;
            if (!battle_try_start(42u, 3u, &forged)) return 33;
            tl_playback stopped;
            if (!battle_try_stop(42u, &forged, &stopped) || !battle_try_stop(42u, &stopped, &next) || !playback_is(&stopped, &next)) return 34;
            if (battle_try_seek(42u, &stopped, 1, &failed, &next) || !playback_is(&next, &stopped)) return 35;
            tl_playback invalid_start = { INT64_C(9), 9u, 9u, 9u, 9u };
            if (battle_try_start(41u, 10u, &invalid_start) || invalid_start.position != INT64_C(0) || invalid_start.game_tick != 0u || invalid_start.owner != 0u || invalid_start.flags != 0u || invalid_start.reserved != 0u) return 36;

            receipt finite = { 0 };
            tl_playback finite_playback;
            if (!finite_try_start(43u, 7u, &finite_playback)) return 37;
            if (!finite_try_seek(43u, &finite_playback, 8, &finite, &next)) return 38;
            finite_playback = next;
            if (finite_playback.position != INT64_C(8) || finite_playback.game_tick != 15u || (finite.frames[finite.count - 1u].flags & TL_FRAME_COMPLETED_AFTER) == 0u) return 39;
            uint32_t finite_count = finite.count;
            if (finite_try_seek(43u, &finite_playback, 1, &finite, &next) || !playback_is(&next, &finite_playback) || finite.count != finite_count) return 40;
            if (!finite_try_seek(43u, &finite_playback, -1, &finite, &next)) return 41;
            finite_playback = next;
            if ((finite.frames[finite.count - 1u].flags & (TL_FRAME_REVERSE | TL_FRAME_COMPLETED_BEFORE)) != (TL_FRAME_REVERSE | TL_FRAME_COMPLETED_BEFORE)) return 42;
            tl_playback invalid_origin = { INT64_C(-1), 0u, 43u, TL_PLAYBACK_STARTED, 0u };
            if (finite_try_seek(43u, &invalid_origin, 0, NULL, &next) || !playback_is(&next, &invalid_origin)) return 43;

            tl_playback empty_playback;
            if (!empty_try_start(45u, 123u, &empty_playback)) return 44;
            if (!empty_try_seek(45u, &empty_playback, 0, NULL, &next) || !playback_is(&next, &empty_playback)) return 45;
            if (empty_try_seek(45u, &empty_playback, 1, NULL, &next) || !playback_is(&next, &empty_playback)) return 46;
            if (empty_try_seek(45u, &empty_playback, -1, NULL, &next) || !playback_is(&next, &empty_playback)) return 47;

            receipt one = { 0 };
            tl_playback one_playback;
            if (!one_try_start(46u, 50u, &one_playback)) return 48;
            tl_playback one_start = one_playback;
            if (!one_try_seek(46u, &one_playback, 1, &one, &next)) return 49;
            one_playback = next;
            tl_frame_flags forward_flags = (tl_frame_flags)(TL_FRAME_CLIP_START | TL_FRAME_CLIP_END | TL_FRAME_TIMELINE_START | TL_FRAME_TIMELINE_END | TL_FRAME_COMPLETED_AFTER);
            if (one.count != 1u || one.frames[0].flags != forward_flags || one.frames[0].game_tick != 50u) return 50;
            if (!one_try_seek(46u, &one_playback, -1, &one, &next)) return 51;
            one_playback = next;
            tl_frame_flags reverse_flags = (tl_frame_flags)(TL_FRAME_CLIP_START | TL_FRAME_CLIP_END | TL_FRAME_TIMELINE_START | TL_FRAME_TIMELINE_END | TL_FRAME_COMPLETED_BEFORE | TL_FRAME_REVERSE);
            if (one.count != 2u || one.frames[1].flags != reverse_flags || one.frames[1].game_tick != 50u || !playback_is(&one_playback, &one_start) || one.balance != INT64_C(0)) return 52;

            receipt aliased = { 0 };
            if ((void *)&aliased != (void *)&aliased.playback) return 53;
            if (!battle_try_start(42u, 77u, &aliased.playback)) return 54;
            if (!battle_try_seek(42u, &aliased.playback, 2, &aliased.playback, &aliased.playback)) return 55;
            if (aliased.playback.position != INT64_C(2) || aliased.playback.game_tick != 79u || aliased.count != 1u) return 56;
            tl_playback before_stop = aliased.playback;
            if (!battle_try_stop(42u, &aliased.playback, &aliased.playback)) return 57;
            if (aliased.playback.position != before_stop.position || aliased.playback.game_tick != before_stop.game_tick || aliased.playback.owner != before_stop.owner || aliased.playback.flags != (TL_PLAYBACK_STARTED | TL_PLAYBACK_STOPPED)) return 58;
            if (!battle_try_stop(42u, &aliased.playback, &aliased.playback) || aliased.playback.flags != (TL_PLAYBACK_STARTED | TL_PLAYBACK_STOPPED)) return 59;

            printf("c11 signed seek parity: %u mirrored frames; lifecycle exact\n", 16u);
            return 0;
        }
        """;

    private const string WideConsumer = """
        #include "wide.h"
        #include <stdint.h>
        #include <stdio.h>

        typedef struct { uint16_t tracks[24]; tl_frame_flags flags[24]; uint32_t count; int64_t balance; } receipt;

        void wide_operation_seek(void *context, const tl_frame *frame)
        {
            receipt *value = (receipt *)context;
            int64_t amount = (int64_t)frame->track_index + INT64_C(1);
            value->tracks[value->count] = frame->track_index;
            value->flags[value->count] = frame->flags;
            value->count++;
            value->balance += (frame->flags & TL_FRAME_REVERSE) != 0u ? -amount : amount;
        }

        static int group_is(const receipt *value, uint32_t offset, tl_frame_flags flags, int reverse)
        {
            static const uint16_t forward[] = { 255u, 256u, UINT16_MAX };
            static const uint16_t backward[] = { UINT16_MAX, 256u, 255u };
            const uint16_t *expected = reverse != 0 ? backward : forward;
            for (uint32_t index = 0u; index < 3u; index++)
            {
                uint32_t at = offset + index;
                if (value->tracks[at] != expected[index] || value->flags[at] != flags) return 0;
            }
            return 1;
        }

        int main(void)
        {
            receipt value = { 0 };
            tl_playback playback;
            tl_playback next;
            if (!wide_try_start(44u, 0u, &playback)) return 1;
            tl_playback started = playback;
            if (!wide_try_seek(44u, &playback, 4, &value, &next)) return 2;
            playback = next;
            if (!wide_try_seek(44u, &playback, -4, &value, &next)) return 3;
            playback = next;
            if (value.count != 18u || value.balance != INT64_C(0) || playback.position != started.position || playback.game_tick != started.game_tick || playback.owner != started.owner || playback.flags != started.flags) return 4;
            if (!group_is(&value, 0u, TL_FRAME_CLIP_START, 0)) return 5;
            if (!group_is(&value, 3u, 0u, 0)) return 6;
            if (!group_is(&value, 6u, (tl_frame_flags)(TL_FRAME_CLIP_END | TL_FRAME_TIMELINE_END | TL_FRAME_COMPLETED_AFTER), 0)) return 7;
            if (!group_is(&value, 9u, (tl_frame_flags)(TL_FRAME_CLIP_END | TL_FRAME_TIMELINE_END | TL_FRAME_COMPLETED_BEFORE | TL_FRAME_REVERSE), 1)) return 8;
            if (!group_is(&value, 12u, TL_FRAME_REVERSE, 1)) return 9;
            if (!group_is(&value, 15u, (tl_frame_flags)(TL_FRAME_CLIP_START | TL_FRAME_REVERSE), 1)) return 10;
            printf("c11 wide reverse order: %u frames; balance %lld\n", (unsigned)value.count, (long long)value.balance);
            return 0;
        }
        """;
}
