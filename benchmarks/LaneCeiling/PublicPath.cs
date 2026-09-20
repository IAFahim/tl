using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tl.TestSupport;

namespace Tl.LaneCeilingProbe;

public readonly record struct ProbeClip(float Amount);

public readonly record struct ProbeTrack(float Scale) : IBlend<ProbeClip>
{
    public void Blend(in ProbeClip first, in ProbeClip second, float factor, out ProbeClip result)
        => result = new ProbeClip(first.Amount + (second.Amount - first.Amount) * factor);
}

internal static unsafe class PublicPath
{
    static TimelineAsset? _loop6;
    static TimelineAsset? _loop1024;
    static TimelineAsset? _finite1024;

    internal static void Install()
    {
        PairRuntime<ProbeTrack, ProbeClip>.Consume(&Execute, &BindFloat);
        _loop6 = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<ProbeTrack, ProbeClip>(new ProbeTrack(1.5f))
            .Clip(0, 0, 3, new ProbeClip(0.25f))
            .Clip(0, 3, 6, new ProbeClip(1.75f))
            .Looping()
            .Bake()));
        _loop1024 = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<ProbeTrack, ProbeClip>(new ProbeTrack(2f))
            .Clip(0, 0, 600, new ProbeClip(1.25f))
            .Clip(0, 600, 1024, new ProbeClip(-0.5f))
            .Looping()
            .Bake()));
        _finite1024 = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<ProbeTrack, ProbeClip>(new ProbeTrack(2f))
            .Clip(0, 0, 1024, new ProbeClip(0.75f))
            .Bake()));
    }

    internal static List<Row> Time(int rows, int reps, int rounds)
    {
        var results = new List<Row>();
        foreach (var (asset, name) in new[] { (_loop6!, "loop6"), (_loop1024!, "loop1024"), (_finite1024!, "finite1024") })
        {
            var index = asset.Index;
            foreach (var (shapeName, seed) in new (string, ushort[])[] { ("staggered", Positions(rows, 1024)), ("uniform", Uniform(rows)) })
            {
                var pos = new ushort[rows];
                var seedFx = Fx(rows);
                var fx = (float[])seedFx.Clone();
                var best = double.MaxValue;
                var allocated = 0L;
                for (var round = 0; round < rounds; round++)
                for (var rep = 0; rep < reps; rep++)
                {
                    Array.Copy(seed, pos, rows);
                    Array.Copy(seedFx, fx, rows);
                    var before = GC.GetTotalAllocatedBytes(precise: true);
                    var t = Stopwatch.GetTimestamp();
                    Timeline<ProbeTrack, ProbeClip>.Apply(index, pos, true, fx); Timeline.Advance(index, pos, true);
                    var ms = Stopwatch.GetElapsedTime(t).TotalMilliseconds;
                    allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
                    if (ms < best) best = ms;
                }
                results.Add(new Row($"public/{name}", shapeName, "Timeline<TTrack,TClip>.Apply+Step", best, best * 1_000_000.0 / rows, allocated));
            }
            {
                var ids = new ushort[rows];
                Array.Fill(ids, index);
                var pos = new ushort[rows];
                var seed = Uniform(rows);
                var best = double.MaxValue;
                var allocated = 0L;
                for (var round = 0; round < rounds; round++)
                for (var rep = 0; rep < reps; rep++)
                {
                    Array.Copy(seed, pos, rows);
                    var before = GC.GetTotalAllocatedBytes(precise: true);
                    var t = Stopwatch.GetTimestamp();
                    Timeline<ProbeTrack, ProbeClip>.Advance(index, pos, true);
                    var ms = Stopwatch.GetElapsedTime(t).TotalMilliseconds;
                    allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
                    if (ms < best) best = ms;
                }
                results.Add(new Row($"public/{name}", "uniform", "Timeline.Advance(index)", best, best * 1_000_000.0 / rows, allocated));
            }
            {
                var ids = new ushort[rows];
                Array.Fill(ids, index);
                var pos = new ushort[rows];
                var seed = Uniform(rows);
                var best = double.MaxValue;
                var allocated = 0L;
                for (var round = 0; round < rounds; round++)
                for (var rep = 0; rep < reps; rep++)
                {
                    Array.Copy(seed, pos, rows);
                    var before = GC.GetTotalAllocatedBytes(precise: true);
                    var t = Stopwatch.GetTimestamp();
                    Timeline<ProbeTrack, ProbeClip>.Advance(ids, pos, true);
                    var ms = Stopwatch.GetElapsedTime(t).TotalMilliseconds;
                    allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
                    if (ms < best) best = ms;
                }
                results.Add(new Row($"public/{name}", "uniform", "Timeline.Advance(ids)", best, best * 1_000_000.0 / rows, allocated));
            }
        }
        return results;
    }

    static ushort[] Positions(int n, int duration)
    {
        var a = new ushort[n];
        for (var i = 0; i < n; i++) a[i] = (ushort)(i % duration);
        return a;
    }

    static ushort[] Uniform(int n)
    {
        var a = new ushort[n];
        Array.Fill(a, (ushort)3);
        return a;
    }

    static float[] Fx(int n)
    {
        var fx = new float[n];
        var state = 0x243F6A8885A308D3ul;
        for (var i = 0; i < n; i++)
        {
            state ^= state << 13; state ^= state >> 7; state ^= state << 17;
            fx[i] = (float)((state >> 11) / 9007199254740992d) * 64f - 32f;
        }
        return fx;
    }

    static void Execute(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
        ProbeClip scratch = default;
        var frame = TickFrame.ToFrame<ProbeTrack, ProbeClip>(slot, pair, tick, flags, ref scratch);
        var sign = frame.Has(FrameFlags.Reverse) ? -1f : 1f;
        ((float*)columns[0])[row] += sign * frame.Clip.Amount * frame.Track.Scale;
    }

    static void BindFloat(ulong* keys, int keyCount, byte* table)
    {
        for (var i = 0; i < keyCount; i++)
            if (keys[i] == TypeKey<float>.Value)
            {
                table[0] = (byte)(i + 1);
                return;
            }
    }
}
