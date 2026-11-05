using KindGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class KindGroupRunTests : IDisposable
{
    const ushort Ember = 1, Flare = 2;
    const int N = 3;

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

    public KindGroupRunTests()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(System.IO.Path.Combine(dir, "tl.slnx")); i++) dir = System.IO.Path.GetDirectoryName(dir)!;
        _world = new TimelineWorld(File.ReadAllBytes(System.IO.Path.Combine(dir, "tests", "Tl.Systems.Tests", "GroupLibrary", "group.tlib")));
        _store = new KindStore(_world, _players);
    }

    public void Dispose()
    {
        _players.Dispose();
        _world.Dispose();
    }

    void Step()
    {
        _world.Tick(_players, true);
        new Movement { Motion = _motion, Legacy = _legacy, WalkStat = _stat, JumpPower = _power, Stamina = _stamina, Heat = _heat }.Run(_world, _players, true);
        new Vitals { Health = _health }.Run(_world, _players, true);
    }

    [Fact]
    public void MembersOfAGroupShareOneTableWithOneRowPerEntity()
    {
        var burn = _store.IndexOf("Burn");
        var chill = _store.IndexOf("Chill");
        Assert.True(_store.Table(burn) == _store.Table(chill));
        Assert.True(_store.Table(burn)->Grouped);
        Assert.Equal(16, _store.Table(burn)->Stride);
        Assert.Equal(N, _store.Table(burn)->Rows);
        Assert.Equal(_store.RowOf(1, burn), _store.RowOf(1, chill));
        Assert.False(_store.Table(_store.IndexOf("Lift"))->Grouped);
    }

    [Fact]
    public void EachMemberAppliesOnlyItsOwnEntriesFromTheSharedTable()
    {
        var chill = _store.Pulses<int>("Chill");
        _players.Start(0, Flare);
        _players.Start(0, Ember);
        Step();
        Assert.Equal(5f, _heat[0].Value);
        Assert.Equal(0, _stamina[0].Value);
        chill.To(0, 3);
        Step();
        Assert.Equal(10f, _heat[0].Value);
        Assert.Equal(-3, _stamina[0].Value);
        Step();
        Assert.Equal(15f, _heat[0].Value);
        Assert.Equal(-7, _stamina[0].Value);
    }

    [Fact]
    public void StoppingOnePlaybackRemovesOnlyItsSpansFromTheSharedRow()
    {
        _players.Start(0, Flare);
        _players.Start(0, Ember);
        Step();
        var table = _store.Table(_store.IndexOf("Burn"));
        var row = _store.RowOf(0, _store.IndexOf("Burn"));
        Assert.Equal(2, table->Length[row]);
        _players.Stop(0, Ember);
        Step();
        Assert.Equal(1, table->Length[row]);
        Assert.Equal(8f, _heat[0].Value);
        _players.StopAll(0);
        Step();
        Assert.False(table->IsActive(row));
        Assert.Equal(8f, _heat[0].Value);
    }

    [Fact]
    public void ALibraryBakedBeforeTheGroupExistedLeavesItsKindsOut()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(System.IO.Path.Combine(dir, "tl.slnx")); i++) dir = System.IO.Path.GetDirectoryName(dir)!;
        using var players = new PlaybackTable(1);
        using var world = new TimelineWorld(File.ReadAllBytes(System.IO.Path.Combine(dir, "tests", "Tl.Systems.Tests", "KindLibrary", "game.tlib")));
        using var store = new KindStore(world, players);
        Assert.Equal(-1, store.IndexOf("Burn"));
    }
}
