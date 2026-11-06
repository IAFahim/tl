using KindGame;
using Tl;
using Xunit;

namespace Tl.Systems.Tests;

public sealed unsafe class KindSampleHashTests
{
    const ushort Jump = 1, PotionHop = 2, Run = 3, SpeedBoost = 5, Stomp = 6;
    const int Rows = 4;
    const ulong BulletsPinned = 3081412629212866412UL;

    sealed class Host : IDisposable
    {
        readonly TimelineWorld _world;
        readonly PlaybackTable _players;
        readonly KindStore _store;
        readonly KindPulses<int> _damage;
        readonly Motion[] _motion;
        readonly LegacyMotion[] _legacy;
        readonly WalkStat[] _stat;
        readonly JumpPower[] _power;
        readonly Stamina[] _stamina;
        readonly Heat[] _heat;
        readonly Health[] _health;

        public Host(int count)
        {
            var dir = AppContext.BaseDirectory;
            for (var i = 0; i < 8 && !File.Exists(System.IO.Path.Combine(dir, "tl.slnx")); i++) dir = System.IO.Path.GetDirectoryName(dir)!;
            _world = new TimelineWorld(File.ReadAllBytes(System.IO.Path.Combine(dir, "tests", "Tl.Systems.Tests", "KindLibrary", "unity", "game.tlib")));
            _players = new PlaybackTable(count);
            _store = new KindStore(_world, _players);
            _damage = _store.Pulses<int>("Damage");
            _motion = new Motion[count];
            _legacy = new LegacyMotion[count];
            _stat = new WalkStat[count];
            _power = new JumpPower[count];
            _stamina = new Stamina[count];
            _heat = new Heat[count];
            _health = new Health[count];
            for (var i = 0; i < count; i++)
            {
                _stat[i] = new WalkStat { Value = 1f };
                _power[i] = new JumpPower { Value = 2f };
                _health[i] = new Health { Value = 1000 };
            }
        }

        public long Dropped => _store.DroppedPulses;

        public void Start(int row, ushort timeline) => _players.Start(row, timeline);

        public void Damage(int target, int amount) => _damage.To(target, amount);

        public void Step()
        {
            _world.Tick(_players, true);
            new Movement { Motion = _motion, Legacy = _legacy, WalkStat = _stat, JumpPower = _power, Stamina = _stamina, Heat = _heat }.Run(_world, _players, true);
            new Vitals { Health = _health }.Run(_world, _players, true);
        }

        public Motion Motion(int row) => _motion[row];

        public int Stamina(int row) => _stamina[row].Value;

        public int Health(int row) => _health[row].Value;

        public bool Playing(int row, ushort timeline) => _players.IsPlaying(row, timeline);

        public int LocomotionEntries(int row)
        {
            var kind = _store.IndexOf("Locomotion");
            return _store.Table(kind)->Length[_store.RowOf(row, kind)];
        }

        public void Dispose()
        {
            _players.Dispose();
            _world.Dispose();
        }
    }

    static ulong MixInt(ulong h, int v) => (h ^ (ulong)(uint)v) * 1099511628211UL;

    static ulong Hash(Host game, ulong h)
    {
        for (var e = 0; e < Rows; e++)
            h = MixInt(h, game.Health(e));
        return h;
    }

    [Fact]
    public void BulletsSampleReplaysToTheHashTheUnityHostExpects()
    {
        using var game = new Host(Rows);
        var h = 14695981039346656037UL;
        game.Start(0, Stomp);
        game.Step();
        h = Hash(game, h);
        game.Damage(1, 7);
        game.Step();
        h = Hash(game, h);
        Assert.Equal(993, game.Health(1));
        game.Damage(0, 10);
        game.Damage(0, 10);
        game.Step();
        h = Hash(game, h);
        Assert.Equal(955, game.Health(0));
        game.Step();
        h = Hash(game, h);
        game.Step();
        h = Hash(game, h);
        Assert.Equal(955, game.Health(0));
        game.Step();
        h = Hash(game, h);
        Assert.Equal(945, game.Health(0));
        Assert.Equal(1000, game.Health(2));
        Assert.Equal(0, game.Dropped);
        Assert.Equal(BulletsPinned, h);
    }

    [Fact]
    public void KindsSampleBoostSectionAgreesWithTheUnityHost()
    {
        using var game = new Host(1);
        game.Start(0, Run);
        game.Step();
        Assert.Equal(2f, game.Motion(0).X);
        game.Start(0, SpeedBoost);
        game.Step();
        Assert.Equal(7f, game.Motion(0).X);
        Assert.Equal(2, game.LocomotionEntries(0));
        for (var s = 0; s < 19; s++) game.Step();
        Assert.Equal(102f, game.Motion(0).X);
        game.Step();
        Assert.Equal(104f, game.Motion(0).X);
        Assert.Equal(1, game.LocomotionEntries(0));
    }

    [Fact]
    public void KindsSampleLiftSectionAgreesWithTheUnityHost()
    {
        using var game = new Host(1);
        game.Start(0, Jump);
        game.Step();
        game.Step();
        game.Start(0, PotionHop);
        for (var s = 0; s < 4; s++) game.Step();
        Assert.Equal(44f, game.Motion(0).Y);
        Assert.Equal(-1, game.Stamina(0));
        game.Step();
        Assert.Equal(9, game.Stamina(0));
        for (var s = 0; s < 8; s++) game.Step();
        Assert.Equal(84f, game.Motion(0).Y);
        Assert.Equal(12f, game.Motion(0).X);
        Assert.Equal(9, game.Stamina(0));
        Assert.False(game.Playing(0, Jump));
    }
}
