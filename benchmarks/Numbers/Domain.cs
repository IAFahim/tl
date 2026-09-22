using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Tl;
using Tl.Gen.Tlb;

namespace NumbersBench;

public readonly record struct LaneClip(float Amount);

public readonly record struct LaneTrack(float Scale) : IBlend<LaneClip>
{
    public void Blend(in LaneClip first, in LaneClip second, float factor, out LaneClip result)
        => result = new LaneClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static class Domain
{
    public static void AddScalar(Span<float> effects, float delta)
    {
        for (var i = 0; i < effects.Length; i++)
            effects[i] += delta;
    }

    public static void AddVector(Span<float> effects, float delta)
    {
        ref var start = ref MemoryMarshal.GetReference(effects);
        var width = Vector<float>.Count;
        var wide = new Vector<float>(delta);
        var i = 0;
        for (; i <= effects.Length - width; i += width)
        {
            ref var at = ref Unsafe.Add(ref start, i);
            (Vector.LoadUnsafe(ref at) + wide).StoreUnsafe(ref at);
        }
        for (; i < effects.Length; i++)
            Unsafe.Add(ref start, i) += delta;
    }
}

public static unsafe class Host
{
    public const int Duration = 1024;
    public const int Timelines = 100;

    public static ushort Gold;
    public static ushort Finite;
    public static readonly ushort[] Variants = new ushort[Timelines];

    [ModuleInitializer]
    internal static void Install()
    {
        PairRuntime<LaneTrack, LaneClip>.Consume(&ExecuteLane, &BindFloat);
    }

    public static void LoadAssets()
    {
        var resolver = new BakerAssemblyResolver();
        Gold = TimelineAsset.Load(TimelineBakerFast.BakeJsonUtf8(
            Encoding.UTF8.GetBytes(LaneJson("gold", 2f, 600, looping: true)), resolver));
        Finite = TimelineAsset.Load(TimelineBakerFast.BakeJsonUtf8(
            Encoding.UTF8.GetBytes(LaneJson("finite", 2f, 600, looping: false)), resolver));
        for (var variant = 0; variant < Timelines; variant++)
            Variants[variant] = TimelineAsset.Load(TimelineBakerFast.BakeJsonUtf8(
                Encoding.UTF8.GetBytes(LaneJson($"v{variant}", 1f + variant * 0.25f, 300 + variant * 7, looping: true)), resolver));
    }

    static string LaneJson(string name, float scale, int split, bool looping) =>
        $$$"""
        {"name":"{{{name}}}","duration":{{{Duration}}},"loop":{{{(looping ? "true" : "false")}}},"tracks":[
          {"name":"arc","namespace":"NumbersBench","type":"LaneTrack","data":{"Scale":{{{Float(scale)}}}},
           "clips":[
             {"name":"rise","namespace":"NumbersBench","type":"LaneClip","start":0,"end":{{{split}}},"data":{"Amount":1.25}},
             {"name":"fall","namespace":"NumbersBench","type":"LaneClip","start":{{{split}}},"end":{{{Duration}}},"data":{"Amount":-0.5}}]}
        ]}
        """;

    static string Float(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    static void ExecuteLane(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        LaneClip scratch = default;
        var frame = TickFrame.ToFrame<LaneTrack, LaneClip>(slot, pair, tick, flags, ref scratch);
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

internal struct SplitMix64(ulong seed)
{
    private ulong _state = seed;

    internal uint NextUInt32()
    {
        _state += 0x9E37_79B9_7F4A_7C15ul;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58_476D_1CE4_E5B9ul;
        z = (z ^ (z >> 27)) * 0x94D0_49BB_1331_11EBul;
        z ^= z >> 31;
        return (uint)(z >> 32);
    }
}

internal static class Corpus
{
    private const int TrackCount = 128;
    private const int ClipsPerTrack = 16;
    private const uint Duration = 65500;
    private static readonly int[] FieldCounts = [595, 640, 665, 690, 715, 740, 785];

    internal static string Generate(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "numbers-game.json");
        if (File.Exists(path)) return path;
        var rng = new SplitMix64(0x544C_4E55_4D42_4552ul);
        var sb = new StringBuilder(24_000_000);
        sb.Append("{\"name\":\"numbers_game\",\"duration\":").Append(Duration).Append(",\"loop\":true,\"tracks\":[");
        for (var t = 0; t < TrackCount; t++)
        {
            if (t > 0) sb.Append(',');
            var pair = t % FieldCounts.Length;
            var fields = FieldCounts[pair];
            sb.Append('{');
            if (t % 5 != 4) sb.Append("\"name\":\"track_").Append(t).Append("\",");
            sb.Append("\"namespace\":\"GameBench\",\"type\":\"G").Append(pair).Append("Track\"");
            sb.Append(",\"data\":{\"Code\":").Append(rng.NextUInt32() % 100000)
              .Append(",\"Scale\":").Append(Float(rng.NextUInt32() % 2_000_000))
              .Append(",\"Flag\":").Append(rng.NextUInt32() % 2 == 0 ? "true" : "false").Append('}');
            sb.Append(",\"clips\":[");
            var edges = Edges(ref rng);
            for (var c = 0; c < ClipsPerTrack; c++)
            {
                if (c > 0) sb.Append(',');
                var start = c == 0 ? 0u : edges[c - 1];
                var end = edges[c];
                if (c % 4 == 3 && c + 1 < ClipsPerTrack)
                {
                    var overlap = 150 + rng.NextUInt32() % 350;
                    end = Math.Min(end + overlap, edges[c + 1]);
                }
                sb.Append('{');
                if ((t + c) % 3 == 0) sb.Append("\"name\":\"clip_").Append(t).Append('_').Append(c).Append("\",");
                sb.Append("\"namespace\":\"GameBench\",\"type\":\"G").Append(pair).Append("Clip\"");
                sb.Append(",\"start\":").Append(start).Append(",\"end\":").Append(end);
                sb.Append(",\"data\":{");
                for (var f = 0; f < fields; f++)
                {
                    if (f > 0) sb.Append(',');
                    sb.Append("\"F").Append(f).Append("\":").Append(FloatText(ref rng));
                }
                sb.Append(",\"I0\":").Append(rng.NextUInt32() % 65536);
                sb.Append(",\"B0\":").Append(rng.NextUInt32() % 2 == 0 ? "true" : "false");
                sb.Append("}}");
            }
            sb.Append("]}");
        }
        sb.Append("]}");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    static uint[] Edges(ref SplitMix64 rng)
    {
        var edges = new uint[ClipsPerTrack];
        for (var j = 0; j < ClipsPerTrack; j++)
        {
            var ideal = (uint)((j + 1) * Duration / ClipsPerTrack);
            var edge = j == ClipsPerTrack - 1 ? Duration : ideal - 200 + rng.NextUInt32() % 400;
            if (j > 0 && edge <= edges[j - 1]) edge = edges[j - 1] + 1;
            edges[j] = edge;
        }
        return edges;
    }

    static string Float(uint raw) =>
        ((int)(raw % 2_000_000) - 1_000_000) / 1000.0 is var v and not 0
            ? v.ToString("0.###", CultureInfo.InvariantCulture)
            : "0";

    static string FloatText(ref SplitMix64 rng)
    {
        var kind = rng.NextUInt32() % 10;
        if (kind < 7) return Float(rng.NextUInt32() % 2_000_000);
        if (kind < 9) return (rng.NextUInt32() % 200).ToString(CultureInfo.InvariantCulture);
        return (rng.NextUInt32() % 2_000_000 / 1_000_000.0).ToString("0.######", CultureInfo.InvariantCulture);
    }
}
