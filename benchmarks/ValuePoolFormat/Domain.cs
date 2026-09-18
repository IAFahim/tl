using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Tl;
using Tl.Gen.Tlb;

namespace LaneBench
{
    public readonly record struct LaneClip(float Amount);

    public readonly record struct LaneTrack(float Scale) : IBlend<LaneClip>
    {
        public void Blend(in LaneClip first, in LaneClip second, float factor, out LaneClip result)
            => result = new LaneClip(first.Amount + (second.Amount - first.Amount) * factor);
    }
}

namespace Tl.ValuePoolFormat
{

public static unsafe class Host
{
    public const int LaneRows = 100_000;

    public const string DualBlendJson = """
    {
      "name": "boss_phase_one",
      "duration": 64,
      "loop": true,
      "tracks": [
        {
          "name": "main_damage",
          "namespace": "Tlb",
          "type": "DualTrack",
          "data": { "Code": 2 },
          "clips": [
            { "name": "opening_hit", "namespace": "Tlb", "type": "DualAlphaClip", "start": 0, "end": 12, "data": { "Value": 5 } },
            { "name": "mid_stun", "namespace": "Tlb", "type": "DualBetaClip", "start": 20, "end": 26, "data": { "Amount": 2 } },
            { "name": "heavy_hit", "namespace": "Tlb", "type": "DualAlphaClip", "start": 40, "end": 52, "data": { "Value": 9 } }
          ]
        },
        {
          "name": "armor_buff",
          "namespace": "Tlb",
          "type": "BlendTrack",
          "data": { "Scale": 0.5 },
          "clips": [
            { "name": "ramp_up", "namespace": "Tlb", "type": "BlendClip", "start": 8, "end": 30, "data": { "Amount": 3 } }
          ]
        }
      ]
    }
    """;

    public const string LaneJson = """
    {
      "duration": 1024,
      "loop": true,
      "tracks": [
        {
          "namespace": "LaneBench",
          "type": "LaneTrack",
          "data": { "Scale": 2.0 },
          "clips": [
            { "namespace": "LaneBench", "type": "LaneClip", "start": 0, "end": 600, "data": { "Amount": 1.25 } },
            { "namespace": "LaneBench", "type": "LaneClip", "start": 600, "end": 1024, "data": { "Amount": -0.5 } }
          ]
        }
      ]
    }
    """;

    public static readonly TimelineAsset DualBlendAsset = TimelineAsset.Of(TimelineAsset.Load(TimelineBaker.BakeJson(DualBlendJson)));
    public static readonly TimelineAsset LaneAsset = TimelineAsset.Of(TimelineAsset.Load(TimelineBaker.BakeJson(LaneJson)));

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<Tlb.DualTrack, Tlb.DualAlphaClip>.Consume(&ExecuteDualAlpha, &NoBind);
        PairRuntime<Tlb.DualTrack, Tlb.DualBetaClip>.Consume(&ExecuteDualBeta, &NoBind);
        PairRuntime<Tlb.BlendTrack, Tlb.BlendClip>.Consume(&ExecuteBlend, &NoBind);
        PairRuntime<LaneBench.LaneTrack, LaneBench.LaneClip>.Consume(&ExecuteLane, &BindFloat);
    }

    private static void NoBind(ulong* keys, int keyCount, byte* table)
    {
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

    private static void ExecuteBlend(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        Tlb.BlendClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.BlendTrack, Tlb.BlendClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }

    private static void ExecuteLane(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        LaneBench.LaneClip scratch = default;
        var frame = TickFrame.ToFrame<LaneBench.LaneTrack, LaneBench.LaneClip>(slot, pair, tick, flags, ref scratch);
        ((float*)columns[0])[row] += frame.Clip.Amount * frame.Track.Scale;
    }

    public static string Checksum(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    public static string QueryChecksum(TimelineAsset asset, ushort duration, params int[] pairs)
    {
        var effects = new float[64];
        var component = new TimelineComponent(asset.Reference);
        for (var position = 0; position <= duration; position++)
        {
            component.Position = (ushort)position;
            if (pairs.Contains(0))
                foreach (var frame in Timeline.Query<Tlb.DualTrack, Tlb.DualAlphaClip>(in component))
                    effects[position % effects.Length] += frame.Clip.Value * frame.Track.Code;
            if (pairs.Contains(1))
                foreach (var frame in Timeline.Query<Tlb.DualTrack, Tlb.DualBetaClip>(in component))
                    effects[position % effects.Length] += frame.Clip.Amount * frame.Track.Code;
            if (pairs.Contains(2))
                foreach (var frame in Timeline.Query<Tlb.BlendTrack, Tlb.BlendClip>(in component))
                    effects[position % effects.Length] += frame.Clip.Amount * frame.Track.Scale;
        }
        var bytes = new float[effects.Length];
        effects.CopyTo(bytes, 0);
        return Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(bytes.AsSpan())));
    }
}
}
