using System.Runtime.InteropServices;
using Tl;
using Tl.TestSupport;

internal readonly record struct DamageClip(float Amount);
internal readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly record struct HealClip(float Amount);
internal readonly record struct HealTrack(float Multiplier) : IBlend<HealClip>
{
    public void Blend(in HealClip first, in HealClip second, float factor, out HealClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly record struct TandemClip(float Amount);
internal readonly record struct TandemTrack(float Multiplier) : IBlend<TandemClip>
{
    public void Blend(in TandemClip first, in TandemClip second, float factor, out TandemClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

internal readonly record struct ImpureClip(float Amount);
internal readonly record struct ImpureTrack(float Multiplier) : IBlend<ImpureClip>
{
    public void Blend(in ImpureClip first, in ImpureClip second, float factor, out ImpureClip result) => result = first;
}

internal readonly struct DamageJob : ITrack<DamageTrack, DamageClip>
{
    public static void Execute(in Frame<DamageTrack, DamageClip> frame, ref float vitality)
        => vitality -= frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

internal readonly struct HealJob : ITrack<HealTrack, HealClip>
{
    public static void Execute(in Frame<HealTrack, HealClip> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

internal readonly struct TandemFirstJob : ITrack<TandemTrack, TandemClip>
{
    public static void Execute(in Frame<TandemTrack, TandemClip> frame, ref float vitality)
        => vitality += frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

internal readonly struct TandemSecondJob : ITrack<TandemTrack, TandemClip>
{
    public static void Execute(in Frame<TandemTrack, TandemClip> frame, ref float vitality)
        => vitality += frame.Direction * 7f;
}

internal readonly struct ImpureJob : ITrack<ImpureTrack, ImpureClip>
{
    public static void Execute(in Frame<ImpureTrack, ImpureClip> frame, ref float vitality)
        => vitality *= 2f;
}

internal static class DataAuthoredReceipts
{
    static float Blend(float first, float second, uint tick, uint start, uint span)
    {
        if (span == 0) return first;
        var factor = span == 1 ? 0.5f : (tick - start) / (span - 1f);
        return first + (second - first) * factor;
    }

    internal static void All()
    {
        MovementLaw();
        WrapCounts();
        FoldAndBlend();
        RewindAndCatchUp();
        TimelineSets();
        Faults();
        Validation();
        Console.WriteLine("receipts: movement, wrap-count, fold+blend, rewind, catch-up, timeline sets, faults, validation PASS");
    }

    internal static void MovementLaw()
    {
        foreach (var looping in new[] { true, false })
        {
            var baker = new DomainBaker()
                .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
                .Clip(0, 0u, 12u, new DamageClip(5f));
            if (looping)
                baker.Looping();
            using var asset = TimelineAsset.LoadAsset(baker.Bake());
            BakedLane<DamageTrack, DamageClip>.Bind(asset);
            Require(BakedLane<DamageTrack, DamageClip>.Effect(0) == -10f, "effect table matches the authored fold");
            var duration = BakedLane<DamageTrack, DamageClip>.Duration;
            var positions = new ushort[64];
            var values = new float[64];
            for (var i = 0; i < positions.Length; i++)
                positions[i] = (ushort)(i % (duration + 2));
            var oraclePositions = (ushort[])positions.Clone();
            var oracleValues = new float[64];

            for (var tick = 0; tick < 200; tick++)
            {
                var forward = tick % 3 != 2;
                Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, forward).Apply(values);
                for (var i = 0; i < positions.Length; i++)
                {
                    if (!TimelineMovement.Select(new TimelineState(1, oraclePositions[i]), duration, looping, !forward, out var next, out var timelineTick, out _))
                        continue;
                    oracleValues[i] += forward ? BakedLane<DamageTrack, DamageClip>.Effect(timelineTick) : BakedLane<DamageTrack, DamageClip>.InverseEffect(timelineTick);
                    oraclePositions[i] = next.Position;
                }
            }

            Require(positions.SequenceEqual(oraclePositions), "movement positions match the law");
            Require(values.SequenceEqual(oracleValues), "folded effects match the law");
        }
    }

    internal static void WrapCounts()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Clip(0, 0u, 12u, new DamageClip(5f))
            .Looping()
            .Bake());
        BakedLane<DamageTrack, DamageClip>.Bind(asset);
        const ushort Duration = 12;
        const int Rows = 48;
        const int Steps = 400;

        var random = new Random(113);
        var positions = new ushort[Rows];
        var loops = new long[Rows];
        var oraclePositions = new ushort[Rows];
        var oracleLoops = new long[Rows];
        long forwardWraps = 0, backwardWraps = 0;
        for (var i = 0; i < Rows; i++)
            positions[i] = oraclePositions[i] = (ushort)(i * 7 % Duration);

        for (var step = 0; step < Steps; step++)
        {
            var forward = random.Next(3) != 2;
            var reverse = !forward;
            for (var i = 0; i < Rows; i++)
            {
                if (!TimelineMovement.Select(new TimelineState(1, positions[i]), Duration, true, reverse, out var next, out _, out var flags))
                    continue;
                if ((flags & FrameFlags.TimelineEnd) != 0)
                {
                    if ((flags & FrameFlags.Reverse) != 0) backwardWraps++;
                    else forwardWraps++;
                    loops[i] += (flags & FrameFlags.Reverse) != 0 ? -1 : 1;
                }
                positions[i] = next.Position;
            }
            for (var i = 0; i < Rows; i++)
            {
                if (!TimelineMovement.Select(new TimelineState(1, oraclePositions[i]), Duration, true, reverse, out var next, out var tick, out _))
                    continue;
                if (tick == Duration - 1)
                    oracleLoops[i] += reverse ? -1 : 1;
                oraclePositions[i] = next.Position;
            }
        }

        Require(positions.SequenceEqual(oraclePositions), "wrap receipt positions match the movement law");
        Require(loops.SequenceEqual(oracleLoops), "flag-reconstructed loop counts match the removed engine cycle");

        long laneSum = 0, oracleSum = 0;
        for (var i = 0; i < Rows; i++) { laneSum += loops[i]; oracleSum += oracleLoops[i]; }
        Require(laneSum == oracleSum, "wrap receipt loop totals agree");
        Require(forwardWraps > 0 && backwardWraps > 0, "wrap receipt exercised wraps in both directions on the randomized schedule");
        Console.WriteLine($"wrap-count: {Rows} rows x {Steps} randomized steps reconstruct loop counts from TimelineEnd/Reverse flags (finite wraps carry CompletedAfter/CompletedBefore)");
    }

    internal static void FoldAndBlend()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Track<HealTrack, HealClip>(new HealTrack(1f))
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 8u, new DamageClip(8f))
            .Clip(0, 4u, 8u, new DamageClip(4f))
            .Clip(1, 0u, 8u, new HealClip(3f))
            .Clip(2, 2u, 8u, new DamageClip(2f))
            .Looping()
            .Bake());
        BakedLane<DamageTrack, DamageClip>.Bind(asset);

        for (var tick = 0; tick < 8; tick++)
        {
            var amount = tick < 4 ? 8f : Blend(8f, 4f, (uint)tick, 4u, 4u);
            var damage = 2f * amount + (tick >= 2 ? 1f * 2f : 0f);
            var heal = 3f;
            Require(BakedLane<DamageTrack, DamageClip>.Effect((ushort)tick) == heal - damage, $"folded effect at {tick}");
            Require(BakedLane<DamageTrack, DamageClip>.InverseEffect((ushort)tick) == damage - heal, $"folded inverse at {tick}");
        }
    }

    internal static void RewindAndCatchUp()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<TandemTrack, TandemClip>(new TandemTrack(2f))
            .Clip(0, 0u, 6u, new TandemClip(4f))
            .Looping()
            .Bake());
        BakedLane<TandemTrack, TandemClip>.Bind(asset);
        var positions = new ushort[32];
        var values = new float[32];
        for (var i = 0; i < positions.Length; i++)
            positions[i] = (ushort)(i % 6);
        var initialPositions = (ushort[])positions.Clone();

        for (var tick = 0; tick < 25; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
        for (var tick = 0; tick < 25; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, false).Apply(values);

        Require(positions.SequenceEqual(initialPositions), "rewind restores positions");
        Require(values.All(static value => value == 0f), "rewind restores values exactly");

        Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
        var single = values[0];
        for (var i = 0; i < 2; i++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
        Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, false).Apply(values);
        Require(values[0] == single * 2, "catch-up calls are linear and backward cancels one");
    }

    internal static void Faults()
    {
        using var impure = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<ImpureTrack, ImpureClip>(new ImpureTrack(1f))
            .Clip(0, 0u, 6u, new ImpureClip(5f))
            .Looping()
            .Bake());
        BakedLane<ImpureTrack, ImpureClip>.Bind(impure);
        Require(BakedLane<ImpureTrack, ImpureClip>.Effect(0) == 0f, "column-folding consumer bakes its zero-seed baseline");

        using var foreign = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 0u, 6u, new DamageClip(5f))
            .Looping()
            .Bake());
        RequireThrows<ArgumentException>(() => BakedLane<HealTrack, HealClip>.Bind(foreign), "asset without the pair rejected at bind");
    }

    internal static void Validation()
    {
        var positions = new ushort[4];
        var values = new float[3];
        RequireThrows<ArgumentException>(() => Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(values), "length mismatch rejected");
        values = new float[4];
        var buffer = new ushort[10];
        var overlappingPositions = buffer.AsSpan(0, 4);
        var overlapping = MemoryMarshal.Cast<ushort, float>(buffer.AsSpan(1, 8));
        var threw = false;
        try
        {
            Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(overlappingPositions, true).Apply(overlapping);
        }
        catch (ArgumentException)
        {
            threw = true;
        }
        Require(threw, "overlapping columns rejected");
    }

    internal static void TimelineSets()
    {
        using var loopingAsset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(2f))
            .Clip(0, 0u, 12u, new DamageClip(5f))
            .Looping()
            .Bake());
        using var finiteAsset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
            .Clip(0, 2u, 8u, new DamageClip(4f))
            .Bake());
        BakedLane<DamageTrack, DamageClip>.Bind(loopingAsset);
        var loopingDuration = (int)BakedLane<DamageTrack, DamageClip>.Duration;
        var loopingEffect = new float[loopingDuration];
        var loopingInverse = new float[loopingDuration];
        for (var tick = 0; tick < loopingDuration; tick++)
        {
            loopingEffect[tick] = BakedLane<DamageTrack, DamageClip>.Effect((ushort)tick);
            loopingInverse[tick] = BakedLane<DamageTrack, DamageClip>.InverseEffect((ushort)tick);
        }
        BakedLane<DamageTrack, DamageClip>.Bind(finiteAsset);
        var finiteDuration = (int)BakedLane<DamageTrack, DamageClip>.Duration;
        var finiteEffect = new float[finiteDuration];
        var finiteInverse = new float[finiteDuration];
        for (var tick = 0; tick < finiteDuration; tick++)
        {
            finiteEffect[tick] = BakedLane<DamageTrack, DamageClip>.Effect((ushort)tick);
            finiteInverse[tick] = BakedLane<DamageTrack, DamageClip>.InverseEffect((ushort)tick);
        }

        using var timelines = new TimelineSet<DamageTrack, DamageClip>();
        var loopingId = timelines.Add(loopingAsset);
        var finiteId = timelines.Add(finiteAsset);

        const int Rows = 700;
        const int Frames = 120;
        var ids = new ushort[Rows];
        var positions = new ushort[Rows];
        var values = new float[Rows];
        for (var i = 0; i < Rows; i++)
        {
            ids[i] = i < 300 ? loopingId : i % 2 == 0 ? finiteId : loopingId;
            positions[i] = (ushort)(i % 14);
        }
        var oraclePositions = (ushort[])positions.Clone();
        var oracleValues = new float[Rows];

        for (var frame = 0; frame < Frames; frame++)
        {
            var forward = frame % 3 != 2;
            timelines.Gather(ids).Seek(positions, forward).Apply(values);
            for (var i = 0; i < Rows; i++)
            {
                var isLooping = ids[i] == loopingId;
                var duration = isLooping ? loopingDuration : finiteDuration;
                if (!TimelineMovement.Select(new TimelineState(1, oraclePositions[i]), (ushort)duration, isLooping, !forward, out var next, out var timelineTick, out _))
                    continue;
                oracleValues[i] += forward
                    ? isLooping ? loopingEffect[timelineTick] : finiteEffect[timelineTick]
                    : isLooping ? loopingInverse[timelineTick] : finiteInverse[timelineTick];
                oraclePositions[i] = next.Position;
            }
        }
        Require(positions.SequenceEqual(oraclePositions), "set positions match the law");
        Require(values.SequenceEqual(oracleValues), "set folded effects match the law");

        RequireThrows<ArgumentException>(() =>
            timelines.Gather(new ushort[] { loopingId, 2 }).Seek(new ushort[] { 0, 0 }, true).Apply(new float[2]), "unbound timeline id rejected");

        timelines.Dispose();
        RequireThrows<ObjectDisposedException>(() => timelines.Gather(ids), "disposed set rejected");
        Console.WriteLine($"timeline sets: {Rows} rows over 2 baked timelines x {Frames} frames, ids {loopingId}/{finiteId}, uniform, gather, streak, and mixed chunk paths");
    }

    internal static void Memory()
    {
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<TandemTrack, TandemClip>(new TandemTrack(2f))
            .Clip(0, 0u, 64u, new TandemClip(1f))
            .Looping()
            .Bake());
        BakedLane<TandemTrack, TandemClip>.Bind(asset);
        var positions = new ushort[256];
        var values = new float[256];

        long allocated;
        for (var attempt = 0; ; attempt++)
        {
            for (var pass = 0; pass < 1_000; pass++)
                Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 100_000; pass++)
                Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated == 0 || attempt >= 8) break;
        }
        Require(allocated == 0, $"warm lane allocated {allocated} B after settle attempts");
        Console.WriteLine($"allocation: 100k x 256-row lane applies retained {allocated} B; table+record bytes per tick {BakedLane<TandemTrack, TandemClip>.Duration * 28}");
        Console.WriteLine($"frame bytes: TimelineState 8, TimelineComponent 16, movement record 8");
    }

    internal static void BatchCapacity()
    {
        const int Rows = 200_000;
        using var asset = TimelineAsset.LoadAsset(new DomainBaker()
            .Track<TandemTrack, TandemClip>(new TandemTrack(1f))
            .Clip(0, 0u, 64u, new TandemClip(2f))
            .Looping()
            .Bake());
        BakedLane<TandemTrack, TandemClip>.Bind(asset);
        var positions = new ushort[Rows];
        var values = new float[Rows];
        for (var i = 0; i < Rows; i++)
            positions[i] = (ushort)(i % 64);

        for (var tick = 0; tick < 64; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);

        long checksum = 0;
        for (var i = 0; i < Rows; i++)
            checksum = unchecked(checksum * 31 + (long)values[i]);
        Require(checksum != 0, "batch capacity checksum computed");
        Require(values[0] == 576f && values[Rows - 1] == 576f, "capacity fold is 64 ticks x 9 per row");
        Console.WriteLine($"capacity: {Rows} rows x 64 ticks checksum {checksum}");
    }

    internal static void ModuleCapacity()
    {
        const int Tracks = 256;
        var baker = new DomainBaker();
        for (var track = 1; track <= Tracks; track++)
            baker.Track<TandemTrack, TandemClip>(new TandemTrack(track)).Clip(track - 1, 0u, 64u, new TandemClip(1f));
        using var asset = TimelineAsset.LoadAsset(baker.Looping().Bake());
        BakedLane<TandemTrack, TandemClip>.Bind(asset);

        var expected = 0f;
        for (var track = 1; track <= Tracks; track++)
            expected += track + 7f;
        for (var tick = 0; tick < 64; tick++)
            Require(BakedLane<TandemTrack, TandemClip>.Effect((ushort)tick) == expected, $"module fold at {tick}");

        var positions = new ushort[16];
        var values = new float[16];
        for (var tick = 0; tick < 10; tick++)
            Timeline<BakedLane<TandemTrack, TandemClip>>.Seek(positions, true).Apply(values);
        Require(values.All(value => value == expected * 10), "module capacity fold applied");
        Console.WriteLine($"module-capacity: {Tracks} tracks fold to {expected} per tick, x10 applied");
    }

    static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"receipt failed: {label}");
    }

    static void RequireThrows<TException>(Action action, string label) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException($"receipt failed: expected {typeof(TException).Name} ({label})");
    }
}
