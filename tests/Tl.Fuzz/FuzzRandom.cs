namespace Tl.Fuzz;

public struct FuzzRandom(ulong seed)
{
    private const ulong SeedA = 0x243F6A8885A308D3ul;
    private const ulong SeedB = 0x13198A2E03707344ul;

    ulong _state = seed == 0 ? SeedA : seed;

    public static FuzzRandom FromSeeds(ulong a, ulong b) => new(a * 0x9E3779B97F4A7C15ul ^ b << 32 ^ SeedB);

    private ulong NextUlong()
    {
        _state += 0x9E3779B97F4A7C15ul;
        var z = _state;
        z = (z ^ z >> 30) * 0xBF58476D1CE4E5B9ul;
        z = (z ^ z >> 27) * 0x94D049BB133111EBul;
        return z ^ z >> 31;
    }

    public uint NextUInt() => (uint)(NextUlong() >> 32);

    public int NextInt(int bound) => bound <= 0 ? 0 : (int)(NextUInt() % (uint)bound);

    public bool NextBool() => (NextUInt() & 1) != 0;

    public T Pick<T>(IReadOnlyList<T> items) => items[NextInt(items.Count)];

    public void Fill(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)NextUInt();
    }
}
