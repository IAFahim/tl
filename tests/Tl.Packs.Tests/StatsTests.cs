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

    static ulong Mix(ulong h, int v) { var s = new global::Tl.TlStateHash { Value = h }; s.Int(v); return s.Value; }

    internal static ulong Run(byte[] library, int steps, Action<int, Health, Attack>? checkpoint, out long warm)
    {
        using var world = new TimelineWorld(library);
        using var players = new PlaybackTable(1);
        using var kinds = new KindStore(world, players);
        var health = new[] { new Health { Value = 100, Max = 100 } };
        var attack = new[] { new Attack { Base = 10 } };
        var hit = new HitClip { Amount = 12 };
        var poison = new PoisonClip { Amount = 1, Every = 20 };
        var heal = new HealClip { Amount = 20 };
        var buff = new BuffClip { AttackBonus = 10 };
        var prevTimeline = new ushort[256];
        var prevTick = new ushort[256];
        var h = global::Tl.TlStateHash.Seed;
        warm = 0;
        for (var step = 0; step < steps; step++)
        {
            if (step == 0) players.Start(0, Duel);
            if (step == 100 || step == 250) players.Start(0, Haste);
            var before = GC.GetAllocatedBytesForCurrentThread();
            world.Tick(players, true);
            unsafe
            {
                var damage = 0;
                var hasDamage = false;
                var closing = players.Closing;
                for (var ci = 0; ci < closing->Count; ci++)
                {
                    closing->Get(ci, out var ce, out _, out var cpb);
                    if (cpb.Timeline != Haste) continue;
                    if ((cpb.Flags & (PlaybackFlags.StartPending | PlaybackFlags.Ended)) != 0) continue;
                    if (cpb.Tick >= 300 || cpb.Tick >= 299) continue;
                    var cbackward = (cpb.Flags & PlaybackFlags.Reverse) != 0;
                    var cflags = cbackward ? FrameFlags.Reverse : FrameFlags.None;
                    var lost = BuffTrack.Lose(new TlFrame<BuffClip>(new Frame<TlNoSettings<BuffClip>, BuffClip>(default, buff, cpb.Tick, 300, cpb.Tick, 0, cflags)));
                    StatsAttackBonus.Apply(in lost, ref attack[ce]);
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
                    var backward = (flags & PlaybackFlags.Reverse) != 0;
                    var frameFlags = backward ? FrameFlags.Reverse : FrameFlags.None;
                    if ((flags & PlaybackFlags.Stopping) == 0)
                    {
                        if (timeline == Duel)
                        {
                            if (began)
                            {
                                if (tick is 60 or 120 or 180)
                                {
                                    var v = HitTrack.Hit(new TlFrame<HitClip>(new Frame<TlNoSettings<HitClip>, HitClip>(default, hit, tick, 1, 0, 0, frameFlags)));
                                    StatsDamage.Combine(ref damage, in v);
                                    hasDamage = true;
                                }
                                if (tick == 500)
                                {
                                    var v = HealTrack.Heal(new TlFrame<HealClip>(new Frame<TlNoSettings<HealClip>, HealClip>(default, heal, tick, 1, 0, 0, frameFlags)));
                                    StatsDamage.Combine(ref damage, in v);
                                    hasDamage = true;
                                }
                            }
                            else if (moved)
                            {
                                if (tick is 60 or 120 or 180)
                                {
                                    var v = HitTrack.Hit(new TlFrame<HitClip>(new Frame<TlNoSettings<HitClip>, HitClip>(default, hit, tick, 1, 0, 0, frameFlags)));
                                    StatsDamage.Combine(ref damage, in v);
                                    hasDamage = true;
                                }
                                if (tick == 500)
                                {
                                    var v = HealTrack.Heal(new TlFrame<HealClip>(new Frame<TlNoSettings<HealClip>, HealClip>(default, heal, tick, 1, 0, 0, frameFlags)));
                                    StatsDamage.Combine(ref damage, in v);
                                    hasDamage = true;
                                }
                            }
                            if ((flags & (PlaybackFlags.Paused | PlaybackFlags.Ended)) == 0 && tick >= 240 && tick < 480)
                            {
                                var v = PoisonTrack.Tick(new TlFrame<PoisonClip>(new Frame<TlNoSettings<PoisonClip>, PoisonClip>(default, poison, tick, 240, (ushort)(tick - 240), 1, frameFlags)));
                                StatsDamage.Combine(ref damage, in v);
                                hasDamage = true;
                            }
                        }
                        else if (timeline == Haste)
                        {
                            if (began)
                            {
                                var v = BuffTrack.Gain(new TlFrame<BuffClip>(new Frame<TlNoSettings<BuffClip>, BuffClip>(default, buff, tick, 300, tick, 0, frameFlags)));
                                StatsAttackBonus.Apply(in v, ref attack[0]);
                            }
                            else if (moved && tick == 299)
                            {
                                var v = BuffTrack.Lose(new TlFrame<BuffClip>(new Frame<TlNoSettings<BuffClip>, BuffClip>(default, buff, tick, 300, tick, 0, frameFlags)));
                                StatsAttackBonus.Apply(in v, ref attack[0]);
                            }
                        }
                    }
                    prevTimeline[s] = timeline;
                    prevTick[s] = tick;
                }
                if (hasDamage) StatsDamage.Apply(in damage, ref health[0]);
            }
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
