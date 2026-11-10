using System;
using System.Threading.Tasks;

namespace Tl;

public static class ParallelApply
{
    public static void ForEachChunk(int rows, int workers, Action<int, int> body)
    {
        if (body is null) throw new ArgumentNullException(nameof(body));
        if (rows < 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (rows == 0) return;
        var count = workers <= 0 ? Environment.ProcessorCount : workers;
        if (count < 1) count = 1;
        if (count > rows) count = rows;
        if (count == 1)
        {
            body(0, rows);
            return;
        }
        var baseSize = rows / count;
        var extra = rows % count;
        Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = count }, i =>
        {
            var start = i * baseSize + (i < extra ? i : extra);
            var size = baseSize + (i < extra ? 1 : 0);
            body(start, start + size);
        });
    }

    public static void ParallelAccumulate(float[] acc, int[] rows, float[] values, int workers)
    {
        if (acc is null) throw new ArgumentNullException(nameof(acc));
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (rows.Length != values.Length) throw new ArgumentException("rows and values must have the same length.", nameof(rows));
        var count = rows.Length;
        if (count == 0) return;
        ForEachChunk(count, workers, (start, end) =>
        {
            for (var i = start; i < end; i++) acc[rows[i]] += values[i];
        });
    }
}
