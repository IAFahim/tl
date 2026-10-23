using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Tl;
using Tl.Gen.Tlb;
using Tlb;

namespace Tl.Bake.Tests;

internal sealed record ConformanceLane(ulong Key, uint[] Forward, uint[] Backward);

internal sealed record ConformanceClock(bool Forward, ushort Position, bool Active, ushort Tick, ushort Next, uint Effect);

internal sealed record ConformancePair(ulong PairKey, string Fold, ConformanceLane[] Lanes, ConformanceClock[] Clock);

internal sealed record ConformanceVector(
    string Name, string SourceKind, string Source, string TlbSha256,
    ushort Duration, bool Looping, ConformancePair[] Pairs);

internal static class ConformanceVectors
{
    public const string Extension = ".tlcv";

    static readonly (string Name, string Kind, string Fold, Func<string>? Json)[] Definitions =
    [
        ("minimal", "fixture", ConformanceConsumers.JobFold, null),
        ("blended", "fixture", ConformanceConsumers.JobFold, null),
        ("blended", "fixture", ConformanceConsumers.AmbienceFold, null),
        ("stress-blend", "stress", ConformanceConsumers.JobFold, BlendJson),
        ("stress-blend", "stress", ConformanceConsumers.AmbienceFold, BlendJson),
        ("stress-segmented", "stress", ConformanceConsumers.AmbienceFold, SegmentedJson),
        ("stress-tiny", "stress", ConformanceConsumers.JobFold, TinyJson),
    ];

    public static IReadOnlyList<string> Names => Definitions.GroupBy(d => d.Name).Select(g => g.Key).ToList();

    public static string GoldenRoot()
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
        while (dir != null && !(File.Exists(Path.Combine(dir.FullName, "Tl.Bake.Tests.csproj")) &&
                                File.Exists(Path.Combine(dir.FullName, "golden", "fixtures", "minimal.tlb"))))
            dir = dir.Parent!;
        return Path.Combine(dir!.FullName, "golden");
    }

    public static string VectorPath(string name)
        => Path.Combine(GoldenRoot(), "vectors", name + Extension);

    public static byte[] SourceBytes(string name)
    {
        var entries = Definitions.Where(d => d.Name == name).ToArray();
        if (entries[0].Kind == "fixture")
            return File.ReadAllBytes(Path.Combine(GoldenRoot(), "fixtures", name + ".tlb"));
        return BakeStress(entries.First(d => d.Json != null).Json!());
    }

    public static byte[] BakeStress(string json)
        => TimelineBaker.BakeJson(json, BakerAssemblyResolver.FromAssemblies([typeof(JobTrack).Assembly]));

    public static ConformanceVector Build(string name)
    {
        var entries = Definitions.Where(d => d.Name == name).ToArray();
        var bytes = SourceBytes(name);
        var index = TimelineAsset.Load(bytes);
        var sections = new List<FoldedPair>();
        foreach (var entry in entries)
            sections.Add(entry.Fold == ConformanceConsumers.JobFold
                ? BuildPair<JobTrack, JobClip>(index, entry.Fold)
                : BuildPair<FoldTrack, FoldClip>(index, entry.Fold));
        sections.Sort((a, b) => a.Pair.PairKey.CompareTo(b.Pair.PairKey));
        return new ConformanceVector(
            name, entries[0].Kind,
            entries[0].Kind == "fixture" ? name + ".tlb" : name,
            Sha256Hex(bytes), sections[0].Duration, sections[0].Looping, sections.Select(s => s.Pair).ToArray());
    }

    sealed record FoldedPair(ConformancePair Pair, ushort Duration, bool Looping);

    static FoldedPair BuildPair<TTrack, TClip>(ushort index, string fold)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        _ = Timeline<TTrack, TClip>.View(index);
        var copy = Inspection.CopyLanes<TTrack, TClip>(index)
            ?? throw new InvalidOperationException($"bank for pair ({typeof(TTrack).Name}, {typeof(TClip).Name}) did not fold at index {index}.");
        var lanes = copy.Lanes.Select(l => new ConformanceLane(l.Key, l.Forward, l.Backward)).ToArray();
        var clock = new List<ConformanceClock>();
        foreach (var forward in new[] { true, false })
            for (var position = 0; position <= copy.Duration; position++)
            {
                var moved = TimelineMovement.Select(
                    new TimelineState(1, (ushort)position), copy.Duration, copy.Looping, !forward,
                    out var next, out var tick, out _);
                uint effect = 0;
                if (moved) effect = forward ? lanes[0].Forward[tick] : lanes[0].Backward[tick];
                clock.Add(new ConformanceClock(forward, (ushort)position, moved, tick, moved ? next.Position : (ushort)position, effect));
            }
        return new FoldedPair(new ConformancePair(PairRuntime<TTrack, TClip>.Key, fold, lanes, [.. clock]), copy.Duration, copy.Looping);
    }

    public static string Render(ConformanceVector vector)
    {
        var payload = new StringBuilder();
        void Line(string text) => payload.Append(text).Append('\n');
        Line("tlcv1");
        if (vector.SourceKind == "fixture") Line($"fixture {vector.Source}");
        else Line($"stress {vector.Source}");
        Line($"tlbsha256 {vector.TlbSha256}");
        Line($"duration {vector.Duration.ToString(CultureInfo.InvariantCulture)}");
        Line($"looping {(vector.Looping ? 1 : 0).ToString(CultureInfo.InvariantCulture)}");
        Line($"pairs {vector.Pairs.Length.ToString(CultureInfo.InvariantCulture)}");
        for (var p = 0; p < vector.Pairs.Length; p++)
        {
            var pair = vector.Pairs[p];
            Line($"pair {p.ToString(CultureInfo.InvariantCulture)}");
            Line($"key {pair.PairKey:x16}");
            Line($"fold {pair.Fold}");
            Line($"lanes {pair.Lanes.Length.ToString(CultureInfo.InvariantCulture)}");
            for (var l = 0; l < pair.Lanes.Length; l++)
            {
                var lane = pair.Lanes[l];
                Line($"lane {l.ToString(CultureInfo.InvariantCulture)} key {lane.Key:x16} ticks {lane.Forward.Length.ToString(CultureInfo.InvariantCulture)}");
                Line("f " + Words(lane.Forward));
                Line("b " + Words(lane.Backward));
            }
            foreach (var entry in pair.Clock)
                Line($"clock {(entry.Forward ? 'f' : 'b')} {entry.Position.ToString(CultureInfo.InvariantCulture)} " +
                     $"a{(entry.Active ? 1 : 0)} t{entry.Tick.ToString(CultureInfo.InvariantCulture)} " +
                     $"n{entry.Next.ToString(CultureInfo.InvariantCulture)} e{entry.Effect:x8}");
        }
        var body = payload.ToString();
        return body + $"sha256 {Sha256Hex(Encoding.ASCII.GetBytes(body))}\n";
    }

    static string Words(uint[] values)
    {
        var text = new StringBuilder(values.Length * 9);
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) text.Append(' ');
            text.Append(values[i].ToString("x8", CultureInfo.InvariantCulture));
        }
        return text.ToString();
    }

    public static string Sha256Hex(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value);

    public static ConformanceVector Parse(string name, string text)
    {
        var lines = text.Split('\n');
        if (lines[^1] != string.Empty) throw new InvalidOperationException($"{name}{Extension}: missing trailing newline.");
        var bodyLines = lines.Take(lines.Length - 2).ToArray();
        if (!lines[^2].StartsWith("sha256 ", StringComparison.Ordinal))
            throw new InvalidOperationException($"{name}{Extension}: missing sha256 trailer.");
        var body = string.Concat(bodyLines.Select(l => l + "\n"));
        if (lines[^2]["sha256 ".Length..] != Sha256Hex(Encoding.ASCII.GetBytes(body)))
            throw new InvalidOperationException($"{name}{Extension}: sha256 trailer does not cover the payload.");
        var position = 0;
        string Next()
        {
            if (position >= bodyLines.Length) throw new InvalidOperationException($"{name}{Extension}: truncated payload.");
            return bodyLines[position++];
        }
        string[] Fields(string line, int count, string token)
        {
            var fields = line.Split(' ');
            if (fields.Length != count || fields[0] != token)
                throw new InvalidOperationException($"{name}{Extension}: malformed {token} line '{line}'.");
            return fields;
        }
        if (Next() != "tlcv1") throw new InvalidOperationException($"{name}{Extension}: unknown format.");
        var source = Next().Split(' ');
        if (source.Length != 2 || source[0] is not ("fixture" or "stress"))
            throw new InvalidOperationException($"{name}{Extension}: malformed source line.");
        var tlbSha = Fields(Next(), 2, "tlbsha256")[1];
        var duration = ushort.Parse(Fields(Next(), 2, "duration")[1], CultureInfo.InvariantCulture);
        var looping = Fields(Next(), 2, "looping")[1] == "1";
        var pairCount = int.Parse(Fields(Next(), 2, "pairs")[1], CultureInfo.InvariantCulture);
        var pairs = new ConformancePair[pairCount];
        for (var p = 0; p < pairCount; p++)
        {
            Fields(Next(), 2, "pair");
            var pairKey = ulong.Parse(Fields(Next(), 2, "key")[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var fold = Fields(Next(), 2, "fold")[1];
            var laneCount = int.Parse(Fields(Next(), 2, "lanes")[1], CultureInfo.InvariantCulture);
            var lanes = new ConformanceLane[laneCount];
            for (var l = 0; l < laneCount; l++)
            {
                var lane = Fields(Next(), 6, "lane");
                var key = ulong.Parse(lane[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var ticks = int.Parse(lane[5], CultureInfo.InvariantCulture);
                lanes[l] = new ConformanceLane(key, Words(Fields(Next(), ticks + 1, "f")), Words(Fields(Next(), ticks + 1, "b")));
            }
            var clock = new List<ConformanceClock>();
            while (position < bodyLines.Length && bodyLines[position].StartsWith("clock ", StringComparison.Ordinal))
            {
                var entry = Fields(Next(), 7, "clock");
                clock.Add(new ConformanceClock(
                    entry[1] == "f",
                    ushort.Parse(entry[2], CultureInfo.InvariantCulture),
                    entry[3] == "a1",
                    ushort.Parse(entry[4][1..], CultureInfo.InvariantCulture),
                    ushort.Parse(entry[5][1..], CultureInfo.InvariantCulture),
                    uint.Parse(entry[6][1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
            }
            pairs[p] = new ConformancePair(pairKey, fold, lanes, [.. clock]);
        }
        if (position != bodyLines.Length) throw new InvalidOperationException($"{name}{Extension}: unexpected trailing lines.");
        return new ConformanceVector(name, source[0], source[1], tlbSha, duration, looping, pairs);
    }

    static uint[] Words(string[] fields)
    {
        var words = new uint[fields.Length - 1];
        for (var i = 0; i < words.Length; i++)
            words[i] = uint.Parse(fields[i + 1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return words;
    }

    static string BlendJson() => """
    {
      "name": "stress_blend",
      "duration": 40,
      "loop": false,
      "tracks": [
        {
          "name": "job",
          "namespace": "Tlb",
          "type": "JobTrack",
          "data": { "Multiplier": 3 },
          "clips": [
            { "name": "a", "namespace": "Tlb", "type": "JobClip", "start": 0, "end": 11, "data": { "Amount": 2, "Steps": 1 } },
            { "name": "b", "namespace": "Tlb", "type": "JobClip", "start": 8, "end": 19, "data": { "Amount": 9, "Steps": 2 } },
            { "namespace": "Tlb", "type": "JobClip", "start": 19, "end": 33, "data": { "Amount": 4, "Steps": 1 } },
            { "namespace": "Tlb", "type": "JobClip", "start": 33, "end": 40, "data": { "Amount": 6, "Steps": 3 } }
          ]
        },
        {
          "name": "amb",
          "namespace": "Tlb",
          "type": "FoldTrack",
          "data": { "Scale": 5 },
          "clips": [
            { "namespace": "Tlb", "type": "FoldClip", "start": 0, "end": 20, "data": { "Height": 11 } },
            { "namespace": "Tlb", "type": "FoldClip", "start": 20, "end": 40, "data": { "Height": 13 } }
          ]
        }
      ]
    }
    """;

    static string SegmentedJson()
    {
        const int duration = 2111, span = 81;
        var text = new StringBuilder();
        text.Append("{\"name\":\"stress_segmented\",\"duration\":").Append(duration)
            .Append(",\"loop\":true,\"tracks\":[{\"name\":\"amb\",\"namespace\":\"Tlb\",\"type\":\"FoldTrack\",\"data\":{\"Scale\":4},\"clips\":[");
        for (var start = 0; start < duration; start += span)
        {
            var end = Math.Min(start + span, duration);
            if (start > 0) text.Append(',');
            text.Append("{\"namespace\":\"Tlb\",\"type\":\"FoldClip\",\"start\":").Append(start)
                .Append(",\"end\":").Append(end)
                .Append(",\"data\":{\"Height\":").Append(3 + start / span * 7 % 23).Append("}}");
        }
        text.Append("]}]}");
        return text.ToString();
    }

    static string TinyJson() => """
    {
      "name": "stress_tiny",
      "duration": 1,
      "loop": false,
      "tracks": [
        {
          "name": "job",
          "namespace": "Tlb",
          "type": "JobTrack",
          "data": { "Multiplier": 2 },
          "clips": [
            { "namespace": "Tlb", "type": "JobClip", "start": 0, "end": 1, "data": { "Amount": 5, "Steps": 1 } }
          ]
        }
      ]
    }
    """;
}
