using Game;
using Runner;
using Tl;

string LibraryPath() => Path.Combine(AppContext.BaseDirectory, "game.tlib");

const int entities = 3;
const int hero = 0;
const int slider = 1;
const int sipper = 2;

using var world = new TimelineWorld(File.ReadAllBytes(LibraryPath()));
using var players = new PlaybackTable(entityCapacity: entities, slotsPerEntity: 4);

var motion = new Motion[entities];
var power = new JumpPower[entities];
var stats = new Stats[entities];
var input = new InputState[entities];
var position = new Position[entities];
var heads = new HeadIndex[entities];
using var cues = new Events<Cue>(16);
using var hits = new Events<StompHit>(16);

for (var i = 0; i < entities; i++)
{
    power[i] = new JumpPower(1f);
    stats[i] = new Stats { Health = 100, JumpCounter = 3 };
    heads[i] = new HeadIndex(i == slider ? 1 : 0);
}

var slots = new ushort[3][] { new ushort[3], new ushort[3], new ushort[3] };
slots[hero][(int)Skill.Jump] = Timelines.Jump;
slots[hero][(int)Skill.Slide] = Timelines.Slide;
slots[slider][(int)Skill.Slide] = Timelines.Slide;

players.Start(hero, Timelines.Run);
players.Start(slider, Timelines.Slide);
players.Start(sipper, Timelines.Run);
players.Start(sipper, Timelines.PotionHop);

void Step(bool forward)
{
    world.ApplyRequests(players);
    new Movement { Motion = motion, JumpPower = power, Stats = stats, Input = input, Requests = world.Requests(players), Cues = cues.Writer }.Run(world, players, forward);
    new Combat { Position = position, Heads = heads, Hits = hits.Writer }.Run(world, players, forward);
    if (forward)
        world.Advance(players);
    else
        world.Advance(players, false);
    foreach (var rec in cues.Drain())
        Console.WriteLine($"  cue {rec.Value.Voice} on entity {rec.Entity.Index}");
    foreach (var rec in hits.Drain())
        Console.WriteLine($"  stomp r={rec.Value.Radius} on entity {rec.Entity.Index} at ({rec.Value.At.X:0.0},{rec.Value.At.Y:0.0})");
}

if (args.Contains("--verify"))
    return Verify();

Console.WriteLine("three entities share one library: hero runs, slider slides, sipper sips a potion mid-run.");
for (var frame = 0; frame < 60; frame++)
{
    if (frame == 10)
        players.Start(hero, slots[hero][(int)Skill.Jump]);
    input[slider] = frame >= 8 && frame < 12 ? new InputState(Button.Attack) : new InputState(Button.None);
    for (var i = 0; i < entities; i++)
        position[i] = new Position(motion[i].X, motion[i].Y);
    Step(true);
    if (frame % 10 == 9)
        Console.WriteLine($"  tick {frame + 1,2}: hero y = {motion[hero].Y:0.0} m, slider x = {motion[slider].X:0.0} m, jumps left = {stats[hero].JumpCounter}");
}
Console.WriteLine($"done: hero y = {motion[hero].Y:0.0} m (run loop carries no lift), slider x = {motion[slider].X:0.0} m.");
return 0;

int Verify()
{
    players.Start(hero, Timelines.Run);
    for (var frame = 0; frame < 10; frame++)
        Step(true);
    players.Start(hero, slots[hero][(int)Skill.Jump]);
    var takeoff = motion[hero].Y;
    for (var frame = 0; frame < 30; frame++)
        Step(true);
    if (motion[hero].Y != takeoff)
    {
        Console.WriteLine($"FAIL: Jump_lands_exactly_where_it_took_off: took off at {takeoff}, landed at {motion[hero].Y}");
        return 1;
    }
    Console.WriteLine($"PASS: Jump_lands_exactly_where_it_took_off: took off and landed at {takeoff}");
    for (var frame = 0; frame < 30; frame++)
        Step(false);
    if (motion[hero].Y != takeoff)
    {
        Console.WriteLine($"FAIL: rewind did not restore takeoff: {motion[hero].Y} != {takeoff}");
        return 1;
    }
    Console.WriteLine("PASS: 30 backward steps restore the takeoff height exactly");
    var first = Hash(600);
    var second = Hash(600);
    if (first != second)
    {
        Console.WriteLine($"FAIL: state hash differs across identical runs: {first} != {second}");
        return 1;
    }
    Console.WriteLine($"PASS: deterministic state hash {first} across two 600-step runs");
    return 0;

    ulong Hash(int steps)
    {
        using var inner = new TimelineWorld(File.ReadAllBytes(LibraryPath()));
        using var table = new PlaybackTable(entityCapacity: entities, slotsPerEntity: 4);
        var m = new Motion[entities];
        var s = new Stats[entities];
        using var local = new Events<Cue>(16);
        table.Start(hero, Timelines.Run);
        table.Start(slider, Timelines.Slide);
        var h = 14695981039346656037UL;
        for (var frame = 0; frame < steps; frame++)
        {
            inner.ApplyRequests(table);
            new Movement { Motion = m, JumpPower = power, Stats = s, Input = input, Requests = inner.Requests(table), Cues = local.Writer }.Run(inner, table);
            inner.Advance(table);
            h = Mix(h, m[hero].X); h = Mix(h, m[hero].Y); h = MixInt(h, s[hero].JumpCounter);
            h = Mix(h, m[slider].X); h = Mix(h, m[slider].Y);
        }
        return h;
    }
    ulong Mix(ulong h, float v) => (h ^ (ulong)BitConverter.SingleToInt32Bits(v)) * 1099511628211UL;
    ulong MixInt(ulong h, int v) => Mix(h, BitConverter.Int32BitsToSingle(v));
}
