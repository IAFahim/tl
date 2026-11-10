using System.Runtime.InteropServices;
using Tl.Gen.Tlb;
using Tl.Pack.Inventory;
using Xunit;

namespace Tl.Packs.Tests;

public sealed unsafe class InventoryTests
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

    static ulong Mix(ulong h, int v) { var s = new global::Tl.TlStateHash { Value = h }; s.Int(v); return s.Value; }

    static ulong Mix(ulong h, in Bag bag)
        => Mix(Mix(Mix(Mix(Mix(Mix(Mix(Mix(h, bag.Wood), bag.Stone), bag.Plank), bag.Potion), bag.Sword), bag.Coin), bag.Key), bag.Gem);

    static void LootStep(PlaybackTable players, Bag[] bags, ushort[] prevTimeline, ushort[] prevTick, bool forward)
    {
        unsafe
        {
            var closing = players.Closing;
            for (var ci = 0; ci < closing->Count; ci++)
            {
                closing->Get(ci, out var ce, out _, out var cpb);
                if (cpb.Timeline != 1) continue;
                if ((cpb.Flags & (PlaybackFlags.StartPending | PlaybackFlags.Ended)) != 0) continue;
                if (cpb.Tick < 180 || cpb.Tick >= 300 || cpb.Tick >= 299) continue;
                var cbackward = forward ? (cpb.Flags & PlaybackFlags.Reverse) != 0 : (cpb.Flags & PlaybackFlags.Reverse) == 0;
                var cflags = cbackward ? FrameFlags.Reverse : FrameFlags.None;
                var craft = new CraftClip { Input = Item.Wood, InputCount = 4, Output = Item.Plank, OutputCount = 2 };
                var produced = CraftTrack.Produce(new TlFrame<CraftClip>(new Frame<TlNoSettings<CraftClip>, CraftClip>(default, craft, cpb.Tick, 120, (ushort)(cpb.Tick - 180), 2, cflags)));
                InventoryItems.Apply(in produced, ref bags[ce]);
            }
            var count = players.CountOf(0);
            for (var s = 0; s < count; s++)
            {
                var pb = players.View.SlotOf(0, s);
                var timeline = pb->Timeline;
                var tick = pb->Tick;
                var flags = pb->Flags;
                if (timeline == 0)
                {
                    prevTimeline[s] = 0;
                    prevTick[s] = 0;
                    continue;
                }
                var began = (flags & PlaybackFlags.Began) != 0;
                var moved = prevTimeline[s] == timeline && tick != prevTick[s];
                if (timeline == 1 && (flags & PlaybackFlags.Stopping) == 0 && (began || moved))
                {
                    var backward = forward ? (flags & PlaybackFlags.Reverse) != 0 : (flags & PlaybackFlags.Reverse) == 0;
                    var frameFlags = backward ? FrameFlags.Reverse : FrameFlags.None;
                    if (tick == 0 || tick == 60 || tick == 120 || tick == 360 || tick == 540)
                    {
                        var give = tick == 0 ? Give(Item.Wood, 5) : tick == 60 ? Give(Item.Stone, 3) : tick == 120 ? Give(Item.Potion, 2) : tick == 360 ? Give(Item.Potion, -1) : Give(Item.Sword, 1);
                        var delta = GiveTrack.Give(new TlFrame<GiveClip>(new Frame<TlNoSettings<GiveClip>, GiveClip>(default, give, tick, 1, 0, (ushort)(tick == 360 ? 1 : 0), frameFlags)));
                        InventoryItems.Apply(in delta, ref bags[0]);
                    }
                    if ((began && tick >= 180 && tick < 300) || (moved && tick == 180))
                    {
                        var craft = new CraftClip { Input = Item.Wood, InputCount = 4, Output = Item.Plank, OutputCount = 2 };
                        var consumed = CraftTrack.Consume(new TlFrame<CraftClip>(new Frame<TlNoSettings<CraftClip>, CraftClip>(default, craft, tick, 120, (ushort)(tick - 180), 2, frameFlags)));
                        InventoryItems.Apply(in consumed, ref bags[0]);
                    }
                    if (began || moved)
                    {
                        if (tick == 299)
                        {
                            var craft = new CraftClip { Input = Item.Wood, InputCount = 4, Output = Item.Plank, OutputCount = 2 };
                            var produced = CraftTrack.Produce(new TlFrame<CraftClip>(new Frame<TlNoSettings<CraftClip>, CraftClip>(default, craft, tick, 120, (ushort)(tick - 180), 2, frameFlags)));
                            InventoryItems.Apply(in produced, ref bags[0]);
                        }
                        if (tick == 420)
                        {
                            var marker = new PickupMarker { Item = Item.Coin, Count = 10 };
                            var picked = Pickup.OnPickup(new MarkerFrame<PickupMarker>(in marker, tick, 0, frameFlags));
                            InventoryItems.Apply(in picked, ref bags[0]);
                        }
                        if (tick == 480)
                        {
                            var marker = new PickupMarker { Item = Item.Gem, Count = 1 };
                            var picked = Pickup.OnPickup(new MarkerFrame<PickupMarker>(in marker, tick, 0, frameFlags));
                            InventoryItems.Apply(in picked, ref bags[0]);
                        }
                    }
                }
                prevTimeline[s] = timeline;
                prevTick[s] = tick;
            }
        }
    }

    static ulong Play(byte[] library, int forward, int back, ref ulong h, ref long warm, out Bag bag)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var bags = new Bag[1];
        var prevTimeline = new ushort[256];
        var prevTick = new ushort[256];
        players.Start(0, 1);
        for (var step = 0; step < forward + back; step++)
        {
            var direction = step < forward;
            var before = GC.GetAllocatedBytesForCurrentThread();
            world.Tick(players, direction);
            LootStep(players, bags, prevTimeline, prevTick, direction);
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (step >= 10) warm += delta;
            h = Mix(h, bags[0]);
        }
        bag = bags[0];
        return h;
    }

    internal static ulong Run(byte[] library, out Bag rewound, out Bag final, out long warm)
    {
        var h = global::Tl.TlStateHash.Seed;
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
    public void ReversingTheRunEmptiesTheBagParksItAndPlayingOnRefillsItInOneWorld()
    {
        using var world = new TimelineWorld(Library());
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var bags = new Bag[1];
        var prevTimeline = new ushort[256];
        var prevTick = new ushort[256];
        players.Start(0, 1);
        for (var step = 0; step < 1500; step++)
        {
            if (step == 450) players.Reverse(0, 1, true);
            if (step == 900)
            {
                Assert.Equal(Show(default), Show(bags[0]));
                Assert.Equal(PlaybackRules.Parked | PlaybackFlags.Reverse, players.View.SlotOf(0, 0)->Flags);
                Assert.Equal(0, players.View.SlotOf(0, 0)->Tick);
                players.Reverse(0, 1, false);
            }
            world.Tick(players);
            LootStep(players, bags, prevTimeline, prevTick, true);
        }
        Assert.Equal(599, players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(Show(new Bag { Wood = 1, Stone = 3, Plank = 2, Potion = 1, Sword = 1, Coin = 10, Key = 0, Gem = 1 }), Show(bags[0]));
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
