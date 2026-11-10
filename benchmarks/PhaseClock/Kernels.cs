using System.Numerics;

namespace PhaseClock;

public static class Kernels
{
    public static int Tick(ushort phase, int frame, int duration, int dir)
    {
        var t = (int)phase + dir * frame;
        t %= duration;
        if (t < 0) t += duration;
        return t;
    }

    public static void ScalarPhase(float[] acc, int off, int[] row, ushort[] phase, float[] lane, int frame, int duration, int dir)
        => ScalarPhase(acc, off, row, phase, lane, frame, duration, dir, Data.N);

    public static void ScalarPhase(float[] acc, int off, int[] row, ushort[] phase, float[] lane, int frame, int duration, int dir, int count)
    {
        for (var i = 0; i < count; i++)
            acc[off + row[i]] += lane[Tick(phase[i], frame, duration, dir)];
    }

    public static void BroadcastDense(float[] acc, int off, float value, int count)
    {
        var v = new Vector<float>(value);
        var i = 0;
        for (; i + Vector<float>.Count <= count; i += Vector<float>.Count)
            Vector.StoreUnsafe(Vector.LoadUnsafe(ref acc[off + i]) + v, ref acc[off + i]);
        for (; i < count; i++) acc[off + i] += value;
    }

    public static void BroadcastSparse(float[] acc, int off, int[] row, int start, int length, float value)
    {
        for (var i = start; i < start + length; i++) acc[off + row[i]] += value;
    }

    public static void LaneDense(float[] acc, int off, float[] lane, int count)
    {
        var i = 0;
        for (; i + Vector<float>.Count <= count; i += Vector<float>.Count)
            Vector.StoreUnsafe(Vector.LoadUnsafe(ref acc[off + i]) + Vector.LoadUnsafe(ref lane[i]), ref acc[off + i]);
        for (; i < count; i++) acc[off + i] += lane[i];
    }

    public static void LaneGatherScalar(float[] acc, int off, int[] row, ushort[] phase, float[] lane, int frame, int duration, int dir)
        => LaneGatherScalar(acc, off, row, phase, lane, frame, duration, dir, Data.N);

    public static void LaneGatherScalar(float[] acc, int off, int[] row, ushort[] phase, float[] lane, int frame, int duration, int dir, int count)
    {
        for (var i = 0; i < count; i++)
            acc[off + row[i]] += lane[Tick(phase[i], frame, duration, dir)];
    }

    public static void RunBroadcast(float[] acc, int off, int[] sortedRow, (int Start, int Length, ushort Phase)[] runs, float[] lane, int frame, int duration, int dir)
    {
        foreach (var (start, length, ph) in runs)
        {
            var value = lane[Tick(ph, frame, duration, dir)];
            BroadcastSparse(acc, off, sortedRow, start, length, value);
        }
    }
}
