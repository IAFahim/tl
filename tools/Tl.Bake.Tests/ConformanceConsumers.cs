using System.Runtime.CompilerServices;
using Tl;
using Tlb;

namespace Tl.Bake.Tests;

internal static unsafe class ConformanceConsumers
{
    public const string JobFold = "job1";
    public const string AmbienceFold = "fold2";

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<JobTrack, JobClip>.Consume(&FoldJob, &KeysJob);
        PairRuntime<FoldTrack, FoldClip>.Consume(&FoldAmbience, &KeysAmbience);
    }

    static int KeysJob(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            meta[0] = 4 | 16;
        }
        return 1;
    }

    static void FoldJob(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        JobClip scratch = default;
        var frame = TickFrame.ToFrame<JobTrack, JobClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] = frame.Direction * frame.Clip.Amount * frame.Track.Multiplier + frame.WithinClip;
    }

    static int KeysAmbience(ulong* keys, byte* meta)
    {
        if (keys != null)
        {
            keys[0] = TypeKey<float>.Value;
            meta[0] = 4 | 16;
            keys[1] = TypeKey<int>.Value;
            meta[1] = 4 | 16 | 128;
        }
        return 2;
    }

    static void FoldAmbience(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        FoldClip scratch = default;
        var frame = TickFrame.ToFrame<FoldTrack, FoldClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] = frame.Direction * frame.Clip.Height * frame.Track.Scale + frame.WithinClip;
        ((int*)columns[1])[row] = (int)frame.Clip.Height;
    }
}
