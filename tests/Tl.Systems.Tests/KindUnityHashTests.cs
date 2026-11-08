using KindGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class KindUnityHashTests : IDisposable
{
    const ushort Jump = 1, PotionHop = 2, Run = 3, SpeedBoost = 5, Stomp = 6;
    const int N = 4;
    const ulong Pinned = 13943614770702343869UL;

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

    public KindUnityHashTests()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(System.IO.Path.Combine(dir, "tl.slnx")); i++) dir = System.IO.Path.GetDirectoryName(dir)!;
        _world = new TimelineWorld(File.ReadAllBytes(System.IO.Path.Combine(dir, "tests", "Tl.Systems.Tests", "KindLibrary", "unity", "game.tlib")));
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

    static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    static ulong Mix(ulong h, float v) { var s = new global::Tl.TlStateHash { Value = h }; s.FloatLane(v); return s.Value; }

    static ulong MixInt(ulong h, int v) { var s = new global::Tl.TlStateHash { Value = h }; s.Int(v); return s.Value; }

    ulong Play(int steps)
    {
        var ids = new ushort[] { Jump, PotionHop, Run, SpeedBoost, Stomp };
        var damage = _store.Pulses<int>("KindGame.Damage");
        var stats = _store.Pulses<int>("KindGame.StatChange");
        var rng = 0x2545F491u;
        var h = global::Tl.TlStateHash.Seed;
        for (var step = 0; step < steps; step++)
        {
            var forward = !(step >= 200 && step < 240);
            for (var e = 0; e < N; e++)
            {
                var r = Next(ref rng);
                switch (r & 15)
                {
                    case 0:
                    case 1:
                        _players.Start(e, ids[(r >> 8) % 5]);
                        break;
                    case 2:
                        _players.Stop(e, ids[(r >> 8) % 5]);
                        break;
                    case 3:
                        if (((r >> 8) & 7) == 0) _players.StopChannel(e, (byte)(1 + ((r >> 12) & 1)));
                        break;
                    case 4:
                        if (((r >> 8) & 15) == 0) _players.StopAll(e);
                        break;
                    case 5:
                    case 6:
                        damage.To((int)((r >> 8) & 3), 3 + (int)((r >> 10) & 7));
                        break;
                    case 7:
                        stats.To(e, (int)((r >> 8) & 3) - 1);
                        break;
                }
            }
            _world.Tick(_players, forward);
            new Movement { Motion = _motion, Legacy = _legacy, WalkStat = _stat, JumpPower = _power, Stamina = _stamina, Heat = _heat }.Run(_world, _players, forward);
            new Vitals { Health = _health }.Run(_world, _players, forward);
            for (var e = 0; e < N; e++)
            {
                h = Mix(h, _motion[e].X);
                h = Mix(h, _motion[e].Y);
                h = MixInt(h, _stamina[e].Value);
                h = MixInt(h, _health[e].Value);
            }
        }
        return h;
    }

    [Fact]
    public void SixHundredStepKindScriptKeepsItsPinnedHash()
    {
        var hash = Play(600);
        Assert.Equal(Pinned, hash);
        Assert.Contains(_motion, static m => m.X != 0f);
        Assert.Contains(_motion, static m => m.Y != 0f);
        Assert.Contains(_health, static h => h.Value < 1000);
        Assert.Contains(_stamina, static s => s.Value != 0);
        Assert.Equal(0, _store.DroppedPulses);
    }
}
