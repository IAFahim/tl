using System.Runtime.CompilerServices;
using Tl;
using Tl.TestSupport;

namespace Tl.FusedAdvanceProbe;

public readonly record struct LaneClip(float Amount);

public readonly record struct LaneTrack(float Scale) : IBlend<LaneClip>
{
    public void Blend(in LaneClip first, in LaneClip second, float factor, out LaneClip result)
        => result = new LaneClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly record struct EdgeClip(float Amount);

public readonly record struct EdgeTrack(float Scale) : IBlend<EdgeClip>
{
    public void Blend(in EdgeClip first, in EdgeClip second, float factor, out EdgeClip result)
        => result = new EdgeClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public static unsafe class Host
{
    public const int Duration = 1024;

    public static readonly TimelineAsset LoopingAsset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
        .Track<LaneTrack, LaneClip>(new LaneTrack(2f))
        .Clip(0, 0, 600, new LaneClip(1.25f))
        .Clip(0, 600, 1024, new LaneClip(-0.5f))
        .Looping()
        .Bake()));

    public static readonly TimelineAsset FiniteAsset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
        .Track<EdgeTrack, EdgeClip>(new EdgeTrack(2f))
        .Clip(0, 0, 1024, new EdgeClip(0.75f))
        .Bake()));

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<LaneTrack, LaneClip>.Consume(&ExecuteLane, &BindFloat);
        PairRuntime<EdgeTrack, EdgeClip>.Consume(&ExecuteEdge, &BindFloat);
    }

    public static ushort SlotLane() => LoopingAsset.Index;

    public static ushort SlotFinite() => FiniteAsset.Index;

    public static readonly TimelineAsset[] VariantAssets = BuildVariants();

    static TimelineAsset[] BuildVariants()
    {
        var assets = new TimelineAsset[8];
        for (var variant = 0; variant < 8; variant++)
        {
            var scale = 2f + variant * 0.03125f;
            var split = 600;
            assets[variant] = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                .Track<LaneTrack, LaneClip>(new LaneTrack(scale))
                .Clip(0, 0, (uint)split, new LaneClip(1.25f))
                .Clip(0, (uint)split, Duration, new LaneClip(-0.5f))
                .Looping()
                .Bake()));
        }
        return assets;
    }

    public static ushort[] BuildIndices(int timelines)
    {
        var indices = new ushort[timelines];
        for (var i = 0; i < timelines; i++)
            indices[i] = VariantAssets[i].Index;
        return indices;
    }

    static void ExecuteLane(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        LaneClip scratch = default;
        var frame = TickFrame.ToFrame<LaneTrack, LaneClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }

    static void ExecuteEdge(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        EdgeClip scratch = default;
        var frame = TickFrame.ToFrame<EdgeTrack, EdgeClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }

    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }
}

public enum Clock
{
    Uniform,
    Waves,
    Staggered,
    Clamped,
}

public static class Seeds
{
    public static ushort[] Positions(int rows, Clock clock)
    {
        var positions = new ushort[rows];
        for (var i = 0; i < rows; i++)
            positions[i] = clock switch
            {
                Clock.Uniform => 5,
                Clock.Waves => (ushort)(i / 100 % Host.Duration),
                Clock.Staggered => (ushort)(i % Host.Duration),
                _ => (ushort)(i % (Host.Duration + 1)),
            };
        return positions;
    }

    public static ushort[] Ids(int rows, int timelines)
    {
        var ids = new ushort[rows];
        for (var i = 0; i < rows; i++) ids[i] = (ushort)(i % timelines);
        return ids;
    }

    public static float[] Effects(int rows)
    {
        var effects = new float[rows];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < rows; i++)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            effects[i] = (float)((state >> 11) / 9007199254740992d) * 64f - 32f;
        }
        return effects;
    }
}
