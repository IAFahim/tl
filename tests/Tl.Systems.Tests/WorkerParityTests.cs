using System.Runtime.InteropServices;
using Game;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

// Phase 6 claimed "hash-exact at 1/2/3/8/11 workers" but only an out-of-repo harness checked it, and that
// hash was one final value (every entity ended equal), which cannot see an ordering bug. This test lives in the
// repo and hashes EVERY column and the ORDERED event stream after EVERY tick.
public sealed unsafe class WorkerParityTests
{
    const int N = 700;
    const int Ticks = 90;
    static readonly ushort[] Rates = [16, 32, 8, 24, 40, 12, 20];

    static byte[] Library() => File.ReadAllBytes(Path.Combine(LoaderRunTests.LoaderLibraryDir(), "game.tlib"));

    static ulong Mix(ulong hash, ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    static ulong MixInt(ulong hash, int value) => Mix(hash, MemoryMarshal.AsBytes(new ReadOnlySpan<int>(in value)));

    // One full scenario; returns one hash per tick (columns + ordered events + playback ticks), so a mismatch names the tick.
    static ulong[] Scenario(int workerCount)
    {
        using var world = new TimelineWorld(Library());
        using var players = new PlaybackTable(N);
        using var cues = new Events<Cue>(1 << 16);
        using var workers = workerCount > 0 ? new TlWorkers(workerCount) : null;
        var motion = new Motion[N];
        var power = new JumpPower[N];
        var stats = new Stats[N];
        var input = new InputState[N];
        var anim = new Anim[N];
        var feet = new Foot[N];
        for (var i = 0; i < N; i++) power[i] = new JumpPower(1f);
        for (var e = 0; e < N; e++)
        {
            players.Start(e, 1);
            players.SetSpeed(e, 1, Rates[e % Rates.Length]);
        }
        var hashes = new ulong[Ticks];
        for (var t = 0; t < Ticks; t++)
        {
            if (t == 25) for (var e = 0; e < N; e += 11) players.Pause(e, 1);
            if (t == 45)
                for (var e = 0; e < N; e += 11)
                {
                    players.Resume(e, 1);
                    players.SetSpeed(e, 1, Rates[(e + 3) % Rates.Length]);
                }
            if (t == 60) for (var e = 5; e < N; e += 13) players.Start(e, 1);
            world.ApplyRequests(players);
            var system = new Movement { Motion = motion, JumpPower = power, Stats = stats, Input = input, Anim = anim, Feet = feet, Requests = world.Requests(players), Cues = cues.Writer };
            if (workers is null) system.Run(world, players, true);
            else system.Run(world, players, true, workers);
            world.Advance(players, true);
            var hash = 14695981039346656037UL;
            hash = Mix(hash, MemoryMarshal.AsBytes(new ReadOnlySpan<Motion>(motion)));
            hash = Mix(hash, MemoryMarshal.AsBytes(new ReadOnlySpan<Stats>(stats)));
            hash = Mix(hash, MemoryMarshal.AsBytes(new ReadOnlySpan<Anim>(anim)));
            hash = Mix(hash, MemoryMarshal.AsBytes(new ReadOnlySpan<Foot>(feet)));
            foreach (var record in cues.Drain())
            {
                hash = MixInt(hash, record.Entity.Index);
                hash = MixInt(hash, record.Slot);
                hash = MixInt(hash, record.Value.Code);
            }
            var view = players.View;
            for (var e = 0; e < N; e++)
            {
                var count = players.CountOf(e);
                hash = MixInt(hash, count);
                for (var s = 0; s < count; s++)
                {
                    var slot = view.SlotOf(e, s);
                    hash = MixInt(hash, slot->Tick);
                    hash = MixInt(hash, slot->Phase);
                    hash = MixInt(hash, (int)slot->Flags);
                }
            }
            hashes[t] = hash;
        }
        return hashes;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(11)]
    public void EveryWorkerCountMatchesTheSequentialRunOnEveryTick(int workerCount)
    {
        var sequential = Scenario(0);
        var parallel = Scenario(workerCount);
        for (var t = 0; t < Ticks; t++)
            Assert.True(sequential[t] == parallel[t], $"workers={workerCount}: first differing tick is {t}");
    }

    [Fact]
    public void TheScenarioActuallyChangesStateAndEventsEveryFewTicks()
    {
        var sequential = Scenario(0);
        Assert.True(sequential.Distinct().Count() > Ticks / 2, "the scenario must produce many distinct states, or equality proves nothing");
    }

    [Fact]
    public void ARunTwiceWithTheSameWorkersGivesTheSameHashes()
    {
        Assert.Equal(Scenario(8), Scenario(8));
    }
}
