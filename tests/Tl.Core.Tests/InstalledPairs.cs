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
        var health = (Health*)columns[0];
        if (health != null)
            health[row].Value += current.Clip.Value * current.Track.Code;
    }

    private static void BetaExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row) => throw new InvalidOperationException("Beta consumer failed.");

}
