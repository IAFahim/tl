using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tl;
using Xunit;

namespace Tl.Bake.Tests
{
public sealed unsafe class KindTickTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "tl-tick-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    static string Ns(string type, string data) => $"\"namespace\":\"KindFixture\",\"type\":\"{type}\",\"data\":{{{data}}}";

    string Pack(int seed)
    {
        Directory.CreateDirectory(_dir);
        var rng = new Random(seed);
        const int count = 8;
        for (var id = 0; id < count; id++)
        {
            var duration = rng.Next(2, 31);
            var loop = rng.Next(4) == 0;
            var next = !loop && rng.Next(3) == 0 ? (id + 1 + rng.Next(count - 1)) % count : -1;
            var hold = !loop && next < 0 && rng.Next(3) == 0;
            var sb = new StringBuilder();
            sb.Append($"{{\"name\":\"t{id}\",\"duration\":{duration}");
            if (loop) sb.Append(",\"loop\":true");
            if (hold) sb.Append(",\"hold\":true");
            if (next >= 0) sb.Append($",\"next\":\"t{next}\"");
            var channel = rng.Next(3);
            if (channel > 0) sb.Append(channel == 1 ? ",\"channel\":\"locomotion\"" : ",\"channel\":\"action\"");
            sb.Append(",\"tracks\":[");
            var clips = rng.Next(0, 6);
            for (var c = 0; c < clips; c++)
            {
                if (c > 0) sb.Append(',');
                var start = rng.Next(duration);
                var end = rng.Next(start + 1, duration + 1);
                string track, clip;
                switch (rng.Next(5))
                {
                    case 0: track = Ns("WalkTrack", "\"Scale\":1"); clip = Ns("WalkClip", $"\"Speed\":{rng.Next(1, 5)},\"Priority\":{rng.Next(0, 9)}"); break;
                    case 1: track = Ns("ArcTrack", "\"Scale\":1"); clip = Ns("ArcClip", $"\"Velocity\":{rng.Next(1, 5)}"); break;
                    case 2: track = Ns("HitTrack", "\"Scale\":1"); clip = Ns("HitClip", $"\"Amount\":{rng.Next(1, 30)}"); break;
                    case 3: track = Ns("FlameTrack", "\"Scale\":1"); clip = Ns("FlameClip", $"\"Power\":{rng.Next(1, 5)}"); break;
                    default: track = Ns("ChillTrack", "\"Scale\":1"); clip = Ns("ChillClip", $"\"Amount\":{rng.Next(1, 9)}"); break;
                }
                sb.Append($"{{{track},\"clips\":[{{{clip.Replace("\"data\"", $"\"start\":{start},\"end\":{end},\"data\"")}}}]}}");
            }
            sb.Append("],\"markers\":[");
            var markers = rng.Next(0, 3);
            for (var m = 0; m < markers; m++)
            {
                if (m > 0) sb.Append(',');
                sb.Append($"{{\"namespace\":\"KindFixture\",\"type\":\"StepMarker\",\"tick\":{rng.Next(duration)},\"data\":{{\"Delta\":{rng.Next(-3, 4)}}}}}");
            }
            sb.Append("]}");
            File.WriteAllText(Path.Combine(_dir, $"t{id}.json"), sb.ToString());
        }
        File.WriteAllText(Path.Combine(_dir, "tl.project.json"), "{\"schema\":1,\"channels\":[\"locomotion\",\"action\"],\"timelines\":[\"t*.json\"],\"kinds\":{\"KindFixture.Damage\":{\"gamePulses\":2}}}");
        var rc = LibraryPack.Run([_dir, "--assembly", typeof(KindTickTests).Assembly.Location, "--namespace", "KindFixture"]);
        Assert.Equal(0, rc);
        return Path.Combine(_dir, "game.tlib");
    }

    readonly record struct Key(byte Flags, byte Slot, ushort Timeline, ushort Step, byte Feeder, int Value);

    sealed class Run : IDisposable
    {
        public TimelineWorld World = null!;
        public PlaybackTable Players = null!;
        public KindStore Store = null!;
        public Dictionary<int, List<(ushort Window, ushort Feeder)>> Pairs = new();
        public readonly List<(int Target, int Value)> Sent = new();
        public void Dispose()
        {
            Store?.Dispose();
            Players?.Dispose();
            World?.Dispose();
        }
    }

    static Run Open(string tlib, int entities, string[]? names = null, ulong[]? reach = null)
    {
        var run = new Run { World = new TimelineWorld(File.ReadAllBytes(tlib)) };
        run.Players = new PlaybackTable(entities, 2);
        run.Store = new KindStore(run.World, run.Players, names, reach);
        return run;
    }

    static void CollectPairs(Run run)
    {
        run.Pairs.Clear();
        var sec = run.World.View.Kinds;
        for (ushort id = 1; id <= sec.TimelineCount; id++)
        {
            var tk = sec.TimelineOf(id);
            var set = new SortedSet<(ushort, ushort)>();
            if (tk->KindMask != 0)
            {
                var events = sec.Events(tk);
                for (var n = 0; n < tk->EventCount; n++)
                    if (events[n].Op != (byte)KindEventOp.Remove) set.Add((events[n].Window, events[n].Feeder));
            }
            run.Pairs[id] = set.ToList();
        }
    }

    static long Compare(Run run, int step, bool verbose)
    {
        long mismatches = 0;
        var sec = run.World.View.Kinds;
        var p = run.Players;
        var want = new Dictionary<(int, int), List<Key>>();
        List<Key> Bucket(int e, int k) => want.TryGetValue((e, k), out var l) ? l : want[(e, k)] = new List<Key>();
        for (var e = 0; e < p.EntityCapacity; e++)
            for (var i = 0; i < p.CountOf(e); i++)
            {
                var s = p.SlotsOf(e) + i;
                var id = s->Timeline;
                if (id == 0) continue;
                int t = s->Tick;
                var stopping = (s->Flags & PlaybackFlags.Stopping) != 0;
                var ended = (s->Flags & PlaybackFlags.Ended) != 0;
                var pending = (s->Flags & PlaybackFlags.StartPending) != 0;
                var began = (s->Flags & PlaybackFlags.Began) != 0;
                var windows = sec.Windows(sec.TimelineOf(id));
                foreach (var (w, f) in run.Pairs[id])
                {
                    var window = windows[w];
                    var feeder = sec.Feeders[f];
                    var inside = t >= window.Start && t < window.End;
                    var key = (byte)0;
                    if (feeder.Trigger == 0)
                    {
                        if (inside && !stopping && !ended) Bucket(e, feeder.Kind).Add(new Key((byte)KindEntryFlags.Span, (byte)i, id, w, feeder.Ordinal, 0));
                        continue;
                    }
                    if (stopping)
                    {
                        if (!pending && !ended && feeder.Trigger == 2 && inside && t < window.End - 1)
                            Bucket(e, feeder.Kind).Add(new Key((byte)(KindEntryFlags.Pulse | KindEntryFlags.Interrupted), (byte)i, id, w, feeder.Ordinal, 0));
                        continue;
                    }
                    if (!began && (ended || (s->Flags & PlaybackFlags.Paused) != 0)) continue;
                    if ((s->Flags & PlaybackFlags.ReenterPending) != 0) continue;
                    if (feeder.Trigger == 1)
                    {
                        if (t == window.Start) key = (byte)KindEntryFlags.Pulse;
                        else if (began && inside) key = (byte)(KindEntryFlags.Pulse | KindEntryFlags.Synthetic);
                    }
                    else if (feeder.Trigger == 2) { if (t == window.End - 1) key = (byte)KindEntryFlags.Pulse; }
                    else if (t == window.Start) key = (byte)KindEntryFlags.Pulse;
                    if (key != 0) Bucket(e, feeder.Kind).Add(new Key(key, (byte)i, id, w, feeder.Ordinal, 0));
                }
            }
        var damage = sec.IndexOf("KindFixture.Damage");
        for (var k = 0; k < run.Sent.Count;)
        {
            var (target, value) = run.Sent[k++];
            if (run.Store.RowOf(target, damage) >= 0) Bucket(target, damage).Add(new Key((byte)KindEntryFlags.Pulse, 0, 0, 0, 0, value));
        }
        for (var e = 0; e < p.EntityCapacity; e++)
            for (var k = 0; k < sec.KindCount; k++)
            {
                var row = run.Store.RowOf(e, k);
                var table = run.Store.Table(k);
                var have = new List<Key>();
                if (row >= 0)
                    for (var j = 0; j < table->Length[row]; j++)
                    {
                        var entry = table->At(row, j);
                        if (table->KindOf(entry) != k) continue;
                        var flags = (byte)entry->Flags;
                        if ((entry->Flags & KindEntryFlags.Span) != 0) flags &= unchecked((byte)~(byte)KindEntryFlags.Synthetic);
                        have.Add(new Key(flags, entry->Timeline == 0 ? (byte)0 : entry->Slot, entry->Timeline, entry->Step, entry->Timeline == 0 ? (byte)0 : entry->Feeder, entry->Timeline == 0 ? *(int*)((byte*)entry + 8) : 0));
                    }
                var expect = want.TryGetValue((e, k), out var l) ? l : new List<Key>();
                var a = have.OrderBy(x => x.GetHashCode()).ThenBy(x => x.ToString(), StringComparer.Ordinal).ToList();
                var b = expect.OrderBy(x => x.GetHashCode()).ThenBy(x => x.ToString(), StringComparer.Ordinal).ToList();
                var active = row >= 0 && table->IsActive(row);
                if (!a.SequenceEqual(b) || active != (row >= 0 && table->Length[row] > 0))
                {
                    mismatches++;
                    if (verbose && mismatches < 6) Console.WriteLine($"TICK step={step} entity={e} kind={sec.NameOf(k)} active={active} have=[{string.Join(",", a)}] want=[{string.Join(",", b)}]");
                }
            }
        return mismatches;
    }

    static ulong Script(Run run, int seed, int steps, bool check, int reloadAt, string? reloadPath, bool fold = false)
    {
        var rng = new Random(seed * 7919 + 1);
        var hash = 14695981039346656037ul;
        long mismatches = 0;
        var damage = run.World.View.Kinds.IndexOf("KindFixture.Damage");
        var pulses = run.Store.Pulses<int>("KindFixture.Damage");
        var folding = run.Store.Pulses<int, SumFold>("KindFixture.Damage");
        var sec = run.World.View.Kinds;
        for (var step = 0; step < steps; step++)
        {
            if (step == reloadAt && reloadPath != null)
            {
                run.World.Reload(File.ReadAllBytes(reloadPath));
                CollectPairs(run);
                sec = run.World.View.Kinds;
            }
            var p = run.Players;
            var n = rng.Next(0, 5);
            for (var r = 0; r < n; r++)
            {
                var e = rng.Next(p.EntityCapacity);
                var op = rng.Next(12);
                if (op < 6) p.Start(e, (ushort)rng.Next(1, 9), (ushort)rng.Next(0, 4));
                else if (op < 8) p.Stop(e, (ushort)rng.Next(1, 9));
                else if (op < 9) p.StopChannel(e, (byte)rng.Next(1, 3));
                else if (op < 10) p.StopAll(e);
            }
            run.Sent.Clear();
            for (var s = rng.Next(0, 4); s > 0; s--)
            {
                var target = rng.Next(p.EntityCapacity);
                var value = rng.Next(1, 20);
                if (!fold)
                {
                    pulses.To(target, value);
                    run.Sent.Add((target, value));
                    continue;
                }
                folding.To(target, value);
                if (run.Sent.Count > 0 && run.Sent[^1].Target == target) run.Sent[^1] = (target, run.Sent[^1].Value + value);
                else run.Sent.Add((target, value));
            }
            run.World.Tick(p, rng.Next(8) != 0);
            if (check) mismatches += Compare(run, step, true);
            for (var e = 0; e < p.EntityCapacity; e++)
            {
                for (var i = 0; i < p.CountOf(e); i++)
                {
                    var s = p.SlotsOf(e) + i;
                    hash = (hash ^ s->Timeline) * 1099511628211ul;
                    hash = (hash ^ s->Tick) * 1099511628211ul;
                    hash = (hash ^ (ulong)s->Flags) * 1099511628211ul;
                }
                for (var k = 0; k < sec.KindCount; k++)
                {
                    var row = run.Store.RowOf(e, k);
                    if (row < 0) continue;
                    var table = run.Store.Table(k);
                    for (var j = 0; j < table->Length[row]; j++)
                    {
                        var entry = table->At(row, j);
                        hash = (hash ^ entry->Timeline) * 1099511628211ul;
                        hash = (hash ^ entry->Step) * 1099511628211ul;
                        hash = (hash ^ (ulong)entry->Flags) * 1099511628211ul;
                    }
                }
            }
            run.Store.ClearPulses();
        }
        _ = damage;
        Assert.Equal(0, mismatches);
        return hash;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void IncrementalEntriesEqualTheFromScratchOracleOnRandomScripts(int seed)
    {
        var tlib = Pack(seed);
        ulong first, second;
        using (var run = Open(tlib, 10))
        {
            CollectPairs(run);
            first = Script(run, seed, 2500, true, -1, null);
            Assert.True(run.Store.GetStats(run.World.View.Kinds.IndexOf("KindFixture.Damage")).Rows > 0);
            Assert.True(run.Store.Table(run.World.View.Kinds.IndexOf("KindFixture.Burn"))->Grouped);
            Assert.True(run.Store.Table(run.World.View.Kinds.IndexOf("KindFixture.Burn")) == run.Store.Table(run.World.View.Kinds.IndexOf("KindFixture.Chill")));
        }
        using (var run = Open(tlib, 10))
        {
            CollectPairs(run);
            second = Script(run, seed, 2500, false, -1, null);
        }
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void FoldedGamePulsesEqualTheOracleWithConsecutiveTargetsMerged(int seed)
    {
        var tlib = Pack(seed);
        using var run = Open(tlib, 10);
        CollectPairs(run);
        Script(run, seed, 2500, true, -1, null, true);
        Assert.True(run.Store.GetStats(run.World.View.Kinds.IndexOf("KindFixture.Damage")).Rows > 0);
    }

    [Fact]
    public void ReloadRebuildsEntriesFromTheLivePlaybacks()
    {
        var tlib = Pack(5);
        using var run = Open(tlib, 10);
        CollectPairs(run);
        Script(run, 5, 1500, true, 700, tlib);
        Assert.True(run.World.View.Version == 2);
    }

    struct SumFold : IPulseFold<int>
    {
        public void Fold(ref int total, in int next) => total += next;
    }

    static int PulseSum(Run run, int entity, int kind, out int entries)
    {
        var row = run.Store.RowOf(entity, kind);
        var table = run.Store.Table(kind);
        var sum = 0;
        entries = 0;
        for (var i = 0; i < table->Length[row]; i++)
        {
            var at = table->At(row, i);
            if ((at->Flags & KindEntryFlags.Pulse) == 0) continue;
            sum += *(int*)((byte*)at + 8);
            entries++;
        }
        return sum;
    }

    [Fact]
    public void FoldedPulsesKeepTheSumAndMergeOnlyConsecutiveSameTargets()
    {
        var tlib = Pack(8);
        using var plain = Open(tlib, 4);
        using var folded = Open(tlib, 4);
        var a = plain.Store.Pulses<int>("KindFixture.Damage");
        var b = folded.Store.Pulses<int, SumFold>("KindFixture.Damage");
        foreach (var (target, value) in new[] { (0, 1), (0, 2), (0, 3), (1, 4), (0, 5), (0, 6), (2, 7) })
        {
            a.To(target, value);
            b.To(target, value);
        }
        plain.World.Tick(plain.Players);
        folded.World.Tick(folded.Players);
        var damage = plain.World.View.Kinds.IndexOf("KindFixture.Damage");
        foreach (var (entity, sum, entries, foldedEntries) in new[] { (0, 17, 5, 2), (1, 4, 1, 1), (2, 7, 1, 1), (3, 0, 0, 0) })
        {
            Assert.Equal(sum, PulseSum(plain, entity, damage, out var p));
            Assert.Equal(sum, PulseSum(folded, entity, damage, out var f));
            Assert.Equal(entries, p);
            Assert.Equal(foldedEntries, f);
        }
    }

    [Fact]
    public void FoldingNeedsTheFlagOnTheKind()
    {
        using var run = Open(Pack(8), 2);
        Assert.Throws<ArgumentException>(() => run.Store.Pulses<int, SumFold>("KindFixture.StatChange"));
        Assert.Throws<ArgumentException>(() => run.Store.Pulses<long, FoldLong>("KindFixture.Damage"));
        Assert.Throws<ArgumentException>(() => run.Store.Pulses<float, FoldFloat>("KindFixture.Lift"));
    }

    struct FoldLong : IPulseFold<long>
    {
        public void Fold(ref long total, in long next) => total += next;
    }

    struct FoldFloat : IPulseFold<float>
    {
        public void Fold(ref float total, in float next) => total += next;
    }

    [Fact]
    public void RequestsTickAndAdvanceGuardTheirPreconditions()
    {
        var tlib = Pack(6);
        using var world = new TimelineWorld(File.ReadAllBytes(tlib));
        using var plain = new PlaybackTable(2);
        Assert.Throws<InvalidOperationException>(() => world.Tick(plain));
        using var run = Open(tlib, 2);
        Assert.Throws<InvalidOperationException>(() => run.World.Advance(run.Players));
        Assert.Throws<ArgumentException>(() => run.Store.Pulses<long>("KindFixture.Damage"));
        Assert.Throws<ArgumentException>(() => run.Store.Pulses<int>("KindFixture.Locomotion"));
        Assert.Throws<ArgumentException>(() => run.Store.Pulses<int>("Poison"));
        Assert.Throws<InvalidOperationException>(() => new KindStore(run.World, run.Players));
    }

    [Fact]
    public void EntitiesOnlyReceiveTheirReachableKindsAndMissedPulsesAreCounted()
    {
        var tlib = Pack(7);
        using var run = Open(tlib, 4, ["KindFixture.Damage", "KindFixture.Locomotion"], [0b01ul, 0b10ul, 0b11ul, 0ul]);
        var damage = run.World.View.Kinds.IndexOf("KindFixture.Damage");
        var lift = run.World.View.Kinds.IndexOf("KindFixture.Lift");
        Assert.True(run.Store.RowOf(0, damage) >= 0);
        Assert.True(run.Store.RowOf(1, damage) < 0);
        Assert.True(run.Store.RowOf(2, lift) < 0);
        Assert.True(run.Store.GetStats(damage).Rows == 2);
        var pulses = run.Store.Pulses<int>("KindFixture.Damage");
        pulses.To(0, 5);
        pulses.To(1, 6);
        pulses.To(3, 7);
        run.World.Tick(run.Players);
        Assert.Equal(2, run.Store.DroppedPulses);
        Assert.Equal(1, run.Store.Table(damage)->Length[run.Store.RowOf(0, damage)]);
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            for (ushort id = 1; id <= 8; id++)
            {
                run.Players.Start(3, id);
                run.World.Tick(run.Players);
            }
        });
        Assert.Contains("cannot reach kind", ex.Message);
    }
}
}
