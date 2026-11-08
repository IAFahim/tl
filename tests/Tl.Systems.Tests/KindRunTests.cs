using KindGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class KindRunTests : IDisposable
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
        Assert.True(File.Exists(file), $"Kind library golden missing; bake it with: tl library tests/Tl.Systems.Tests/KindLibrary --assembly <Tl.Systems.Tests.dll> --namespace KindGame --ids <scratch>/Timelines.g.cs --kinds <scratch>/Kinds.g.cs ({name}).");
        return file;
    }

    public KindRunTests() : this("game.tlib") { }

    KindRunTests(string library)
    {
        _world = new TimelineWorld(File.ReadAllBytes(Path(library)));
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
    public void SpeedBoostOverridesTheNormalWalkByPriorityThenRunTakesOver()
    {
        _players.Start(0, Run);
        Step();
        Assert.Equal(2f, _motion[0].X);
        _players.Start(0, SpeedBoost);
        Step();
        Assert.Equal(7f, _motion[0].X);
        for (var s = 2; s <= 20; s++) Step();
        Assert.Equal(102f, _motion[0].X);
        Step();
        Assert.Equal(104f, _motion[0].X);
        Assert.Equal(1, _store.GetStats(_store.IndexOf("KindGame.Locomotion")).MaxLength - 1);
    }

    [Fact]
    public void LiftsAddUpAndStatChangePulsesFromClipsAndMarkersApplyEachOnce()
    {
        _players.Start(0, Jump);
        for (var s = 0; s < 2; s++) Step();
        _players.Start(0, PotionHop);
        for (var s = 2; s <= 5; s++) Step();
        Assert.Equal(44f, _motion[0].Y);
        Assert.Equal(-1, _stamina[0].Value);
        Step();
        Assert.Equal(9, _stamina[0].Value);
        for (var s = 6; s < 14; s++) Step();
        Assert.Equal(84f, _motion[0].Y);
        Assert.Equal(12f, _motion[0].X);
        Assert.Equal(9, _stamina[0].Value);
        Assert.False(_players.IsPlaying(0, Jump));
    }

    [Fact]
    public void BulletsAndTimelineHitsSumIntoOneHealthWritePerStep()
    {
        var bullets = _store.Pulses<int>("KindGame.Damage");
        _players.Start(0, Stomp);
        Step();
        bullets.To(1, 7);
        Step();
        Assert.Equal(993, _health[1].Value);
        bullets.To(0, 10);
        bullets.To(0, 10);
        Step();
        Assert.Equal(955, _health[0].Value);
        Step();
        Step();
        Assert.Equal(955, _health[0].Value);
        Step();
        Assert.Equal(945, _health[0].Value);
        Assert.Equal(1000, _health[2].Value);
        Assert.Equal(0, _store.DroppedPulses);
    }

    [Fact]
    public void KindPathAndLegacyConsumerAgreeBitForBitAcrossLoopWrap()
    {
        _stat[0] = _stat[1] = new WalkStat { Value = 1.1f };
        _players.Start(0, Run);
        _players.Start(1, RunLegacy);
        for (var s = 0; s < 100; s++)
        {
            Step();
            Assert.Equal(BitConverter.SingleToInt32Bits(_legacy[1].X), BitConverter.SingleToInt32Bits(_motion[0].X));
        }
        Assert.True(_motion[0].X > 200f);
        Assert.Equal(0f, _motion[1].X);
        Assert.Equal(0f, _legacy[0].X);
    }

    [Fact]
    public void ADisabledKindRowSkipsItsEntriesUntilEnabledAgain()
    {
        _players.Start(0, Run);
        Step();
        var kind = _store.IndexOf("KindGame.Locomotion");
        var row = _store.RowOf(0, kind);
        _store.Table(kind)->SetEnabled(row, false);
        for (var s = 0; s < 3; s++) Step();
        Assert.Equal(2f, _motion[0].X);
        _store.Table(kind)->SetEnabled(row, true);
        Step();
        Assert.Equal(4f, _motion[0].X);
    }

    [Fact]
    public void SystemsRunLegacyConsumersWithoutAKindStore()
    {
        using var plain = new PlaybackTable(N);
        plain.Start(1, RunLegacy);
        for (var s = 0; s < 5; s++)
        {
            _world.ApplyRequests(plain);
            new Movement { Motion = _motion, Legacy = _legacy, WalkStat = _stat, JumpPower = _power, Stamina = _stamina, Heat = _heat }.Run(_world, plain, true);
            _world.Advance(plain, true);
        }
        Assert.Equal(10f, _legacy[1].X);
        Assert.Equal(0f, _motion[1].X);
    }

    [Fact]
    public void ReloadKeepsTheTickAndReentersSpansWithoutRepeatingStartPulses()
    {
        _players.Start(0, Jump);
        Step();
        Assert.Equal(-1, _stamina[0].Value);
        Assert.Equal(0, _players.View.SlotOf(0, 0)->Tick);
        _world.Reload(File.ReadAllBytes(Path("game.tlib")));
        Step();
        Assert.Equal(0, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(-1, _stamina[0].Value);
        Assert.Equal(12f, _motion[0].Y);
        var lift = _store.IndexOf("KindGame.Lift");
        var table = _store.Table(lift);
        var row = _store.RowOf(0, lift);
        Assert.Equal(1, table->Length[row]);
        Assert.Equal(KindEntryFlags.Span | KindEntryFlags.Synthetic, table->At(row, 0)->Flags);
        Step();
        Assert.Equal(1, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(-1, _stamina[0].Value);
    }

    [Fact]
    public void ReloadPreservesAPausedPlaybackWithoutApplyingItsSpans()
    {
        _players.Start(0, Jump);
        Step();
        _players.Pause(0, Jump);
        Step();
        var before = _motion[0].Y;
        _world.Reload(File.ReadAllBytes(Path("game.tlib")));
        Step();
        Assert.Equal(PlaybackFlags.Paused, _players.View.SlotOf(0, 0)->Flags & PlaybackFlags.Paused);
        Assert.Equal(0, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(before, _motion[0].Y);
        Assert.Equal(-1, _stamina[0].Value);
        _players.Resume(0, Jump);
        Step();
        Assert.Equal(1, _players.View.SlotOf(0, 0)->Tick);
        Assert.Equal(before + 6f, _motion[0].Y);
    }

    [Fact]
    public void ReloadDropsStoppingSlotsWithoutReenteringThem()
    {
        _players.Start(0, Jump);
        Step();
        _players.Stop(0, Jump);
        _world.ApplyRequests(_players);
        _world.Reload(File.ReadAllBytes(Path("game.tlib")));
        Step();
        Assert.Equal(0, _players.View.SlotOf(0, 0)->Timeline);
        Assert.Equal(6f, _motion[0].Y);
        Assert.Equal(-1, _stamina[0].Value);
    }

    [Fact]
    public void ALibraryBakedAgainstOtherFeedersFailsWithARebakeHint()
    {
        using var stale = new KindRunTests("stale.tlib");
        var ex = Assert.Throws<InvalidOperationException>(() => stale.Step());
        Assert.Contains("rebake", ex.Message);
        Assert.Contains("KindGame.", ex.Message);
    }
}
