using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using Tl;
using Tl.TestSupport;

namespace InternContention;

public readonly record struct ContentionClip(float Amount);
public readonly record struct ContentionTrack(float Scale) : IBlend<ContentionClip>
{
    public void Blend(in ContentionClip first, in ContentionClip second, float factor, out ContentionClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class ContentionConsumer
{
    [ModuleInitializer]
    internal static void Install()
        => PairRuntime<ContentionTrack, ContentionClip>.ConsumeDispatch(&Row, "contention\0Row");

    static long calls;

    private static void Row(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
        => Interlocked.Increment(ref calls);

    internal static long Calls => Volatile.Read(ref calls);
}

public static class Program
{
    const int DispatchThreads = 8;
    const int Seconds = 3;
    const int LoaderContents = 256;
    const int SampleEvery = 64;

    static byte[] Anchor() => new DomainBaker()
        .Track<ContentionTrack, ContentionClip>(new ContentionTrack(2f))
        .Clip(0, 0, 64, new ContentionClip(1f))
        .Bake();

    static byte[][] LoadPool()
    {
        var pool = new byte[LoaderContents][];
        for (var i = 0; i < pool.Length; i++)
            pool[i] = new DomainBaker()
                .Track<ContentionTrack, ContentionClip>(new ContentionTrack(1f + i))
                .Clip(0, 0, (uint)(8 + i % 57), new ContentionClip(i))
                .Bake();
        return pool;
    }

    sealed class Worker(double seconds, ushort id)
    {
        internal long iterations;
        internal long[] samples = new long[4096];
        internal int sampled;

        internal void Run()
        {
            var ids = new[] { id };
            var positions = new[] { (ushort)3 };
            var deadline = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);
            var sw = new Stopwatch();
            var n = 0;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                sw.Restart();
                Timeline<ContentionTrack, ContentionClip>.Apply<ushort, ushort>(ids, positions, true);
                sw.Stop();
                iterations++;
                if (n++ % SampleEvery == 0 && sampled < samples.Length)
                    samples[sampled++] = sw.ElapsedTicks;
            }
        }
    }

    static (double p50, double p99, long iterations) Median(string label, bool load, byte[][] pool)
    {
        using var anchor = TimelineAsset.Of(TimelineAsset.Load(Anchor()));
        var id = anchor.Index;
        var workers = new Worker[DispatchThreads];
        for (var i = 0; i < workers.Length; i++)
            workers[i] = new Worker(Seconds, id);
        var threads = workers.Select(w => new Thread(w.Run) { IsBackground = true }).ToArray();
        Thread? loader = null;
        var loading = true;
        if (load)
        {
            loader = new Thread(() =>
            {
                var n = 0;
                while (Volatile.Read(ref loading))
                {
                    using var lease = TimelineAsset.Of(TimelineAsset.Load(pool[n++ % pool.Length]));
                }
            })
            { IsBackground = true };
        }
        var rowsBefore = ContentionConsumer.Calls;
        var start = Stopwatch.GetTimestamp();
        foreach (var t in threads) t.Start();
        loader?.Start();
        foreach (var t in threads) t.Join();
        var elapsed = Stopwatch.GetTimestamp() - start;
        Volatile.Write(ref loading, false);
        loader?.Join(2000);
        var rows = ContentionConsumer.Calls - rowsBefore;
        var all = workers.SelectMany(w => w.samples.Take(w.sampled)).OrderBy(v => v).ToArray();
        double P(double q) => all.Length == 0 ? 0 : all[(int)(q * (all.Length - 1))] * 1_000_000_000.0 / Stopwatch.Frequency;
        var iterations = workers.Sum(w => w.iterations);
        Console.WriteLine($"{label}: {iterations} dispatches on {DispatchThreads} threads in {elapsed * 1.0 / Stopwatch.Frequency:F2}s, {iterations * 1.0 * Stopwatch.Frequency / elapsed:F0}/s, per-call p50 {P(0.50):F0} ns, p99 {P(0.99):F0} ns, dispatch rows {rows} (ratio {rows * 1.0 / iterations:F2})");
        return (P(0.50), P(0.99), iterations);
    }

    static void Main(string[] args)
    {
        var pool = LoadPool();
        var quiet = Median("quiet", false, pool);
        var loaded = Median("loaded", true, pool);
        var again = Median("quiet-again", false, pool);
        var ratioP50 = loaded.p50 / Math.Max(1, Math.Min(quiet.p50, again.p50));
        var ratioP99 = loaded.p99 / Math.Max(1, Math.Min(quiet.p99, again.p99));
        var ratioThroughput = loaded.iterations / (double)Math.Max(1, Math.Max(quiet.iterations, again.iterations));
        Console.WriteLine($"contention: loaded/quiet p50 {ratioP50:F2}x, p99 {ratioP99:F2}x, throughput {ratioThroughput:F2}x");
        var json = new StringBuilder();
        json.AppendLine("{");
        json.AppendLine($"  \"dispatchThreads\": {DispatchThreads}, \"seconds\": {Seconds}, \"loaderContents\": {LoaderContents},");
        json.AppendLine($"  \"quiet\": {{\"p50Ns\": {quiet.p50:F1}, \"p99Ns\": {quiet.p99:F1}, \"iterations\": {quiet.iterations}}},");
        json.AppendLine($"  \"quietAgain\": {{\"p50Ns\": {again.p50:F1}, \"p99Ns\": {again.p99:F1}, \"iterations\": {again.iterations}}},");
        json.AppendLine($"  \"loaded\": {{\"p50Ns\": {loaded.p50:F1}, \"p99Ns\": {loaded.p99:F1}, \"iterations\": {loaded.iterations}}},");
        json.AppendLine($"  \"ratios\": {{\"p50\": {ratioP50:F3}, \"p99\": {ratioP99:F3}, \"throughput\": {ratioThroughput:F3}}}");
        json.AppendLine("}");
        var outDir = Path.Combine(AppContext.BaseDirectory, "results");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, args.Length > 0 ? args[0] : "run.json"), json.ToString());
        File.WriteAllText(Path.Combine(outDir, args.Length > 0 ? args[0] : "run.json") + ".txt",
            $"quiet p50={quiet.p50:F0} p99={quiet.p99:F0} iters={quiet.iterations}\nquiet-again p50={again.p50:F0} p99={again.p99:F0} iters={again.iterations}\nloaded p50={loaded.p50:F0} p99={loaded.p99:F0} iters={loaded.iterations}\nratios p50={ratioP50:F3} p99={ratioP99:F3}\n");
        if (args.Contains("--require") && (ratioP99 > 1.50 || ratioThroughput < 0.80))
        {
            Console.WriteLine("contention receipt FAILED: loaded dispatch latency moved with the intern gate.");
            Environment.Exit(1);
        }
    }
}
