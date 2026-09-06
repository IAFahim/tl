using System.Runtime.CompilerServices;

namespace Tl.Algorithms;

internal static class Verification
{
    public static void Run<T>() where T : struct, IGenerated
    {
        var data = T.Create();
        var state = 0;
        var cursor = 0;
        long checks = 0;

        for (var tick = 0; tick < data.Duration; tick++)
        {
            TraceSink expected = default;
            data.Oracle(tick, ref expected);
            TraceSink actual = default;
            data.Emit(data.Binary(tick), tick, ref actual);
            Check(expected, actual, tick, "binary");
            actual = default;
            data.Emit(data.Dense[tick], tick, ref actual);
            Check(expected, actual, tick, "dense");
            actual = default;
            data.Emit(data.Rank(tick), tick, ref actual);
            Check(expected, actual, tick, "rank");
            actual = default;
            T.Tree(tick, data.Payloads, ref actual);
            Check(expected, actual, tick, "tree");
            actual = default;
            data.Cursor(tick, ref cursor, ref actual);
            Check(expected, actual, tick, "cursor");
            actual = default;
            T.State(tick, ref state, data.Payloads, ref actual);
            Check(expected, actual, tick, "state");
            checks += 6;
        }

        state = 0;
        cursor = 0;
        var previous = -1;

        for (var i = 0; i < Sampling<T>.Operations; i++)
        {
            var tick = (int)((long)i * 7 % data.Duration);
            if (tick < previous)
                state = cursor = 0;
            previous = tick;
            TraceSink expected = default;
            data.Oracle(tick, ref expected);
            TraceSink actual = default;
            T.State(tick, ref state, data.Payloads, ref actual);
            Check(expected, actual, tick, "state/skip/wrap");
            actual = default;
            data.Cursor(tick, ref cursor, ref actual);
            Check(expected, actual, tick, "cursor/skip/wrap");
            checks += 2;
        }

        var regionBytes = data.Regions.Length * Unsafe.SizeOf<Region>();
        Console.WriteLine($"{typeof(T).Name}: {checks:N0} exact trace comparisons passed; " +
            $"{data.Clips.Length} clips; {data.Duration} ticks; {data.Regions.Length} regions; " +
            $"region elements {regionBytes} B; dense elements {data.Dense.Length * 2} B; " +
            $"rank elements {data.Bits.Length * 8 + data.Prefix.Length * 2} B.");

        var playback = new Playback<T> { Step = 1 };
        playback.Setup();
        var reference = playback.Binary();
        var results = new[] { playback.Dense(), playback.Rank(), playback.GeneratedTree(), playback.Cursor(), playback.GeneratedState() };
        foreach (var result in results)
            if (result != reference)
                throw new InvalidOperationException($"Aggregate receipt mismatch: {reference} / {result}.");
        var seeking = new Seeking<T>();
        seeking.Setup();
        reference = seeking.Binary();
        results = [seeking.Dense(), seeking.Rank(), seeking.GeneratedTree()];
        foreach (var result in results)
            if (result != reference)
                throw new InvalidOperationException($"Seek receipt mismatch: {reference} / {result}.");
        Console.WriteLine($"{typeof(T).Name}: benchmark receipts match for playback and random seeks.");
    }

    private static void Check(TraceSink expected, TraceSink actual, int tick, string method)
    {
        if (expected != actual)
            throw new InvalidOperationException($"{method} at {tick}: expected {expected}; got {actual}.");
    }
}
