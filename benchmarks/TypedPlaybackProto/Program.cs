using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CombatLib;

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
    Console.WriteLine($"=== Timeline<T>.Forward/.Backward prototype: {n:N0} rows x {Ticks} ticks, segments of {Chunk}, best of {Runs} (Release) ===");

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

    Console.WriteLine("  expectations (this host, Ryzen 5 8500G): uniform ~0.15-0.25, waves ~0.20-0.30,");
    Console.WriteLine("  staggered ~4.1-4.5 (sort ties-to-loses here, loses clearly at 1M, loses ~13x");
    Console.WriteLine("  on uniform), backward == forward, catch-up x2 linear to slightly superlinear at 1M.");
    Lane("plain rows HP+=1        ", Stagger, () => PlainPass(staggered), n);
    Lane("hand lane               ", Stagger, () => HandPass(staggered, 1), n);
    Lane("Forward  (staggered)    ", Stagger, () => FwdPass(staggered, 1), n);
    Lane("Forward+sort (staggered)", Stagger, () => SortPass(staggered), n);
    Lane("Forward  (uniform)      ", Uni, () => FwdPass(uniform, 1), n);
    Lane("Forward+sort (uniform)  ", Uni, () => SortPass(uniform), n);
    Lane("Forward  (waves of 100) ", Wave, () => FwdPass(waves, 1), n);
    Lane("Forward  x2 catch-up    ", Stagger, () => FwdPass(staggered, 2), n);
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

static void TickHand<T>(World? x, int steps) where T : IAsset<T>
{
    if (x == null) return;
    for (var s = 0; s < x.N; s += Chunk)
    {
        var len = Math.Min(Chunk, x.N - s);
        HandChunk(x.Pos.AsSpan(s, len), x.Cyc.AsSpan(s, len), x.Hp.AsSpan(s, len), T.Duration, T.Effect, steps);
    }
}

static void HandChunk(Span<uint> pos, Span<uint> cyc, Span<float> hp, uint dur, Func<uint, float> effect, int steps)
{
    for (var i = 0; i < pos.Length; i++)
    {
        var p = pos[i];
        float d = 0;
        for (var s = 0; s < steps; s++)
        {
            d += effect(p);
            if (++p == dur) { p = 0; cyc[i]++; }
        }
        hp[i] += d;
        pos[i] = p;
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

static void TickWorld<T>(World? x, int dir) where T : IAsset<T>
{
    if (x == null) return;
    for (var s = 0; s < x.N; s += Chunk)
    {
        var len = Math.Min(Chunk, x.N - s);
        if (dir > 0)
            Timeline<T>.Forward(x.Pos.AsSpan(s, len)).Apply(x.Hp.AsSpan(s, len), x.Cyc.AsSpan(s, len));
        else
            Timeline<T>.Backward(x.Pos.AsSpan(s, len)).Apply(x.Hp.AsSpan(s, len), x.Cyc.AsSpan(s, len));
    }
}

static void TickSorted<T>(World? x) where T : IAsset<T>
{
    if (x == null) return;
    for (var s = 0; s < x.N; s += Chunk)
    {
        var len = Math.Min(Chunk, x.N - s);
        Sorted<T>.Tick(x.Pos.AsSpan(s, len), x.Cyc.AsSpan(s, len), x.Hp.AsSpan(s, len));
    }
}

long Sum(Worlds w)
{
    long s = 0;
    SumWorld(w.Combat, 31, 97, 1);
    SumWorld(w.Pulse, 7, 11, 3);
    SumWorld(w.Big, 13, 17, 5);
    return s;

    void SumWorld(World? x, int wp, int wc, int wh)
    {
        if (x == null) return;
        for (var i = 0; i < x.N; i++) s += (long)x.Hp[i] * wh + x.Pos[i] * wp + x.Cyc[i] * wc;
    }
}

sealed class World(int n, Func<int, uint> init)
{
    public readonly uint[] Pos = new uint[n];
    public readonly uint[] Cyc = new uint[n];
    public readonly float[] Hp = new float[n];
    public readonly int N = n;
    readonly Func<int, uint> _init = init;

    public World Reset()
    {
        for (var i = 0; i < N; i++) { Pos[i] = _init(i); Cyc[i] = 0; Hp[i] = 100; }
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
        new World(m, i => (uint)(i % CombatLib.Combat.Duration)).Reset(),
        new World(m, _ => 0).Reset(),
        new World(m, i => (uint)(i % CombatLib.Big.Duration)).Reset());

    public static Worlds Uniform(int n) => new(
        new World(n, _ => 5).Reset(), null, null);

    public static Worlds Waves(int n) => new(
        new World(n, i => (uint)(i / 100 % CombatLib.Combat.Duration)).Reset(), null, null);

    public void ResetAll()
    {
        Combat?.Reset();
        Pulse?.Reset();
        Big?.Reset();
    }
}

public static class Timeline<T> where T : IAsset<T>
{
    public static Plan Forward(Span<uint> pos) => Plan.Scan(pos, 1);
    public static Plan Backward(Span<uint> pos) => Plan.Scan(pos, -1);

    public ref struct Plan
    {
        Span<uint> _pos;
        int _count;

        public static Plan Scan(Span<uint> pos, int dir)
        {
            Runs<T>.Ensure(pos.Length);
            var runs = Runs<T>.Buffer;
            var starts = Runs<T>.Starts;
            var lens = Runs<T>.Lens;
            var n = pos.Length;
            var dur = T.Duration;
            var i = 0;
            var count = 0;
            while (i < n)
            {
                var e = RunEnd(pos, i);
                var q = pos[i];
                float d;
                uint np;
                uint wrap;
                if (dir > 0)
                {
                    d = T.Effect(q);
                    if (q + 1 == dur) { np = 0; wrap = 1; } else { np = q + 1; wrap = 0; }
                }
                else
                {
                    if (q == 0) { np = dur - 1; wrap = 0xFFu; } else { np = q - 1; wrap = 0; }
                    d = -T.Effect(np);
                }
                runs[count] = (ulong)BitConverter.SingleToUInt32Bits(d) << 32 | np << 8 | wrap;
                starts[count] = i;
                lens[count] = e - i;
                count++;
                i = e;
            }
            return new Plan { _pos = pos, _count = count };
        }

        public void Apply(Span<float> hp, Span<uint> cyc)
        {
            var runs = Runs<T>.Buffer;
            var starts = Runs<T>.Starts;
            var lens = Runs<T>.Lens;
            for (var r = 0; r < _count; r++)
            {
                var rec = runs[r];
                var d = BitConverter.UInt32BitsToSingle((uint)(rec >> 32));
                var np = (uint)((rec >> 8) & 0xFFFFFFu);
                var wrap = (uint)(rec & 0xFFu);
                if (wrap == 0xFFu) wrap = 0xFFFFFFFFu;
                var i = starts[r];
                var len = lens[r];
                AddTo(hp, i, d, len);
                FillU32(_pos, i, np, len);
                if (wrap != 0)
                    AddU32(cyc, i, wrap, len);
            }
        }

        static int RunEnd(Span<uint> p, int i)
        {
            var n = p.Length;
            var p0 = p[i];
            var j = i + 1;
            if (Vector512.IsHardwareAccelerated)
            {
                ref var pr = ref MemoryMarshal.GetReference(p);
                var vp0 = Vector512.Create(p0);
                var lim = n - 16;
                while (j <= lim)
                {
                    var m = Vector512.ExtractMostSignificantBits(Vector512.Equals(Vector512.LoadUnsafe(ref pr, (nuint)j), vp0));
                    if (m != 0xFFFFu)
                        return j + BitOperations.TrailingZeroCount(~m);
                    j += 16;
                }
            }
            while (j < n && p[j] == p0) j++;
            return j;
        }

        static void AddTo(Span<float> f, int i, float d, int len)
        {
            var k = 0;
            if (Vector512.IsHardwareAccelerated)
            {
                var v = Vector512.Create(d);
                var lim = len & ~15;
                for (; k < lim; k += 16) Vector512.Add(Vector512.LoadUnsafe(ref f[i], (nuint)k), v).StoreUnsafe(ref f[i], (nuint)k);
            }
            for (; k < len; k++) f[i + k] += d;
        }

        static void FillU32(Span<uint> p, int i, uint np, int len)
        {
            var k = 0;
            if (Vector512.IsHardwareAccelerated && len >= 16)
            {
                var v = Vector512.Create(np);
                var lim = len & ~15;
                for (; k < lim; k += 16) v.StoreUnsafe(ref p[i], (nuint)k);
            }
            for (; k < len; k++) p[i + k] = np;
        }

        static void AddU32(Span<uint> p, int i, uint add, int len)
        {
            var k = 0;
            if (Vector512.IsHardwareAccelerated && len >= 16)
            {
                var v = Vector512.Create(add);
                var lim = len & ~15;
                for (; k < lim; k += 16) Vector512.Add(Vector512.LoadUnsafe(ref p[i], (nuint)k), v).StoreUnsafe(ref p[i], (nuint)k);
            }
            for (; k < len; k++) p[i + k] += add;
        }
    }
}

static class Runs<T> where T : IAsset<T>
{
    public static ulong[] Buffer = [];
    public static int[] Starts = [];
    public static int[] Lens = [];

    public static void Ensure(int n)
    {
        if (Buffer.Length >= n) return;
        Buffer = new ulong[n];
        Starts = new int[n];
        Lens = new int[n];
    }
}

static class Sorted<T> where T : IAsset<T>
{
    public static void Tick(Span<uint> pos, Span<uint> cyc, Span<float> hp)
    {
        var n = pos.Length;
        if (n == 0) return;
        SortBuf<T>.Ensure(n, (int)T.Duration);
        var sorted = SortBuf<T>.Sorted;
        var sPos = SortBuf<T>.SPos;
        var sHp = SortBuf<T>.SHp;
        var sCyc = SortBuf<T>.SCyc;
        var counts = SortBuf<T>.Counts;
        var k = (int)T.Duration;
        Array.Clear(counts, 0, k);
        for (var i = 0; i < n; i++) counts[pos[i]]++;
        var total = 0;
        for (var c = 0; c < k; c++) { var v = counts[c]; counts[c] = total; total += v; }
        for (var i = 0; i < n; i++) sorted[counts[pos[i]]++] = i;
        for (var r = 0; r < n; r++) { var i = sorted[r]; sPos[r] = pos[i]; sHp[r] = hp[i]; sCyc[r] = cyc[i]; }
        Timeline<T>.Forward(sPos.AsSpan()).Apply(sHp.AsSpan(), sCyc.AsSpan());
        for (var r = 0; r < n; r++) { var i = sorted[r]; pos[i] = sPos[r]; hp[i] = sHp[r]; cyc[i] = sCyc[r]; }
    }
}

static class SortBuf<T> where T : IAsset<T>
{
    public static int[] Sorted = [];
    public static uint[] SPos = [];
    public static float[] SHp = [];
    public static uint[] SCyc = [];
    public static int[] Counts = [];

    public static void Ensure(int n, int k)
    {
        if (Sorted.Length >= n && Counts.Length >= k) return;
        Sorted = new int[n];
        SPos = new uint[n];
        SHp = new float[n];
        SCyc = new uint[n];
        Counts = new int[k];
    }
}

namespace CombatLib
{
    public interface IAsset<T> where T : IAsset<T>
    {
        static abstract uint Duration { get; }
        static abstract float Effect(uint pos);
    }

    public readonly struct Combat : IAsset<Combat>
    {
        public static uint Duration => 20;
        public static float Effect(uint pos) => pos < 10 ? 7f : -10f;
    }

    public readonly struct Pulse : IAsset<Pulse>
    {
        public static uint Duration => 1;
        public static float Effect(uint pos) => 3f;
    }

    public readonly struct Big : IAsset<Big>
    {
        public static readonly float[] Seg = [2, -4, 6, -8, 10, -12, 14, -16];
        public static uint Duration => 128;
        public static float Effect(uint pos) => Seg[pos >> 4];
    }
}
