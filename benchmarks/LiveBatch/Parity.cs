using System.Diagnostics.CodeAnalysis;
using Tl;

namespace LiveBatch;

internal static class Parity
{
    internal const int Rows = 8192;
    internal const int Frames = 97;

    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "exact float parity is the receipt")]
    internal static int Run()
    {
        var failures = 0;
        failures += ShapeA();
        failures += ShapeB();
        failures += ShapeC();
        failures += ShapeD();
        failures += WarmAllocation();
        Console.WriteLine(failures == 0
            ? $"LIVEBATCH PARITY PASS: {Rows} rows x {Frames} frames forward and backward, batch-vs-singles bit-exact"
            : $"LIVEBATCH PARITY FAIL: {failures} mismatches");
        return failures;
    }

    static ushort[] Spread(ushort[] bank)
    {
        var ids = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            ids[i] = bank[i % Host.Variants];
        return ids;
    }

    static ushort[] StartClocks()
    {
        var clocks = new ushort[Rows];
        for (var i = 0; i < Rows; i++)
            clocks[i] = (ushort)(i * 7 % Host.Duration);
        return clocks;
    }

    static ulong Checksum(ReadOnlySpan<float> values, ReadOnlySpan<ushort> positions)
    {
        var hash = 1469598103934665603UL;
        foreach (var value in values)
        {
            hash ^= (uint)BitConverter.SingleToInt32Bits(value);
            hash *= 1099511628211UL;
        }
        foreach (var position in positions)
        {
            hash ^= position;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    static int Fail(ref int failures, string message)
    {
        Console.WriteLine($"parity mismatch: {message}");
        return failures += 1;
    }

    static int ShapeA()
    {
        var failures = 0;
        var ids = Spread(Host.A);
        var batch = new float[Rows];
        var clocks = StartClocks();
        for (var frame = 0; frame < Frames; frame++)
        {
            Timeline<LiftTrack, LiftClip>.Apply(ids, clocks, true, batch);
            Timeline.Advance(ids, clocks, true);
        }
        var forward = Checksum(batch, clocks);
        var oracle = new float[Rows];
        var oracleClocks = StartClocks();
        var oneId = new ushort[1];
        var oneClock = new ushort[1];
        var oneEffect = new float[1];
        for (var i = 0; i < Rows; i++)
        {
            oneId[0] = ids[i];
            oneClock[0] = oracleClocks[i];
            oneEffect[0] = 0;
            for (var frame = 0; frame < Frames; frame++)
            {
                Timeline<LiftTrack, LiftClip>.Apply(oneId, oneClock, true, oneEffect);
                Timeline.Advance(oneId, oneClock, true);
            }
            oracle[i] = oneEffect[0];
            oracleClocks[i] = oneClock[0];
        }
        for (var i = 0; i < Rows; i++)
        {
            if (batch[i] != oracle[i]) Fail(ref failures, $"A effect row {i}: batch {batch[i]} vs single {oracle[i]}");
            if (clocks[i] != oracleClocks[i]) Fail(ref failures, $"A clock row {i}: batch {clocks[i]} vs single {oracleClocks[i]}");
            if (failures > 8) return failures;
        }
        for (var frame = 0; frame < Frames; frame++)
        {
            Timeline<LiftTrack, LiftClip>.Apply(ids, clocks, false, batch);
            Timeline.Advance(ids, clocks, false);
        }
        var rewind = 0f;
        for (var i = 0; i < Rows; i++)
        {
            var residue = Math.Abs(batch[i]);
            if (residue > rewind) rewind = residue;
            if (residue > 1e-3f) Fail(ref failures, $"A rewind effect row {i}: {batch[i]} exceeds 1e-3");
            if (clocks[i] != (ushort)(i * 7 % Host.Duration)) Fail(ref failures, $"A rewind clock row {i}: {clocks[i]}");
            if (failures > 8) return failures;
        }
        Console.WriteLine($"parity A folded: forward {forward:X16}, rewind max residue {rewind:R} (blend-lane float order, batch==singles bit-exact)");
        return failures;
    }
    static void ApplyB(ushort[] ids, ushort[] clocks, bool forward, float[] height, int[] pushes)
    {
        var columns = new ColumnSet();
        columns.Add(height);
        columns.Add(pushes);
        Timeline<ArcTrack, ArcClip>.Apply(ids, clocks, forward, in columns);
    }
    static void PlayB(ushort[] ids, ushort[] clocks, bool forward, float[] height, int[] pushes)
    {
        var columns = new ColumnSet();
        columns.Add(height);
        columns.Add(pushes);
        var state = Timeline<ArcTrack, ArcClip>.Prepare(ids, in columns);
        try
        {
            Timeline<ArcTrack, ArcClip>.Play(in state, clocks, forward);
        }
        finally
        {
            Timeline<ArcTrack, ArcClip>.Release(ref state);
        }
    }

    static int ShapeB()
    {
        var failures = 0;
        var ids = Spread(Host.B);
        var pushes = new int[Rows];
        for (var i = 0; i < Rows; i++) pushes[i] = 1 + i % 4;

        var height = new float[Rows];
        var clocks = StartClocks();
        for (var frame = 0; frame < Frames; frame++)
        {
            ApplyB(ids, clocks, true, height, pushes);
            Timeline.Advance(ids, clocks, true);
        }
        var forward = Checksum(height, clocks);

        var split = new float[Rows];
        var splitClocks = StartClocks();
        for (var frame = 0; frame < Frames; frame++)
        {
            PlayB(ids, splitClocks, true, split, pushes);
            Timeline.Advance(ids, splitClocks, true);
        }
        for (var i = 0; i < Rows; i++)
        {
            if (height[i] != split[i]) Fail(ref failures, $"B prepare/play effect row {i}: {height[i]} vs {split[i]}");
            if (clocks[i] != splitClocks[i]) Fail(ref failures, $"B prepare/play clock row {i}");
            if (failures > 8) return failures;
        }

        var chunked = new float[Rows];
        var chunkClocks = StartClocks();
        for (var frame = 0; frame < Frames; frame++)
        {
            for (var start = 0; start < Rows; start += LiveBatchBenchmarks.Chunk)
            {
                var count = Math.Min(LiveBatchBenchmarks.Chunk, Rows - start);
                var columns = new ColumnSet();
                columns.Add(chunked.AsSpan(start, count));
                columns.Add(pushes.AsSpan(start, count));
                Timeline<ArcTrack, ArcClip>.Apply(
                    ids.AsSpan(start, count), chunkClocks.AsSpan(start, count), true, in columns);
            }
            Timeline.Advance(ids, chunkClocks, true);
        }
        for (var i = 0; i < Rows; i++)
        {
            if (height[i] != chunked[i]) Fail(ref failures, $"B chunk128 effect row {i}: {height[i]} vs {chunked[i]}");
            if (clocks[i] != chunkClocks[i]) Fail(ref failures, $"B chunk128 clock row {i}");
            if (failures > 8) return failures;
        }

        var oracle = new float[Rows];
        var oracleClocks = StartClocks();
        var oneId = new ushort[1];
        var oneClock = new ushort[1];
        var oneHeight = new float[1];
        var onePush = new int[1];
        for (var i = 0; i < Rows; i++)
        {
            oneId[0] = ids[i];
            oneClock[0] = oracleClocks[i];
            oneHeight[0] = 0;
            onePush[0] = pushes[i];
            for (var frame = 0; frame < Frames; frame++)
            {
                var columns = new ColumnSet();
                columns.Add(oneHeight);
                columns.Add(onePush);
                Timeline<ArcTrack, ArcClip>.Apply(oneId, oneClock, true, in columns);
                Timeline.Advance(oneId, oneClock, true);
            }
            oracle[i] = oneHeight[0];
            oracleClocks[i] = oneClock[0];
        }
        for (var i = 0; i < Rows; i++)
        {
            if (height[i] != oracle[i]) Fail(ref failures, $"B effect row {i}: batch {height[i]} vs single {oracle[i]}");
            if (clocks[i] != oracleClocks[i]) Fail(ref failures, $"B clock row {i}");
            if (failures > 8) return failures;
        }

        for (var frame = 0; frame < Frames; frame++)
        {
            ApplyB(ids, clocks, false, height, pushes);
            Timeline.Advance(ids, clocks, false);
        }
        for (var i = 0; i < Rows; i++)
        {
            if (height[i] != 0) Fail(ref failures, $"B rewind effect row {i}: {height[i]} != 0");
            if (clocks[i] != (ushort)(i * 7 % Host.Duration)) Fail(ref failures, $"B rewind clock row {i}");
            if (failures > 8) return failures;
        }
        Console.WriteLine($"parity B fold-fed compose: forward {forward:X16}, prepare/play + chunk128 + singles exact, rewind exact");
        return failures;
    }

    static int ShapeC()
    {
        var failures = 0;
        var ids = Spread(Host.C);
        var batch = new float[Rows];
        var clocks = StartClocks();
        for (var frame = 0; frame < Frames; frame++)
        {
            Timeline<PushTrack, PushClip>.Apply(ids, clocks, true, batch);
            Timeline.Advance(ids, clocks, true);
        }
        var forward = Checksum(batch, clocks);
        var oracle = new float[Rows];
        var oracleClocks = StartClocks();
        var oneId = new ushort[1];
        var oneClock = new ushort[1];
        var oneEffect = new float[1];
        for (var i = 0; i < Rows; i++)
        {
            oneId[0] = ids[i];
            oneClock[0] = oracleClocks[i];
            oneEffect[0] = 0;
            for (var frame = 0; frame < Frames; frame++)
            {
                Timeline<PushTrack, PushClip>.Apply(oneId, oneClock, true, oneEffect);
                Timeline.Advance(oneId, oneClock, true);
            }
            oracle[i] = oneEffect[0];
            oracleClocks[i] = oneClock[0];
        }
        for (var i = 0; i < Rows; i++)
        {
            if (batch[i] != oracle[i]) Fail(ref failures, $"C effect row {i}: batch {batch[i]} vs single {oracle[i]}");
            if (clocks[i] != oracleClocks[i]) Fail(ref failures, $"C clock row {i}");
            if (failures > 8) return failures;
        }
        for (var frame = 0; frame < Frames; frame++)
        {
            Timeline<PushTrack, PushClip>.Apply(ids, clocks, false, batch);
            Timeline.Advance(ids, clocks, false);
        }
        for (var i = 0; i < Rows; i++)
        {
            if (batch[i] != 0) Fail(ref failures, $"C rewind effect row {i}: {batch[i]} != 0");
            if (clocks[i] != (ushort)(i * 7 % Host.Duration)) Fail(ref failures, $"C rewind clock row {i}");
            if (failures > 8) return failures;
        }
        Console.WriteLine($"parity C frame consumer: forward {forward:X16}, rewind exact");
        return failures;
    }

    static int ShapeD()
    {
        var failures = 0;
        var ids = Spread(Host.D);
        var d0 = new float[Rows];
        var d1 = new float[Rows];
        var d2 = new float[Rows];
        var d3 = new float[Rows];
        var clocks = StartClocks();
        for (var frame = 0; frame < Frames; frame++)
        {
            Timeline<D0Track, D0Clip>.Apply(ids, clocks, true, d0);
            Timeline<D1Track, D1Clip>.Apply(ids, clocks, true, d1);
            Timeline<D2Track, D2Clip>.Apply(ids, clocks, true, d2);
            Timeline<D3Track, D3Clip>.Apply(ids, clocks, true, d3);
            Timeline.Advance(ids, clocks, true);
        }
        var forward = Checksum(d0, clocks) ^ Checksum(d1, clocks) ^ Checksum(d2, clocks) ^ Checksum(d3, clocks);
        var o0 = new float[Rows];
        var o1 = new float[Rows];
        var o2 = new float[Rows];
        var o3 = new float[Rows];
        var oracleClocks = StartClocks();
        var oneId = new ushort[1];
        var oneClock = new ushort[1];
        var e0 = new float[1];
        var e1 = new float[1];
        var e2 = new float[1];
        var e3 = new float[1];
        for (var i = 0; i < Rows; i++)
        {
            oneId[0] = ids[i];
            oneClock[0] = oracleClocks[i];
            e0[0] = e1[0] = e2[0] = e3[0] = 0;
            for (var frame = 0; frame < Frames; frame++)
            {
                Timeline<D0Track, D0Clip>.Apply(oneId, oneClock, true, e0);
                Timeline<D1Track, D1Clip>.Apply(oneId, oneClock, true, e1);
                Timeline<D2Track, D2Clip>.Apply(oneId, oneClock, true, e2);
                Timeline<D3Track, D3Clip>.Apply(oneId, oneClock, true, e3);
                Timeline.Advance(oneId, oneClock, true);
            }
            o0[i] = e0[0];
            o1[i] = e1[0];
            o2[i] = e2[0];
            o3[i] = e3[0];
            oracleClocks[i] = oneClock[0];
        }
        for (var i = 0; i < Rows; i++)
        {
            if (d0[i] != o0[i]) Fail(ref failures, $"D effect d0 row {i}: batch {d0[i]} vs single {o0[i]}");
            if (d1[i] != o1[i]) Fail(ref failures, $"D effect d1 row {i}: batch {d1[i]} vs single {o1[i]}");
            if (d2[i] != o2[i]) Fail(ref failures, $"D effect d2 row {i}: batch {d2[i]} vs single {o2[i]}");
            if (d3[i] != o3[i]) Fail(ref failures, $"D effect d3 row {i}: batch {d3[i]} vs single {o3[i]}");
            if (clocks[i] != oracleClocks[i]) Fail(ref failures, $"D clock row {i}");
            if (failures > 8) return failures;
        }
        for (var frame = 0; frame < Frames; frame++)
        {
            Timeline<D0Track, D0Clip>.Apply(ids, clocks, false, d0);
            Timeline<D1Track, D1Clip>.Apply(ids, clocks, false, d1);
            Timeline<D2Track, D2Clip>.Apply(ids, clocks, false, d2);
            Timeline<D3Track, D3Clip>.Apply(ids, clocks, false, d3);
            Timeline.Advance(ids, clocks, false);
        }
        for (var i = 0; i < Rows; i++)
        {
            if (d0[i] != 0) Fail(ref failures, $"D rewind d0 row {i}: {d0[i]} != 0");
            if (d1[i] != 0) Fail(ref failures, $"D rewind d1 row {i}: {d1[i]} != 0");
            if (d2[i] != 0) Fail(ref failures, $"D rewind d2 row {i}: {d2[i]} != 0");
            if (d3[i] != 0) Fail(ref failures, $"D rewind d3 row {i}: {d3[i]} != 0");
            if (clocks[i] != (ushort)(i * 7 % Host.Duration)) Fail(ref failures, $"D rewind clock row {i}");
            if (failures > 8) return failures;
        }
        Console.WriteLine($"parity D four pairs: forward {forward:X16}, rewind exact");
        return failures;
    }

    static int WarmAllocation()
    {
        var failures = 0;
        failures += Warm("A", RunA);
        failures += Warm("B-apply", RunBApply);
        failures += Warm("B-prepareplay", RunBPreparePlay);
        failures += Warm("B-chunk128", RunBChunk);
        failures += Warm("C", RunC);
        failures += Warm("D", RunD);
        return failures;
    }

    static int Warm(string name, Action step)
    {
        for (var pass = 0; pass < 16; pass++)
            step();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 128; pass++)
            step();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0)
        {
            Console.WriteLine($"parity mismatch: warm {name} allocated {allocated} B over 128 runs");
            return 1;
        }
        Console.WriteLine($"allocation: 128 x {name} runs retained 0 B");
        return 0;
    }

    static LiveBatchBenchmarks? _bench;

    static LiveBatchBenchmarks Bench()
    {
        if (_bench is null)
        {
            _bench = new LiveBatchBenchmarks();
            _bench.Setup();
        }
        return _bench;
    }

    static void RunA() => Bench().A_Folded();
    static void RunBApply() => Bench().B_Apply();
    static void RunBPreparePlay() => Bench().B_PreparePlay();
    static void RunBChunk() => Bench().B_Chunk128();
    static void RunC() => Bench().C_Frame();
    static void RunD() => Bench().D_FourPairs();
}
