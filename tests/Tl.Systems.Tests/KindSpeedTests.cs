using KindGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class KindSpeedTests
{
    const ushort Jump = 1, Run = 3, SpeedBoost = 5, Stomp = 6;

    sealed class Game : IDisposable
    {
        public readonly TimelineWorld World;
        public readonly PlaybackTable Players;
        public readonly KindStore Store;
        public readonly Motion[] Motion;
        readonly LegacyMotion[] _legacy;
        readonly WalkStat[] _stat;
        readonly JumpPower[] _power;
        public readonly Stamina[] Stamina;
        readonly Heat[] _heat;
        public readonly Health[] Health;

        public Game(int n)
        {
            var dir = AppContext.BaseDirectory;
            for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "tl.slnx")); i++) dir = Path.GetDirectoryName(dir)!;
            World = new TimelineWorld(File.ReadAllBytes(Path.Combine(dir, "tests", "Tl.Systems.Tests", "KindLibrary", "game.tlib")));
            Players = new PlaybackTable(n);
            Store = new KindStore(World, Players);
            Motion = new Motion[n];
            _legacy = new LegacyMotion[n];
            _stat = new WalkStat[n];
            _power = new JumpPower[n];
            Stamina = new Stamina[n];
            _heat = new Heat[n];
            Health = new Health[n];
            for (var i = 0; i < n; i++)
            {
                _stat[i] = new WalkStat { Value = 1f };
                _power[i] = new JumpPower { Value = 2f };
                Health[i] = new Health { Value = 100 };
            }
        }

        public void Step(bool forward = true)
        {
            World.Tick(Players, forward);
            new Movement { Motion = Motion, Legacy = _legacy, WalkStat = _stat, JumpPower = _power, Stamina = Stamina, Heat = _heat }.Run(World, Players, forward);
            new Vitals { Health = Health }.Run(World, Players, forward);
        }

        public void Dispose()
        {
            Players.Dispose();
            World.Dispose();
        }
    }

    [Fact]
    public void TwoRatesFeedingOneKindResolvePriorityPerSubstep()
    {
        using var game = new Game(1);
        game.Players.Start(0, Run);
        game.Players.Start(0, SpeedBoost);
        game.Players.SetSpeed(0, SpeedBoost, 32);
        var expected = 0f;
        for (var s = 1; s <= 14; s++)
        {
            game.Step();
            expected += s <= 10 ? 5f + 5f : 2f;
            Assert.Equal(expected, game.Motion[0].X);
        }
        Assert.Equal(108f, game.Motion[0].X);
    }

    [Fact]
    public void TwoXOverSixStepsEqualsOneXOverTwelveWithEachModePulsesAndMarkers()
    {
        using var fast = new Game(1);
        using var slow = new Game(1);
        fast.Players.Start(0, Jump);
        fast.Players.SetSpeed(0, Jump, 32);
        slow.Players.Start(0, Jump);
        for (var s = 1; s <= 7; s++)
        {
            fast.Step();
            slow.Step();
            slow.Step();
            Assert.Equal(slow.Motion[0].X, fast.Motion[0].X);
            Assert.Equal(slow.Motion[0].Y, fast.Motion[0].Y);
            Assert.Equal(slow.Stamina[0].Value, fast.Stamina[0].Value);
        }
        Assert.Equal(12f, fast.Motion[0].X);
        Assert.Equal(72f, fast.Motion[0].Y);
        Assert.Equal(9, fast.Stamina[0].Value);
    }

    [Fact]
    public void HalfSpeedContributesNothingInAZeroCrossingStep()
    {
        using var game = new Game(2);
        game.Players.Start(0, Run);
        game.Players.SetSpeed(0, Run, 8);
        game.Players.Start(1, Jump);
        game.Players.SetSpeed(1, Jump, 8);
        var xs = new float[6];
        var ys = new float[6];
        for (var s = 0; s < 6; s++)
        {
            game.Step();
            xs[s] = game.Motion[0].X;
            ys[s] = game.Motion[1].Y;
        }
        Assert.Equal(new[] { 0f, 2f, 2f, 4f, 4f, 6f }, xs);
        Assert.Equal(new[] { 0f, 6f, 6f, 12f, 12f, 18f }, ys);
        Assert.Equal(-1, game.Stamina[1].Value);
    }

    [Fact]
    public void ReversingAtTwoXUndoesWhatOneXUndoesInTwiceTheSteps()
    {
        using var fast = new Game(1);
        using var slow = new Game(1);
        fast.Players.Start(0, Jump);
        fast.Players.SetSpeed(0, Jump, 32);
        slow.Players.Start(0, Jump);
        for (var s = 0; s < 4; s++) fast.Step();
        for (var s = 0; s < 8; s++) slow.Step();
        Assert.Equal(slow.Stamina[0].Value, fast.Stamina[0].Value);
        fast.Players.Reverse(0, Jump, true);
        slow.Players.Reverse(0, Jump, true);
        for (var s = 1; s <= 4; s++)
        {
            fast.Step();
            slow.Step();
            slow.Step();
            Assert.Equal(slow.Motion[0].X, fast.Motion[0].X);
            Assert.Equal(slow.Motion[0].Y, fast.Motion[0].Y);
            Assert.Equal(slow.Stamina[0].Value, fast.Stamina[0].Value);
            Assert.Equal(slow.Players.View.SlotOf(0, 0)->Tick, fast.Players.View.SlotOf(0, 0)->Tick);
        }
        Assert.Equal(PlaybackRules.Parked | PlaybackFlags.Reverse, fast.Players.View.SlotOf(0, 0)->Flags);
        Assert.Equal(6f, fast.Motion[0].Y);
    }

    [Fact]
    public void AGamePulseLandsOnceInSubstepZeroOfASampledEntity()
    {
        using var game = new Game(1);
        var damage = game.Store.Pulses<int>("KindGame.Damage");
        game.Players.Start(0, Stomp);
        game.Players.SetSpeed(0, Stomp, 48);
        var health = new int[4];
        for (var s = 0; s < 4; s++)
        {
            damage.To(0, 1);
            game.Step();
            health[s] = game.Health[0].Value;
        }
        Assert.Equal(new[] { 74, 63, 62, 61 }, health);
    }
}
