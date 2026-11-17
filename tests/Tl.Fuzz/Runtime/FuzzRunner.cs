using System.Globalization;
using System.Runtime.InteropServices;

namespace Tl.Fuzz.Runtime;

public static class FuzzRunner
{
    public static int Scale { get; } = ParseScale();
    public static bool HasExplicitSeed { get; } = TryParseSeed(out _explicitSeed);
    public static ulong ExplicitSeed => _explicitSeed;
    public static bool HasExplicitIter { get; } = TryParseIter(out _explicitIter);
    public static int ExplicitIter => _explicitIter;

    static readonly ulong _explicitSeed;
    static readonly int _explicitIter;

    static int ParseScale()
    {
        var raw = Environment.GetEnvironmentVariable("TL_FUZZ_SCALE");
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 1 ? value : 1;
    }

    static bool TryParseSeed(out ulong seed)
    {
        var raw = Environment.GetEnvironmentVariable("TL_FUZZ_SEED");
        if (string.IsNullOrWhiteSpace(raw))
        {
            seed = 0;
            return false;
        }
        raw = raw.Trim();
        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.TryParse(raw.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out seed);
        return ulong.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed);
    }

    static bool TryParseIter(out int iter)
    {
        var raw = Environment.GetEnvironmentVariable("TL_FUZZ_ITER");
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0)
        {
            iter = value;
            return true;
        }
        iter = 0;
        return false;
    }

    public static void Run(string target, ulong defaultSeed, int baseCases, Action<FuzzRandom, int> body)
    {
        var seed = HasExplicitSeed ? ExplicitSeed : defaultSeed;
        var start = HasExplicitIter ? ExplicitIter : 0;
        var end = HasExplicitIter ? start + 1 : Math.Max(1, baseCases * Scale);
        for (var iter = start; iter < end; iter++)
        {
            try
            {
                body(FuzzRandom.FromSeeds(seed, (ulong)iter), iter);
            }
            catch (FuzzFailureException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FuzzFailureException(target, seed, iter, ex.Message, ex);
            }
        }
    }

    public static void Fail(string target, ulong seed, int iter, string message)
        => throw new FuzzFailureException(target, seed, iter, message, null);

    public static ulong Mix(ulong hash, ReadOnlySpan<byte> bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            hash ^= bytes[i];
            hash *= 1099511628211UL;
        }
        return hash;
    }

    public static ulong MixStruct<T>(ulong hash, ReadOnlySpan<T> span) where T : unmanaged
        => Mix(hash, MemoryMarshal.AsBytes(span));

    public static string? CompareBytes(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, string label)
    {
        if (left.Length != right.Length)
            return $"{label}: length {left.Length} != {right.Length}";
        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
                return $"{label}: byte[{i}] 0x{left[i]:X2} != 0x{right[i]:X2}";
        }
        return null;
    }

    public static unsafe string? ComparePlaybacks(PlaybackTable left, PlaybackTable right)
    {
        if (left.EntityCapacity != right.EntityCapacity)
            return $"EntityCapacity {left.EntityCapacity} != {right.EntityCapacity}";
        var n = left.EntityCapacity;
        var pw = left.PlayingWords;
        if (pw != right.PlayingWords)
            return $"PlayingWords {pw} != {right.PlayingWords}";
        for (var w = 0; w < pw; w++)
        {
            if (left.PlayingBits[w] != right.PlayingBits[w])
                return $"Playing[{w}] 0x{left.PlayingBits[w]:X16} != 0x{right.PlayingBits[w]:X16}";
        }
        var ls = stackalloc Playback[PlaybackTable.MaxSlots];
        var rs = stackalloc Playback[PlaybackTable.MaxSlots];
        for (var e = 0; e < n; e++)
        {
            var lc = left.CountOf(e);
            var rc = right.CountOf(e);
            if (lc != rc)
                return $"entity {e} slot count {lc} != {rc}";
            Decode(left, e, ls);
            Decode(right, e, rs);
            var lw = (ulong*)ls;
            var rw = (ulong*)rs;
            for (var s = 0; s < lc; s++)
            {
                if (lw[s] != rw[s])
                    return $"entity {e} slot {s} differs: left=(tl={ls[s].Timeline},ch={ls[s].Channel},tick={ls[s].Tick},ph={ls[s].Phase},rate={ls[s].Rate},flg={ls[s].Flags}) right=(tl={rs[s].Timeline},ch={rs[s].Channel},tick={rs[s].Tick},ph={rs[s].Phase},rate={rs[s].Rate},flg={rs[s].Flags})";
            }
        }
        return null;
    }

    public static unsafe void Decode(PlaybackTable players, int entity, Playback* into)
    {
        var count = players.CountOf(entity);
        new ReadOnlySpan<Playback>(players.SlotsOf(entity), count).CopyTo(new Span<Playback>(into, count));
        if (players.Frozen) LazyTicks.Shift(into, count, players.Clock, players.Forward);
    }

    public static void MixPlaybacks(ref TlStateHash hash, PlaybackTable players)
    {
        unsafe
        {
            var n = players.EntityCapacity;
            var slots = stackalloc Playback[PlaybackTable.MaxSlots];
            for (var w = 0; w < players.PlayingWords; w++)
                hash.Lane(players.PlayingBits[w]);
            for (var e = 0; e < n; e++)
            {
                Decode(players, e, slots);
                hash.Slots(slots, players.CountOf(e));
            }
        }
    }
}

public sealed class FuzzFailureException : Exception
{
    public FuzzFailureException(string target, ulong seed, int iter, string detail, Exception? inner)
        : base($"[{target}] FAIL seed=0x{seed:X} iter={iter} replay: TL_FUZZ_SEED=0x{seed:X} TL_FUZZ_ITER={iter}\n{detail}", inner)
    {
        Target = target;
        Seed = seed;
        Iter = iter;
    }

    public string Target { get; }
    public ulong Seed { get; }
    public int Iter { get; }
}
