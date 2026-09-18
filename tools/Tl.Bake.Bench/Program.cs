using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tl;
using Tl.Gen.Tlb;

namespace Tl.Bake.Bench;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "corpus")
            return CorpusCommand(args);
        if (args.Length >= 2 && args[0] == "parity")
            return ParityCommand(args);
        if (args.Length >= 2 && args[0] == "timing")
            return TimingCommand(args);
        return 1;
    }

    private static int CorpusCommand(string[] args)
    {
        string? directory = ValueOf(args, "--out");
        Corpus.GenerateAll(directory);
        Console.WriteLine(Corpus.Describe(Corpus.GamePath(directory).Path, Corpus.GamePath(directory).Description));
        Console.WriteLine(Corpus.Describe(Corpus.SmallPath(directory).Path, Corpus.SmallPath(directory).Description));
        return 0;
    }

    private static string? ValueOf(string[] args, string name)
    {
        for (var i = 1; i < args.Length - 1; i++)
            if (args[i] == name)
                return args[i + 1];
        return null;
    }

    private static int ParityCommand(string[] args)
    {
        var ok = true;
        foreach (var path in args[1..])
        {
            var bytes = File.ReadAllBytes(path);
            var text = Encoding.UTF8.GetString(bytes);
            var legacy = ReceiptOf(() => TimelineBaker.BakeJsonLegacy(text, new BakerAssemblyResolver()));
            var fastString = ReceiptOf(() => TimelineBaker.BakeJson(text, new BakerAssemblyResolver()));
            var fastBytes = ReceiptOf(() => TimelineBakerFast.BakeJsonUtf8(bytes, new BakerAssemblyResolver()));
            var simd = SimdReceiptOf(bytes);
            var equal = legacy == fastString && legacy == fastBytes && simd.Receipt == legacy;
            ok &= equal;
            Console.WriteLine($"{path}: {(equal ? "IDENTICAL" : "DIVERGED")}");
            Console.WriteLine($"  legacy {legacy}");
            Console.WriteLine($"  string {fastString}");
            Console.WriteLine($"  bytes  {fastBytes}");
            Console.WriteLine($"  simd   {simd.Receipt} fastPathTaken={simd.Taken}");
        }
        return ok ? 0 : 1;
    }

    private static (string Receipt, bool Taken) SimdReceiptOf(byte[] bytes)
    {
        try
        {
            var taken = TimelineBakerSimd.TryParseFast(bytes, new BakerAssemblyResolver(), out var doc);
            if (!taken)
                return ("FALLBACK", false);
            return ($"OK {Convert.ToHexString(SHA256.HashData(TimelineBakerFastCore.BakeFast(doc, new BakerAssemblyResolver()))).ToLowerInvariant()}", true);
        }
        catch (Exception ex)
        {
            return ($"REJECTED {ex.GetType().Name}: {ex.Message}", true);
        }
    }

    private static string ReceiptOf(Func<byte[]> bake)
    {
        try
        {
            return $"OK {Convert.ToHexString(SHA256.HashData(bake())).ToLowerInvariant()}";
        }
        catch (Exception ex)
        {
            return $"REJECTED {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static int TimingCommand(string[] args)
    {
        var gamePath = args[1];
        var rounds = ParsedValueOf(args, "--rounds", 3);
        var reps = ParsedValueOf(args, "--reps", 5);
        var core = ParsedValueOf(args, "--core", 2);

        if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows())
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1 << core);
        var load1 = File.ReadLines("/proc/loadavg").First().Split(' ')[0];

        Console.WriteLine($"corpus {Corpus.Describe(gamePath, Corpus.GamePath(null).Description)}");
        Console.WriteLine($"host {Environment.ProcessorCount} cores, loadavg1 {load1}, pinned core {core}, rounds {rounds}, reps {reps}, best-of reported (max in parens)");
        Console.WriteLine($"dotnet {Environment.Version}, gc {(System.Runtime.GCSettings.IsServerGC ? "server" : "workstation")}");

        WarmUp(gamePath);

        var legacyTotal = new Stage();
        var byteRead = new Stage();
        var byteParse = new Stage();
        var byteBake = new Stage();
        var byteTotal = new Stage();
        var loadStage = new Stage();
        var simdScan = new Stage();
        var simdParse = new Stage();
        var simdBake = new Stage();
        var simdTotal = new Stage();

        for (var round = 0; round < rounds; round++)
        {
            for (var rep = 0; rep < reps; rep++)
            {
                byteRead.Take(() => File.ReadAllBytes(gamePath));
                var bytes = File.ReadAllBytes(gamePath);

                byteParse.Take(() => TimelineBakerFast.ParseFast(bytes, new BakerAssemblyResolver()));
                var byteDoc = TimelineBakerFast.ParseFast(bytes, new BakerAssemblyResolver());
                byteBake.Take(() => TimelineBakerFastCore.BakeFast(byteDoc, new BakerAssemblyResolver()));

                byteTotal.Take(() => TimelineBaker.BakeJson(File.ReadAllBytes(gamePath), new BakerAssemblyResolver()));
                legacyTotal.Take(() => TimelineBaker.BakeJsonLegacy(File.ReadAllText(gamePath, Encoding.UTF8), new BakerAssemblyResolver()));

                simdScan.Take(() => TimelineBakerSimd.Scan(bytes));
                simdParse.Take(() =>
                {
                    if (!TimelineBakerSimd.TryParseFast(bytes, new BakerAssemblyResolver(), out var simdDoc))
                        throw new InvalidOperationException("simd fast path did not accept the corpus");
                    _ = simdDoc;
                });
                var simdDocForBake = TimelineBakerSimd.TryParseFast(bytes, new BakerAssemblyResolver(), out var simdParsed)
                    ? simdParsed
                    : throw new InvalidOperationException("simd fast path did not accept the corpus");
                simdBake.Take(() => TimelineBakerFastCore.BakeFast(simdDocForBake, new BakerAssemblyResolver()));
                simdTotal.Take(() => TimelineBakerFast.BakeJsonUtf8(bytes, new BakerAssemblyResolver()));

                var baked = TimelineBakerFast.BakeJsonUtf8(bytes, new BakerAssemblyResolver());
                loadStage.Take(() =>
                {
                    _ = TimelineAsset.Load(baked);
                });
            }
            Console.WriteLine($"round {round} done");
        }

        Console.WriteLine();
        Console.WriteLine("| stage | legacy-oracle ms | ref-fused ms | simd ms |");
        Console.WriteLine("|---|---:|---:|---:|");
        Console.WriteLine($"| read | - | {byteRead.MsText} | {byteRead.MsText} |");
        Console.WriteLine($"| scan | - | - | {simdScan.MsText} |");
        Console.WriteLine($"| parse | - | {byteParse.MsText} | {simdParse.MsText} |");
        Console.WriteLine($"| bake | - | {byteBake.MsText} | {simdBake.MsText} |");
        Console.WriteLine($"| total bake | {legacyTotal.MsText} | {byteTotal.MsText} | {simdTotal.MsText} |");
        Console.WriteLine($"| load (context) | - | {loadStage.MsText} | - |");
        Console.WriteLine();
        Console.WriteLine($"allocation per total bake: legacy {legacyTotal.MbText} MB, ref {byteTotal.MbText} MB, simd {simdTotal.MbText} MB; simd parse {simdParse.MbText} MB (scan {simdScan.MbText} MB)");
        return 0;
    }

    private static void WarmUp(string gamePath)
    {
        var bytes = File.ReadAllBytes(gamePath);
        var text = Encoding.UTF8.GetString(bytes);
        for (var i = 0; i < 2; i++)
        {
            TimelineBakerFast.BakeJsonUtf8(bytes, new BakerAssemblyResolver());
            TimelineBaker.BakeJson(text, new BakerAssemblyResolver());
            TimelineBakerFast.ParseFast(bytes, new BakerAssemblyResolver());
            TimelineBakerSimd.Scan(bytes);
        }
    }

    private static int ParsedValueOf(string[] args, string name, int fallback)
    {
        var raw = ValueOf(args, name);
        return raw != null && int.TryParse(raw, out var value) ? value : fallback;
    }

    private sealed class Stage
    {
        private readonly List<double> _ms = [];
        private readonly List<double> _mb = [];

        internal void Take(Action action)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            action();
            _ms.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            _mb.Add((GC.GetAllocatedBytesForCurrentThread() - allocatedBefore) / (1024.0 * 1024.0));
        }

        internal string MsText => Format(_ms);
        internal string MbText => Format(_mb);

        private static string Format(List<double> values) =>
            values.Count == 0
                ? "-"
                : $"{values.Min():0.00} ({values.Max():0.00})";
    }
}
