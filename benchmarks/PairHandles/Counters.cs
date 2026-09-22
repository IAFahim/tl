using System.Diagnostics;
using Tl;

namespace PairHandles;

internal static class Counters
{
    const string Protocol =
        "run under: taskset -c <P-core> perf stat -D <delayMs> -e cycles,instructions,branches,branch-misses,cache-references,cache-misses -- <apphost> --counters <shape> --counters-delay <delayMs> [--counters-rows N] [--counters-iters N]; the printed wall_ns_per_row is the in-window wall clock cross-check against the BenchmarkDotNet medians.";

    public static int Run(string shapeId, int rows, int iters, int delayMs)
    {
        var gold = Host.SlotGoldLane();
        ushort[] positions;
        ushort[] handles;
        float[] effects;
        var scalar = false;
        var fused = false;
        ushort[]? next = null;
        var shapeName = shapeId;
        if (shapeId.StartsWith("fused-"))
        {
            fused = true;
            shapeName = shapeId["fused-".Length..];
        }
        if (Enum.TryParse<ShapeKind>(shapeName, out var kind))
        {
            (positions, handles, effects) = PairHandleBenchmarks.Build(kind, rows);
            if (fused) next = new ushort[rows];
        }
        else if (shapeName is "per-entity-lane" or "per-entity-fused" or "per-entity-apply-step")
        {
            scalar = true;
            handles = new ushort[rows];
            positions = new ushort[rows];
            Array.Fill(handles, gold);
            for (var i = 0; i < rows; i++) positions[i] = (ushort)(i % Host.Duration);
            effects = Seeds.Effects(rows);
        }
        else
        {
            Console.WriteLine($"unknown counters shape '{shapeId}'; shapes: {string.Join(", ", Enum.GetNames<ShapeKind>())}, fused-<shape>, per-entity-lane, per-entity-fused, per-entity-apply-step");
            return 2;
        }

        for (var i = 0; i < 400; i++) Step(scalar, fused, shapeName, gold, handles, positions, next, effects, 0, rows);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Console.WriteLine(Protocol);
        Console.WriteLine($"READY shape={shapeId} rows={rows} iters={iters}");
        Console.Out.Flush();
        var sinceMain = Stopwatch.StartNew();
        var sleep = delayMs - (int)sinceMain.ElapsedMilliseconds;
        if (sleep > 0) Thread.Sleep(sleep);
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < iters; i++) Step(scalar, fused, shapeName, gold, handles, positions, next, effects, 0, rows);
        var nsPerRow = (Stopwatch.GetTimestamp() - start) * 1e9 / Stopwatch.Frequency / ((long)rows * iters);
        Console.WriteLine(
            $"COUNTERS shape={shapeId} rows={rows} iters={iters} total_rows={(long)rows * iters} wall_ns_per_row={nsPerRow.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}");
        return 0;
    }

    static void Step(bool scalar, bool fused, string shapeId, ushort gold, ushort[] handles, ushort[] positions, ushort[]? next, float[] effects, int start, int count)
    {
        if (!scalar && fused)
        {
            var n = (next ?? throw new InvalidOperationException("fused arms need a next column.")).AsSpan(start, count);
            Timeline<LaneTrack, LaneClip>.Apply(handles.AsSpan(start, count), positions.AsSpan(start, count), n, true, effects.AsSpan(start, count));
            return;
        }
        if (!scalar)
        {
            var h = handles.AsSpan(start, count);
            var p = positions.AsSpan(start, count);
            var e = effects.AsSpan(start, count);
            Timeline<LaneTrack, LaneClip>.Apply(h, p, true, e);
            Timeline.Advance(h, p, true);
            return;
        }
        switch (shapeId)
        {
            case "per-entity-lane":
                for (var i = start; i < start + count; i++)
                    Timeline<LaneTrack, LaneClip>.Apply(gold, positions[i], true, ref effects[i]);
                break;
            case "per-entity-fused":
                for (var i = start; i < start + count; i++)
                    Timeline<LaneTrack, LaneClip>.Apply(gold, new ReadOnlySpan<ushort>(in positions[i]), new Span<ushort>(ref positions[i]), true, new Span<float>(ref effects[i]));
                break;
            case "per-entity-apply-step":
                for (var i = start; i < start + count; i++)
                {
                    Timeline<LaneTrack, LaneClip>.Apply(gold, new ReadOnlySpan<ushort>(in positions[i]), true, new Span<float>(ref effects[i]));
                    Timeline.Advance(gold, new Span<ushort>(ref positions[i]), true);
                }
                break;
        }
    }
}
