using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;

unsafe
{
    var e = 50000; var a = 100; var threads = 1; var steps = 40; var warm = 10;
    for (var i = 0; i + 1 < args.Length; i += 2)
    {
        if (args[i] == "-e") e = int.Parse(args[i + 1]);
        if (args[i] == "-a") a = int.Parse(args[i + 1]);
        if (args[i] == "-t") threads = int.Parse(args[i + 1]);
        if (args[i] == "-s") steps = int.Parse(args[i + 1]);
    }
    var m = new Model(e, a);
    m.Seed();
    var samples = new double[steps];
    for (var s = 0; s < warm + steps; s++)
    {
        m.Frame++;
        var t0 = Stopwatch.GetTimestamp();
        if (threads == 1) m.Range(0, e);
        else Parallel.For(0, (e + 127) / 128, new ParallelOptions { MaxDegreeOfParallelism = threads }, c => m.Range(c * 128, Math.Min(e, c * 128 + 128)));
        var t1 = Stopwatch.GetTimestamp();
        if (s >= warm) samples[s - warm] = (t1 - t0) * 1000.0 / Stopwatch.Frequency;
    }
    Array.Sort(samples);
    Console.WriteLine($"net-2B-lean-vector256 entities={e} active={a} threads={threads} stepMs={samples[steps / 2]:F3} minMs={samples[0]:F3} nsPerSlot={samples[steps / 2] * 1e6 / ((double)e * a):F3} hash={m.Checksum():x16}");
}

unsafe sealed class Model
{
    const int Timelines = 500;
    readonly int _e, _a;
    readonly int[] _duration = new int[Timelines];
    readonly int[] _base = new int[Timelines];
    readonly short* _timelineOfRow;
    readonly int* _rows;
    readonly ushort* _origin;
    readonly float* _motion;
    readonly int* _vitals;
    readonly float* _stats;
    ulong _rng = 0x9E3779B97F4A7C15ul;
    public uint Frame;

    uint Rnd() { _rng ^= _rng << 13; _rng ^= _rng >> 7; _rng ^= _rng << 17; return (uint)_rng; }

    public Model(int e, int a)
    {
        _e = e; _a = a;
        var total = 0;
        for (var t = 0; t < Timelines; t++) { _duration[t] = 30 + t * 7 % 31; _base[t] = total; total += _duration[t]; }
        _timelineOfRow = (short*)NativeMemory.AllocZeroed(65536 * 2);
        _rows = (int*)NativeMemory.AlignedAlloc((nuint)(total * 32), 64);
        for (var t = 0; t < Timelines; t++)
            for (var k = 0; k < _duration[t]; k++)
            {
                _timelineOfRow[_base[t] + k] = (short)t;
                uint head = 0; float speed = 0, lift = 0; int prio = 0, stat, damage = 0;
                if (Rnd() % 3 != 0) { head |= 1; speed = (Rnd() % 64) * 0.25f; prio = (int)(Rnd() % 4); }
                if (Rnd() % 2 != 0) { head |= 2; lift = ((int)(Rnd() % 33) - 16) * 0.125f + 0.5f; }
                stat = Rnd() % 5 == 0 ? (int)(Rnd() % 7) - 3 : 0;
                if (Rnd() % 8 == 0) damage = (int)(1 + Rnd() % 9);
                var r = _rows + (_base[t] + k) * 8;
                r[0] = (int)head;
                ((float*)r)[1] = (head & 1) != 0 ? speed : 0f;
                r[2] = (head & 1) != 0 ? prio << 8 : -1;
                ((float*)r)[3] = (head & 2) != 0 ? lift : 0f;
                r[4] = damage;
                r[5] = stat;
                r[6] = k == _duration[t] - 1 ? 1 : 0;
                r[7] = 0;
            }
        _origin = (ushort*)NativeMemory.AlignedAlloc((nuint)((long)e * a * 2 + 64), 64);
        _motion = (float*)NativeMemory.AlignedAlloc((nuint)(e * 8), 64);
        _vitals = (int*)NativeMemory.AlignedAlloc((nuint)(e * 8), 64);
        _stats = (float*)NativeMemory.AlignedAlloc((nuint)(e * 8), 64);
    }

    public void Seed()
    {
        Frame = 1000;
        _rng = 0x1234567ul;
        for (var e = 0; e < _e; e++)
        {
            _motion[e * 2] = 0; _motion[e * 2 + 1] = 0;
            _vitals[e * 2] = 1000000; _vitals[e * 2 + 1] = 0;
            _stats[e * 2] = 1.0f + e % 7 * 0.5f; _stats[e * 2 + 1] = 2.0f + e % 3;
            for (var a = 0; a < _a; a++)
            {
                var t = (int)(Rnd() % Timelines);
                var s = (int)(Rnd() % (uint)(_duration[t] - 1));
                _origin[(long)e * _a + a] = (ushort)(_base[t] + s - Frame);
            }
        }
    }

    public void Range(int from, int to)
    {
        var f = Frame;
        var rows = _rows;
        var ended = stackalloc ulong[4];
        for (var e = from; e < to; e++)
        {
            var o = _origin + (long)e * _a;
            var fsum = Vector256<float>.Zero;
            var isum = Vector256<int>.Zero;
            var best = Vector256.Create(-1);
            ended[0] = ended[1] = ended[2] = ended[3] = 0;
            for (var a = 0; a < _a; a++)
            {
                var r = rows + ((ushort)(o[a] + f) << 3);
                var v = Vector256.LoadAligned(r);
                fsum += v.AsSingle();
                isum += v;
                best = Vector256.Max(best, v | Vector256.Create(a));
                ended[a >> 6] |= (ulong)(uint)r[6] << (a & 63);
            }
            var key = best.GetElement(2);
            var speed = key >= 0 ? ((float*)(rows + ((ushort)(o[key & 255] + f) << 3)))[1] : 0f;
            _motion[e * 2] += speed * _stats[e * 2];
            _motion[e * 2 + 1] += fsum.GetElement(3) * _stats[e * 2 + 1];
            _vitals[e * 2] -= isum.GetElement(4);
            _vitals[e * 2 + 1] += isum.GetElement(5);
            for (var w = 0; w < 4; w++)
                for (var mask = ended[w]; mask != 0; mask &= mask - 1)
                {
                    var a = w * 64 + System.Numerics.BitOperations.TrailingZeroCount(mask);
                    o[a] = (ushort)(o[a] - _duration[_timelineOfRow[(ushort)(o[a] + f)]]);
                }
        }
    }

    public ulong Checksum()
    {
        var h = 1469598103934665603ul;
        for (var e = 0; e < _e; e++)
        {
            var w = new uint[4];
            w[0] = *(uint*)(_motion + e * 2); w[1] = *(uint*)(_motion + e * 2 + 1);
            w[2] = (uint)_vitals[e * 2]; w[3] = (uint)_vitals[e * 2 + 1];
            for (var i = 0; i < 4; i++) h = (h ^ w[i]) * 1099511628211ul;
        }
        return h;
    }
}
