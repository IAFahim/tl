using System.Diagnostics;
using Tl.Compiler;
using Tl.Gen.C;
using Xunit;

namespace Tl.Gen.Tests;

public sealed class CBackendTests
{
    private static readonly OperationId Combat = new("combat");
    private static readonly OperationId Audit = new("audit");

    [Fact]
    public void OutputIsDeterministicAndStructurallyPortable()
    {
        var plan = CreatePlan("battle", 42, true);
        var binding = CreateBinding("battle");

        var first = CEmitter.Emit(plan, binding);
        var second = CEmitter.Emit(plan, binding);
        var emission = CEmitter.Generate(plan, binding);

        Assert.True(first.SequenceEqual(second));
        Assert.Equal(["battle.h", "battle.c"], first.Select(static artifact => artifact.RelativePath));
        Assert.Contains("_Static_assert(sizeof(tl_playback) == 12", first[0].Content);
        Assert.Contains("effective >= 4u && effective < 5u", first[1].Content);
        Assert.Contains("combat_forward(context, &frame);", first[1].Content);
        Assert.DoesNotContain("global::", string.Concat(first.Select(static artifact => artifact.Content)));
        Assert.Equal(1, emission.Report.PlanFormatVersion);
        Assert.Equal(1, emission.Report.AbiVersion);
        Assert.Equal(2, emission.Report.TrackCount);
        Assert.Equal(3, emission.Report.ClipCount);
        Assert.Equal(6, emission.Report.RegionCount);
        Assert.Equal(8u, emission.Report.Duration);
        Assert.True(emission.Report.Loops);
        Assert.Equal(emission.Artifacts.Sum(static artifact => artifact.Utf8Bytes), emission.Report.SourceUtf8Bytes);
        Assert.Equal(12, emission.Report.PlaybackBytes);
        Assert.Equal(24, emission.Report.FrameBytes);
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
            [
                new ClipPlan(0, 1, 0, 8),
                new ClipPlan(0, 2, 1, 7),
                new ClipPlan(0, 3, 2, 6)
            ]);

        Assert.Throws<ArgumentException>(() => CEmitter.Emit(tripleOverlap, CreateBinding("invalid")));
        Assert.Throws<ArgumentException>(() => CEmitter.Emit(CreatePlan("battle", 42, true), new CBinding("while", "battle.h", [])));
        Assert.Throws<ArgumentException>(() => CEmitter.Emit(CreatePlan("battle", 42, true), new CBinding("_battle", "battle.h", [])));
    }

    [Theory]
    [InlineData("reserved_try_forward")]
    [InlineData("reserved_apply_backward")]
    [InlineData("TL_reserved_H")]
    [InlineData("tl_frame")]
    [InlineData("TL_CLIP_ENTER")]
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
    [InlineData("int_least32_t")]
    [InlineData("uint_fast64_t")]
    [InlineData("intptr_t")]
    [InlineData("uintptr_t")]
    [InlineData("intmax_t")]
    [InlineData("uintmax_t")]
    [InlineData("UINT16_MAX")]
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
            new CBinding(
                "reserved",
                "reserved.h",
                [new COperationBinding(Combat, symbol, "operation_backward")])));
        Assert.Throws<ArgumentException>(() => CEmitter.Emit(
            plan,
            new CBinding(
                "reserved",
                "reserved.h",
                [new COperationBinding(Combat, "operation_forward", symbol)])));
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
            [new COperationBinding(Combat, "capacity_operation_forward", "capacity_operation_backward")]);

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
    public void GeneratedC11MatchesPlaybackAndRegionSemantics()
    {
        if (!CanRun("cc", "--version"))
            return;

        var root = Path.Combine(Path.GetTempPath(), "tl-c-backend-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Write(root, CEmitter.Emit(CreatePlan("battle", 42, true), CreateBinding("battle")));
            Write(root, CEmitter.Emit(CreatePlan("finite", 43, false), CreateBinding("finite")));
            Write(root, CEmitter.Emit(new TimelinePlan("empty", 45, false, [], []), new CBinding("empty", "empty.h", [])));
            File.WriteAllText(Path.Combine(root, "consumer.c"), Consumer);

            var executable = Path.Combine(root, "receipt");
            var compilation = Run(root, "cc", "-std=c11", "-Wall", "-Wextra", "-Werror", "battle.c", "finite.c", "empty.c", "consumer.c", "-o", executable);
            Assert.True(compilation.ExitCode == 0, compilation.Output);

            var receipt = Run(root, executable);
            Assert.True(receipt.ExitCode == 0, receipt.Output);
            Assert.Equal("c11 parity: 9 frames; cycles 0; lifecycle exact\n", receipt.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void GeneratedC11PreservesFullWidthTrackIndicesAndDirectionMirror()
    {
        if (!CanRun("cc", "--version"))
            return;

        var plan = new TimelinePlan(
            "wide",
            44,
            false,
            [
                new TrackPlan(255, 2_550, Combat),
                new TrackPlan(256, 2_560, Combat),
                new TrackPlan(ushort.MaxValue, 65_535, Combat)
            ],
            [
                new ClipPlan(255, 2_551, 1, 4),
                new ClipPlan(256, 2_561, 1, 4),
                new ClipPlan(ushort.MaxValue, 65_534, 1, 4)
            ]);
        var binding = new CBinding(
            "wide",
            "wide.h",
            [new COperationBinding(Combat, "wide_operation_forward", "wide_operation_backward")]);
        var root = Path.Combine(Path.GetTempPath(), "tl-c-wide-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Write(root, CEmitter.Emit(plan, binding));
            File.WriteAllText(Path.Combine(root, "consumer.c"), WideConsumer);

            var executable = Path.Combine(root, "receipt");
            var compilation = Run(root, "cc", "-std=c11", "-Wall", "-Wextra", "-Werror", "wide.c", "consumer.c", "-o", executable);
            Assert.True(compilation.ExitCode == 0, compilation.Output);

            var receipt = Run(root, executable);
            Assert.True(receipt.ExitCode == 0, receipt.Output);
            Assert.Equal("c11 wide mirror: 15 frames; balance 0\n", receipt.Output.Replace("\r\n", "\n", StringComparison.Ordinal));
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
            [
                new TrackPlan(0, 9, Combat),
                new TrackPlan(1, 11, Audit)
            ],
            [
                new ClipPlan(0, 10, 2, 5),
                new ClipPlan(0, 20, 4, 8),
                new ClipPlan(1, 7, 1, 3)
            ]);

    private static CBinding CreateBinding(string prefix)
        => new(
            prefix,
            prefix + ".h",
            [
                new COperationBinding(Combat, "combat_forward", "combat_backward"),
                new COperationBinding(Audit, "audit_forward", "audit_backward")
            ]);

    private static void Write(string root, IEnumerable<CArtifact> artifacts)
    {
        foreach (var artifact in artifacts)
            File.WriteAllText(Path.Combine(root, artifact.RelativePath), artifact.Content);
    }

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
        #include <math.h>
        #include <stdio.h>

        typedef struct { tl_frame frames[16]; uint8_t backward[16]; uint32_t count; } receipt;

        static void record_frame(void *context, const tl_frame *frame, uint8_t backward)
        {
            receipt *value = (receipt *)context;
            value->frames[value->count] = *frame;
            value->backward[value->count] = backward;
            value->count++;
        }

        void combat_forward(void *context, const tl_frame *frame) { record_frame(context, frame, 0u); }
        void combat_backward(void *context, const tl_frame *frame) { record_frame(context, frame, 1u); }
        void audit_forward(void *context, const tl_frame *frame) { record_frame(context, frame, 0u); }
        void audit_backward(void *context, const tl_frame *frame) { record_frame(context, frame, 1u); }

        static int frame_is(const receipt *value, uint32_t index, uint32_t tick, uint16_t track, uint8_t state, uint8_t count, uint32_t first, uint32_t second, float factor, uint8_t backward)
        {
            const tl_frame *frame = &value->frames[index];
            return frame->tick == tick && frame->track_index == track && frame->state == state && frame->payload_count == count && frame->first_payload == first && frame->second_payload == second && fabsf(frame->factor - factor) < 0.0001f && value->backward[index] == backward;
        }

        static int playback_is(const tl_playback *first, const tl_playback *second)
        {
            return first->tick == second->tick && first->cycles == second->cycles && first->owner == second->owner && first->flags == second->flags && first->reserved == second->reserved;
        }

        int main(void)
        {
            receipt value = { 0 };
            tl_playback playback;
            tl_playback next;
            if (battle_id() != 42u || battle_duration() != 8u || !battle_is_looping()) return 1;
            if (!battle_try_start(42u, 0u, &playback)) return 2;
            if (!battle_try_forward(42u, &playback, 0u, NULL, &next) || value.count != 0u) return 32;
            next = (tl_playback){ 99u, 99u, 99u, 99u, 99u };
            if (battle_try_forward(42u, &playback, 1u, NULL, &next) || !playback_is(&next, &playback) || value.count != 0u) return 33;
            if (!battle_try_forward(42u, &playback, 1u, &value, &next)) return 3;
            playback = next;
            if (!battle_try_forward(42u, &playback, 2u, &value, &next)) return 4;
            playback = next;
            if (!battle_try_forward(42u, &playback, 4u, &value, &next)) return 5;
            playback = next;
            if (!battle_try_forward(42u, &playback, 7u, &value, &next)) return 6;
            playback = next;
            if ((playback.flags & TL_PLAYBACK_LAST_LOOP_FRAME) == 0u) return 7;
            if (!battle_try_forward(42u, &playback, 8u, &value, &next)) return 8;
            playback = next;
            if (playback.cycles != 1u || playback.flags != TL_PLAYBACK_STARTED) return 9;
            if (!battle_try_backward(42u, &playback, 7u, &value, &next)) return 10;
            playback = next;
            if (playback.cycles != 0u || (playback.flags & TL_PLAYBACK_LAST_LOOP_FRAME) == 0u) return 11;
            if (!battle_try_backward(42u, &playback, 4u, &value, &next)) return 12;
            playback = next;
            if (!battle_try_backward(42u, &playback, 2u, &value, &next)) return 13;
            playback = next;
            if (value.count != 9u) return 14;
            if (!frame_is(&value, 0u, 1u, 1u, TL_CLIP_ENTER, 1u, 7u, 0u, 0.0f, 0u)) return 15;
            if (!frame_is(&value, 1u, 2u, 0u, TL_CLIP_ENTER, 1u, 10u, 0u, 0.0f, 0u)) return 16;
            if (!frame_is(&value, 2u, 2u, 1u, TL_CLIP_EXIT, 1u, 7u, 0u, 0.0f, 0u)) return 17;
            if (!frame_is(&value, 3u, 4u, 0u, TL_CLIP_STAY, 2u, 10u, 20u, 0.5f, 0u)) return 18;
            if (!frame_is(&value, 4u, 7u, 0u, TL_CLIP_EXIT, 1u, 20u, 0u, 0.0f, 0u)) return 19;
            if (!frame_is(&value, 5u, 7u, 0u, TL_CLIP_ENTER, 1u, 20u, 0u, 0.0f, 1u)) return 20;
            if (!frame_is(&value, 6u, 4u, 0u, TL_CLIP_STAY, 2u, 10u, 20u, 0.5f, 1u)) return 21;
            if (!frame_is(&value, 7u, 2u, 0u, TL_CLIP_EXIT, 1u, 10u, 0u, 0.0f, 1u)) return 22;
            if (!frame_is(&value, 8u, 2u, 1u, TL_CLIP_ENTER, 1u, 7u, 0u, 0.0f, 1u)) return 23;
            if (value.frames[1].track_payload != 9u || value.frames[2].track_payload != 11u) return 31;
            tl_playback stopped;
            if (!battle_try_stop(42u, &playback, &stopped)) return 24;
            next = (tl_playback){ 99u, 99u, 99u, 99u, 99u };
            if (battle_try_forward(42u, &stopped, 3u, &value, &next) || next.tick != stopped.tick || next.flags != stopped.flags) return 25;
            if (!battle_try_start(42u, 0u, &playback)) return 34;
            playback.cycles = UINT16_MAX;
            next = (tl_playback){ 99u, 99u, 99u, 99u, 99u };
            if (battle_try_forward(42u, &playback, 9u, &value, &next) || !playback_is(&next, &playback) || value.count != 9u) return 35;
            if (!finite_try_start(43u, 0u, &playback)) return 26;
            if (!finite_try_forward(43u, &playback, 8u, &value, &next)) return 27;
            playback = next;
            if ((playback.flags & TL_PLAYBACK_COMPLETED) == 0u || playback.cycles != 0u) return 28;
            if (!finite_try_backward(43u, &playback, 0u, &value, &next)) return 29;
            if ((next.flags & TL_PLAYBACK_COMPLETED) == 0u) return 30;
            if (!empty_try_start(45u, 123u, &playback)) return 36;
            if (!empty_try_forward(45u, &playback, 456u, NULL, &next) || next.tick != 456u || (next.flags & TL_PLAYBACK_COMPLETED) == 0u) return 37;
            playback = next;
            if (!empty_try_backward(45u, &playback, 0u, NULL, &next) || next.tick != 0u || (next.flags & TL_PLAYBACK_COMPLETED) == 0u) return 38;
            printf("c11 parity: %u frames; cycles %u; lifecycle exact\n", 9u, (unsigned)next.cycles);
            return 0;
        }
        """;

    private const string WideConsumer = """
        #include "wide.h"
        #include <stdint.h>
        #include <stdio.h>

        typedef struct
        {
            uint16_t tracks[16];
            uint8_t states[16];
            uint8_t backward[16];
            uint32_t count;
            int64_t balance;
        } receipt;

        static void record_frame(void *context, const tl_frame *frame, uint8_t backward)
        {
            receipt *value = (receipt *)context;
            value->tracks[value->count] = frame->track_index;
            value->states[value->count] = frame->state;
            value->backward[value->count] = backward;
            value->count++;
            if (frame->state == TL_CLIP_STAY)
            {
                int64_t amount = (int64_t)frame->track_index + 1;
                value->balance += backward != 0u ? -amount : amount;
            }
        }

        void wide_operation_forward(void *context, const tl_frame *frame) { record_frame(context, frame, 0u); }
        void wide_operation_backward(void *context, const tl_frame *frame) { record_frame(context, frame, 1u); }

        static int group_is(const receipt *value, uint32_t offset, uint8_t state, uint8_t backward)
        {
            static const uint16_t expected[] = { 255u, 256u, UINT16_MAX };
            for (uint32_t index = 0; index < 3u; index++)
            {
                uint32_t at = offset + index;
                if (value->tracks[at] != expected[index] || value->states[at] != state || value->backward[at] != backward)
                    return 0;
            }
            return 1;
        }

        int main(void)
        {
            receipt value = { 0 };
            tl_playback playback;
            tl_playback next;
            if (!wide_try_start(44u, 0u, &playback)) return 1;
            if (!wide_try_forward(44u, &playback, 1u, &value, &next)) return 2;
            playback = next;
            if (!wide_try_forward(44u, &playback, 2u, &value, &next)) return 3;
            playback = next;
            if (!wide_try_forward(44u, &playback, 3u, &value, &next)) return 4;
            playback = next;
            if ((playback.flags & TL_PLAYBACK_COMPLETED) == 0u) return 5;
            if (!wide_try_backward(44u, &playback, 2u, &value, &next)) return 6;
            playback = next;
            if (!wide_try_backward(44u, &playback, 1u, &value, &next)) return 7;
            playback = next;
            if (!wide_try_backward(44u, &playback, 0u, &value, &next)) return 8;
            playback = next;
            if ((playback.flags & TL_PLAYBACK_COMPLETED) == 0u) return 9;
            if (value.count != 15u || value.balance != 0) return 10;
            if (!group_is(&value, 0u, TL_CLIP_ENTER, 0u)) return 11;
            if (!group_is(&value, 3u, TL_CLIP_STAY, 0u)) return 12;
            if (!group_is(&value, 6u, TL_CLIP_EXIT, 0u)) return 13;
            if (!group_is(&value, 9u, TL_CLIP_STAY, 1u)) return 14;
            if (!group_is(&value, 12u, TL_CLIP_EXIT, 1u)) return 15;
            printf("c11 wide mirror: %u frames; balance %lld\n", (unsigned)value.count, (long long)value.balance);
            return 0;
        }
        """;
}
