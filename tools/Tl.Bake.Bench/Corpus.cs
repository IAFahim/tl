using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Tl.Bake.Bench;

internal static class Corpus
{
    internal const int TrackCount = 128;
    internal const int ClipsPerTrack = 16;
    internal const uint Duration = 65500;
    internal static readonly int[] ClipFloatFields = [595, 640, 665, 690, 715, 740, 785];

    internal sealed record CorpusFile(string Path, string Description);

    internal static string DefaultDirectory => Path.Combine(Path.GetTempPath(), "tl108-corpus");

    internal static CorpusFile GamePath(string? directory) => new(
        Path.Combine(directory ?? DefaultDirectory, "corpus-game.json"),
        "128 tracks x 16 clips, 7 (track,clip) pairs, duration 65500, loop, float-heavy clip data");

    internal static CorpusFile SmallPath(string? directory) => new(
        Path.Combine(directory ?? DefaultDirectory, "corpus-small.json"),
        "3 tracks x 4 clips, single-file incremental case");

    internal static void GenerateAll(string? directory)
    {
        Directory.CreateDirectory(directory ?? DefaultDirectory);
        WriteGame(GamePath(directory).Path);
        WriteSmall(SmallPath(directory).Path);
    }

    internal static void WriteGame(string path)
    {
        var rng = new SplitMix64(0x544C_3130_3853_4543ul);
        var sb = new StringBuilder(24_000_000);
        sb.Append("{\"name\":\"full_game\",\"duration\":").Append(Duration).Append(",\"loop\":true,\"tracks\":[");
        for (var t = 0; t < TrackCount; t++)
        {
            if (t > 0)
                sb.Append(',');
            var pair = t % ClipFloatFields.Length;
            sb.Append('{');
            if (t % 5 != 4)
                sb.Append("\"name\":\"track_").Append(t).Append("\",");
            sb.Append("\"namespace\":\"GameBench\",\"type\":\"G").Append(pair).Append("Track\"");
            sb.Append(",\"data\":{\"Code\":").Append(rng.NextUInt32() % 100000)
              .Append(",\"Scale\":").Append(FloatText(rng.NextUInt32() % 2000000))
              .Append(",\"Flag\":").Append(rng.NextUInt32() % 2 == 0 ? "true" : "false").Append('}');
            sb.Append(",\"clips\":[");
            var edges = ClipEdges(ref rng);
            for (var c = 0; c < ClipsPerTrack; c++)
            {
                if (c > 0)
                    sb.Append(',');
                var start = c == 0 ? 0 : edges[c - 1];
                var end = edges[c];
                if (c % 4 == 3 && c + 1 < ClipsPerTrack)
                {
                    var overlap = 150 + rng.NextUInt32() % 350;
                    end = Math.Min(end + overlap, edges[c + 1]);
                }
                sb.Append('{');
                if ((t + c) % 3 == 0)
                    sb.Append("\"name\":\"clip_").Append(t).Append('_').Append(c).Append("\",");
                sb.Append("\"namespace\":\"GameBench\",\"type\":\"G").Append(pair).Append("Clip\"");
                sb.Append(",\"start\":").Append(start).Append(",\"end\":").Append(end);
                sb.Append(",\"data\":{");
                var fieldCount = ClipFloatFields[pair];
                for (var f = 0; f < fieldCount; f++)
                {
                    if (f > 0)
                        sb.Append(',');
                    sb.Append("\"F").Append(f).Append("\":").Append(NextFloatText(ref rng));
                }
                sb.Append(",\"I0\":").Append(rng.NextUInt32() % 65536);
                sb.Append(",\"B0\":").Append(rng.NextUInt32() % 2 == 0 ? "true" : "false");
                sb.Append("}}");
            }
            sb.Append("]}");
        }
        sb.Append("]}");
        File.WriteAllText(path, sb.ToString());
    }

    internal static void WriteSmall(string path)
    {
        var rng = new SplitMix64(0x544C_3130_3853_4543ul ^ 0x534D_414C_4C30_3031ul);
        var sb = new StringBuilder(12_000);
        sb.Append("{\"name\":\"single_room\",\"duration\":2000,\"loop\":false,\"tracks\":[");
        for (var t = 0; t < 3; t++)
        {
            if (t > 0)
                sb.Append(',');
            var pair = t;
            sb.Append("{\"name\":\"room_track_").Append(t)
              .Append("\",\"namespace\":\"GameBench\",\"type\":\"G").Append(pair).Append("Track\"");
            sb.Append(",\"data\":{\"Code\":").Append(rng.NextUInt32() % 1000)
              .Append(",\"Scale\":").Append(FloatText(rng.NextUInt32() % 2000000))
              .Append(",\"Flag\":true}");
            sb.Append(",\"clips\":[");
            for (var c = 0; c < 4; c++)
            {
                if (c > 0)
                    sb.Append(',');
                var start = (uint)(c * 500);
                var end = c == 3 ? 2000u : start + 500 + (c % 2 == 0 ? 60u : 0u);
                sb.Append('{');
                if (c % 2 == 0)
                    sb.Append("\"name\":\"beat_").Append(c).Append("\",");
                sb.Append("\"namespace\":\"GameBench\",\"type\":\"G").Append(pair).Append("Clip\"");
                sb.Append(",\"start\":").Append(start).Append(",\"end\":").Append(end);
                sb.Append(",\"data\":{");
                for (var f = 0; f < 31; f++)
                {
                    if (f > 0)
                        sb.Append(',');
                    sb.Append("\"F").Append(f).Append("\":").Append(NextFloatText(ref rng));
                }
                sb.Append(",\"I0\":").Append(rng.NextUInt32() % 1000).Append(",\"B0\":false}}");
            }
            sb.Append("]}");
        }
        sb.Append("]}");
        File.WriteAllText(path, sb.ToString());
    }

    private static uint[] ClipEdges(ref SplitMix64 rng)
    {
        var edges = new uint[ClipsPerTrack];
        for (var j = 0; j < ClipsPerTrack; j++)
        {
            var ideal = (uint)((j + 1) * (long)Duration / ClipsPerTrack);
            var jitter = rng.NextUInt32() % 400;
            var edge = j == ClipsPerTrack - 1 ? Duration : ideal - 200 + jitter;
            if (j > 0 && edge <= edges[j - 1])
                edge = edges[j - 1] + 1;
            edges[j] = edge;
        }
        return edges;
    }

    internal static string NextFloatText(ref SplitMix64 rng)
    {
        var kind = rng.NextUInt32() % 10;
        if (kind < 7)
            return FloatText(rng.NextUInt32() % 2_000_000);
        if (kind < 9)
            return (rng.NextUInt32() % 200).ToString(CultureInfo.InvariantCulture);
        return (rng.NextUInt32() % 2_000_000 / 1_000_000.0).ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string FloatText(uint raw) =>
        ((int)(raw % 2_000_000) - 1_000_000) / 1000.0 is var v and not 0
            ? v.ToString("0.###", CultureInfo.InvariantCulture)
            : "0";

    internal static (long Bytes, string Sha256) Stat(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return (bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    internal static string Describe(string path, string description)
    {
        var (bytes, sha) = Stat(path);
        return $"{path}: {bytes} B, sha256 {sha} — {description}";
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
