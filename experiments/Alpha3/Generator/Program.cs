using System.Reflection;
using Tl;

namespace GeneratorPathProof;

internal static class Program
{
    public static void Main()
    {
        GeneratorPathProposal.ProposalReceipt.Verify();
        var methods = typeof(Attack)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(static method => method.Name)
            .ToHashSet(StringComparer.Ordinal);
        Require(methods.Contains("Start"), "generated Start missing");
        Require(methods.Contains("TrySeek"), "generated TrySeek missing");
        Require(!methods.Contains("Select"), "unexpected generated selector");
        Require(!methods.Contains("Complete"), "unexpected generated completion");

        var health = new Health(100);
        var calls = new Calls();
        var playback = Attack.Start(41u);
        var data = new Attack.Data(ref playback, calls: ref calls, health: ref health);
        Require(Attack.TrySeek(ref data, 0), "zero seek rejected");
        Require(health.Value == 100 && calls.Value == 0 && playback.Position == 0, "zero seek changed state");
        Require(Attack.TrySeek(ref data, 1), "forward seek rejected");
        Require(health.Value == 93 && calls.Value == 1, "TrySeek did not execute the operation eagerly");
        Require(playback.Position == 1 && playback.GameTick == 42u, "TrySeek did not commit playback");
        Require(Attack.TrackCount == 1 && Attack.ClipCount == 1 && Attack.RegionCount == 1, "generated schedule shape changed");

        Console.WriteLine("current generator: Start + borrowed Data + eager TrySeek");
        Console.WriteLine($"generated schedule: {Attack.TrackCount} track, {Attack.ClipCount} clip, {Attack.RegionCount} region");
        Console.WriteLine("proposed authoring: Track(settings).Use<TJob>() compiles");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

public readonly record struct Damage(int Value);
public record struct Health(int Value);
public record struct Calls(int Value);

public readonly struct DamageJob : ITrack<Damage>
{
    public void Blend(in Damage first, in Damage second, float factor, out Damage result)
        => result = first;

    public static void Seek(in Frame<DamageJob, Damage> frame, ref Health health, ref Calls calls)
    {
        health.Value -= frame.Direction * frame.Clip.Value;
        calls.Value++;
    }
}

public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var damage = builder.Track(new DamageJob());
        builder.Clip(damage, new Damage(7), 0u, 1u);
    }
}
