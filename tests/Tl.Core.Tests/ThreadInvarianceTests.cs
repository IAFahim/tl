using Xunit;

namespace Tl.Core.Tests;

public unsafe class ThreadInvarianceTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void ChunkCoverageEveryRowExactlyOnce(int workers)
    {
        const int rows = 999;
        var seen = new int[rows];
        ParallelApply.ForEachChunk(rows, workers, (start, end) =>
        {
            for (var i = start; i < end; i++) System.Threading.Interlocked.Increment(ref seen[i]);
        });
        for (var i = 0; i < rows; i++) Assert.Equal(1, seen[i]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void ChunkPartitionIsContiguousAndCoversAllRows(int workers)
    {
        const int rows = 37;
        var chunks = new List<(int Start, int End)>();
        var gate = new object();
        ParallelApply.ForEachChunk(rows, workers, (start, end) =>
        {
            lock (gate) chunks.Add((start, end));
        });
        chunks.Sort((a, b) => a.Start.CompareTo(b.Start));
        Assert.Equal(Math.Min(workers, rows), chunks.Count);
        var cursor = 0;
        foreach (var (start, end) in chunks)
        {
            Assert.Equal(cursor, start);
            Assert.True(end > start);
            cursor = end;
        }
        Assert.Equal(rows, cursor);
    }

    [Fact]
    public void WorkerOneRunsASingleChunk()
    {
        var calls = 0;
        var seenStart = -1;
        var seenEnd = -1;
        ParallelApply.ForEachChunk(25, 1, (start, end) =>
        {
            calls++;
            seenStart = start;
            seenEnd = end;
        });
        Assert.Equal(1, calls);
        Assert.Equal(0, seenStart);
        Assert.Equal(25, seenEnd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void NonPositiveWorkersStillCoverEveryRow(int workers)
    {
        const int rows = 128;
        var seen = new int[rows];
        ParallelApply.ForEachChunk(rows, workers, (start, end) =>
        {
            for (var i = start; i < end; i++) System.Threading.Interlocked.Increment(ref seen[i]);
        });
        for (var i = 0; i < rows; i++) Assert.Equal(1, seen[i]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void ParallelAccumulateHashMatchesSerial(int workers)
    {
        const int n = 10000;
        var rows = new int[n];
        var values = new float[n];
        for (var i = 0; i < n; i++)
        {
            rows[i] = (i * 7919) % n;
            values[i] = ((i % 251) - 125) * 0.0625f + ((i % 13) - 6) * 1.25f;
        }
        var serial = new float[n];
        for (var i = 0; i < n; i++) serial[i] = 0.75f;
        for (var i = 0; i < n; i++) serial[rows[i]] += values[i];
        var acc = new float[n];
        for (var i = 0; i < n; i++) acc[i] = 0.75f;
        ParallelApply.ParallelAccumulate(acc, rows, values, workers);
        Assert.Equal(Hash(serial), Hash(acc));
        for (var i = 0; i < n; i++) Assert.Equal(serial[i], acc[i]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void NonPositiveWorkersAccumulateMatchesSerial(int workers)
    {
        const int n = 10000;
        var rows = new int[n];
        var values = new float[n];
        for (var i = 0; i < n; i++)
        {
            rows[i] = (i * 7919) % n;
            values[i] = ((i % 251) - 125) * 0.0625f + ((i % 13) - 6) * 1.25f;
        }
        var serial = new float[n];
        for (var i = 0; i < n; i++) serial[i] = 0.75f;
        for (var i = 0; i < n; i++) serial[rows[i]] += values[i];
        var acc = new float[n];
        for (var i = 0; i < n; i++) acc[i] = 0.75f;
        ParallelApply.ParallelAccumulate(acc, rows, values, workers);
        Assert.Equal(Hash(serial), Hash(acc));
    }

    [Fact]
    public void EmptyForEachChunkIsNoOp()
    {
        var calls = 0;
        ParallelApply.ForEachChunk(0, 4, (start, end) =>
        {
            System.Threading.Interlocked.Increment(ref calls);
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public void EmptyParallelAccumulateIsNoOp()
    {
        var acc = new float[] { 1f, 2f, 3f };
        ParallelApply.ParallelAccumulate(acc, new int[0], new float[0], 4);
        Assert.Equal(1f, acc[0]);
        Assert.Equal(2f, acc[1]);
        Assert.Equal(3f, acc[2]);
    }

    static ulong Hash(float[] xs)
    {
        var h = TlStateHash.Begin();
        for (var i = 0; i < xs.Length; i++) h.Float(xs[i]);
        return h.Value;
    }
}
