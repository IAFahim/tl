using IdleGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class IdleRunTests : IDisposable
{
    const ushort Film = 1;
    const int N = 2;
    const int Ticks = 30;

    readonly TimelineWorld _world;
    readonly PlaybackTable _players = new(N);
    readonly KindStore _store;
    readonly Screen[] _screen = new Screen[N];

    public IdleRunTests()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(System.IO.Path.Combine(dir, "tl.slnx")); i++) dir = System.IO.Path.GetDirectoryName(dir)!;
        var file = System.IO.Path.Combine(dir, "tests", "Tl.Systems.Tests", "IdleLibrary", "game.tlib");
        Assert.True(File.Exists(file), "Idle library golden missing; bake it with: tl library tests/Tl.Systems.Tests/IdleLibrary --assembly <Tl.Systems.Tests.dll> --namespace IdleGame --ids <scratch>/Timelines.g.cs --kinds <scratch>/Kinds.g.cs.");
        _world = new TimelineWorld(File.ReadAllBytes(file));
        _store = new KindStore(_world, _players);
    }

    public void Dispose()
    {
        _players.Dispose();
        _world.Dispose();
    }

    void Step(bool forward = true)
    {
        _world.Tick(_players, forward);
        new Stage { Screen = _screen }.Run(_world, _players, forward);
    }

    static int CaptionAt(int tick) => tick is >= 5 and < 10 ? 7 : tick is >= 12 and < 15 ? 9 : -1;

    static int FootageAt(int tick) => tick is >= 8 ? 3 : 0;

    [Fact]
    public void ACaptionFedKindFallsBackToItsIdleValueWhereNoClipCoversTheTick()
    {
        _players.Start(0, Film);
        for (var tick = 0; tick < Ticks; tick++)
        {
            Step();
            Assert.Equal(CaptionAt(tick), _screen[0].Caption);
        }
    }

    [Fact]
    public void AKindWithoutIdleStillHoldsItsLastValue()
    {
        _players.Start(0, Film);
        for (var tick = 0; tick < Ticks; tick++)
        {
            Step();
            Assert.Equal(FootageAt(tick), _screen[0].Footage);
        }
    }

    [Fact]
    public void IdleReachesEveryEnabledRowIncludingEntitiesThatNeverStartedAPlayback()
    {
        _players.Start(0, Film);
        Step();
        Assert.Equal(-1, _screen[1].Caption);
        for (var tick = 1; tick < Ticks; tick++) Step();
        Assert.Equal(-1, _screen[1].Caption);
        Assert.Equal(0, _screen[1].Footage);
    }

    [Fact]
    public void IdleKeepsApplyingAfterThePlaybackHasEnded()
    {
        _players.Start(0, Film);
        for (var tick = 0; tick < Ticks + 5; tick++) Step();
        Assert.False(_players.IsPlaying(0, Film));
        Assert.Equal(-1, _screen[0].Caption);
    }

    [Fact]
    public void RewindingThroughTheTimelineRestoresTheSameCaptionOnEveryTick()
    {
        _players.Start(0, Film);
        var forward = new int[Ticks];
        for (var tick = 0; tick < Ticks; tick++)
        {
            Step();
            forward[tick] = _screen[0].Caption;
        }
        for (var tick = 0; tick < Ticks; tick++) Assert.Equal(CaptionAt(tick), forward[tick]);
        var rewound = new List<int>();
        for (var back = 0; back < Ticks; back++)
        {
            Step(false);
            rewound.Add(_screen[0].Caption);
        }
        Assert.Contains(7, rewound);
        Assert.Contains(9, rewound);
        for (var i = 0; i < rewound.Count; i++)
        {
            var tick = Ticks - 2 - i;
            if (tick >= 0) Assert.Equal(forward[tick], rewound[i]);
        }
    }

    [Fact]
    public void ADisabledRowSkipsIdleLikeItSkipsItsEntries()
    {
        _players.Start(0, Film);
        Step();
        Assert.Equal(-1, _screen[0].Caption);
        _screen[0].Caption = 42;
        var kind = _store.IndexOf("IdleGame.Caption");
        _store.Table(kind)->SetEnabled(_store.RowOf(0, kind), false);
        Step();
        Assert.Equal(42, _screen[0].Caption);
        _store.Table(kind)->SetEnabled(_store.RowOf(0, kind), true);
        Step();
        Assert.Equal(-1, _screen[0].Caption);
    }
}
