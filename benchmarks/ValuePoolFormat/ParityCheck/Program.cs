using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tl;
using Tl.Gen.Tlb;

namespace ParityCheck;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("capture" or "compare"))
        {
            Console.Error.WriteLine("usage: ParityCheck <capture|compare> <corpusDir> <receiptPath>");
            return 2;
        }
        var corpusDir = Path.GetFullPath(args[1]);
        var receiptPath = Path.GetFullPath(args[2]);
        if (args[0] == "capture" && Environment.GetEnvironmentVariable("PARITY_SIZES") is "1")
        {
            Sizes(corpusDir, receiptPath);
            return 0;
        }
        var text = Capture(corpusDir, out var assets);
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        if (args[0] == "capture")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(receiptPath)!);
            File.WriteAllText(receiptPath, $"corpus-sha256 {sha}\n{text}", new UTF8Encoding(false));
            Console.WriteLine($"captured assets={assets} sha256={sha}");
            return 0;
        }
        var reference = File.ReadAllLines(receiptPath);
        var expected = reference[0].Split(' ')[^1];
        if (expected == sha)
        {
            Console.WriteLine($"PASS assets={assets} sha256={sha}");
            return 0;
        }
        var candidate = File.ReadAllLines(CreateReceipt(text));
        Console.WriteLine($"FAIL expected {expected} got {sha}");
        var shown = 0;
        for (var i = 1; i < Math.Max(reference.Length, candidate.Length) && shown < 40; i++)
        {
            var before = i < reference.Length ? reference[i] : "<missing>";
            var after = i < candidate.Length ? candidate[i] : "<missing>";
            if (before != after)
            {
                Console.WriteLine($"line {i}:");
                Console.WriteLine($"  reference: {before}");
                Console.WriteLine($"  candidate: {after}");
                shown++;
            }
        }
        return 1;
    }

    private static string CreateReceipt(string text)
    {
        var path = Path.Combine(Path.GetTempPath(), "tlb-parity-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, $"corpus-sha256 x\n{text}", new UTF8Encoding(false));
        return path;
    }

    private static string Capture(string corpusDir, out int assets)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(corpusDir, "manifest.json")), options)!;
        var writer = new StringBuilder(1 << 16);
        assets = 0;
        foreach (var document in manifest.Documents)
        {
            var json = File.ReadAllText(Path.Combine(corpusDir, document.Name));
            writer.Append("A ").Append(document.Name).Append('\n');
            byte[] bytes;
            try
            {
                bytes = TimelineBaker.BakeJson(json);
            }
            catch (BakeDiagnosticException ex)
            {
                writer.Append("B re ").Append(OneLine(ex.Message)).Append('\n');
                continue;
            }
            using var asset = TimelineAsset.Load(bytes);
            for (var pair = 0; pair < Pairs.Count; pair++)
                AppendLane(ref writer, asset, pair);
            var component = new TimelineComponent(asset.Reference);
            for (var position = 0; position <= Duration(asset); position++)
            {
                component.Position = (ushort)position;
                AppendFrames(ref writer, in component);
            }
            assets++;
        }
        return writer.ToString();
    }

    private static void AppendFrames(ref StringBuilder writer, in TimelineComponent component)
    {
        foreach (var frame in Timeline.Query<ManyEntities.MoveTrack, ManyEntities.MoveClip>(in component))
            Line(ref writer, 0, in frame);
        foreach (var frame in Timeline.Query<ManyEntities.PulseTrack, ManyEntities.PulseClip>(in component))
            Line(ref writer, 1, in frame);
        foreach (var frame in Timeline.Query<ManyEntities.WindowTrack, ManyEntities.WindowClip>(in component))
            Line(ref writer, 2, in frame);
        foreach (var frame in Timeline.Query<Fresh.DamageTrack, Fresh.DamageClip>(in component))
            Line(ref writer, 3, in frame);
        foreach (var frame in Timeline.Query<GaGlobalTrack, GaGlobalClip>(in component))
            Line(ref writer, 4, in frame);
        foreach (var frame in Timeline.Query<Tlb.JobTrack, Tlb.JobClip>(in component))
            Line(ref writer, 5, in frame);
        foreach (var frame in Timeline.Query<Tlb.AlphaTrack, Tlb.AlphaClip>(in component))
            Line(ref writer, 6, in frame);
        foreach (var frame in Timeline.Query<Tlb.BlendTrack, Tlb.BlendClip>(in component))
            Line(ref writer, 7, in frame);
        foreach (var frame in Timeline.Query<Tlb.DualTrack, Tlb.DualAlphaClip>(in component))
            Line(ref writer, 8, in frame);
        foreach (var frame in Timeline.Query<Tlb.DualTrack, Tlb.DualBetaClip>(in component))
            Line(ref writer, 9, in frame);
        foreach (var frame in Timeline.Query<Tlb.EchoTrack, Tlb.EchoClip>(in component))
            Line(ref writer, 10, in frame);
        foreach (var frame in Timeline.Query<Play.ScaleTrack, Play.AmountClip>(in component))
            Line(ref writer, 11, in frame);
    }

    private static void Line<TTrack, TClip>(ref StringBuilder writer, int pair, in Frame<TTrack, TClip> frame)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        writer.Append("Q ").Append(frame.TimelineTick).Append(' ').Append(pair).Append(' ')
            .Append((int)frame.Flags).Append(' ').Append(frame.TrackIndex).Append(' ')
            .Append(frame.ClipLength).Append(' ').Append(frame.WithinClip).Append(' ')
            .Append(Bits(frame.Track)).Append(' ').Append(Bits(frame.Clip)).Append('\n');
    }

    private static string Bits<T>(in T value) where T : unmanaged
        => Convert.ToHexString(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));

    private static ushort Duration(TimelineAsset asset)
    {
        using var measured = MeasuredLanes.Measure(asset);
        return measured.Duration;
    }

    private static void AppendLane(ref StringBuilder writer, TimelineAsset asset, int pair)
    {
        try
        {
            switch (pair)
            {
                case Pairs.Move: LaneTable<ManyEntities.MoveTrack, ManyEntities.MoveClip>(ref writer, asset); break;
                case Pairs.Pulse: LaneTable<ManyEntities.PulseTrack, ManyEntities.PulseClip>(ref writer, asset); break;
                case Pairs.Window: LaneTable<ManyEntities.WindowTrack, ManyEntities.WindowClip>(ref writer, asset); break;
                case Pairs.Damage: LaneTable<Fresh.DamageTrack, Fresh.DamageClip>(ref writer, asset); break;
                case Pairs.GaGlobal: LaneTable<GaGlobalTrack, GaGlobalClip>(ref writer, asset); break;
                case Pairs.Job: LaneTable<Tlb.JobTrack, Tlb.JobClip>(ref writer, asset); break;
                case Pairs.Alpha: LaneTable<Tlb.AlphaTrack, Tlb.AlphaClip>(ref writer, asset); break;
                case Pairs.Blend: LaneTable<Tlb.BlendTrack, Tlb.BlendClip>(ref writer, asset); break;
                case Pairs.DualAlpha: LaneTable<Tlb.DualTrack, Tlb.DualAlphaClip>(ref writer, asset); break;
                case Pairs.DualBeta: LaneTable<Tlb.DualTrack, Tlb.DualBetaClip>(ref writer, asset); break;
                case Pairs.Echo: LaneTable<Tlb.EchoTrack, Tlb.EchoClip>(ref writer, asset); break;
                case Pairs.Scale: LaneTable<Play.ScaleTrack, Play.AmountClip>(ref writer, asset); break;
            }
        }
        catch (ArgumentException)
        {
            writer.Append("L ").Append(Pairs.Names[pair]).Append(" absent\n");
        }
    }

    private static void LaneTable<TTrack, TClip>(ref StringBuilder writer, TimelineAsset asset)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        BakedLane<TTrack, TClip>.Bind(asset);
        var forward = new StringBuilder(128);
        var backward = new StringBuilder(128);
        for (var i = 0; i <= BakedLane<TTrack, TClip>.Duration; i++)
        {
            forward.Append(BitConverter.SingleToUInt32Bits(BakedLane<TTrack, TClip>.Effect((ushort)i)).ToString("x8"));
            backward.Append(BitConverter.SingleToUInt32Bits(BakedLane<TTrack, TClip>.InverseEffect((ushort)i)).ToString("x8"));
        }
        writer.Append("L ").Append(Pairs.Names[Pairs.IndexOf<TTrack, TClip>()]).Append(" F ").Append(forward).Append('\n');
        writer.Append("L ").Append(Pairs.Names[Pairs.IndexOf<TTrack, TClip>()]).Append(" B ").Append(backward).Append('\n');
    }

    private static string OneLine(string message) => message.Replace('\r', ' ').Replace('\n', ' ');

    private sealed class Manifest
    {
        public List<Document> Documents { get; set; } = [];
    }

    private sealed class Document
    {
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
    }

    
    private static void Sizes(string corpusDir, string receiptPath)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(corpusDir, "manifest.json")), options)!;
        var writer = new StringBuilder();
        foreach (var document in manifest.Documents)
        {
            try
            {
                var bytes = TimelineBaker.BakeJson(File.ReadAllText(Path.Combine(corpusDir, document.Name)));
                writer.Append(document.Source).Append(",").Append(bytes.Length).AppendLine();
            }
            catch (BakeDiagnosticException ex)
            {
                writer.Append(document.Source).Append(",diagnostic,").AppendLine(OneLine(ex.Message));
            }
        }
        File.WriteAllText(receiptPath, writer.ToString(), new UTF8Encoding(false));
        Console.WriteLine("sizes written");
    }
}
