using Game;
using Tl;
using Xunit;
using Tl.Systems.Tests;

public sealed unsafe class RuntimeTests : IDisposable
{
    readonly ViewScope _scope = new();
    readonly TimelineWorld _world;
    readonly PlaybackTable _players = new(8);
    readonly Motion[] _motion = new Motion[8];
    readonly JumpPower[] _power = new JumpPower[8];
    readonly Stats[] _stats = new Stats[8];
    readonly InputState[] _input = new InputState[8];
    readonly Anim[] _anim = new Anim[8];
    readonly Foot[] _feet = new Foot[8];
    readonly Position[] _position = new Position[8];
    readonly HeadIndex[] _heads = new HeadIndex[8];
    readonly Sound[] _sound = new Sound[8];
    readonly Events<Cue> _cues = new(4096);
    readonly Events<StompHit> _hits = new(4096);
    readonly List<int> _cueLog = new();
    readonly List<StompHit> _hitLog = new();

    public RuntimeTests()
    {
        _cueLog.Capacity = 4096;
        _hitLog.Capacity = 4096;
        for (var i = 0; i < 8; i++)
        {
            _power[i] = new JumpPower(1f);
            _heads[i] = new HeadIndex(10);
            _input[i] = new InputState(Button.None);
        }
        FixtureWorlds.BuildStandard(_scope);
        _world = new TimelineWorld();
        _scope.InjectInto(_world);
        _scope.RegisterMarkers(_world);
        GC.SuppressFinalize(_world);
    }

    public void Dispose()
    {
        _players.Dispose();
        _cues.Dispose();
        _hits.Dispose();
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }

    void Step(bool forward = true)
    {
        _world.ApplyRequests(_players);
        var movement = new Movement
        {
            Motion = _motion,
            JumpPower = _power,
            Stats = _stats,
            Input = _input,
            Anim = _anim,
            Feet = _feet,
            Requests = _world.Requests(_players),
            Cues = _cues.Writer,
        };
        movement.Run(_world, _players, forward);
        var combat = new Combat
        {
            Position = _position,
            Heads = _heads,
            Sound = _sound,
            Hits = _hits.Writer,
        };
        combat.Run(_world, _players, forward);
        Drain();
        _world.Advance(_players, forward);
    }

    void Drain()
    {
        foreach (var record in _cues.Drain()) _cueLog.Add(record.Value.Code);
        foreach (var record in _hits.Drain()) _hitLog.Add(record.Value);
    }

    [Fact]
    public void RewindWalksTheArcBackExactly()
    {
        for (var e = 0; e < 4; e++) _players.Start(e, 1);
        for (var f = 0; f < 40; f++) Step();
        for (var e = 0; e < 4; e++) Assert.Equal(80f, _motion[e].Y);
        for (var f = 0; f < 40; f++) Step(forward: false);
        for (var e = 0; e < 4; e++)
        {
            Assert.Equal(0f, _motion[e].Y);
            Assert.Equal(0, _players.SlotsOf(e)[0].Tick);
        }
        Assert.Equal(new[] { 10, 10, 10, 10, 1001, 1001, 1001, 1001, 1002, 1002, 1002, 1002, 10, 10, 10, 10, 1002, 1002, 1002, 1002, 1001, 1001, 1001, 1001 }, _cueLog);
    }

    [Fact]
    public void BarrelWindowRequestsOnlyOnPress()
    {
        _players.Start(0, 3);
        for (var f = 0; f < 16; f++) Step();
        Assert.False(_players.IsPlaying(0, 4));
        Assert.DoesNotContain(100, _cueLog);
    }

    [Fact]
    public void BarrelWindowPressStartsBarrel()
    {
        _players.Start(0, 3);
        for (var f = 0; f < 16; f++)
        {
            _input[0] = new InputState(_players.SlotsOf(0)[0].Tick == 10 ? Button.Attack : Button.None);
            Step();
        }
        Assert.True(_players.IsPlaying(0, 4));
        var barrels = 0;
        for (var s = 0; s < _players.CountOf(0); s++)
            if (_players.SlotsOf(0)[s].Timeline == 4) barrels++;
        Assert.Equal(1, barrels);
        for (var f = 0; f < 4; f++) Step();
        Assert.Contains(100, _cueLog);
    }

    [Fact]
    public void ChannelReplaceKeepsSlotAndChannel()
    {
        _players.Start(0, 2);
        for (var f = 0; f < 5; f++) Step();
        _players.Start(0, 3);
        _world.ApplyRequests(_players);
        Assert.False(_players.IsPlaying(0, 2));
        Assert.True(_players.IsPlaying(0, 3));
        Assert.Equal(1, _players.CountOf(0));
        Assert.Equal(1, _players.SlotsOf(0)[0].Channel);
        Assert.Equal(0, _players.SlotsOf(0)[0].Tick);
    }

    [Fact]
    public void SyntheticStartMarksStartPending()
    {
        _players.Start(0, 3, at: 10);
        _world.ApplyRequests(_players);
        var playback = _players.SlotsOf(0)[0];
        Assert.Equal(10, playback.Tick);
        Assert.True((playback.Flags & PlaybackFlags.StartPending) != 0);
    }

    [Fact]
    public void InterruptOnStopRaisesInterruptedEnd()
    {
        _players.Start(0, 4);
        for (var f = 0; f < 6; f++) Step();
        var before = _hitLog.Count;
        Assert.True(before > 0);
        _players.Stop(0, 4);
        _world.ApplyRequests(_players);
        Assert.False(_players.IsPlaying(0, 4));
        Step();
        Assert.Equal(before + 1, _hitLog.Count);
        Assert.Equal(-1, _hitLog[^1].Head);
    }

    [Fact]
    public void LoopWrapsBackward()
    {
        _players.Start(0, 1);
        _world.ApplyRequests(_players);
        _world.Advance(_players, forward: false);
        Assert.True(_players.IsPlaying(0, 1));
        Assert.Equal(39, _players.SlotsOf(0)[0].Tick);
        for (var f = 0; f < 39; f++) _world.Advance(_players, forward: false);
        Assert.Equal(0, _players.SlotsOf(0)[0].Tick);
    }

    [Fact]
    public void PauseHoldsTheTickAndResumeContinues()
    {
        _players.Start(0, 2);
        _world.ApplyRequests(_players);
        for (var f = 0; f < 5; f++) _world.Advance(_players, forward: true);
        var tick = _players.SlotsOf(0)[0].Tick;
        _players.Pause(0, 2);
        _world.ApplyRequests(_players);
        Assert.NotEqual(0, (int)(_players.SlotsOf(0)[0].Flags & PlaybackFlags.Paused));
        for (var f = 0; f < 5; f++) _world.Advance(_players, forward: true);
        Assert.Equal(tick, _players.SlotsOf(0)[0].Tick);
        _players.Resume(0, 2);
        _world.ApplyRequests(_players);
        Assert.Equal(0, (int)(_players.SlotsOf(0)[0].Flags & PlaybackFlags.Paused));
        _world.Advance(_players, forward: true);
        Assert.Equal(tick + 1, _players.SlotsOf(0)[0].Tick);
    }

    [Fact]
    public void StopChannelRemovesOnlyThatChannel()
    {
        _players.Start(0, 2);
        _players.Start(0, 4);
        _world.ApplyRequests(_players);
        Assert.Equal(2, _players.CountOf(0));
        _players.StopChannel(0, 1);
        _world.ApplyRequests(_players);
        Assert.False(_players.IsPlaying(0, 2));
        Assert.True(_players.IsPlaying(0, 4));
        Assert.Equal(2, _players.CountOf(0));
        Assert.Equal(0, _players.SlotsOf(0)[0].Timeline);
        Assert.Equal(4, _players.SlotsOf(0)[1].Timeline);
    }

    [Fact]
    public void SlotsKeepTheirIndexReuseLowestFreeAndGrow()
    {
        using var players = new PlaybackTable(2, 2);
        players.Start(0, 1);
        players.Start(0, 2);
        players.Start(0, 4);
        _world.ApplyRequests(players);
        Assert.Equal(4, players.SlotsPerEntity);
        Assert.Equal(3, players.CountOf(0));
        players.Stop(0, 1);
        _world.ApplyRequests(players);
        Assert.Equal(2, players.SlotsOf(0)[1].Timeline);
        Assert.Equal(4, players.SlotsOf(0)[2].Timeline);
        players.Start(0, 5);
        _world.ApplyRequests(players);
        Assert.Equal(5, players.SlotsOf(0)[0].Timeline);
        players.Stop(0, 4);
        _world.ApplyRequests(players);
        Assert.Equal(2, players.CountOf(0));
    }

    [Fact]
    public void StopAllClearsEveryPlayback()
    {
        _players.Start(0, 2);
        _players.Start(0, 4);
        _players.Start(1, 1);
        _world.ApplyRequests(_players);
        _players.StopAll(0);
        _players.StopAll(1);
        _world.ApplyRequests(_players);
        Assert.Equal(0, _players.CountOf(0));
        Assert.Equal(0, _players.CountOf(1));
    }

    [Fact]
    public void EventTickBitsMatchEventList()
    {
        var view = _scope.View;
        for (var t = 1; t <= view.TimelineCount; t++)
        {
            var tl = LibraryViewReader.GetTimeline(in view, new TimelineId((ushort)t));
            Assert.False(tl is null);
            var events = LibraryViewReader.Events(tl);
            for (var tick = 0u; tick < tl->Duration; tick++)
            {
                var expected = false;
                foreach (var e in events) if (e.Tick == tick) { expected = true; break; }
                Assert.Equal(expected, LibraryViewReader.HasEvent(tl, tick));
            }
        }
    }

    [Fact]
    public void NextHandsOverToRunAtZero()
    {
        _players.Start(0, 2);
        for (var f = 0; f < 30; f++) Step();
        Assert.True(_players.IsPlaying(0, 1));
        Assert.False(_players.IsPlaying(0, 2));
        var playback = _players.SlotsOf(0)[0];
        Assert.Equal(1, playback.Timeline);
        Assert.Equal(0, playback.Tick);
        Assert.True((playback.Flags & PlaybackFlags.StartPending) != 0);
        Step();
        playback = _players.SlotsOf(0)[0];
        Assert.Equal(1, playback.Tick);
        Assert.False((playback.Flags & PlaybackFlags.StartPending) != 0);
    }

    [Fact]
    public void FinishedWithoutHoldRemovesPlayback()
    {
        _players.Start(0, 6);
        for (var f = 0; f < 8; f++) Step();
        Assert.False(_players.IsPlaying(0, 6));
        Assert.Equal(0, _players.CountOf(0));
    }

    [Fact]
    public void FinishedWithHoldEndsPlayback()
    {
        _players.Start(0, 5);
        for (var f = 0; f < 12; f++) Step();
        Assert.True(_players.IsPlaying(0, 5));
        var playback = _players.SlotsOf(0)[0];
        Assert.True((playback.Flags & PlaybackFlags.Ended) != 0);
        Assert.Equal(11, playback.Tick);
        for (var f = 0; f < 4; f++) Step();
        playback = _players.SlotsOf(0)[0];
        Assert.Equal(11, playback.Tick);
        Assert.True((playback.Flags & PlaybackFlags.Ended) != 0);
    }

    [Fact]
    public void TwoSystemsShareOneTimeline()
    {
        _players.Start(0, 4);
        for (var f = 0; f < 3; f++) Step();
        Assert.Contains(100, _cueLog);
        Assert.Equal(3, _hitLog.Count);
    }
    [Fact]
    public void MismatchedColumnsAreRejected()
    {
        _players.Start(0, 1);
        _world.ApplyRequests(_players);
        var shortPower = new JumpPower[4];
        var movement = new Movement
        {
            Motion = _motion,
            JumpPower = shortPower,
            Stats = _stats,
            Input = _input,
            Anim = _anim,
            Feet = _feet,
            Requests = _world.Requests(_players),
            Cues = _cues.Writer,
        };
        System.Exception? thrown = null;
        try { movement.Run(_world, _players); }
        catch (System.ArgumentException ex) { thrown = ex; }
        Assert.NotNull(thrown);
    }
    [Fact]
    public void BatchBlockMatchesManualFold()
    {
        _players.Start(0, 1);
        for (var f = 0; f < 40; f++) Step();
        Assert.Equal(80f, _motion[0].Y);
    }

    [Fact]
    public void FastFoldAddMatchesScalarLoop()
    {
        const int count = 1000;
        var lane = new float[64];
        var ticks = new ushort[count];
        var fast = new float[count];
        var plain = new float[count];
        for (var i = 0; i < 64; i++) lane[i] = i * 0.5f;
        for (var i = 0; i < count; i++) ticks[i] = (ushort)((i * 37) & 63);
        fixed (float* lanePtr = lane)
        fixed (ushort* tickPtr = ticks)
        fixed (float* fastPtr = fast)
            SystemDispatch.FastFoldAdd(lanePtr, tickPtr, fastPtr, count);
        for (var i = 0; i < count; i++) plain[i] += lane[ticks[i]];
        Assert.Equal(plain, fast);
    }

    [Fact]
    public void FullFlowHashesIdenticallyTwice()
    {
        var first = FlowOnce();
        var second = FlowOnce();
        Assert.Equal(first.Motion, second.Motion);
        Assert.Equal(first.Stats, second.Stats);
        Assert.Equal(first.Cues, second.Cues);
        Assert.Equal(first.Hits, second.Hits);
    }

    (float[] Motion, int[] Stats, int[] Cues, string Hits) FlowOnce()
    {
        using var scope = new ViewScope();
        FixtureWorlds.BuildStandard(scope);
        var world = new TimelineWorld();
        scope.InjectInto(world);
        scope.RegisterMarkers(world);
        GC.SuppressFinalize(world);
        using var players = new PlaybackTable(8);
        using var cues = new Events<Cue>(4096);
        using var hits = new Events<StompHit>(4096);
        var motion = new Motion[8];
        var power = new JumpPower[8];
        var stats = new Stats[8];
        var input = new InputState[8];
        var anim = new Anim[8];
        var feet = new Foot[8];
        var position = new Position[8];
        var heads = new HeadIndex[8];
        var sound = new Sound[8];
        for (var i = 0; i < 8; i++)
        {
            power[i] = new JumpPower(1f);
            heads[i] = new HeadIndex(10);
            input[i] = new InputState(Button.None);
        }
        var cueCodes = new List<int>();
        var hitStrings = new List<string>();
        players.Start(0, 2);
        players.Start(1, 3);
        for (var f = 0; f < 60; f++)
        {
            input[1] = new InputState(players.CountOf(1) > 0 && players.SlotsOf(1)[0].Timeline == 3 && players.SlotsOf(1)[0].Tick == 10 ? Button.Attack : Button.None);
            world.ApplyRequests(players);
            var movement = new Movement { Motion = motion, JumpPower = power, Stats = stats, Input = input, Anim = anim, Feet = feet, Requests = world.Requests(players), Cues = cues.Writer };
            movement.Run(world, players);
            var combat = new Combat { Position = position, Heads = heads, Sound = sound, Hits = hits.Writer };
            combat.Run(world, players);
            foreach (var record in cues.Drain()) cueCodes.Add(record.Value.Code);
            foreach (var record in hits.Drain()) hitStrings.Add(record.Value.Head + ":" + record.Value.Radius);
            world.Advance(players);
        }
        var ys = new float[8];
        var counters = new int[8];
        for (var i = 0; i < 8; i++)
        {
            ys[i] = motion[i].Y;
            counters[i] = stats[i].JumpCounter;
        }
        return (ys, counters, cueCodes.ToArray(), string.Join(",", hitStrings));
    }

    [Fact]
    public void WarmPathAllocatesNothing()
    {
        for (var e = 0; e < 4; e++) _players.Start(e, 1);
        for (var f = 0; f < 20; f++) Step();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var f = 0; f < 128; f++) Step();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
    }
    [Fact]
    public void MissingFoldLaneFallsBackToFrameFold()
    {
        using var scope = new ViewScope();
        var keys = FixtureWorlds.Keys.Value;
        var arc = new ViewScope.TimelineSpec { Loop = 1, Duration = 30 };
        arc.Steps.Add(new ViewScope.StepSpec
        {
            PairKey = keys["Arc"],
            Track = FixtureWorlds.Image(new Game.ArcTrack(1f)),
            First = FixtureWorlds.Image(new Game.ArcClip(2f)),
            WindowStart = 0,
            WindowEnd = 30,
        });
        scope.Build(new System.Collections.Generic.List<ViewScope.TimelineSpec> { arc });
        var world = new TimelineWorld();
        scope.InjectInto(world);
        GC.SuppressFinalize(world);
        using var players = new PlaybackTable(1);
        var motion = new Motion[1];
        var power = new[] { new JumpPower(1f) };
        var stats = new Stats[1];
        var input = new InputState[1];
        var anim = new Anim[1];
        var feet = new Foot[1];
        using var cues = new Events<Cue>(64);
        players.Start(0, 1);
        for (var f = 0; f < 30; f++)
        {
            world.ApplyRequests(players);
            var movement = new Movement
            {
                Motion = motion,
                JumpPower = power,
                Stats = stats,
                Input = input,
                Anim = anim,
                Feet = feet,
                Requests = world.Requests(players),
                Cues = cues.Writer,
            };
            movement.Run(world, players, true);
            world.Advance(players, true);
        }
        Assert.Equal(60f, motion[0].Y);
    }
}
