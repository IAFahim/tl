using KindGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class ControlRunTests : IDisposable
{
    const ushort Jump = 1, Run = 3;
    const byte Locomotion = 1;
    const int N = 2;

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

    public ControlRunTests()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
        _world = new TimelineWorld(File.ReadAllBytes(Path.Combine(dir, "tests", "Tl.Systems.Tests", "KindLibrary", "game.tlib")));
        _store = new KindStore(_world, _players);
        for (var i = 0; i < N; i++)
        {
            _stat[i] = new WalkStat { Value = 1f };
            _power[i] = new JumpPower { Value = 2f };
        }
    }

    public void Dispose()
    {
        _players.Dispose();
        _world.Dispose();
    }

    Playback Slot => *_players.View.SlotOf(0, 0);

    void Step(bool forward = true)
    {
        _world.Tick(_players, forward);
        new Movement { Motion = _motion, Legacy = _legacy, WalkStat = _stat, JumpPower = _power, Stamina = _stamina, Heat = _heat }.Run(_world, _players, forward);
        new Vitals { Health = _health }.Run(_world, _players, forward);
    }

    void Expect(ushort tick, float x, float y, int stamina)
    {
        Assert.Equal(tick, Slot.Tick);
        Assert.Equal(x, _motion[0].X);
        Assert.Equal(y, _motion[0].Y);
        Assert.Equal(stamina, _stamina[0].Value);
    }

    [Fact]
    public void SeekLandsWithoutCrossingThenPlaysThroughTheMarkerAgain()
    {
        _players.Start(0, Jump);
        for (var s = 0; s < 8; s++) Step();
        Expect(7, 8f, 48f, 9);
        _players.Seek(0, Jump, 2);
        Step();
        Expect(2, 9f, 54f, 9);
        Assert.Equal(PlaybackFlags.Began | PlaybackFlags.ReenterPending, Slot.Flags);
        for (var s = 0; s < 3; s++) Step();
        Expect(5, 12f, 72f, 9);
        Step();
        Expect(6, 13f, 78f, 19);
    }

    [Fact]
    public void SeekForwardSkipsTheMarkerAndClampsPastTheEnd()
    {
        _players.Start(0, Jump);
        Step();
        _players.Seek(0, Jump, 9);
        Step();
        Expect(9, 2f, 12f, -1);
        _players.Seek(0, Jump, 500);
        Step();
        Expect(11, 3f, 18f, -1);
        Step();
        Assert.Equal(0, Slot.Timeline);
        Assert.Equal(18f, _motion[0].Y);
    }

    [Fact]
    public void ASoughtPausedPlaybackLandsAndStaysPausedUntilResumed()
    {
        _players.Start(0, Jump);
        for (var s = 0; s < 3; s++) Step();
        _players.Pause(0, Jump);
        Step();
        Expect(2, 3f, 18f, -1);
        _players.Seek(0, Jump, 7);
        Step();
        Step(false);
        Expect(7, 3f, 18f, -1);
        Assert.Equal(PlaybackFlags.Paused, Slot.Flags & PlaybackFlags.Paused);
        _players.Resume(0, Jump);
        Step();
        Expect(8, 4f, 24f, -1);
    }

    [Fact]
    public void ReversingOnePlaybackUndoesItsTicksExactlyAndEndsAtTheStart()
    {
        _players.Start(0, Jump);
        _players.Start(1, Jump);
        for (var s = 0; s < 7; s++) Step();
        Expect(6, 7f, 42f, 9);
        _players.Reverse(0, Jump, true);
        Step();
        Expect(5, 6f, 36f, 9);
        Assert.Equal(7, _players.View.SlotOf(1, 0)->Tick);
        for (var s = 0; s < 5; s++) Step();
        Expect(0, 1f, 6f, 10);
        Step();
        Assert.Equal(0, Slot.Timeline);
        Assert.Equal(1f, _motion[0].X);
        Assert.Equal(6f, _motion[0].Y);
        _players.Start(1, Jump);
        Step();
        _players.Reverse(1, Jump, true);
        _players.Reverse(1, Jump, false);
        Step();
        Assert.Equal(PlaybackFlags.None, _players.View.SlotOf(1, 0)->Flags);
        Assert.Equal(1, _players.View.SlotOf(1, 0)->Tick);
    }

    [Fact]
    public void ChannelVerbsActOnTheChannelsPlaybackAndChannelZeroDoesNothing()
    {
        _players.Start(0, Jump);
        for (var s = 0; s < 3; s++) Step();
        _players.PauseChannel(0, 0);
        _players.SeekChannel(0, 0, 9);
        _players.ReverseChannel(0, 0, true);
        Step();
        Expect(3, 4f, 24f, -1);
        _players.PauseChannel(0, Locomotion);
        Step();
        Expect(3, 4f, 24f, -1);
        _players.ResumeChannel(0, Locomotion);
        Step();
        Expect(4, 5f, 30f, -1);
        _players.SeekChannel(0, Locomotion, 8);
        Step();
        Expect(8, 6f, 36f, -1);
        _players.ReverseChannel(0, Locomotion, true);
        Step();
        Expect(7, 5f, 30f, -1);
        _players.Start(0, Run);
        _players.SeekChannel(0, Locomotion, 39);
        Step();
        Assert.Equal(Run, _players.View.SlotOf(0, 1)->Timeline);
        Assert.Equal(39, _players.View.SlotOf(0, 1)->Tick);
    }
}
