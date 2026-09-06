using System.Numerics;
using System.Runtime.CompilerServices;

namespace Tl.Algorithms;

public readonly record struct Clip(int Start, int End, int Payload);
public readonly record struct Payload(int Id, float Value);
public readonly record struct Region(int Start, int End, int A, int B, int BlendStart, int BlendLength);

public interface ISink
{
    void Sample(in Payload payload, float weight);
}

public readonly record struct Receipt(float Sum, long Ids, int Count);

public struct SumSink : ISink
{
    public float Sum;
    public long Ids;
    public int Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sample(in Payload payload, float weight)
    {
        Sum += payload.Value * weight;
        Ids += payload.Id;
        Count++;
    }

    public readonly Receipt Result => new(Sum, Ids, Count);
}

public record struct TraceSink : ISink
{
    public int Count;
    public int First;
    public int Second;
    public int WeightA;
    public int WeightB;

    public void Sample(in Payload payload, float weight)
    {
        if (Count == 0)
        {
            First = payload.Id;
            WeightA = BitConverter.SingleToInt32Bits(weight);
        }
        else if (Count == 1)
        {
            Second = payload.Id;
            WeightB = BitConverter.SingleToInt32Bits(weight);
        }
        else
        {
            throw new InvalidOperationException("More than two samples.");
        }

        Count++;
    }
}

public interface IGenerated
{
    static abstract Fixture Create();
    static abstract void Tree<TSink>(int tick, Payload[] payloads, ref TSink sink) where TSink : struct, ISink;
    static abstract void State<TSink>(int tick, ref int state, Payload[] payloads, ref TSink sink) where TSink : struct, ISink;
}

public sealed class Fixture
{
    public required int Duration { get; init; }
    public required Clip[] Clips { get; init; }
    public required Payload[] Payloads { get; init; }
    public required Region[] Regions { get; init; }
    public required ushort[] Dense { get; init; }
    public required ulong[] Bits { get; init; }
    public required ushort[] Prefix { get; init; }

    public static Fixture Build(int count, int duration)
    {
        uint random = 0x13E48AB9;
        uint Next()
        {
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            return random;
        }

        var step = duration / (count + 1);
        var starts = new int[count];
        var clips = new Clip[count];
        var payloads = new Payload[count];

        for (var i = 0; i < count; i++)
        {
            starts[i] = (i + 1) * step + (int)(Next() % (uint)Math.Max(1, step / 3));
            payloads[i] = new Payload(i + 1, (Next() % 1000 + 1) * 0.001f);
        }

        for (var i = 0; i < count; i++)
        {
            var end = i + 1 == count ? duration - 2 : starts[i + 1];

            if (i + 1 < count)
            {
                end += (i % 4) switch
                {
                    0 => -Math.Max(1, step / 4),
                    1 => Math.Max(1, step / 3),
                    2 => 0,
                    _ => 1
                };
            }

            clips[i] = new Clip(starts[i], end, i);
        }

        var cuts = new SortedSet<int> { 0, duration };
        foreach (var clip in clips)
        {
            cuts.Add(clip.Start);
            cuts.Add(clip.End);
        }

        var points = cuts.ToArray();
        var regions = new Region[points.Length - 1];

        for (var i = 0; i < regions.Length; i++)
        {
            var active = new List<int>();
            for (var j = 0; j < clips.Length; j++)
                if (clips[j].Start <= points[i] && points[i] < clips[j].End)
                    active.Add(j);

            if (active.Count > 2)
                throw new InvalidOperationException("Invalid fixture overlap.");

            var a = active.Count > 0 ? active[0] : -1;
            var b = active.Count > 1 ? active[1] : -1;
            var start = b >= 0 ? Math.Max(clips[a].Start, clips[b].Start) : 0;
            var end = b >= 0 ? Math.Min(clips[a].End, clips[b].End) : 0;
            regions[i] = new Region(points[i], points[i + 1], a, b, start, end - start);
        }

        var dense = new ushort[duration];
        var bits = new ulong[(duration + 63) / 64];
        var prefix = new ushort[bits.Length];

        for (var i = 0; i < regions.Length; i++)
        {
            var region = regions[i];
            Array.Fill(dense, checked((ushort)i), region.Start, region.End - region.Start);
            bits[region.Start >> 6] |= 1UL << (region.Start & 63);
        }

        var total = 0;
        for (var i = 0; i < bits.Length; i++)
        {
            prefix[i] = checked((ushort)total);
            total += BitOperations.PopCount(bits[i]);
        }

        return new Fixture
        {
            Duration = duration, Clips = clips, Payloads = payloads,
            Regions = regions, Dense = dense, Bits = bits, Prefix = prefix
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Binary(int tick)
    {
        var lo = 0;
        var hi = Regions.Length;

        while (lo < hi)
        {
            var mid = (lo + hi) >>> 1;
            if (Regions[mid].Start <= tick)
                lo = mid + 1;
            else
                hi = mid;
        }

        return lo - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Rank(int tick)
    {
        var word = tick >> 6;
        var mask = ulong.MaxValue >> (63 - (tick & 63));
        return Prefix[word] + BitOperations.PopCount(Bits[word] & mask) - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit<TSink>(int id, int tick, ref TSink sink) where TSink : struct, ISink
    {
        ref readonly var region = ref Regions[id];
        if (region.A < 0)
            return;

        if (region.B < 0)
        {
            sink.Sample(in Payloads[region.A], 1f);
            return;
        }

        var factor = region.BlendLength <= 1 ? 0.5f : (float)(tick - region.BlendStart) / (region.BlendLength - 1);
        sink.Sample(in Payloads[region.A], 1f - factor);
        sink.Sample(in Payloads[region.B], factor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Cursor<TSink>(int tick, ref int state, ref TSink sink) where TSink : struct, ISink
    {
        while (tick >= Regions[state].End)
            state++;

        Emit(state, tick, ref sink);
    }

    public void Oracle<TSink>(int tick, ref TSink sink) where TSink : struct, ISink
    {
        var a = -1;
        var b = -1;

        for (var i = 0; i < Clips.Length; i++)
        {
            if (tick < Clips[i].Start || tick >= Clips[i].End)
                continue;

            if (a < 0) a = i;
            else if (b < 0) b = i;
            else throw new InvalidOperationException("Invalid oracle overlap.");
        }

        if (a < 0) return;
        if (b < 0)
        {
            sink.Sample(in Payloads[a], 1f);
            return;
        }

        var start = Math.Max(Clips[a].Start, Clips[b].Start);
        var length = Math.Min(Clips[a].End, Clips[b].End) - start;
        var factor = length <= 1 ? 0.5f : (float)(tick - start) / (length - 1);
        sink.Sample(in Payloads[a], 1f - factor);
        sink.Sample(in Payloads[b], factor);
    }
}
