using System.Globalization;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Tl;
using Tl.Gen.Tlb;

namespace Tl.ValuePoolFormat;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "--verify")
            return Verify();

        var label = "validated";
        var labelIndex = Array.IndexOf(args, "--run");
        if (labelIndex >= 0 && labelIndex + 1 < args.Length)
            label = args[labelIndex + 1];

        var artifacts = Path.Combine(Directory.GetCurrentDirectory(), "benchmarks", "ValuePoolFormat", "results", label);
        if (!Directory.Exists(Path.GetDirectoryName(artifacts)))
            artifacts = Path.Combine(AppContext.BaseDirectory, "results", label);
        var config = ManualConfig.CreateEmpty()
            .AddJob(Job.Default.WithWarmupCount(8).WithIterationCount(16).WithIterationTime(Perfolizer.Horology.TimeInterval.FromMilliseconds(200)))
            .AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max, StatisticColumn.P90)
            .AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
            .WithArtifactsPath(artifacts)
            .AddExporter(BenchmarkDotNet.Exporters.Json.JsonExporter.Full)
            .WithSummaryStyle(SummaryStyle.Default.WithCultureInfo(CultureInfo.InvariantCulture));
        BenchmarkRunner.Run([typeof(QueryScan), typeof(LaneApply)], config, args.Where(a => a != "--run" && a != label).ToArray());
        return 0;
    }

    private static int Verify()
    {
        var bytes = TimelineBaker.BakeJson(Host.DualBlendJson);
        Console.WriteLine($"dual-blend asset sha256 {Host.Checksum(bytes)}");
        Console.WriteLine($"query checksum dual-alpha {Host.QueryChecksum(Host.DualBlendAsset, 64, 0)}");
        Console.WriteLine($"query checksum dual-beta  {Host.QueryChecksum(Host.DualBlendAsset, 64, 1)}");
        Console.WriteLine($"query checksum blend      {Host.QueryChecksum(Host.DualBlendAsset, 64, 2)}");

        var lanePositions = new ushort[Host.LaneRows];
        var laneEffects = new float[Host.LaneRows];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < Host.LaneRows; i++)
        {
            lanePositions[i] = (ushort)(i % 1024);
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            laneEffects[i] = (float)((state >> 11) / 9007199254740992d) * 64f - 32f;
        }
        Timeline<LaneBench.LaneTrack, LaneBench.LaneClip>.Slot(Host.LaneAsset);
        Timeline<LaneBench.LaneTrack, LaneBench.LaneClip>.Advance(Host.LaneAsset, lanePositions, true, laneEffects);
        var positionFnv = 14695981039346656037ul;
        var effectFnv = 14695981039346656037ul;
        foreach (var position in lanePositions)
            positionFnv = (positionFnv ^ position) * 1099511628211;
        foreach (var effect in laneEffects)
            effectFnv = (effectFnv ^ BitConverter.SingleToUInt32Bits(effect)) * 1099511628211;
        Console.WriteLine($"lane 100,000 rows staggered forward PASS pos=0x{positionFnv:x16} eff=0x{effectFnv:x16}");
        Console.WriteLine($"positions={string.Join(',', lanePositions.Take(8))} first-effects={string.Join(',', laneEffects.Take(4).Select(v => v.ToString("R", CultureInfo.InvariantCulture)))}");
        return 0;
    }
}
