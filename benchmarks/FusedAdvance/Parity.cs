namespace Tl.FusedAdvanceProbe;

internal static class Parity
{
    const ulong Offset = 14695981039346656037, Prime = 1099511628211;
    const int Passes = 3;

    public static int Run()
    {
        var laneSlot = Host.SlotLane();
        using var set = Host.BuildSet(8);
        var failures = 0;
        foreach (var rows in new[] { 100_000, 1_000_000 })
            foreach (var clock in new[] { Clock.Uniform, Clock.Waves, Clock.Staggered })
                foreach (var forward in new[] { true, false })
                {
                    failures += Lane(rows, clock, forward, laneSlot);
                    failures += Set(rows, clock, forward, mixedIds: true, set);
                }
        failures += Set(100_000, Clock.Uniform, forward: true, mixedIds: false, set);
        failures += Finite(100_000, Clock.Clamped, forward: true, Host.SlotFinite());
        failures += Finite(100_000, Clock.Clamped, forward: false, Host.SlotFinite());
        Console.WriteLine(failures == 0 ? "parity: all cases PASS" : $"parity: {failures} case(s) FAILED");
        return failures;
    }

    static int Lane(int rows, Clock clock, bool forward, ushort laneSlot)
    {
        var ids = new ushort[rows];
        Array.Fill(ids, laneSlot);
        var reference = Seeds.Positions(rows, clock);
        var candidate = Seeds.Positions(rows, clock);
        var referenceEffects = Seeds.Effects(rows);
        var candidateEffects = Seeds.Effects(rows);
        for (var pass = 0; pass < Passes; pass++)
        {
            Timeline<LaneTrack, LaneClip>.Seek(ids, reference, forward).Apply(referenceEffects);
            Timeline<LaneTrack, LaneClip>.Advance(ids, candidate, forward, candidateEffects);
        }
        return Report("lane", rows, clock, forward, reference, candidate, referenceEffects, candidateEffects);
    }

    static int Set(int rows, Clock clock, bool forward, bool mixedIds, TimelineSet<LaneTrack, LaneClip> set)
    {
        var ids = mixedIds ? Seeds.Ids(rows, 8) : new ushort[rows];
        var reference = Seeds.Positions(rows, clock);
        var candidate = Seeds.Positions(rows, clock);
        var referenceEffects = Seeds.Effects(rows);
        var candidateEffects = Seeds.Effects(rows);
        for (var pass = 0; pass < Passes; pass++)
        {
            set.Gather(ids).Seek(reference, forward).Apply(referenceEffects);
            set.Advance(ids, candidate, forward, candidateEffects);
        }
        return Report(mixedIds ? "set-mixed" : "set-one ", rows, clock, forward, reference, candidate, referenceEffects, candidateEffects);
    }

    static int Finite(int rows, Clock clock, bool forward, ushort finiteSlot)
    {
        var ids = new ushort[rows];
        Array.Fill(ids, finiteSlot);
        var reference = Seeds.Positions(rows, clock);
        var candidate = Seeds.Positions(rows, clock);
        var referenceEffects = Seeds.Effects(rows);
        var candidateEffects = Seeds.Effects(rows);
        for (var pass = 0; pass < Passes; pass++)
        {
            Timeline<EdgeTrack, EdgeClip>.Seek(ids, reference, forward).Apply(referenceEffects);
            Timeline<EdgeTrack, EdgeClip>.Advance(ids, candidate, forward, candidateEffects);
        }
        return Report("edge", rows, clock, forward, reference, candidate, referenceEffects, candidateEffects);
    }

    static int Report(string label, int rows, Clock clock, bool forward, ushort[] referencePositions, ushort[] candidatePositions, float[] referenceEffects, float[] candidateEffects)
    {
        var positionIndex = FirstPositionMismatch(referencePositions, candidatePositions);
        var effectIndex = FirstEffectBitMismatch(referenceEffects, candidateEffects);
        var ok = positionIndex < 0 && effectIndex < 0;
        if (!ok)
        {
            if (positionIndex >= 0)
                Console.WriteLine($"  position mismatch at row {positionIndex}: {referencePositions[positionIndex]} vs {candidatePositions[positionIndex]}");
            if (effectIndex >= 0)
                Console.WriteLine($"  effect bit mismatch at row {effectIndex}: {Bits(referenceEffects[effectIndex])} vs {Bits(candidateEffects[effectIndex])}");
        }
        Console.WriteLine($"{label,-9} {rows,7:N0} rows {ClockName(clock),-9} {(forward ? "forward " : "backward")} {(ok ? "PASS" : "FAIL")} pos=0x{Sum(referencePositions):x16} eff=0x{SumBits(referenceEffects):x16}");
        return ok ? 0 : 1;
    }

    static string ClockName(Clock clock) => clock.ToString().ToLowerInvariant();

    static string Bits(float value) => $"0x{BitConverter.SingleToInt32Bits(value):x8}";

    static int FirstPositionMismatch(ushort[] reference, ushort[] candidate)
    {
        for (var i = 0; i < reference.Length; i++)
            if (reference[i] != candidate[i]) return i;
        return -1;
    }

    static int FirstEffectBitMismatch(float[] reference, float[] candidate)
    {
        for (var i = 0; i < reference.Length; i++)
            if (BitConverter.SingleToInt32Bits(reference[i]) != BitConverter.SingleToInt32Bits(candidate[i])) return i;
        return -1;
    }

    static ulong Sum(ushort[] values)
    {
        var hash = Offset;
        foreach (var value in values) hash = (hash ^ value) * Prime;
        return hash | 1;
    }

    static ulong SumBits(float[] values)
    {
        var hash = Offset;
        foreach (var value in values) hash = (hash ^ (ulong)BitConverter.SingleToInt32Bits(value)) * Prime;
        return hash | 1;
    }
}
