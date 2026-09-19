using System.Diagnostics;
using CombatLib;
using Tl;

const int Chunk = 256;
const int Ticks = 60;
const int Warmup = 3;
const int Runs = 5;

var size = args.Length > 0 ? int.Parse(args[0]) : 100_000;

RunAll(size);
if (args.Length == 0) RunAll(1_000_000);
return 0;

void RunAll(int n)
{
    Console.WriteLine($"=== Timeline<T>.Seek typed lane: {n:N0} rows x {Ticks} ticks, segments of {Chunk}, best of {Runs} (Release) ===");

    var m = n / 3;
    var staggered = Worlds.Staggered(m);
    var uniform   = Worlds.Uniform(n);
    var waves     = Worlds.Waves(n);

    void Stagger() => staggered.ResetAll();
    void Uni()     => uniform.ResetAll();
    void Wave()    => waves.ResetAll();

    Stagger(); var expect1 = HandPass(staggered, 1);
    Stagger(); var expect0 = Sum(staggered);
    Uni(); var expectUni = HandPass(uniform, 1);
    Wave(); var expectWave = HandPass(waves, 1);
    Stagger(); var expect2 = HandPass(staggered, 2);

    Console.WriteLine("  parity (vs hand lane, exact checksums; any FAIL exits nonzero):");
    Check("staggered fwd    ", expect1, () => { Stagger(); return FwdPass(staggered, 1); });
    Check("uniform   fwd    ", expectUni, () => { Uni(); return FwdPass(uniform, 1); });
    Check("waves     fwd    ", expectWave, () => { Wave(); return FwdPass(waves, 1); });
    Check("backward rewind20", expect0, () => { Stagger(); return Rewind(staggered, 20); });
    Check("catchup fwd x2   ", expect2, () => { Stagger(); return FwdPass(staggered, 2); });
    Check("sorted   fwd    ", expect1, () => { Stagger(); return SortPass(staggered); });
    Check("sorted   uniform", expectUni, () => { Uni(); return SortPass(uniform); });
    Check("sorted   waves  ", expectWave, () => { Wave(); return SortPass(waves); });

    Console.WriteLine("  expectations (this host, Ryzen 5 8500G): uniform ~0.05-0.15, waves ~0.08-0.20,");
    Console.WriteLine("  staggered ~2.5-4.5 (sort ties-to-loses here, loses clearly at 1M),");
    Console.WriteLine("  backward == forward, catch-up x2 linear to slightly superlinear at 1M.");
    Lane("plain rows HP+=1        ", Stagger, () => PlainPass(staggered), n);
    Lane("hand lane               ", Stagger, () => HandPass(staggered, 1), n);
    Lane("Seek     (staggered)    ", Stagger, () => FwdPass(staggered, 1), n);
    Lane("Seek+sort (staggered)   ", Stagger, () => SortPass(staggered), n);
    Lane("Seek     (uniform)      ", Uni, () => FwdPass(uniform, 1), n);
    Lane("Seek+sort (uniform)     ", Uni, () => SortPass(uniform), n);
    Lane("Seek     (waves of 100) ", Wave, () => FwdPass(waves, 1), n);
    Lane("Seek     x2 catch-up    ", Stagger, () => FwdPass(staggered, 2), n);
    Lane("Backward (uniform)      ", Uni, () => BwdPass(uniform), n);
    Console.WriteLine();
}

void Check(string name, long expect, Func<long> proto)
{
    var p = proto();
    var ok = p == expect;
    Console.WriteLine($"    {name}: {(ok ? "PASS" : $"FAIL p={p} want={expect}")}");
    if (!ok) Environment.Exit(1);
}

void Lane(string name, Action reset, Func<long> pass, int n)
{
    for (var i = 0; i < Warmup; i++) { reset(); pass(); }
    double best = double.MaxValue;
    for (var r = 0; r < Runs; r++)
    {
        reset();
        var sw = Stopwatch.StartNew();
        pass();
        sw.Stop();
        best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
    }
    Console.WriteLine($"  {name} {best / Ticks,9:F4} ms/frame {best * 1e6 / (n * Ticks),7:F2} ns/row");
}

long PlainPass(Worlds w)
{
    for (var t = 0; t < Ticks; t++)
        foreach (var x in w.All)
            for (var i = 0; i < x.N; i++) x.Hp[i] += 1;
    return 0;
}

long HandPass(Worlds w, int steps)
{
    for (var t = 0; t < Ticks; t++)
    {
        TickHand<CombatLib.Combat>(w.Combat, steps);
        TickHand<CombatLib.Pulse>(w.Pulse, steps);
        TickHand<CombatLib.Big>(w.Big, steps);
    }
    return Sum(w);
}

static void TickHand<T>(World? x, int steps) where T : unmanaged, ITimelineLane<T>
{
    if (x is null) return;
    for (var s = 0; s < x.N; s += Chunk)
    {
        var len = Math.Min(Chunk, x.N - s);
        HandChunk<T>(x.Pos.AsSpan(s, len), x.Hp.AsSpan(s, len), steps);
    }
}

static void HandChunk<T>(Span<ushort> pos, Span<float> hp, int steps) where T : unmanaged, ITimelineLane<T>
{
    var dur = T.Duration;
    for (var i = 0; i < pos.Length; i++)
    {
        var p = pos[i];
        float d = 0;
        for (var s = 0; s < steps; s++)
        {
            d += T.Effect((ushort)p);
            if (++p == dur) p = 0;
        }
        hp[i] += d;
        pos[i] = (ushort)p;
    }
}

long FwdPass(Worlds w, int calls)
{
    for (var t = 0; t < Ticks; t++)
        for (var k = 0; k < calls; k++)
        {
            TickWorld<CombatLib.Combat>(w.Combat, 1);
            TickWorld<CombatLib.Pulse>(w.Pulse, 1);
            TickWorld<CombatLib.Big>(w.Big, 1);
        }
    return Sum(w);
}

long BwdPass(Worlds w)
{
    for (var t = 0; t < Ticks; t++)
        TickWorld<CombatLib.Combat>(w.Combat, -1);
    return Sum(w);
}

long Rewind(Worlds w, int back)
{
    for (var t = 0; t < back; t++)
    {
        TickWorld<CombatLib.Combat>(w.Combat, 1);
        TickWorld<CombatLib.Pulse>(w.Pulse, 1);
        TickWorld<CombatLib.Big>(w.Big, 1);
    }
    for (var t = 0; t < back; t++)
    {
        TickWorld<CombatLib.Combat>(w.Combat, -1);
        TickWorld<CombatLib.Pulse>(w.Pulse, -1);
        TickWorld<CombatLib.Big>(w.Big, -1);
    }
    return Sum(w);
}

long SortPass(Worlds w)
{
    for (var t = 0; t < Ticks; t++)
    {
        TickSorted<CombatLib.Combat>(w.Combat);
        TickSorted<CombatLib.Pulse>(w.Pulse);
        TickSorted<CombatLib.Big>(w.Big);
    }
    return Sum(w);
}

static void TickWorld<T>(World? x, int dir) where T : unmanaged, ITimelineLane<T>
{
    if (x is null) return;
    for (var s = 0; s < x.N; s += Chunk)
    {
        var len = Math.Min(Chunk, x.N - s);
        Timeline<T>.Apply(x.Pos.AsSpan(s, len), dir > 0, x.Hp.AsSpan(s, len)); Timeline<T>.Step(x.Pos.AsSpan(s, len), dir > 0);
    }
}

static void TickSorted<T>(World? x) where T : unmanaged, ITimelineLane<T>
{
    if (x is null) return;
    for (var s = 0; s < x.N; s += Chunk)
    {
        var len = Math.Min(Chunk, x.N - s);
        Sorted<T>.Tick(x.Pos.AsSpan(s, len), x.Hp.AsSpan(s, len));
    }
}

long Sum(Worlds w)
{
    long s = 0;
    SumWorld(w.Combat, 31, 1);
    SumWorld(w.Pulse, 7, 3);
    SumWorld(w.Big, 13, 5);
    return s;

    void SumWorld(World? x, int wp, int wh)
    {
        if (x is null) return;
        for (var i = 0; i < x.N; i++) s += (long)x.Hp[i] * wh + x.Pos[i] * wp;
    }
}

sealed class World(int n, Func<int, ushort> init)
{
    public readonly ushort[] Pos = new ushort[n];
    public readonly float[] Hp = new float[n];
    public readonly int N = n;
    readonly Func<int, ushort> _init = init;

    public World Reset()
    {
        for (var i = 0; i < N; i++) { Pos[i] = _init(i); Hp[i] = 100; }
        return this;
    }
}

sealed class Worlds(World combat, World? pulse, World? big)
{
    public readonly World Combat = combat;
    public readonly World? Pulse = pulse;
    public readonly World? Big = big;
    public IEnumerable<World> All { get { if (Combat != null) yield return Combat; if (Pulse != null) yield return Pulse; if (Big != null) yield return Big; } }

    public static Worlds Staggered(int m) => new(
        new World(m, i => (ushort)(i % CombatLib.Combat.Duration)).Reset(),
        new World(m, _ => 0).Reset(),
        new World(m, i => (ushort)(i % CombatLib.Big.Duration)).Reset());

    public static Worlds Uniform(int n) => new(
        new World(n, _ => (ushort)5).Reset(), null, null);

    public static Worlds Waves(int n) => new(
        new World(n, i => (ushort)(i / 100 % CombatLib.Combat.Duration)).Reset(), null, null);

    public void ResetAll()
    {
        Combat?.Reset();
        Pulse?.Reset();
        Big?.Reset();
    }
}

static class Sorted<T> where T : unmanaged, ITimelineLane<T>
{
    public static void Tick(Span<ushort> pos, Span<float> hp)
    {
        var n = pos.Length;
        if (n == 0) return;
        SortBuf<T>.Ensure(n, (int)T.Duration);
        var sorted = SortBuf<T>.Sorted;
        var sPos = SortBuf<T>.SPos;
        var sHp = SortBuf<T>.SHp;
        var counts = SortBuf<T>.Counts;
        var k = (int)T.Duration;
        Array.Clear(counts, 0, k);
        for (var i = 0; i < n; i++) counts[pos[i]]++;
        var total = 0;
        for (var c = 0; c < k; c++) { var v = counts[c]; counts[c] = total; total += v; }
        for (var i = 0; i < n; i++) sorted[counts[pos[i]]++] = i;
        for (var r = 0; r < n; r++) { var i = sorted[r]; sPos[r] = pos[i]; sHp[r] = hp[i]; }
        Timeline<T>.Apply(sPos, true, sHp); Timeline<T>.Step(sPos, true);
        for (var r = 0; r < n; r++) { var i = sorted[r]; pos[i] = sPos[r]; hp[i] = sHp[r]; }
    }
}

static class SortBuf<T> where T : unmanaged, ITimelineLane<T>
{
    public static int[] Sorted = [];
    public static ushort[] SPos = [];
    public static float[] SHp = [];
    public static int[] Counts = [];

    public static void Ensure(int n, int k)
    {
        if (Sorted.Length >= n && Counts.Length >= k) return;
        Sorted = new int[n];
        SPos = new ushort[n];
        SHp = new float[n];
        Counts = new int[k];
    }
}

namespace CombatLib
{
    public readonly struct Combat : Tl.ITimelineLane<Combat>
    {
        public static ushort Duration => 20;
        public static bool Looping => true;
        public static float Effect(ushort pos) => pos < 10 ? 7f : -10f;
        public static float InverseEffect(ushort pos) => pos < 10 ? -7f : 10f;
    }

    public readonly struct Pulse : Tl.ITimelineLane<Pulse>
    {
        public static ushort Duration => 1;
        public static bool Looping => true;
        public static float Effect(ushort pos) => 3f;
        public static float InverseEffect(ushort pos) => -3f;
    }

    public readonly struct Big : Tl.ITimelineLane<Big>
    {
        public static readonly float[] Seg = [2, -4, 6, -8, 10, -12, 14, -16];
        public static ushort Duration => 128;
        public static bool Looping => true;
        public static float Effect(ushort pos) => Seg[pos >> 4];
        public static float InverseEffect(ushort pos) => -Seg[pos >> 4];
    }
}
