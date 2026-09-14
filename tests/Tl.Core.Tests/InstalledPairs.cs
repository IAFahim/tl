using System.Runtime.CompilerServices;
using Xunit;

namespace Tl.Core.Tests;

internal static unsafe class InstalledPairs
{
    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<AlphaTrack, AlphaClip>.Consume(&AlphaExecute, &AlphaBind);
        PairRuntime<BetaTrack, BetaClip>.Consume(&BetaExecute, &NoBind);
        PairRuntime<PhiTrack, PhiClip>.Consume(&PhiExecute, &NoBind);
        PairRuntime<PhiTrack, PhiClip>.Consume(&PhiExecuteSecond, &NoBind);
        PairRuntime<DeltaTrack, DeltaClip>.Consume(&DeltaExecute, &DeltaBind);
    }

    private static void NoBind(ulong* keys, int keyCount, byte* table)
    {
    }

    private static void AlphaBind(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
        {
            if (keys[i] == TypeKey<Health>.Value)
            {
                table[0] = (byte)(i + 1);
                break;
            }
        }
    }

    private static void AlphaExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        AlphaClip scratch = default;
        var current = TickFrame.ToFrame<AlphaTrack, AlphaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        DataTests.Records.Add(new Record('A', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
        var health = (Health*)columns[0];
        if (health != null)
            health[row].Value += current.Clip.Value * current.Track.Code;
    }

    private static void BetaExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row) => throw new InvalidOperationException("Beta consumer failed.");

    private static void PhiExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        PhiClip scratch = default;
        var current = TickFrame.ToFrame<PhiTrack, PhiClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        DataTests.Records.Add(new Record('1', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    private static void PhiExecuteSecond(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        PhiClip scratch = default;
        var current = TickFrame.ToFrame<PhiTrack, PhiClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        DataTests.Records.Add(new Record('2', current.Track.Code, current.Clip.Value, current.TimelineTick, current.GameTick, current.Cycle, current.Flags));
    }

    private static void DeltaExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
    }

    private static void DeltaBind(ulong* keys, int keyCount, byte* table)
    {
        if (DataTests.MarkerRequired)
        {
            var found = false;
            for (var i = 0; i < keyCount; i++)
            {
                if (keys[i] == TypeKey<Marker>.Value) { found = true; break; }
            }
            if (!found) throw new ArgumentException("Delta requires the marker column.");
        }
    }
}
