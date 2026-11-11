using KindGame;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class Phase8RuntimeTests : IDisposable
{
    const ushort Jump = 1, PotionHop = 2, Run = 3, RunLegacy = 4, SpeedBoost = 5, Stomp = 6;
    const int N = 4;

    readonly byte[] _library;
    readonly TimelineWorld _world;
    readonly PlaybackTable _players;
    readonly KindStore _store;
    readonly Motion[] _motion = new Motion[N];
    readonly LegacyMotion[] _legacy = new LegacyMotion[N];
    readonly WalkStat[] _stat = new WalkStat[N];
    readonly JumpPower[] _power = new JumpPower[N];
    readonly Stamina[] _stamina = new Stamina[N];
    readonly Heat[] _heat = new Heat[N];
    readonly Health[] _health = new Health[N];

    static string Golden()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(System.IO.Path.Combine(dir, "tl.slnx")); i++) dir = System.IO.Path.GetDirectoryName(dir)!;
        return System.IO.Path.Combine(dir, "tests", "Tl.Systems.Tests", "KindLibrary", "game.tlib");
    }

    public Phase8RuntimeTests() : this(4) { }

    Phase8RuntimeTests(int slotsPerEntity)
    {
        _library = File.ReadAllBytes(Golden());
        _world = new TimelineWorld(_library);
        _players = new PlaybackTable(N, slotsPerEntity);
        _store = new KindStore(_world, _players);
        for (var i = 0; i < N; i++)
        {
            _stat[i] = new WalkStat { Value = 1f };
            _power[i] = new JumpPower { Value = 2f };
            _health[i] = new Health { Value = 1000 };
        }
    }

    public void Dispose()
    {
        _players.Dispose();
        _world.Dispose();
    }

    void Step(bool forward = true)
    {
        _world.Tick(_players, forward);
        new Movement { Motion = _motion, Legacy = _legacy, WalkStat = _stat, JumpPower = _power, Stamina = _stamina, Heat = _heat }.Run(_world, _players, forward);
        new Vitals { Health = _health }.Run(_world, _players, forward);
    }

    [Fact]
    public void ResetZeroesTheEntitysSlotsAndAddsNothingToTheClosingList()
    {
        _players.Start(0, Stomp);
        Step();
        Assert.Equal(1, _players.CountOf(0));
        Assert.True(_players.IsPlaying(0, Stomp));
        _players.Reset(0);
        Step();
        Assert.Equal(0, _players.CountOf(0));
        Assert.False(_players.IsPlaying(0, Stomp));
        Assert.Equal(0, _players.Closing->Count);
        Assert.Equal(1000, _health[0].Value);
        for (var s = 0; s < 12; s++) Step();
        Assert.Equal(1000, _health[0].Value);
    }

    [Fact]
    public void ResetOfOneEntityLeavesTheOthersPlaying()
    {
        _players.Start(0, Stomp);
        _players.Start(1, Stomp);
        Step();
        _players.Reset(0);
        for (var s = 0; s < 12; s++) Step();
        Assert.Equal(1000, _health[0].Value);
        Assert.Equal(965, _health[1].Value);
    }

    [Fact]
    public void ResetDropsTheQueuedGamePulsesOfThatEntityOnly()
    {
        var damage = _store.Pulses<int>("KindGame.Damage");
        damage.To(0, 41);
        damage.To(1, 7);
        damage.To(0, 1);
        _players.Reset(0);
        Step();
        Assert.Equal(1000, _health[0].Value);
        Assert.Equal(993, _health[1].Value);
        Assert.Equal(0, _store.DroppedPulses);
    }

    [Fact]
    public void ResetThenStartInOneStepGivesTheNewOccupantNothingFromTheOldOne()
    {
        _players.Start(2, Stomp);
        Step();
        Step();
        Step();
        Assert.True(_players.IsPlaying(2, Stomp));
        _players.Reset(2);
        _health[2].Value = 1000;
        _players.Start(2, Run);
        Step();
        Assert.Equal(1, _players.CountOf(2));
        Assert.True(_players.IsPlaying(2, Run));
        Assert.False(_players.IsPlaying(2, Stomp));
        for (var s = 0; s < 12; s++) Step();
        Assert.Equal(1000, _health[2].Value);
    }

    [Fact]
    public void RequestsResetFromAnAnyEntityHandleIsTheSameAsTheTableVerb()
    {
        _players.Start(3, Stomp);
        Step();
        _world.Requests(_players).For(3, -1).Reset();
        _world.ApplyRequests(_players);
        Assert.Equal(0, _players.CountOf(3));
        Assert.False(_players.IsPlaying(3, Stomp));
    }

    [Fact]
    public void ResetReturnsASpilledEntityToItsInlineRunAndItCanStartAgain()
    {
        using var table = new PlaybackTable(N, 1);
        using var store = new KindStore(_world, table);
        table.Start(0, Jump);
        table.Start(0, PotionHop);
        table.Start(0, Stomp);
        _world.Tick(table, true);
        Assert.True(table.CountOf(0) >= 2);
        Assert.True(table.IsPlaying(0, Jump) && table.IsPlaying(0, Stomp));
        table.Reset(0);
        _world.Tick(table, true);
        Assert.Equal(0, table.CountOf(0));
        Assert.Equal(0, table.Closing->Count);
        table.Start(0, Jump);
        _world.Tick(table, true);
        Assert.Equal(1, table.CountOf(0));
        Assert.True(table.IsPlaying(0, Jump));
        Assert.False(table.IsPlaying(0, Stomp));
    }

    [Fact]
    public void StopAllStillClosesWithEndPulsesWhereResetDoesNot()
    {
        _players.Start(0, Run);
        _players.Start(1, Run);
        Step();
        _players.StopAll(0);
        _players.Reset(1);
        Step();
        Assert.Equal(1, _players.Closing->Count);
        Assert.False(_players.IsPlaying(0, Run));
        Assert.False(_players.IsPlaying(1, Run));
    }

    [Fact]
    public void FindResolvesEveryManifestNameToItsId()
    {
        Assert.Equal(Jump, _world.Find("jump"));
        Assert.Equal(PotionHop, _world.Find("potion_hop"));
        Assert.Equal(Run, _world.Find("run"));
        Assert.Equal(RunLegacy, _world.Find("run_legacy"));
        Assert.Equal(SpeedBoost, _world.Find("speed_boost"));
        Assert.Equal(Stomp, _world.Find("stomp"));
        _players.Start(0, _world.Find("stomp"));
        Step();
        Assert.True(_players.IsPlaying(0, Stomp));
    }

    [Fact]
    public void FindIsOrdinalAndAnAbsentNameIsZero()
    {
        Assert.Equal((ushort)0, _world.Find("absent"));
        Assert.Equal((ushort)0, _world.Find(""));
        Assert.Equal((ushort)0, _world.Find(null!));
        Assert.Equal((ushort)0, _world.Find("JUMP"));
        Assert.Equal((ushort)0, _world.Find("jum"));
        Assert.Equal((ushort)0, _world.Find("jumps"));
        Assert.Equal((ushort)0, _world.Find("stomp "));
        Assert.Equal((ushort)0, _world.Find("a"));
        Assert.Equal((ushort)0, _world.Find("zzz"));
    }

    [Fact]
    public void FindFollowsAReloadAndAnEmptyLibraryFindsNothing()
    {
        var other = LibraryAssembler.Assemble([], 60, LibrarySettings.Default);
        _world.Reload(other);
        Assert.Equal((ushort)0, _world.Find("jump"));
        _world.Reload(_library);
        Assert.Equal(Jump, _world.Find("jump"));
    }

    [Fact]
    public void ACompileUnitThatNamesNoIdConstantAndNoLibraryFileStartsATimelineByName()
    {
        const string source = """
            using Tl;
            public static class Program
            {
                public static int Main(byte[] library)
                {
                    using var world = new TimelineWorld(library);
                    using var players = new PlaybackTable(1);
                    using var kinds = new KindStore(world, players);
                    var id = world.Find("stomp");
                    players.Start(0, id);
                    world.Tick(players, true);
                    return id * 10 + (players.IsPlaying(0, id) ? 1 : 0);
                }
            }
            """;
        var image = TestDomain.Compile("FindOnly", source, EmissionTests.References(), "", out _, out _);
        var context = new System.Runtime.Loader.AssemblyLoadContext("FindOnly", false);
        var assembly = context.LoadFromStream(new MemoryStream(image));
        var library = _library;
        var result = (int)assembly.GetType("Program", true)!.GetMethod("Main")!.Invoke(null, [library])!;
        Assert.Equal(Stomp * 10 + 1, result);
    }
}
