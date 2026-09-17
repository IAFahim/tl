using System.Runtime.CompilerServices;
using Tl;

namespace ParityCheck;

internal static unsafe class Pairs
{
    public const int Move = 0;
    public const int Pulse = 1;
    public const int Window = 2;
    public const int Damage = 3;
    public const int GaGlobal = 4;
    public const int Job = 5;
    public const int Alpha = 6;
    public const int Blend = 7;
    public const int DualAlpha = 8;
    public const int DualBeta = 9;
    public const int Echo = 10;
    public const int Scale = 11;
    public const int Count = 12;
    public static readonly string[] Names = ["Move", "Pulse", "Window", "Damage", "GaGlobal", "Job", "Alpha", "Blend", "DualAlpha", "DualBeta", "Echo", "Scale"];

    private static readonly Dictionary<(Type, Type), int> Indices = new()
    {
        [(typeof(ManyEntities.MoveTrack), typeof(ManyEntities.MoveClip))] = Move,
        [(typeof(ManyEntities.PulseTrack), typeof(ManyEntities.PulseClip))] = Pulse,
        [(typeof(ManyEntities.WindowTrack), typeof(ManyEntities.WindowClip))] = Window,
        [(typeof(Fresh.DamageTrack), typeof(Fresh.DamageClip))] = Damage,
        [(typeof(GaGlobalTrack), typeof(GaGlobalClip))] = GaGlobal,
        [(typeof(Tlb.JobTrack), typeof(Tlb.JobClip))] = Job,
        [(typeof(Tlb.AlphaTrack), typeof(Tlb.AlphaClip))] = Alpha,
        [(typeof(Tlb.BlendTrack), typeof(Tlb.BlendClip))] = Blend,
        [(typeof(Tlb.DualTrack), typeof(Tlb.DualAlphaClip))] = DualAlpha,
        [(typeof(Tlb.DualTrack), typeof(Tlb.DualBetaClip))] = DualBeta,
        [(typeof(Tlb.EchoTrack), typeof(Tlb.EchoClip))] = Echo,
        [(typeof(Play.ScaleTrack), typeof(Play.AmountClip))] = Scale,
    };

    public static int IndexOf<TTrack, TClip>()
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
        => Indices[(typeof(TTrack), typeof(TClip))];

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<ManyEntities.MoveTrack, ManyEntities.MoveClip>.Consume(&ExecuteMove, &BindFloat);
        PairRuntime<ManyEntities.PulseTrack, ManyEntities.PulseClip>.Consume(&ExecutePulse, &BindFloat);
        PairRuntime<ManyEntities.WindowTrack, ManyEntities.WindowClip>.Consume(&ExecuteWindow, &BindFloat);
        PairRuntime<Fresh.DamageTrack, Fresh.DamageClip>.Consume(&ExecuteDamage, &BindFloat);
        PairRuntime<GaGlobalTrack, GaGlobalClip>.Consume(&ExecuteGaGlobal, &BindFloat);
        PairRuntime<Tlb.JobTrack, Tlb.JobClip>.Consume(&ExecuteJob, &BindFloat);
        PairRuntime<Tlb.AlphaTrack, Tlb.AlphaClip>.Consume(&ExecuteAlpha, &BindFloat);
        PairRuntime<Tlb.BlendTrack, Tlb.BlendClip>.Consume(&ExecuteBlend, &BindFloat);
        PairRuntime<Tlb.DualTrack, Tlb.DualAlphaClip>.Consume(&ExecuteDualAlpha, &BindFloat);
        PairRuntime<Tlb.DualTrack, Tlb.DualBetaClip>.Consume(&ExecuteDualBeta, &BindFloat);
        PairRuntime<Tlb.EchoTrack, Tlb.EchoClip>.Consume(&ExecuteEcho, &BindFloat);
        PairRuntime<Play.ScaleTrack, Play.AmountClip>.Consume(&ExecuteScale, &BindFloat);
    }

    private static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }

    private static void ExecuteMove(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ManyEntities.MoveClip scratch = default;
        var frame = TickFrame.ToFrame<ManyEntities.MoveTrack, ManyEntities.MoveClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Mult;
    }

    private static void ExecutePulse(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ManyEntities.PulseClip scratch = default;
        var frame = TickFrame.ToFrame<ManyEntities.PulseTrack, ManyEntities.PulseClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Power;
    }

    private static void ExecuteWindow(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ManyEntities.WindowClip scratch = default;
        var frame = TickFrame.ToFrame<ManyEntities.WindowTrack, ManyEntities.WindowClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Power;
    }

    private static void ExecuteDamage(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Fresh.DamageClip scratch = default;
        var frame = TickFrame.ToFrame<Fresh.DamageTrack, Fresh.DamageClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Multiplier;
    }

    private static void ExecuteGaGlobal(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        GaGlobalClip scratch = default;
        var frame = TickFrame.ToFrame<GaGlobalTrack, GaGlobalClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }

    private static void ExecuteJob(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Tlb.JobClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.JobTrack, Tlb.JobClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Multiplier;
    }

    private static void ExecuteAlpha(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Tlb.AlphaClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.AlphaTrack, Tlb.AlphaClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Value * frame.Track.Code;
    }

    private static void ExecuteBlend(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Tlb.BlendClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.BlendTrack, Tlb.BlendClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }

    private static void ExecuteDualAlpha(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Tlb.DualAlphaClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.DualTrack, Tlb.DualAlphaClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Value * frame.Track.Code;
    }

    private static void ExecuteDualBeta(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Tlb.DualBetaClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.DualTrack, Tlb.DualBetaClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Code;
    }

    private static void ExecuteEcho(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Tlb.EchoClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.EchoTrack, Tlb.EchoClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Value * frame.Track.Code;
    }

    private static void ExecuteScale(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Play.AmountClip scratch = default;
        var frame = TickFrame.ToFrame<Play.ScaleTrack, Play.AmountClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }
}
