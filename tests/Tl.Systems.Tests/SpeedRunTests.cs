using Game;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class SpeedRunTests : IDisposable
{
    const int N = 5;
    static readonly ushort[] Rates = [16, 32, 8, 24, 40];

    readonly TimelineWorld _world = new(File.ReadAllBytes(Path.Combine(LoaderRunTests.LoaderLibraryDir(), "game.tlib")));
    readonly PlaybackTable _players = new(N);
    readonly Motion[] _motion = new Motion[N];
    readonly JumpPower[] _power = new JumpPower[N];
    readonly Stats[] _stats = new Stats[N];
    readonly InputState[] _input = new InputState[N];
    readonly Anim[] _anim = new Anim[N];
    readonly Foot[] _feet = new Foot[N];
    readonly Events<Cue> _cues = new(4096);
    readonly List<int>[] _codes = new List<int>[N];

    public SpeedRunTests()
    {
        for (var i = 0; i < N; i++)
        {
            _power[i] = new JumpPower(1f);
            _codes[i] = new List<int>();
        }
    }

    public void Dispose()
    {
        _players.Dispose();
        _cues.Dispose();
        _world.Dispose();
    }

    Movement System => new() { Motion = _motion, JumpPower = _power, Stats = _stats, Input = _input, Anim = _anim, Feet = _feet, Requests = _world.Requests(_players), Cues = _cues.Writer };

    void Step(bool fused, bool forward = true)
    {
        _world.ApplyRequests(_players);
        if (fused) System.RunFused(_world, _players, forward);
        else
        {
            System.Run(_world, _players, forward);
            _world.Advance(_players, forward);
        }
        foreach (var record in _cues.Drain()) _codes[record.Entity.Index].Add(record.Value.Code);
    }

    (float[] Y, int[] Cues) Reference(int ticks, bool fused)
    {
        using var world = new TimelineWorld(File.ReadAllBytes(Path.Combine(LoaderRunTests.LoaderLibraryDir(), "game.tlib")));
        using var players = new PlaybackTable(1);
        using var cues = new Events<Cue>(4096);
        var motion = new Motion[1];
        var y = new float[ticks + 1];
        var count = new int[ticks + 1];
        var total = 0;
        players.Start(0, 1);
        for (var t = 1; t <= ticks; t++)
        {
            world.ApplyRequests(players);
            var system = new Movement { Motion = motion, JumpPower = [new JumpPower(1f)], Stats = new Stats[1], Input = new InputState[1], Anim = new Anim[1], Feet = new Foot[1], Requests = world.Requests(players), Cues = cues.Writer };
            if (fused) system.RunFused(world, players, true);
            else
            {
                system.Run(world, players, true);
                world.Advance(players, true);
            }
            total += cues.Drain().Length;
            y[t] = motion[0].Y;
            count[t] = total;
        }
        return (y, count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EachRateAppliesExactlyWhatOneXAppliesOverTheTicksItCrossed(bool fused)
    {
        var reference = Reference(120, fused);
        for (var e = 0; e < N; e++)
        {
            _players.Start(e, 1);
            _players.SetSpeed(e, 1, Rates[e]);
        }
        for (var s = 1; s <= 40; s++)
        {
            Step(fused);
            for (var e = 0; e < N; e++)
            {
                var crossed = s * Rates[e] >> 4;
                Assert.Equal(reference.Y[crossed], _motion[e].Y);
                Assert.Equal(reference.Cues[crossed], _codes[e].Count);
                Assert.Equal(crossed % 8, _players.View.SlotOf(e, 0)->Tick);
                Assert.Equal(e == 0 ? 0 : s * Rates[e] & 15, _players.View.SlotOf(e, 0)->Phase);
            }
        }
        Assert.Equal(new[] { 1003, 1003, 1003, 1003, 1003 }, _codes[0].ToArray());
        Assert.Equal(10, _codes[1].Count);
        Assert.Equal(new[] { 0f, 0f, 8f, 8f, 8f }, _motion.Select(m => m.Y).ToArray());
    }

    [Fact]
    public void SpeedChangesKeepThePhaseOneXClearsItAndPauseFreezesIt()
    {
        _players.Start(0, 1);
        _players.SetSpeed(0, 1, 6);
        for (var s = 0; s < 3; s++) Step(false);
        Assert.Equal(1, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(2, _players.View.SlotOf(0, 0)->Phase);
        _players.Pause(0, 1);
        for (var s = 0; s < 3; s++) Step(false);
        Assert.Equal(1, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(2, _players.View.SlotOf(0, 0)->Phase);
        _players.Resume(0, 1);
        _players.SetSpeed(0, 1, 30);
        Step(false);
        Assert.Equal(3, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(0, _players.View.SlotOf(0, 0)->Phase);
        Assert.Equal(30, _players.View.SlotOf(0, 0)->Rate);
        _players.SetSpeed(0, 1, 16);
        Step(false);
        Assert.Equal(4, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(0, _players.View.SlotOf(0, 0)->Rate);
        _players.SetChannelSpeed(0, 0, 64);
        Step(false);
        Assert.Equal(5, _players.View.SlotOf(0, 0)->Tick);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AReversedPlaybackAtTwoXUndoesTwoTicksPerStep(bool fused)
    {
        _players.Start(1, 1);
        for (var s = 0; s < 5; s++) Step(fused);
        _players.Start(0, 1);
        _players.SetSpeed(0, 1, 32);
        for (var s = 0; s < 5; s++) Step(fused);
        Assert.Equal(_players.View.SlotOf(1, 0)->Tick, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(_motion[1].Y, _motion[0].Y);
        Assert.Equal(_codes[1].Count, _codes[0].Count);
        _players.Reverse(0, 1, true);
        _players.Reverse(1, 1, true);
        for (var s = 0; s < 2; s++) Step(fused);
        Assert.Equal(6, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(0, _players.View.SlotOf(1, 0)->Tick);
        _players.Pause(0, 1);
        for (var s = 0; s < 2; s++) Step(fused);
        Assert.Equal(_players.View.SlotOf(1, 0)->Tick, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(_motion[1].Y, _motion[0].Y);
        Assert.Equal(_codes[1], _codes[0]);
        Assert.Equal(0f, _motion[0].Y);
    }
}
