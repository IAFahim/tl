using Tl;

namespace PlaybackPrototype;

internal sealed class MovementLane
{
    private const int RowCount = 1_000_000;
    private const int Duration = 64;
    private const int Warmups = 5;
    private const uint FirstTick = 200_000u;

    public string Name => "movement";

    public int Rows => RowCount;

    public LaneResult Run()
    {
        var positions = new uint[RowCount];
        var cycles = new long[RowCount];
        for (var i = 0; i < RowCount; i++) positions[i] = (uint)(i % Duration);
        var gameTick = FirstTick;
        long seed = 0;
        var measurement = Meter.Measure(
            () =>
            {
                for (var row = 0; row < RowCount; row++)
                {
                    var state = new TimelineState(1u, positions[row], cycles[row]);
                    if (!TimelineMovement.Select(in state, Duration, true, false, out var next, out _, out _, out _))
                        continue;
                    positions[row] = next.Position;
                    cycles[row] = next.Cycle;
                }

                for (var row = 0; row < RowCount; row += 4099)
                    seed = unchecked(seed * 31 + positions[row] + cycles[row]);
                gameTick++;
            },
            Warmups,
            LaneSupport.Iterations,
            1);
        return new LaneResult(
            Name,
            "context",
            RowCount,
            true,
            measurement.MedianNsPerPass,
            double.NaN,
            measurement.AllocatedBytesPerPass,
            double.NaN,
            $"seed={seed}");
    }
}

internal sealed class FloorLane
{
    private const int RowCount = 1_000_000;
    private const int Warmups = 5;
    private const uint FirstTick = 200_000u;

    public string Name => "floor";

    public int Rows => RowCount;

    public LaneResult Run()
    {
        var inputs = Inputs.Fill(RowCount, 11u);
        var accs = new Acc[RowCount];
        var gameTick = FirstTick;
        long seed = 0;
        var measurement = Meter.Measure(
            () =>
            {
                Inputs.Mutate(inputs, gameTick);
                for (var i = 0; i < accs.Length; i++)
                    accs[i].Value = unchecked(accs[i].Value * 31 + (int)inputs[i].Value);
                seed = Checksums.Mix(seed, accs[gameTick % RowCount].Value);
                gameTick++;
            },
            Warmups,
            LaneSupport.Iterations,
            1);
        return new LaneResult(
            Name,
            "context",
            RowCount,
            true,
            measurement.MedianNsPerPass,
            double.NaN,
            measurement.AllocatedBytesPerPass,
            double.NaN,
            $"seed={seed}");
    }
}
