namespace HandleInterning;

static class Rng
{
    public const ulong Seed = 0x243F6A8885A308D3UL;

    public static ulong Next(ref ulong state)
    {
        state ^= state << 13;
        state ^= state >> 7;
        state ^= state << 17;
        return state;
    }

    public static byte[] Payload(int index, int size)
    {
        var bytes = new byte[size];
        var state = Seed ^ (ulong)(uint)index * 0x9E3779B97F4A7C15UL;
        for (var i = 0; i < size; i++) bytes[i] = (byte)Next(ref state);
        return bytes;
    }
}
