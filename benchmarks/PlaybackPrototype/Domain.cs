using System.Runtime.CompilerServices;
using Tl;

namespace PlaybackPrototype;

public interface IAmountClip
{
    float Amount { get; }
}

public interface ICodeTrack
{
    int Code { get; }
}

public readonly struct Input
{
    public readonly float Value;

    public Input(float value) => Value = value;
}

public struct Acc
{
    public long Value;
}

public readonly struct Gather
{
    public readonly float Value;

    public Gather(float value) => Value = value;
}

public struct Observed
{
    public int Value;
}

public readonly record struct MoveClip(float Amount) : IAmountClip;

public readonly record struct MoveTrack(int Code) : IBlend<MoveClip>, ICodeTrack
{
    public void Blend(in MoveClip first, in MoveClip second, float factor, out MoveClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct PulseClip(float Amount) : IAmountClip;

public readonly record struct PulseTrack(int Code) : IBlend<PulseClip>, ICodeTrack
{
    public void Blend(in PulseClip first, in PulseClip second, float factor, out PulseClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct WatchClip(float Amount) : IAmountClip;

public readonly record struct WatchTrack(int Code) : IBlend<WatchClip>, ICodeTrack
{
    public void Blend(in WatchClip first, in WatchClip second, float factor, out WatchClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct ChurnClip(float Amount) : IAmountClip;

public readonly record struct ChurnTrack(int Code) : IBlend<ChurnClip>, ICodeTrack
{
    public void Blend(in ChurnClip first, in ChurnClip second, float factor, out ChurnClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public static class MoveJob
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(float amount, int code, uint gameTick, float input, ref Acc acc)
    {
        acc.Value = unchecked(acc.Value * 31 + (int)(amount * input) + code + (int)gameTick);
    }
}

public static class WatchJob
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Contribution(float amount, uint gameTick, float watched)
        => (int)(amount * watched) + (int)gameTick;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Commit(ref Acc target, int contribution)
    {
        target.Value += contribution;
    }
}

public static class ChurnJob
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(float amount, int code, uint gameTick, ref Acc acc)
    {
        acc.Value = unchecked(acc.Value * 31 + (int)amount + code + (int)gameTick);
    }
}

internal static unsafe class Consumers
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<MoveTrack, MoveClip>.Consume(&MoveExecute, &ReadWriteBind);
        PairRuntime<PulseTrack, PulseClip>.Consume(&PulseExecute, &ReadWriteBind);
        PairRuntime<WatchTrack, WatchClip>.Consume(&WatchExecute, &WatchBind);
        PairRuntime<ChurnTrack, ChurnClip>.Consume(&ChurnExecute, &WriteBind);
    }

    private static void ReadWriteBind(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
        {
            if (keys[i] == TypeKey<Input>.Value) table[0] = (byte)(i + 1);
            else if (keys[i] == TypeKey<Acc>.Value) table[1] = (byte)(i + 1);
        }
    }

    private static void WatchBind(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
        {
            if (keys[i] == TypeKey<Gather>.Value) table[0] = (byte)(i + 1);
            else if (keys[i] == TypeKey<Observed>.Value) table[1] = (byte)(i + 1);
        }
    }

    private static void WriteBind(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
        {
            if (keys[i] == TypeKey<Acc>.Value) table[0] = (byte)(i + 1);
        }
    }

    private static void MoveExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        MoveClip scratch = default;
        var frame = TickFrame.ToFrame<MoveTrack, MoveClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        var accs = (Acc*)columns[1];
        if (accs == null) return;
        var inputs = (Input*)columns[0];
        MoveJob.Execute(frame.Clip.Amount, frame.Track.Code, gameTick, inputs != null ? inputs[row].Value : 1f, ref accs[row]);
    }

    private static void PulseExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        PulseClip scratch = default;
        var frame = TickFrame.ToFrame<PulseTrack, PulseClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        var accs = (Acc*)columns[1];
        if (accs == null) return;
        var inputs = (Input*)columns[0];
        MoveJob.Execute(frame.Clip.Amount, frame.Track.Code, gameTick, inputs != null ? inputs[row].Value : 1f, ref accs[row]);
    }

    private static void WatchExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        WatchClip scratch = default;
        var frame = TickFrame.ToFrame<WatchTrack, WatchClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        var observed = (Observed*)columns[1];
        if (observed == null) return;
        var gather = (Gather*)columns[0];
        observed[row].Value = WatchJob.Contribution(frame.Clip.Amount, gameTick, gather != null ? gather[row].Value : 1f);
    }

    private static void ChurnExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        ChurnClip scratch = default;
        var frame = TickFrame.ToFrame<ChurnTrack, ChurnClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        var accs = (Acc*)columns[0];
        if (accs == null) return;
        ChurnJob.Execute(frame.Clip.Amount, frame.Track.Code, gameTick, ref accs[row]);
    }
}
