using System.Diagnostics;
using Tl;

namespace LiveBatch;

internal static class Counters
{
    const string Protocol =
        "run under: taskset -c <P-core> perf stat -D <delayMs> -e cycles,instructions,branches,branch-misses,cache-references,cache-misses -- <apphost> --counters <shape> --counters-delay <delayMs> [--counters-rows N] [--counters-iters N]; the printed wall_ns_per_row is the in-window wall clock cross-check against the BenchmarkDotNet medians.";

    internal static int Run(string shapeId, int rows, int iters, int delayMs)
    {
        var bench = new LiveBatchBenchmarksShim(rows);
        Action step = shapeId switch
        {
            "a" => bench.A,
            "b-apply" => bench.BApply,
            "b-prepareplay" => bench.BPreparePlay,
            "b-chunk128" => bench.BChunk,
            "c" => bench.C,
            "d" => bench.D,
            _ => throw new ArgumentException($"unknown counters shape '{shapeId}'; want a, b-apply, b-prepareplay, b-chunk128, c, d."),
        };
        for (var i = 0; i < 400; i++) step();
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
        for (var i = 0; i < iters; i++) step();
        var nsPerRow = (Stopwatch.GetTimestamp() - start) * 1e9 / Stopwatch.Frequency / ((long)rows * iters);
        Console.WriteLine(
            $"COUNTERS shape={shapeId} rows={rows} iters={iters} total_rows={(long)rows * iters} wall_ns_per_row={nsPerRow.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}");
        return 0;
    }

    sealed class LiveBatchBenchmarksShim
    {
        readonly ushort[] _a;
        readonly ushort[] _b;
        readonly ushort[] _c;
        readonly ushort[] _d;
        readonly ushort[] _clocks;
        readonly float[] _lift;
        readonly float[] _height;
        readonly int[] _pushes;
        readonly float[] _total;
        readonly float[] _d0;
        readonly float[] _d1;
        readonly float[] _d2;
        readonly float[] _d3;
        long _sink;

        internal LiveBatchBenchmarksShim(int rows)
        {
            ushort[] Spread(ushort[] bank)
            {
                var ids = new ushort[rows];
                for (var i = 0; i < rows; i++)
                    ids[i] = bank[i % Host.Variants];
                return ids;
            }
            _a = Spread(Host.A);
            _b = Spread(Host.B);
            _c = Spread(Host.C);
            _d = Spread(Host.D);
            _clocks = new ushort[rows];
            for (var i = 0; i < rows; i++)
                _clocks[i] = (ushort)(i % Host.Duration);
            _lift = new float[rows];
            _height = new float[rows];
            _pushes = new int[rows];
            for (var i = 0; i < rows; i++) _pushes[i] = 1 + i % 4;
            _total = new float[rows];
            _d0 = new float[rows];
            _d1 = new float[rows];
            _d2 = new float[rows];
            _d3 = new float[rows];
        }

        internal void A()
        {
            Timeline<LiftTrack, LiftClip>.Apply(_a, _clocks, true, _lift);
            _sink += (long)_lift[0];
        }

        internal void BApply()
        {
            var columns = new ColumnSet();
            columns.Add(_height);
            columns.Add(_pushes);
            Timeline<ArcTrack, ArcClip>.Apply(_b, _clocks, true, in columns);
            _sink += (long)_height[0];
        }

        internal void BPreparePlay()
        {
            var columns = new ColumnSet();
            columns.Add(_height);
            columns.Add(_pushes);
            var state = Timeline<ArcTrack, ArcClip>.Prepare(_b, in columns);
            try
            {
                Timeline<ArcTrack, ArcClip>.Play(in state, _clocks, true);
            }
            finally
            {
                Timeline<ArcTrack, ArcClip>.Release(ref state);
            }
            _sink += (long)_height[0];
        }

        internal void BChunk()
        {
            var rows = _clocks.Length;
            for (var start = 0; start < rows; start += LiveBatchBenchmarks.Chunk)
            {
                var count = Math.Min(LiveBatchBenchmarks.Chunk, rows - start);
                var columns = new ColumnSet();
                columns.Add(_height.AsSpan(start, count));
                columns.Add(_pushes.AsSpan(start, count));
                Timeline<ArcTrack, ArcClip>.Apply(
                    _b.AsSpan(start, count), _clocks.AsSpan(start, count), true, in columns);
            }
            _sink += (long)_height[0];
        }

        internal void C()
        {
            Timeline<PushTrack, PushClip>.Apply(_c, _clocks, true, _total);
            _sink += (long)_total[0];
        }

        internal void D()
        {
            Timeline<D0Track, D0Clip>.Apply(_d, _clocks, true, _d0);
            Timeline<D1Track, D1Clip>.Apply(_d, _clocks, true, _d1);
            Timeline<D2Track, D2Clip>.Apply(_d, _clocks, true, _d2);
            Timeline<D3Track, D3Clip>.Apply(_d, _clocks, true, _d3);
            _sink += (long)(_d0[0] + _d1[0] + _d2[0] + _d3[0]);
        }
    }
}
