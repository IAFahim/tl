using KindGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class Phase8RuntimeTests : IDisposable
{
    const ushort Jump = 1, PotionHop = 2, Run = 3, RunLegacy = 4, SpeedBoost = 5, Stomp = 6;
    const int N = 4;

    readonly TimelineWorld _world;
    readonly PlaybackTable _players = new(N);
    readonly KindStore _store;
    readonly Motion[] _motion = new Motion[N];
    readonly LegacyMotion[] _legacy = new LegacyMotion[N];
    readonly WalkStat[] _stat = new WalkStat[N];
    readonly JumpPower[] _power = new JumpPower[N];
    readonly Stamina[] _stamina = new Stamina[N];
    readonly Heat[] _heat = new Heat[N];
    readonly Health[] _health = new Health[N];

    static string Path(string name)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(System.IO.Path.Combine(dir, "tl.slnx")); i++) dir = System.IO.Path.GetDirectoryName(dir)!;
        var file = System.IO.Path.Combine(dir, "tests", "Tl.Systems.Tests", "KindLibrary", name);
        Assert.True(File.Exists(file), $"Kind library golden missing: {file}.");
        return file;
    }

    public Phase8RuntimeTests()
    {
        _world = new TimelineWorld(File.ReadAllBytes(Path("game.tlib")));
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
    public void ResetClearsSlotsAndAddsNoClosingEntries()
    {
        _players.Start(0, Stomp);
        _world.Tick(_players, true);
        Assert.NotEqual(0, _players.CountOf(0));
        _players.Reset(0);
        Assert.Equal(0, _players.Closing->Count);
        Step();
        Assert.Equal(0, _players.CountOf(0));
        var slots = _players.SlotsOf(0);
        for (var i = 0; i < 4; i++) Assert.Equal(default, slots[i]);
        Assert.Equal(0, _players.Closing->Count);
    }

    [Fact]
    public void ResetDropsQueuedGamePulses()
    {
        var bullets = _store.Pulses<int>("KindGame.Damage");
        bullets.To(0, 41);
        bullets.To(0, 1);
        Assert.Equal(1000, _health[0].Value);
        _players.Reset(0);
        Step();
        Assert.Equal(1000, _health[0].Value);
        Assert.Equal(0, _store.DroppedPulses);
    }

    [Fact]
    public void ResetThenStartInOneStepStartsClean()
    {
        _players.Start(1, Stomp);
        _world.Tick(_players, true);
        _players.Reset(1);
        _players.Start(1, Stomp);
        Step();
        Assert.Equal(1, _players.CountOf(1));
        for (var s = 0; s < _players.CountOf(1); s++) Assert.Equal(Stomp, _players.SlotsOf(1)[s].Timeline);
        for (var i = 0; i < 20; i++) Step();
        Assert.True(_health[1].Value < 1000);
    }

    [Fact]
    public void StopAllStillFiresEndPulsesUnlikeReset()
    {
        _players.Start(2, Stomp);
        _world.Tick(_players, true);
        var before = _health[2].Value;
        Assert.NotEqual(0, _players.CountOf(2));
        _players.StopAll(2);
        Step();
        Step();
        Assert.False(_players.IsPlaying(2, Stomp));
        Assert.Equal(before, _health[2].Value);
    }

    [Fact]
    public void FindResolvesManifestNames()
    {
        Assert.True(_world.TryFind("jump", out var jump));
        Assert.Equal(Jump, jump);
        Assert.True(_world.TryFind("stomp", out var stomp));
        Assert.Equal(Stomp, stomp);
        Assert.Equal(Run, _world.Find("run"));
        Assert.Equal(RunLegacy, _world.Find("run_legacy"));
    }

    [Fact]
    public void FindDoesNotPanicOnAbsentNames()
    {
        Assert.Equal((ushort)0, _world.Find("absent"));
        Assert.False(_world.TryFind("absent", out var id));
        Assert.Equal((ushort)0, id);
        Assert.False(_world.TryFind("", out _));
        Assert.False(_world.TryFind("JUMP", out _));
        Assert.False(_world.TryFind("jum", out _));
        Assert.False(_world.TryFind("stomp ", out _));
    }

    [Fact]
    public void FindOrdinalEdgeNamesAreAbsent()
    {
        Assert.Equal((ushort)0, _world.Find("j ump"));
        Assert.Equal((ushort)0, _world.Find("⚡"));
        Assert.False(_world.TryFind(null!, out _));
    }

    [Fact]
    public void RequestsResetEnqueuesIdenticalOp()
    {
        _players.Start(3, Stomp);
        _world.Tick(_players, true);
        _world.Requests(_players).For(3, -1).Reset();
        _world.ApplyRequests(_players);
        Assert.Equal(0, _players.CountOf(3));
    }
}
