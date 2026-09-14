using System.Runtime;

namespace PlaybackPrototype;

internal static class Program
{
    public static int Main(string[] args)
    {
        var selected = args.Length > 0 ? args : ["sweep", "movement", "floor"];
        Console.WriteLine($"runtime={Environment.Version} gc={GCSettings.LatencyMode} serverGc={GCSettings.IsServerGC} processorCount={Environment.ProcessorCount}");
        Console.WriteLine($"machine={Environment.MachineName} os={Environment.OSVersion.VersionString}");

        var lanes = new List<Func<LaneResult>>();
        if (selected.Contains("sweep"))
        {
            Console.WriteLine($"asset=move-loop sha256={Fixtures.Hash(Fixtures.Bake(Fixtures.MoveLoop))}");
            lanes.Add(() => new SweepLane { Fixture = Fixtures.MoveLoop }.Run());
        }
        if (selected.Contains("movement"))
            lanes.Add(() => new MovementLane().Run());
        if (selected.Contains("floor"))
            lanes.Add(() => new FloorLane().Run());

        var failures = 0;
        foreach (var lane in lanes)
        {
            var result = lane();
            var facadePerRow = result.FacadeNsPerPass / result.Rows;
            var tablePerRow = result.TableNsPerPass / result.Rows;
            var parity = result.ParityOk ? "ok" : "MISMATCH";
            var tableText = double.IsNaN(tablePerRow) ? "-" : $"{tablePerRow:F3}";
            var ratioText = double.IsNaN(tablePerRow) ? "-" : $"{LaneSupport.Ratio(result.FacadeNsPerPass, result.TableNsPerPass):F2}x";
            Console.WriteLine(
                $"lane={result.Lane} kind={result.Kind} rows={result.Rows} " +
                $"facade={result.FacadeNsPerPass:F0}ns/pass ({facadePerRow:F3} ns/row) " +
                $"table={Format(result.TableNsPerPass)}ns/pass ({tableText} ns/row) ratio={ratioText} " +
                $"allocB/pass: facade={result.FacadeAllocBytesPerPass:F0} table={Format(result.TableAllocBytesPerPass)} " +
                $"parity={parity} {result.Detail}");
            if (!result.ParityOk) failures++;
        }

        return failures == 0 ? 0 : 1;
    }

    private static string Format(double value) => double.IsNaN(value) ? "-" : $"{value:F0}";
}
