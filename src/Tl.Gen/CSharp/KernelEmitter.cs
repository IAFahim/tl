using System.Globalization;
using System.Text;
using Tl.Gen.Analysis;
using Tl.Gen.Model;

namespace Tl.Gen.CSharp;

public static class KernelEmitter
{
    public const string SharedFileName = "CompiledRuntime.g.cs";

    public static string EmitSharedRuntime() => """
        #nullable enable
        using System;
        using System.Runtime.CompilerServices;
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

        public interface ICompiledForward<TTrack, TClip, TInput, TResult>
            where TTrack : struct, IBlend<TClip>
            where TClip : struct
            where TInput : struct
            where TResult : struct, ICompiledForward<TTrack, TClip, TInput, TResult>, ICompiledBackward<TTrack, TClip, TInput, TResult>
        {
            void Forward(in CompiledTracks<TTrack, TClip> tracks, in TInput input, in uint tick, ref TResult result);
        }

        public interface ICompiledBackward<TTrack, TClip, TInput, TResult>
            where TTrack : struct, IBlend<TClip>
            where TClip : struct
            where TInput : struct
            where TResult : struct, ICompiledForward<TTrack, TClip, TInput, TResult>, ICompiledBackward<TTrack, TClip, TInput, TResult>
        {
            void Backward(in CompiledTracks<TTrack, TClip> tracks, in TInput input, in uint tick, ref TResult result);
        }

        internal readonly struct CompiledWorkSlot(
            ushort index,
            ushort first,
            ushort second,
            uint enterF,
            uint enterB,
            uint factorStart,
            uint factorLength,
            ushort blendOrdinal)
        {
            public const ushort Single = ushort.MaxValue;

            public readonly ushort Index = index;
            public readonly ushort First = first;
            public readonly ushort Second = second;
            public readonly uint EnterF = enterF;
            public readonly uint EnterB = enterB;
            public readonly uint FactorStart = factorStart;
            public readonly uint FactorLength = factorLength;
            public readonly ushort BlendOrdinal = blendOrdinal;
        }

        internal readonly record struct CompiledMovement(uint PrevEff, bool Backward, bool Wrapped, bool Full);

        internal static class CompiledPlayback
        {
            public static Playback Mint(uint tick, ushort cycles, PlaybackFlags flags)
                => System.Runtime.CompilerServices.Unsafe.BitCast<ulong, Playback>(
                    tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);
        }

        public readonly ref struct CompiledTracks<TTrack, TClip>
            where TTrack : struct, IBlend<TClip>
            where TClip : struct
        {
            private readonly ReadOnlySpan<CompiledWorkSlot> _slots;
            private readonly uint _tick;
            private readonly Span<TClip> _blendScratch;
            private readonly ReadOnlySpan<TTrack> _trackData;
            private readonly ReadOnlySpan<TClip> _clipData;
            private readonly CompiledMovement _movement;

            internal CompiledTracks(
                ReadOnlySpan<CompiledWorkSlot> slots,
                uint tick,
                Span<TClip> blendScratch,
                ReadOnlySpan<TTrack> trackData,
                ReadOnlySpan<TClip> clipData,
                CompiledMovement movement)
            {
                _slots = slots;
                _tick = tick;
                _blendScratch = blendScratch;
                _trackData = trackData;
                _clipData = clipData;
                _movement = movement;
            }

            public int Count => _slots.Length;

            public CompiledTrackWork<TTrack, TClip> this[int index]
                => new(in _slots[index], _tick, _blendScratch, _trackData, _clipData, _movement);

            public CompiledTracks<TTrack, TClip> Slice(int start, int length)
                => new(_slots.Slice(start, length), _tick, _blendScratch, _trackData, _clipData, _movement);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator()
                => new(_slots, _tick, _blendScratch, _trackData, _clipData, _movement);

            public ref struct Enumerator
            {
                private ReadOnlySpan<CompiledWorkSlot> _slots;
                private readonly uint _tick;
                private readonly Span<TClip> _blendScratch;
                private readonly ReadOnlySpan<TTrack> _trackData;
                private readonly ReadOnlySpan<TClip> _clipData;
                private readonly CompiledMovement _movement;
                private int _i;

                internal Enumerator(
                    ReadOnlySpan<CompiledWorkSlot> slots,
                    uint tick,
                    Span<TClip> blendScratch,
                    ReadOnlySpan<TTrack> trackData,
                    ReadOnlySpan<TClip> clipData,
                    CompiledMovement movement)
                {
                    _slots = slots;
                    _tick = tick;
                    _blendScratch = blendScratch;
                    _trackData = trackData;
                    _clipData = clipData;
                    _movement = movement;
                    _i = -1;
                }

                public CompiledTrackWork<TTrack, TClip> Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => new(in _slots[_i], _tick, _blendScratch, _trackData, _clipData, _movement);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    var i = _i + 1;
                    if ((uint)i >= (uint)_slots.Length)
                        return false;
                    _i = i;
                    return true;
                }
            }
        }

        public readonly ref struct CompiledTrackWork<TTrack, TClip>
            where TTrack : struct, IBlend<TClip>
            where TClip : struct
        {
            private readonly ref readonly CompiledWorkSlot _slot;
            private readonly uint _tick;
            private readonly Span<TClip> _blendScratch;
            private readonly ReadOnlySpan<TTrack> _trackData;
            private readonly ReadOnlySpan<TClip> _clipData;
            private readonly CompiledMovement _movement;

            internal CompiledTrackWork(
                in CompiledWorkSlot slot,
                uint tick,
                Span<TClip> blendScratch,
                ReadOnlySpan<TTrack> trackData,
                ReadOnlySpan<TClip> clipData,
                CompiledMovement movement)
            {
                _slot = ref slot;
                _tick = tick;
                _blendScratch = blendScratch;
                _trackData = trackData;
                _clipData = clipData;
                _movement = movement;
            }

            public ushort Index => _slot.Index;

            public ref readonly TTrack Track => ref _trackData[_slot.Index];

            public ref readonly TClip Clip
            {
                get
                {
                    if (_slot.FactorLength == 0)
                        return ref _clipData[_slot.First];

                    var ordinal = _slot.BlendOrdinal;
                    var factor = _slot.FactorLength <= 1
                        ? 0.5f
                        : (_tick - _slot.FactorStart) / (float)(_slot.FactorLength - 1);
                    _trackData[_slot.Index].Blend(
                        in _clipData[_slot.First],
                        in _clipData[_slot.Second],
                        factor,
                        out _blendScratch[ordinal]);
                    return ref _blendScratch[ordinal];
                }
            }

            public ClipState State
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var backward = _movement.Backward;
                    if (backward ? _tick == _slot.EnterF : _tick == _slot.EnterB - 1u)
                        return ClipState.Exit;

                    var crossed = _movement.Wrapped || _movement.Full
                        || (backward ? _movement.PrevEff >= _slot.EnterB : _movement.PrevEff < _slot.EnterF);
                    return crossed ? ClipState.Enter : ClipState.Stay;
                }
            }
        }
        """ + "\n";

    public static string EmitKernel(TimelinePlan plan, EmittedWorkSlot[][] slots, string kernelName, string sourceFile, int sourceLine)
    {
        var d = plan.Definition;
        var duration = plan.Duration;
        var looping = d.Loops && duration != 0;
        var trackType = d.TrackTypeName;
        var clipType = d.ClipTypeName;

        var writer = new StringBuilder();
        writer.AppendLine($"#nullable enable");
        writer.AppendLine($"using System;");
        writer.AppendLine($"using System.Runtime.CompilerServices;");
        writer.AppendLine($"using Tl;");
        writer.AppendLine($"using Tl.Compiled;");
        writer.AppendLine();
        if (d.Namespace.Length > 0)
            writer.AppendLine($"namespace {d.Namespace};");
        writer.AppendLine();
        writer.AppendLine($"public static class {kernelName}");
        writer.AppendLine("{");

        writer.AppendLine($"    public const uint Duration = {U(duration)};");
        if (d.Loops)
            writer.AppendLine($"    public const bool Loops = true;");
        writer.AppendLine();
        writer.AppendLine($"    private static readonly {trackType}[] s_trackData =");
        writer.AppendLine($"        [{string.Join(", ", d.Tracks.Select(t => t.TrackExpression))}];");
        writer.AppendLine($"    private static readonly {clipType}[] s_clipData =");
        writer.AppendLine($"        [{string.Join(", ", d.Clips.Select(c => c.PayloadExpression))}];");
        writer.AppendLine();
        writer.AppendLine($"    private readonly struct ForwardDirection {{ }}");
        writer.AppendLine($"    private readonly struct BackwardDirection {{ }}");
        writer.AppendLine();

        for (var r = 0; r < slots.Length; r++)
        {
            if (slots[r].Length == 0)
                continue;

            var rows = slots[r].Select(s => s.Second == EmittedWorkSlot.Single
                ? $"new({s.Index}, {s.First}, CompiledWorkSlot.Single, {U(s.EnterF)}, {U(s.EnterB)}, {U(s.FactorStart)}, {U(s.FactorLength)}, {s.BlendOrdinal})"
                : $"new({s.Index}, {s.First}, {s.Second}, {U(s.EnterF)}, {U(s.EnterB)}, {U(s.FactorStart)}, {U(s.FactorLength)}, {s.BlendOrdinal})");
            writer.AppendLine($"    private static readonly CompiledWorkSlot[] s_works{r.ToString(CultureInfo.InvariantCulture)} =");
            writer.AppendLine($"        [{string.Join(", ", rows)}];");
        }

        writer.AppendLine();
        writer.AppendLine($"    public static Playback Start(uint at = 0)");
        writer.AppendLine($"        => CompiledPlayback.Mint(at, 0, PlaybackFlags.Started);");
        writer.AppendLine();
        writer.AppendLine($"    public static Playback Stop(in Playback playback)");
        writer.AppendLine($"    {{");
        writer.AppendLine($"        if (!playback.Has(PlaybackFlags.Started))");
        writer.AppendLine($"            throw new InvalidOperationException(\"Cannot stop a playback that was never started.\");");
        writer.AppendLine($"        return CompiledPlayback.Mint(playback.Tick, playback.Cycles, playback.Flags | PlaybackFlags.Stopped);");
        writer.AppendLine($"    }}");
        writer.AppendLine();
        writer.AppendLine($"    public static Playback Forward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, uint tick)");
        writer.AppendLine(Constraints(trackType, clipType));
        writer.AppendLine($"    {{");
        writer.AppendLine($"        ReadOnlySpan<uint> ticks = [tick];");
        writer.AppendLine($"        return Advance<TInput, TResult, ForwardDirection>(in from, in input, ref result, ticks);");
        writer.AppendLine($"    }}");
        writer.AppendLine();
        writer.AppendLine($"    public static Playback Forward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)");
        writer.AppendLine(Constraints(trackType, clipType));
        writer.AppendLine($"        => Advance<TInput, TResult, ForwardDirection>(in from, in input, ref result, ticks);");
        writer.AppendLine();
        writer.AppendLine($"    public static Playback Backward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, uint tick)");
        writer.AppendLine(Constraints(trackType, clipType));
        writer.AppendLine($"    {{");
        writer.AppendLine($"        ReadOnlySpan<uint> ticks = [tick];");
        writer.AppendLine($"        return Advance<TInput, TResult, BackwardDirection>(in from, in input, ref result, ticks);");
        writer.AppendLine($"    }}");
        writer.AppendLine();
        writer.AppendLine($"    public static Playback Backward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, params ReadOnlySpan<uint> ticks)");
        writer.AppendLine(Constraints(trackType, clipType));
        writer.AppendLine($"        => Advance<TInput, TResult, BackwardDirection>(in from, in input, ref result, ticks);");
        writer.AppendLine();
        writer.AppendLine($"    private static Playback Advance<TInput, TResult, TDirection>(in Playback from, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)");
        writer.AppendLine(Constraints(trackType, clipType));
        writer.AppendLine($"    {{");
        writer.AppendLine($"        if (!from.Has(PlaybackFlags.Started))");
        writer.AppendLine($"            throw new InvalidOperationException(\"Playback was never started; mint one with {kernelName}.Start.\");");
        writer.AppendLine($"        if (from.Has(PlaybackFlags.Stopped))");
        writer.AppendLine($"            throw new InvalidOperationException(\"Playback is stopped.\");");
        writer.AppendLine();
        writer.AppendLine($"        var stateTick = from.Tick;");
        writer.AppendLine($"        var stateCycles = from.Cycles;");
        writer.AppendLine($"        var stateFlags = from.Flags;");
        writer.AppendLine($"        var backward = typeof(TDirection) == typeof(BackwardDirection);");
        writer.AppendLine();

        if (plan.MaxActiveBlends == 0)
            writer.AppendLine($"        Span<{clipType}> scratch = default;");
        else
            writer.AppendLine($"        Span<{clipType}> scratch = stackalloc {clipType}[{plan.MaxActiveBlends.ToString(CultureInfo.InvariantCulture)}];");

        writer.AppendLine();
        writer.AppendLine($"        foreach (var tick in ticks)");
        writer.AppendLine($"        {{");

        if (looping)
        {
            writer.AppendLine($"            var prevEff = stateTick % Duration;");
            writer.AppendLine($"            var tEff = tick % Duration;");
            writer.AppendLine($"            uint cycles;");
            writer.AppendLine($"            bool wrapped, full;");
            writer.AppendLine($"            if (!backward)");
            writer.AppendLine($"            {{");
            writer.AppendLine($"                cycles = tick >= stateTick ? tick / Duration - stateTick / Duration : tEff < prevEff ? 1u : 0u;");
            writer.AppendLine($"                wrapped = cycles == 1u;");
            writer.AppendLine($"                full = cycles >= 2u;");
            writer.AppendLine($"            }}");
            writer.AppendLine($"            else if (tick <= stateTick)");
            writer.AppendLine($"            {{");
            writer.AppendLine($"                cycles = stateTick / Duration - tick / Duration;");
            writer.AppendLine($"                wrapped = cycles == 1u;");
            writer.AppendLine($"                full = cycles >= 2u;");
            writer.AppendLine($"            }}");
            writer.AppendLine($"            else");
            writer.AppendLine($"            {{");
            writer.AppendLine($"                cycles = tEff > prevEff ? 1u : 0u;");
            writer.AppendLine($"                wrapped = tEff > prevEff;");
            writer.AppendLine($"                full = false;");
            writer.AppendLine($"            }}");
            writer.AppendLine();
            writer.AppendLine($"            if (!backward && cycles > ushort.MaxValue - stateCycles)");
            writer.AppendLine($"                throw new ArgumentOutOfRangeException(nameof(ticks), \"Playback cycle capacity exceeded.\");");
            writer.AppendLine($"            var newCycles = backward ? (ushort)(stateCycles - Math.Min(stateCycles, cycles)) : (ushort)(stateCycles + cycles);");
            writer.AppendLine();
            writer.AppendLine($"            var flags = PlaybackFlags.Started;");
            writer.AppendLine($"            if (tEff == Duration - 1u)");
            writer.AppendLine($"                flags |= PlaybackFlags.LastLoopFrame;");
        }
        else
        {
            writer.AppendLine($"            var prevEff = stateTick;");
            writer.AppendLine($"            var tEff = tick;");
            writer.AppendLine($"            const bool wrapped = false;");
            writer.AppendLine($"            const bool full = false;");
            writer.AppendLine($"            var newCycles = stateCycles;");
            writer.AppendLine();
            writer.AppendLine($"            var flags = PlaybackFlags.Started;");
            if (!d.Loops)
            {
                writer.AppendLine($"            if (Duration == 0u || (backward ? tEff == 0u : tEff >= Duration - 1u))");
                writer.AppendLine($"                flags |= PlaybackFlags.Completed;");
            }
        }

        writer.AppendLine();
        writer.AppendLine($"            var works = default(ReadOnlySpan<CompiledWorkSlot>);");
        writer.AppendLine($"            switch (Locate(tEff))");
        writer.AppendLine($"            {{");
        for (var r = 0; r < slots.Length; r++)
        {
            if (slots[r].Length == 0)
                continue;

            writer.AppendLine($"                case {r.ToString(CultureInfo.InvariantCulture)}:");
            writer.AppendLine($"                    works = s_works{r.ToString(CultureInfo.InvariantCulture)};");
            writer.AppendLine($"                    break;");
        }

        writer.AppendLine($"                default:");
        writer.AppendLine($"                    break;");
        writer.AppendLine($"            }}");
        writer.AppendLine();
        writer.AppendLine($"            if (works.Length != 0)");
        writer.AppendLine($"            {{");
        writer.AppendLine($"                var tracks = new CompiledTracks<{trackType}, {clipType}>(works, tEff, scratch, s_trackData, s_clipData, new CompiledMovement(prevEff, backward, wrapped, full));");
        writer.AppendLine($"                if (backward)");
        writer.AppendLine($"                    result.Backward(in tracks, in input, in tEff, ref result);");
        writer.AppendLine($"                else");
        writer.AppendLine($"                    result.Forward(in tracks, in input, in tEff, ref result);");
        writer.AppendLine($"            }}");
        writer.AppendLine();
        writer.AppendLine($"            stateTick = tick;");
        writer.AppendLine($"            stateCycles = newCycles;");
        writer.AppendLine($"            stateFlags = flags;");
        writer.AppendLine($"        }}");
        writer.AppendLine();
        writer.AppendLine($"        return CompiledPlayback.Mint(stateTick, stateCycles, stateFlags);");
        writer.AppendLine($"    }}");
        writer.AppendLine();
        writer.Append(LocateTree(plan.RegionStarts));
        writer.AppendLine("}");

        return writer.ToString();
    }

    private static string LocateTree(uint[] starts)
    {
        var writer = new StringBuilder();
        writer.AppendLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        writer.AppendLine($"    private static int Locate(uint tick)");
        writer.AppendLine($"    {{");
        EmitLocateNode(writer, starts, 0, starts.Length - 1, depth: 2);
        writer.AppendLine($"    }}");
        return writer.ToString();
    }

    private static void EmitLocateNode(StringBuilder writer, uint[] starts, int lo, int hi, int depth)
    {
        var indent = new string(' ', depth * 4);

        if (lo == hi)
        {
            writer.AppendLine($"{indent}return {lo.ToString(CultureInfo.InvariantCulture)};");
            return;
        }

        var mid = (lo + hi + 1) / 2;
        writer.AppendLine($"{indent}if (tick < {U(starts[mid])})");
        writer.AppendLine($"{indent}{{");
        EmitLocateNode(writer, starts, lo, mid - 1, depth + 1);
        writer.AppendLine($"{indent}}}");
        writer.AppendLine($"{indent}else");
        writer.AppendLine($"{indent}{{");
        EmitLocateNode(writer, starts, mid, hi, depth + 1);
        writer.AppendLine($"{indent}}}");
    }

    private static string Constraints(string trackType, string clipType)
        => "        where TInput : struct" + Environment.NewLine
         + $"        where TResult : struct, ICompiledForward<{trackType}, {clipType}, TInput, TResult>, ICompiledBackward<{trackType}, {clipType}, TInput, TResult>";

    private static string U(uint value) => value.ToString(CultureInfo.InvariantCulture) + "u";
}
