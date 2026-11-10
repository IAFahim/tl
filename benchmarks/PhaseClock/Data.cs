namespace PhaseClock;

public static class Data
{
    public const int N = 1024;
    public const int D40 = 40;
    public const int D1024 = 1024;

    public static float[] ConstantLane(int duration, float value)
    {
        var lane = new float[duration];
        for (var i = 0; i < duration; i++) lane[i] = value;
        return lane;
    }

    public static float[] VaryingLane(int duration)
    {
        var lane = new float[duration];
        for (var i = 0; i < duration; i++) lane[i] = 1f + (i * 7 % 13) * 0.5f + (i % 5) * 0.25f;
        return lane;
    }

    public static ushort[] UniformPhase() => new ushort[N];

    public static ushort[] MixedPhase40()
    {
        var phase = new ushort[N];
        for (var e = 0; e < N; e++) phase[e] = (ushort)((e * 37 + 11) % D40);
        return phase;
    }

    public static ushort[] UniquePhase1024()
    {
        var phase = new ushort[N];
        for (var e = 0; e < N; e++) phase[e] = (ushort)e;
        return phase;
    }

    public static int[] DenseRows()
    {
        var row = new int[N];
        for (var i = 0; i < N; i++) row[i] = i;
        return row;
    }

    public static int[] SparseRows()
    {
        var row = new int[N];
        for (var i = 0; i < N; i++) row[i] = (i * 97) % N;
        return row;
    }

    public static ushort[] SortByPhase(ushort[] phase, int[] row, out int[] sortedRow, out ushort[] sortedPhase)
    {
        var order = new int[N];
        for (var i = 0; i < N; i++) order[i] = i;
        Array.Sort(order, (a, b) =>
        {
            var c = phase[a].CompareTo(phase[b]);
            return c != 0 ? c : a.CompareTo(b);
        });
        sortedRow = new int[N];
        sortedPhase = new ushort[N];
        for (var i = 0; i < N; i++)
        {
            sortedRow[i] = row[order[i]];
            sortedPhase[i] = phase[order[i]];
        }
        return sortedPhase;
    }

    public static (int Start, int Length, ushort Phase)[] RunDescriptors(ushort[] sortedPhase)
    {
        var runs = new List<(int, int, ushort)>();
        var i = 0;
        while (i < N)
        {
            var j = i + 1;
            while (j < N && sortedPhase[j] == sortedPhase[i]) j++;
            runs.Add((i, j - i, sortedPhase[i]));
            i = j;
        }
        return runs.ToArray();
    }
}
