using System.Runtime.InteropServices;
using Tl.Gen.Tlb;
using Tl.Pack.Inventory;
using Xunit;

namespace Tl.Packs.Tests;

public sealed class InventoryTests
{
    internal const ulong Pinned = 5645869165897381019UL;

    static byte[] Bytes<T>(T value) where T : unmanaged => MemoryMarshal.AsBytes(new Span<T>(ref value)).ToArray();

    static TypedTrack Track<T>(params (uint Start, uint End, T Clip)[] clips) where T : unmanaged
    {
        var track = new TypedTrack { Type = typeof(TlNoSettings<T>) };
        foreach (var (start, end, clip) in clips) track.Clips.Add(new TypedClip { Type = typeof(T), Start = start, End = end, Data = Bytes(clip) });
        return track;
    }

    static GiveClip Give(Item item, int count) => new() { Item = item, Count = count };

    internal static byte[] Library()
    {
        var run = new TypedTimeline
        {
            Name = "loot_run", Duration = 600,
            Tracks =
            {
                Track((0, 1, Give(Item.Wood, 5)), (60, 61, Give(Item.Stone, 3)), (120, 121, Give(Item.Potion, 2)), (540, 541, Give(Item.Sword, 1))),
                Track((360, 361, Give(Item.Potion, -1))),
                Track((180, 300, new CraftClip { Input = Item.Wood, InputCount = 4, Output = Item.Plank, OutputCount = 2 })),
            },
            Markers =
            {
                new TypedMarker { Type = typeof(PickupMarker), Tick = 420, Data = Bytes(new PickupMarker { Item = Item.Coin, Count = 10 }) },
                new TypedMarker { Type = typeof(PickupMarker), Tick = 480, Data = Bytes(new PickupMarker { Item = Item.Gem, Count = 1 }) },
            },
        };
        var manifest = LibraryBake.Manifest(["loot_run"], [], 60);
        var kinds = LibraryBake.Kinds([typeof(Bag).Assembly]);
        var tlb = LibraryBake.BakeTyped(run, manifest);
        var part = new LibraryPart { Name = "loot_run", Tlb = tlb, Kinds = LibraryBake.KindPayload("loot_run", tlb, kinds), Next = (ushort)LibraryAssembler.NextOf(tlb) };
        return LibraryAssembler.Assemble([part], 60, LibrarySettings.Default);
    }

    static string Show(in Bag b) => $"Wood {b.Wood} Stone {b.Stone} Plank {b.Plank} Potion {b.Potion} Sword {b.Sword} Coin {b.Coin} Key {b.Key} Gem {b.Gem}";

    static ulong Mix(ulong h, int v) => (h ^ (uint)v) * 1099511628211UL;

    static ulong Mix(ulong h, in Bag bag)
        => Mix(Mix(Mix(Mix(Mix(Mix(Mix(Mix(h, bag.Wood), bag.Stone), bag.Plank), bag.Potion), bag.Sword), bag.Coin), bag.Key), bag.Gem);

    static ulong Play(byte[] library, int forward, int back, ref ulong h, ref long warm, out Bag bag)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var bags = new Bag[1];
        players.Start(0, 1);
        for (var step = 0; step < forward + back; step++)
        {
            var direction = step < forward;
            var before = GC.GetAllocatedBytesForCurrentThread();
            world.Tick(players, direction);
            new Tl.Generated.TlAuto_Tl_Pack_Inventory { C0_Bag = bags }.Run(world, players, direction);
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (step >= 10) warm += delta;
            h = Mix(h, bags[0]);
        }
        bag = bags[0];
        return h;
    }

    internal static ulong Run(byte[] library, out Bag rewound, out Bag final, out long warm)
    {
        var h = 14695981039346656037UL;
        warm = 0;
        Play(library, 450, 450, ref h, ref warm, out rewound);
        Play(library, 600, 0, ref h, ref warm, out final);
        return h;
    }

    [Fact]
    public void RewindingEmptiesTheBagAndTheFinalBagMatchesTheDemo()
    {
        Run(Library(), out var rewound, out var final, out _);
        Assert.Equal(Show(default), Show(rewound));
        Assert.Equal(Show(new Bag { Wood = 1, Stone = 3, Plank = 2, Potion = 1, Sword = 1, Coin = 10, Key = 0, Gem = 1 }), Show(final));
    }

    [Fact]
    public void TheRunKeepsThePinnedHashAndAllocatesNothingWarm()
    {
        var library = Library();
        var golden = Path.Combine(ForceTests.Root(), "tests", "Tl.Packs.Tests", "Inventory", "inventory.tlib");
        if (Environment.GetEnvironmentVariable("TL_UPDATE_SNAPSHOT") == "1") File.WriteAllBytes(golden, library);
        Assert.Equal(File.ReadAllBytes(golden), library);
        var hash = Run(library, out _, out _, out var warm);
        Assert.Equal(hash, Run(library, out _, out _, out _));
        Assert.Equal(0L, warm);
        Assert.Equal(Pinned, hash);
    }

    [Fact]
    public void ThePackUnityTestPinsTheSameLibraryAndHash()
    {
        var tests = Path.Combine(ForceTests.Root(), "packs", "Inventory", "Tests");
        Assert.Contains("\"" + Convert.ToBase64String(Library()) + "\"", File.ReadAllText(Path.Combine(tests, "InventoryLibraryBytes.cs")));
        Assert.Contains($"const ulong Pinned = {Pinned}UL;", File.ReadAllText(Path.Combine(tests, "InventoryPackTests.cs")));
    }
}
