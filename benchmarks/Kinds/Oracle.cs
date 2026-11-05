using System.Runtime.InteropServices;

namespace Kinds;

public sealed unsafe class Oracle
{
    readonly record struct Key(EntryFlags Flags, byte Slot, ushort Timeline, ushort Step, int Value);

    public static int Run(int seed, int steps, int entities, bool verbose)
    {
        var timelines = Scripts.RandomTimelines(new Random(seed));
        using var library = new Library(timelines);
        var first = Simulate(library, timelines, seed, steps, entities, true, verbose, out var mismatches);
        var second = Simulate(library, timelines, seed, steps, entities, false, false, out _);
        Console.WriteLine($"ORACLE seed={seed} steps={steps} entities={entities} mismatches={mismatches} hash1={first:x16} hash2={second:x16} deterministic={first == second}");
        return mismatches == 0 && first == second ? 0 : 1;
    }

    static ulong Simulate(Library library, List<TimelineSpec> timelines, int seed, int steps, int entities, bool check, bool verbose, out long mismatches)
    {
        var shapes = new Dictionary<byte, (int, int)>
        {
            [KindId.Locomotion] = (1, 0),
            [KindId.Lift] = (2, 0),
            [KindId.Damage] = (2, 4),
            [KindId.StatChange] = (1, 0),
        };
        using var world = new World(library, entities, _ => 0b1111, shapes, KindId.Damage, inlineSlots: 2);
        using var cols = new ColumnSet(entities);
        using var expected = new ColumnSet(entities);
        for (var e = 0; e < entities; e++)
        {
            cols.WalkStat[e] = expected.WalkStat[e] = 1 + e % 3;
            cols.JumpPower[e] = expected.JumpPower[e] = 1 + e % 2;
            cols.Health[e] = expected.Health[e] = 1000;
        }
        var rng = new Random(seed * 7919 + 1);
        var sentLast = new List<(int target, int value)>();
        var sentNow = new List<(int target, int value)>();
        mismatches = 0;
        for (var step = 0; step < steps; step++)
        {
            var p = world.Playbacks;
            var requests = rng.Next(0, 6);
            for (var r = 0; r < requests; r++)
            {
                var e = rng.Next(entities);
                var op = rng.Next(10);
                if (op < 4)
                {
                    var id = (ushort)rng.Next(1, timelines.Count + 1);
                    p.Start(e, id, (ushort)rng.Next(timelines[id - 1].Duration), rng.Next(5) == 0);
                }
                else if (p.Count[e] > 0)
                {
                    var slot = (byte)rng.Next(p.Count[e]);
                    if (op < 6) p.Stop(e, slot);
                    else if (op < 7) p.Pause(e, slot);
                    else if (op < 9) p.Resume(e, slot);
                    else p.Reverse(e, slot);
                }
            }
            var stepDir = rng.Next(8) == 0 ? -1 : 1;
            world.Tick(stepDir);
            if (check) mismatches += Compare(world, library, sentLast, step, stepDir, expected, verbose && mismatches < 5);
            var c = cols.Columns;
            world.RunKind<Locomotion, Speed>(world.Tables[KindId.Locomotion], c, stepDir);
            world.RunKind<Lift, float>(world.Tables[KindId.Lift], c, stepDir);
            world.RunKind<StatChange, int>(world.Tables[KindId.StatChange], c, stepDir);
            world.RunKind<Damage, int>(world.Tables[KindId.Damage], c, stepDir);
            if (check && !cols.SameAs(expected))
            {
                mismatches++;
                if (verbose) Console.WriteLine($"ORACLE step={step} columns differ: {cols.FirstDifference(expected)}");
                expected.CopyFrom(cols);
            }
            sentNow.Clear();
            var sends = rng.Next(0, 4);
            for (var k = 0; k < sends; k++)
            {
                var target = rng.Next(entities);
                var value = rng.Next(1, 20);
                world.Send(target, value);
                sentNow.Add((target, value));
            }
            (sentLast, sentNow) = (sentNow, sentLast);
        }
        return cols.Hash();
    }

    static long Compare(World world, Library library, List<(int target, int value)> sent, int step, int stepDir, ColumnSet expected, bool verbose)
    {
        long mismatches = 0;
        var p = world.Playbacks;
        var want = new Dictionary<(int, byte), List<Key>>();
        List<Key> Bucket(int e, byte k) => want.TryGetValue((e, k), out var list) ? list : want[(e, k)] = new List<Key>();
        for (var e = 0; e < world.Entities; e++)
            for (var i = 0; i < p.Count[e]; i++)
            {
                var s = p.SlotOf(e, i);
                if (s->Timeline == 0) continue;
                var id = s->Timeline;
                int t = s->Tick;
                var stopping = (s->Flags & SlotFlags.Stopping) != 0;
                var ended = (s->Flags & SlotFlags.Ended) != 0;
                var pending = (s->Flags & SlotFlags.StartPending) != 0;
                for (var c = 0; c < library.ClipCount[id]; c++)
                {
                    var clip = library.ClipOf(id, c);
                    var inside = t >= clip->Start && t < clip->End;
                    if (clip->Mode == ClipMode.Span)
                    {
                        if (inside && !stopping && !ended) Bucket(e, clip->Kind).Add(new Key(EntryFlags.Span, (byte)i, id, (ushort)c, 0));
                        continue;
                    }
                    if (stopping)
                    {
                        if (!pending && !ended && clip->Mode == ClipMode.PulseOnEnd && inside && t < clip->End - 1)
                            Bucket(e, clip->Kind).Add(new Key(EntryFlags.Pulse | EntryFlags.Interrupted, (byte)i, id, (ushort)c, 0));
                        continue;
                    }
                    if ((s->Flags & SlotFlags.Played) == 0) continue;
                    var began = (s->Flags & SlotFlags.Began) != 0;
                    if (clip->Mode == ClipMode.PulseOnStart)
                    {
                        if (t == clip->Start) Bucket(e, clip->Kind).Add(new Key(EntryFlags.Pulse, (byte)i, id, (ushort)c, 0));
                        else if (began && inside) Bucket(e, clip->Kind).Add(new Key(EntryFlags.Pulse | EntryFlags.Synthetic, (byte)i, id, (ushort)c, 0));
                    }
                    else if (t == clip->End - 1) Bucket(e, clip->Kind).Add(new Key(EntryFlags.Pulse, (byte)i, id, (ushort)c, 0));
                }
            }
        foreach (var (target, value) in sent) Bucket(target, KindId.Damage).Add(new Key(EntryFlags.Pulse, 0, 0, 0, value));

        for (var e = 0; e < world.Entities; e++)
            for (byte k = 0; k < 4; k++)
            {
                var table = world.Tables[k]!;
                var row = world.RowOf(e, k);
                var have = new List<Key>();
                for (var j = 0; j < table.Length[row]; j++)
                {
                    var entry = table.At(row, j);
                    var flags = entry->Flags & ~((entry->Flags & EntryFlags.Span) != 0 ? EntryFlags.Synthetic : 0);
                    var value = entry->Timeline == 0 ? *(int*)((byte*)entry + 8) : 0;
                    have.Add(new Key(flags, entry->Timeline == 0 ? (byte)0 : entry->Slot, entry->Timeline, entry->Step, value));
                }
                var active = (table.Active[row >> 6] & (1ul << (row & 63))) != 0;
                var expect = want.TryGetValue((e, k), out var list) ? list : new List<Key>();
                var a = have.OrderBy(x => ((byte)x.Flags, x.Slot, x.Timeline, x.Step, x.Value)).ToList();
                var b = expect.OrderBy(x => ((byte)x.Flags, x.Slot, x.Timeline, x.Step, x.Value)).ToList();
                if (!a.SequenceEqual(b) || active != (have.Count > 0))
                {
                    mismatches++;
                    if (verbose) Console.WriteLine($"ORACLE step={step} entity={e} kind={k} active={active} have=[{string.Join(",", a)}] want=[{string.Join(",", b)}]");
                    continue;
                }
                Fold(world, library, table, row, e, k, stepDir, have, expected);
            }
        return mismatches;
    }

    static void Fold(World world, Library library, KindTable table, int row, int e, byte k, int stepDir, List<Key> order, ColumnSet expected)
    {
        var c = expected.Columns;
        var p = world.Playbacks;
        IEnumerable<Key> sequence = k == KindId.Locomotion ? order : order.OrderBy(x => ((byte)x.Flags, x.Slot, x.Timeline, x.Step, x.Value)).ToList();
        var hasF = false;
        var totalF = 0f;
        var totalI = 0;
        var speed = default(Speed);
        foreach (var key in sequence)
        {
            Entry entry = new() { Timeline = key.Timeline, Step = key.Step, Slot = key.Slot, Flags = key.Flags };
            var slot = p.SlotOf(e, key.Slot);
            if (key.Timeline != 0 && (key.Flags & EntryFlags.Span) != 0 && (slot->Flags & SlotFlags.Paused) != 0) continue;
            var dir = key.Timeline == 0 ? stepDir : (slot->Flags & SlotFlags.Reverse) != 0 ? -stepDir : stepDir;
            switch (k)
            {
                case KindId.Locomotion:
                {
                    var v = Locomotion.Evaluate(library, &entry, slot, dir, e, c);
                    if (!hasF) speed = v;
                    else Locomotion.Combine(ref speed, v);
                    break;
                }
                case KindId.Lift:
                    totalF += Lift.Evaluate(library, &entry, slot, dir, e, c);
                    break;
                case KindId.Damage:
                    totalI += key.Timeline == 0 ? key.Value : Damage.Evaluate(library, &entry, slot, dir, e, c);
                    break;
                case KindId.StatChange:
                    StatChange.Apply(StatChange.Evaluate(library, &entry, slot, dir, e, c), e, c);
                    break;
            }
            hasF = true;
        }
        if (!hasF) return;
        switch (k)
        {
            case KindId.Locomotion: Locomotion.Apply(speed, e, c); break;
            case KindId.Lift: Lift.Apply(totalF, e, c); break;
            case KindId.Damage: Damage.Apply(totalI, e, c); break;
        }
    }
}

public sealed unsafe class ColumnSet : IDisposable
{
    readonly int _n;
    readonly void* _block;
    public readonly Columns Columns;

    public ColumnSet(int n)
    {
        _n = n;
        var bytes = (nuint)n * 4 * 7;
        _block = NativeMemory.AlignedAlloc(bytes, 64);
        NativeMemory.Clear(_block, bytes);
        var f = (float*)_block;
        Columns = new Columns
        {
            MotionX = f,
            MotionY = f + n,
            Fx = f + 2 * n,
            WalkStat = f + 3 * n,
            JumpPower = f + 4 * n,
            Health = (int*)(f + 5 * n),
            Stamina = (int*)(f + 6 * n),
        };
    }

    public float* MotionX => Columns.MotionX;
    public float* MotionY => Columns.MotionY;
    public float* Fx => Columns.Fx;
    public float* WalkStat => Columns.WalkStat;
    public float* JumpPower => Columns.JumpPower;
    public int* Health => Columns.Health;
    public int* Stamina => Columns.Stamina;

    public bool SameAs(ColumnSet other) => new ReadOnlySpan<byte>(_block, _n * 28).SequenceEqual(new ReadOnlySpan<byte>(other._block, _n * 28));

    public void CopyFrom(ColumnSet other) => Buffer.MemoryCopy(other._block, _block, _n * 28, _n * 28);

    public string FirstDifference(ColumnSet other)
    {
        for (var e = 0; e < _n; e++)
        {
            if (BitConverter.SingleToInt32Bits(MotionX[e]) != BitConverter.SingleToInt32Bits(other.MotionX[e])) return $"entity={e} MotionX {MotionX[e]} vs {other.MotionX[e]}";
            if (BitConverter.SingleToInt32Bits(MotionY[e]) != BitConverter.SingleToInt32Bits(other.MotionY[e])) return $"entity={e} MotionY {MotionY[e]} vs {other.MotionY[e]}";
            if (Health[e] != other.Health[e]) return $"entity={e} Health {Health[e]} vs {other.Health[e]}";
            if (Stamina[e] != other.Stamina[e]) return $"entity={e} Stamina {Stamina[e]} vs {other.Stamina[e]}";
        }
        return "fx";
    }

    public ulong Hash()
    {
        var hash = 14695981039346656037ul;
        var bytes = new ReadOnlySpan<byte>(_block, _n * 28);
        foreach (var b in bytes) hash = (hash ^ b) * 1099511628211ul;
        return hash;
    }

    public void Dispose() => NativeMemory.AlignedFree(_block);
}
