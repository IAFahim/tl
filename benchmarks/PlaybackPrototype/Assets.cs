using System.Reflection;
using System.Security.Cryptography;
using Tl;
using Tl.Gen.Tlb;

namespace PlaybackPrototype;

internal sealed record AssetFixture(string Name, string Json, uint Duration, bool Loops)
{
    public string ResourceName => $"PlaybackPrototype.Assets.{Name}.tlb.json";
}

internal static class Fixtures
{
    public static AssetFixture MoveLoop { get; } = Load("move-loop", duration: 64, loops: true);
    public static AssetFixture Pulse { get; } = Load("pulse", duration: 1, loops: true);
    public static AssetFixture Watch { get; } = Load("watch", duration: 64, loops: true);
    public static AssetFixture Churn { get; } = Load("churn", duration: 20, loops: false);

    private static AssetFixture Load(string name, uint duration, bool loops)
    {
        var assembly = typeof(Fixtures).Assembly;
        var resourceName = $"PlaybackPrototype.Assets.{name}.tlb.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded asset '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return new AssetFixture(name, reader.ReadToEnd(), duration, loops);
    }

    public static byte[] Bake(AssetFixture fixture) => TimelineBaker.BakeJson(fixture.Json);

    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static TimelineAsset LoadAsset(AssetFixture fixture) => TimelineAsset.Load(Bake(fixture));
}

internal static class Inputs
{
    public static Input[] Fill(int count, uint seed)
    {
        var inputs = new Input[count];
        var state = seed == 0 ? 0x13E48AB9u : seed;
        for (var i = 0; i < count; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            inputs[i] = new Input((state & 0xFFu) * 0.25f + 0.5f);
        }

        return inputs;
    }

    public static void Mutate(Input[] inputs, uint gameTick)
    {
        var state = gameTick * 0x9E3779B1u;
        for (var k = 0; k < 64; k++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            inputs[(int)((state >> 8) % (uint)inputs.Length)] = new Input((state & 0xFFu) * 0.25f + 0.5f);
        }
    }
}

internal sealed class Meter
{
    public readonly record struct Measurement(double MedianNsPerPass, double AllocatedBytesPerPass, int Iterations, int PassesPerIteration);

    public static Measurement Measure(Action pass, int warmups, int iterations, int passesPerIteration)
    {
        for (var i = 0; i < warmups; i++) pass();
        var samples = new double[iterations];
        var sw = new System.Diagnostics.Stopwatch();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            sw.Restart();
            for (var p = 0; p < passesPerIteration; p++) pass();
            sw.Stop();
            samples[iteration] = sw.Elapsed.TotalMilliseconds * 1e6 / passesPerIteration;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(samples);
        return new Measurement(
            samples[iterations / 2],
            allocated / (double)(iterations * passesPerIteration),
            iterations,
            passesPerIteration);
    }
}
