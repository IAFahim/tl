using System.Runtime.InteropServices;
using Tl.Gen.Tlb;
using Tl.Pack.Stats;
using Xunit;

namespace Tl.Packs.Tests;

public sealed class StatsTests
{
    internal const ulong Pinned = 12183504288633381751UL;
    internal const ushort Duel = 1;
    internal const ushort Haste = 2;

    static byte[] Bytes<T>(T value) where T : unmanaged => MemoryMarshal.AsBytes(new Span<T>(ref value)).ToArray();

    static TypedTrack Track<T>(params (uint Start, uint End, T Clip)[] clips) where T : unmanaged
    {
        var track = new TypedTrack { Type = typeof(TlNoSettings<T>) };
        foreach (var (start, end, clip) in clips) track.Clips.Add(new TypedClip { Type = typeof(T), Start = start, End = end, Data = Bytes(clip) });
        return track;
    }

    internal static byte[] Library()
    {
        var hit = new HitClip { Amount = 12 };
        var timelines = new List<TypedTimeline>
        {
            new()
            {
                Name = "duel", Duration = 600,
                Tracks =
                {
                    Track((60, 61, hit), (120, 121, hit), (180, 181, hit)),
                    Track((240, 480, new PoisonClip { Amount = 1, Every = 20 })),
                    Track((500, 501, new HealClip { Amount = 20 })),
                },
            },
            new() { Name = "haste", Duration = 300, Channel = "haste", Tracks = { Track((0, 300, new BuffClip { AttackBonus = 10 })) } },
        };
        var manifest = LibraryBake.Manifest(timelines.Select(static t => t.Name!).ToList(), ["haste"], 60);
        var kinds = LibraryBake.Kinds([typeof(HitClip).Assembly]);
        var parts = new List<LibraryPart>();
        foreach (var timeline in timelines)
        {
            var tlb = LibraryBake.BakeTyped(timeline, manifest);
            parts.Add(new LibraryPart { Name = timeline.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(timeline.Name!, tlb, kinds), Next = (ushort)LibraryAssembler.NextOf(tlb) });
        }
        var settings = LibrarySettings.Default;
        settings.ChannelCount = 1;
        return LibraryAssembler.Assemble(parts, 60, settings);
    }

    static ulong Mix(ulong h, int v) => (h ^ (uint)v) * 1099511628211UL;

    internal static ulong Run(byte[] library, int steps, Action<int, Health, Attack>? checkpoint, out long warm)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var health = new[] { new Health { Value = 100, Max = 100 } };
        var attack = new[] { new Attack { Base = 10 } };
        var h = 14695981039346656037UL;
        warm = 0;
        for (var step = 0; step < steps; step++)
        {
            if (step == 0) players.Start(0, Duel);
            if (step == 100 || step == 250) players.Start(0, Haste);
            var before = GC.GetAllocatedBytesForCurrentThread();
            world.Tick(players, true);
            new Tl.Generated.TlAuto_Tl_Pack_Stats { C0_Health = health, C1_Attack = attack }.Run(world, players, true);
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (step >= 10) warm += delta;
            h = Mix(Mix(Mix(h, health[0].Value), attack[0].Base), attack[0].Bonus);
            checkpoint?.Invoke(step + 1, health[0], attack[0]);
        }
        return h;
    }

    [Fact]
    public void TheDuelGivesTheDemoCheckpoints()
    {
        var seen = new Dictionary<int, (int Health, int Attack)>();
        Run(Library(), 600, (step, health, attack) =>
        {
            if (step is 200 or 450 or 600) seen[step] = (health.Value, attack.Value);
        }, out _);
        Assert.Equal((64, 20), seen[200]);
        Assert.Equal((53, 20), seen[450]);
        Assert.Equal((72, 10), seen[600]);
    }

    [Fact]
    public void RefreshingHasteNeverStacksTheBonus()
    {
        var most = 0;
        Run(Library(), 600, (_, _, attack) => most = Math.Max(most, attack.Bonus), out _);
        Assert.Equal(10, most);
    }

    [Fact]
    public void SixHundredStepsKeepThePinnedHashAndAllocateNothingWarm()
    {
        var library = Library();
        var golden = Path.Combine(ForceTests.Root(), "tests", "Tl.Packs.Tests", "Stats", "stats.tlib");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1") File.WriteAllBytes(golden, library);
        Assert.Equal(File.ReadAllBytes(golden), library);
        var hash = Run(library, 600, null, out var warm);
        Assert.Equal(hash, Run(library, 600, null, out _));
        Assert.Equal(0L, warm);
        Assert.Equal(Pinned, hash);
    }

    [Fact]
    public void ThePackUnityTestPinsTheSameLibraryAndHash()
    {
        var tests = Path.Combine(ForceTests.Root(), "packs", "Stats", "Tests");
        Assert.Contains("\"" + Convert.ToBase64String(Library()) + "\"", File.ReadAllText(Path.Combine(tests, "StatsLibraryBytes.cs")));
        Assert.Contains($"const ulong Pinned = {Pinned}UL;", File.ReadAllText(Path.Combine(tests, "StatsPackTests.cs")));
    }
}
