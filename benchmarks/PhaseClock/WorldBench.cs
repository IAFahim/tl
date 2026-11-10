using System.Diagnostics;

namespace PhaseClock;

public static class WorldBench
{
    const int N = 1024;
    const int Duration = 40;
    const float Value = 6f;
    const int OracleTicks = 97;
    const int WarmupTicks = 2000;
    const int Rounds = 7;
    const int TicksPerRound = 10000;
    const int Off = 37;
    const double BaselineNs = 14.96;

    public static int RunWorld()
    {
        var lane = Data.ConstantLane(Data.D40, Value);
        var oracleHash = OracleHash();
        using var library = BuildLibrary(lane);
        var worldHash = RunWorldOracle(library);
        var kernelHash = RunKernelOracle();
        var worldPass = worldHash == oracleHash;
        var kernelPass = kernelHash == oracleHash;
        Console.WriteLine($"worldbench uniform-dense N={N} duration={Duration} value={Value}");
        Console.WriteLine($"oracle={oracleHash:x16} world={worldHash:x16} kernel={kernelHash:x16}");
        Console.WriteLine(worldPass ? "WORLD PASS" : "WORLD FAIL");
        Console.WriteLine(kernelPass ? "KERNEL PASS" : "KERNEL FAIL");
        var worldNs = MeasureWorld(library);
        var kernelNs = MeasureKernel();
        Console.WriteLine($"world ns/entity={worldNs:F3} kernel ns/entity={kernelNs:F3} baseline={BaselineNs:F2}");
        Console.WriteLine($"gain world vs baseline {BaselineNs:F2} = {BaselineNs / worldNs:F2}x kernel vs baseline = {BaselineNs / kernelNs:F2}x");
        return worldPass && kernelPass ? 0 : 1;
    }

    static Tl.Library BuildLibrary(float[] lane)
    {
        var step = new Tl.StepSpec(0u, (uint)Duration, 0u, 1ul, lane, lane);
        var timeline = new Tl.TimelineSpec((uint)Duration, true, new[] { step });
        var blob = Tl.LibraryWriter.Write(new[] { timeline });
        var result = Tl.Library.Verify(blob);
        var library = result.Library;
        if (!result.Ok || library is null)
            throw new InvalidOperationException($"Library.Verify failed: {result.Error} at {result.ErrorOffset}");
        return library;
    }

    static ulong OracleHash()
    {
        var expected = new float[N];
        for (var f = 0; f < OracleTicks; f++)
            for (var i = 0; i < N; i++)
                expected[i] += Value;
        return Checker.HashSlice(expected, 0, N);
    }

    static ulong RunWorldOracle(Tl.Library library)
    {
        using var world = new Tl.World(library, N);
        var id = new Tl.TimelineId(1);
        for (var i = 0; i < N; i++)
            world.Requests.Start(new Tl.Row(i), id, 0);
        for (var f = 0; f < OracleTicks; f++)
            world.Tick(Tl.Direction.Forward);
        return Checker.HashSlice(world.Accumulator, 0, N);
    }

    static ulong RunKernelOracle()
    {
        var acc = new float[N + Off + 64];
        for (var f = 0; f < OracleTicks; f++)
            Kernels.BroadcastDense(acc, Off, Value, N);
        return Checker.HashSlice(acc, Off, N);
    }

    static double MeasureWorld(Tl.Library library)
    {
        using var world = new Tl.World(library, N);
        var id = new Tl.TimelineId(1);
        for (var i = 0; i < N; i++)
            world.Requests.Start(new Tl.Row(i), id, 0);
        for (var f = 0; f < WarmupTicks; f++)
            world.Tick(Tl.Direction.Forward);
        var samples = new double[Rounds];
        double sink = 0;
        for (var r = 0; r < Rounds; r++)
        {
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < TicksPerRound; f++)
                world.Tick(Tl.Direction.Forward);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / TicksPerRound / N;
            sink += Checker.HashSlice(world.Accumulator, 0, N) & 0xff;
        }
        Array.Sort(samples);
        Console.WriteLine($"world sink={sink:F0}");
        return samples[0];
    }

    static double MeasureKernel()
    {
        var acc = new float[N + Off + 64];
        for (var f = 0; f < WarmupTicks; f++)
            Kernels.BroadcastDense(acc, Off, Value, N);
        var samples = new double[Rounds];
        double sink = 0;
        for (var r = 0; r < Rounds; r++)
        {
            Array.Clear(acc, Off, N);
            var sw = Stopwatch.StartNew();
            for (var f = 0; f < TicksPerRound; f++)
                Kernels.BroadcastDense(acc, Off, Value, N);
            sw.Stop();
            samples[r] = sw.Elapsed.TotalSeconds * 1e9 / TicksPerRound / N;
            sink += Checker.HashSlice(acc, Off, N) & 0xff;
        }
        Array.Sort(samples);
        Console.WriteLine($"kernel sink={sink:F0}");
        return samples[0];
    }
    public static int RunWorldDensePlayer()
    {
        const int players = 100;
        const int perPlayer = 150;
        const int duration = 40;
        var lane = Data.ConstantLane(duration, Value);
        var specs = new Tl.TimelineSpec[perPlayer];
        for (var t = 0; t < perPlayer; t++)
        {
            var step = new Tl.StepSpec(0u, (uint)duration, 0u, 1ul, lane, lane);
            specs[t] = new Tl.TimelineSpec((uint)duration, true, new[] { step });
        }
        var blob = Tl.LibraryWriter.Write(specs);
        var result = Tl.Library.Verify(blob);
        using var library = result.Library!;
        var expected = new float[players];
        for (var i = 0; i < players; i++) expected[i] = perPlayer * OracleTicks * Value;
        var oracleHash = Checker.HashSlice(expected, 0, players);
        using var world = new Tl.World(library, players);
        var req = world.Requests;
        for (var r = 0; r < players; r++)
            for (var t = 0; t < perPlayer; t++)
                req.Start(new Tl.Row(r), new Tl.TimelineId((ushort)(t + 1)), 0);
        for (var f = 0; f < OracleTicks; f++) world.Tick(Tl.Direction.Forward);
        var worldHash = Checker.HashSlice(world.Accumulator, 0, players);
        var pass = worldHash == oracleHash;
        Console.WriteLine($"worldbench dense-player players={players} perPlayer={perPlayer} duration={duration} total={players * perPlayer}");
        Console.WriteLine($"oracle={oracleHash:x16} world={worldHash:x16}");
        Console.WriteLine(pass ? "WORLD PASS" : "WORLD FAIL");
        var sw = Stopwatch.StartNew();
        const int ticks = 2000;
        for (var f = 0; f < ticks; f++) world.Tick(Tl.Direction.Forward);
        sw.Stop();
        var msPerTick = sw.Elapsed.TotalSeconds * 1e3 / ticks;
        var visits = players * perPlayer;
        var nsPerActive = msPerTick * 1e6 / visits;
        Console.WriteLine($"world ms/tick={msPerTick:F4} ns/active-visit={nsPerActive:F3} visits={visits}");
        return pass ? 0 : 1;
    }
}
