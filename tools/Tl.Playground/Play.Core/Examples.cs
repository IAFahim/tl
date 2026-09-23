using System.Globalization;
using System.Text;

namespace Play;

public sealed record Example(string Id, string Label, string Description, string Source, string TimelineJson);

public static class Examples
{
    const int CrowdRows = 1_000_000;
    const int CrowdGroupSize = 10_000;
    const int CrowdDuration = 1024;
    const int CrowdFrames = 60;

    static string CrowdTimelineJson(float scale, int split) => string.Create(
        CultureInfo.InvariantCulture,
        $$"""
        {
          "name": "gold", "duration": {{CrowdDuration}}, "loop": true,
          "tracks": [
            {
              "name": "arc", "namespace": "Live", "type": "MoveTrack", "data": { "Scale": {{scale.ToString("0.0#", CultureInfo.InvariantCulture)}} },
              "clips": [
                { "name": "rise", "namespace": "Live", "type": "MoveClip", "start": 0, "end": {{split}}, "data": { "Amount": 1.25 } },
                { "name": "fall", "namespace": "Live", "type": "MoveClip", "start": {{split}}, "end": {{CrowdDuration}}, "data": { "Amount": -0.5 } }
              ]
            }
          ]
        }
        """);

    static readonly string GroupsTimelineJson = BuildGroupsTimelineJson();

    static string BuildGroupsTimelineJson()
    {
        const int groups = CrowdRows / CrowdGroupSize;
        var entries = new string[groups];
        for (var group = 0; group < groups; group++)
        {
            var split = 300 + group * 7;
            var scale = (1f + group * 0.25f).ToString("0.##", CultureInfo.InvariantCulture);
            entries[group] = string.Create(
                CultureInfo.InvariantCulture,
                $$$"""{"name":"ability-{{{group}}}","duration":{{{CrowdDuration}}},"loop":true,"tracks":[{"name":"arc","namespace":"Live","type":"MoveTrack","data":{"Scale":{{{scale}}}},"clips":[{"name":"rise","namespace":"Live","type":"MoveClip","start":0,"end":{{{split}}},"data":{"Amount":1.25}},{"name":"fall","namespace":"Live","type":"MoveClip","start":{{{split}}},"end":{{{CrowdDuration}}},"data":{"Amount":-0.5}}]}]}""");
        }
        return "[" + string.Join(",", entries) + "]";
    }

    public static readonly Example Readme = new(
        "readme",
        "readme · jump arc",
        "four characters share one 30-tick jump: the arc peaks at y = 30 m on tick 15, and 30 backward ticks land at exactly 0.0 m",
        LiveAuthoring.DefaultSource,
        LiveAuthoring.DefaultTimelineJson);

    static readonly Example Boss = new(
        "boss",
        "nuget quick start · boss",
        "the NuGet quick-start boss: two ticks of a 4-tick timeline push 5 damage per tick through a ×2 DamageTrack — health 100 → 80, position 2",
        BossSource,
        BossTimelineJson);

    static readonly Example SharedClock = new(
        "shared-clock",
        "numbers · one clock for a million goblins",
        "1,000,000 goblins on one 1024-tick looping timeline (rise at tick 0, fall at tick 600), one shared ushort clock: Apply folds the whole crowd, Advance moves the single clock. README receipt on the reference host: 0.08 ms per frame hot, 0.17 cold — 0.08 ns per goblin",
        SharedClockSource,
        CrowdTimelineJson(scale: 2f, split: 600));

    static readonly Example SyncCrowd = new(
        "sync",
        "numbers · raid in sync, ids+clocks columns",
        "1,000,000 goblins, one 1024-tick looping timeline, every row carries its own id and clock column with all clocks parked on tick 5 — a raid jumping in sync. README receipt: 0.17 ms per frame hot, 0.37 cold — 0.17 ns per goblin",
        SyncCrowdSource,
        CrowdTimelineJson(scale: 2f, split: 600));

    static readonly Example Groups = new(
        "groups",
        "numbers · 100 timelines, 100 ability groups",
        "1,000,000 goblins in 100 groups of 10,000: group i runs its own 1024-tick timeline (rise/fall split at tick 300 + 7i, scale 1 + 0.25i), ids[i] = i / 10,000, clocks[i] = i % 1024. README receipt: 0.35 ms per frame hot, 0.50 cold — 0.35 ns per goblin",
        GroupsSource,
        GroupsTimelineJson);

    static readonly Example Mixed = new(
        "mixed",
        "mixed sample · three tracks, crossfade, rewind",
        "the samples/Mixed attack timeline, three tracks on two ticks: AnimationTrack 1 steps X+Y = 3 on tick 1, then its clips (2,1) and (6,3) overlap on tick 2 and crossfade at factor 0.5 to +6, AnimationTrack 3 shuffles +1 per tick, DamageTrack 2 hits 10 per tick — each system plays its own pair into the shared vitality column, two forward ticks land at vitality -9, position 2 and two rewind ticks restore exactly 0",
        MixedSource,
        MixedTimelineJson);

    static readonly Example Showcase = new(
        "showcase",
        "showcase sample · jump arc + Timeline.Bake",
        "the samples/Showcase program: four characters jump the 30-tick arc (velocity 2 m/tick, apex y = 30 m at tick 15), 30 rewind ticks land at exactly 0.0 m, then Timeline.Bake walks the AttachJumping marker and marks entities 42 and 43 jumping",
        ShowcaseSource,
        LiveAuthoring.DefaultTimelineJson);

    static readonly Example Raw = new(
        "raw",
        "raw binding (advanced)",
        "function-pointer consumer with no ITrack and no generator binding — the exact shape benchmarks/Numbers measures",
        LiveAuthoring.RawBindingSource,
        LiveAuthoring.RawBindingTimelineJson);

    public static readonly Example[] All = [Readme, Boss, SharedClock, SyncCrowd, Groups, Mixed, Showcase, Raw];

    public static Example Of(string id) => All.FirstOrDefault(example => example.Id == id) ?? Readme;

    const string BossSource = """
using Tl;

namespace Live;

public readonly record struct DamageClip(float Amount);

public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
{
    public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float damage)
        => damage += frame.Direction * frame.Clip.Amount * frame.Track.Multiplier;
}

public static class Play
{
    public static void Run(byte[] bossTlb)
    {
        ushort boss = TimelineAsset.Load(bossTlb);
        var positions = new ushort[] { 0 };
        var effects = new float[1];
        var health = 100f;
        Timeline<DamageTrack, DamageClip>.Apply(boss, positions, true, effects); Timeline.Advance(boss, positions, true);
        Timeline<DamageTrack, DamageClip>.Apply(boss, positions, true, effects); Timeline.Advance(boss, positions, true);
        health -= effects[0];
        Console.WriteLine($"flawless: health={health} position={positions[0]}");
    }
}
""";

    const string BossTimelineJson = """
{
  "name": "boss", "duration": 4, "loop": false,
  "tracks": [
    { "name": "damage", "namespace": "Live", "type": "DamageTrack", "data": { "Multiplier": 2.0 },
      "clips": [ { "name": "hit", "namespace": "Live", "type": "DamageClip", "start": 0, "end": 2, "data": { "Amount": 5 } } ] }
  ]
}
""";

    const string MoveDomain = """
using System.Diagnostics;
using Tl;

namespace Live;

public readonly record struct MoveClip(float Amount);

public readonly record struct MoveTrack(float Scale) : IBlend<MoveClip>
{
    public void Blend(in MoveClip first, in MoveClip second, float factor, out MoveClip result)
        => result = new MoveClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct ApplyMove : ITrack<MoveTrack, MoveClip>
{
    public static void OnActive(in Frame<MoveTrack, MoveClip> frame, ref float effect)
        => effect += frame.Direction * frame.Clip.Amount * frame.Track.Scale;
}
""";

    const string SharedClockSource = MoveDomain + """

public static class Play
{
    public static void Run(byte[] crowdTlb)
    {
        ushort crowd = TimelineAsset.Load(crowdTlb);
        var clock = (ushort)5;
        var effects = new float[1_000_000];
        var watch = Stopwatch.StartNew();
        for (var frame = 0; frame < 60; frame++)
        {
            Timeline<MoveTrack, MoveClip>.Apply(crowd, clock, true, effects);
            Timeline<MoveTrack, MoveClip>.Advance(crowd, ref clock, true);
        }
        watch.Stop();
        Console.WriteLine($"1,000,000 goblins on one shared clock: {watch.Elapsed.TotalMilliseconds / 60:0.000} ms/frame in this browser");
        Console.WriteLine("reference-host receipt: 0.08 ms/frame hot, 0.17 cold (0.08 ns per goblin)");
        Console.WriteLine($"after 60 ticks: clock={clock} effects[0]={effects[0]:0.000}");
    }
}
""";

    const string SyncCrowdSource = MoveDomain + """

public static class Play
{
    public static void Run(byte[] crowdTlb)
    {
        ushort crowd = TimelineAsset.Load(crowdTlb);
        var ids = new ushort[1_000_000];
        var clocks = new ushort[1_000_000];
        var effects = new float[1_000_000];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = crowd;
            clocks[i] = 5;
        }
        var watch = Stopwatch.StartNew();
        for (var frame = 0; frame < 60; frame++)
        {
            Timeline<MoveTrack, MoveClip>.Apply(ids, clocks, true, effects);
            Timeline.Advance(ids, clocks, true);
        }
        watch.Stop();
        Console.WriteLine($"1,000,000 goblins marching in sync on id+clock columns: {watch.Elapsed.TotalMilliseconds / 60:0.000} ms/frame in this browser");
        Console.WriteLine("reference-host receipt: 0.17 ms/frame hot, 0.37 cold (0.17 ns per goblin)");
        Console.WriteLine($"after 60 ticks: clock[0]={clocks[0]} effects[0]={effects[0]:0.000}");
    }
}
""";

    const string GroupsSource = MoveDomain + """

public static class Play
{
    public static void Run(byte[][] groups)
    {
        var timelines = new ushort[groups.Length];
        for (var group = 0; group < groups.Length; group++)
            timelines[group] = TimelineAsset.Load(groups[group]);
        var ids = new ushort[1_000_000];
        var clocks = new ushort[1_000_000];
        var effects = new float[1_000_000];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = timelines[i / 10_000 % timelines.Length];
            clocks[i] = (ushort)(i % 1024);
        }
        var watch = Stopwatch.StartNew();
        for (var frame = 0; frame < 60; frame++)
        {
            Timeline<MoveTrack, MoveClip>.Apply(ids, clocks, true, effects);
            Timeline.Advance(ids, clocks, true);
        }
        watch.Stop();
        Console.WriteLine($"1,000,000 goblins across {timelines.Length} ability groups: {watch.Elapsed.TotalMilliseconds / 60:0.000} ms/frame in this browser");
        Console.WriteLine("reference-host receipt: 0.35 ms/frame hot, 0.50 cold (0.35 ns per goblin)");
        Console.WriteLine($"after 60 ticks: group 7 runs timeline {timelines[7]}, effects[0]={effects[0]:0.000}");
    }
}
""";

    const string MixedSource = """
using Tl;

namespace Live;

public readonly record struct AnimationClip(float X, float Y);

public readonly record struct DamageClip(float Amount);

public readonly record struct AnimationTrack(int Code) : IBlend<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new AnimationClip(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);
}

public readonly record struct DamageTrack(int Code) : IBlend<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct AnimationJob : ITrack<AnimationTrack, AnimationClip>
{
    public static void OnActive(in Frame<AnimationTrack, AnimationClip> frame, ref float vitality)
        => vitality += frame.Direction * (frame.Clip.X + frame.Clip.Y);
}

public readonly struct DamageJob : ITrack<DamageTrack, DamageClip>
{
    public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float vitality)
        => vitality -= frame.Direction * frame.Clip.Amount;
}

public static class Play
{
    public static void Run(byte[] attackTlb)
    {
        ushort attack = TimelineAsset.Load(attackTlb);
        var positions = new ushort[] { 0 };
        var vitality = new[] { 0f };
        Timeline<AnimationTrack, AnimationClip>.Apply(attack, positions, true, vitality); Timeline<DamageTrack, DamageClip>.Apply(attack, positions, true, vitality); Timeline.Advance(attack, positions, true);
        Timeline<AnimationTrack, AnimationClip>.Apply(attack, positions, true, vitality); Timeline<DamageTrack, DamageClip>.Apply(attack, positions, true, vitality); Timeline.Advance(attack, positions, true);
        Console.WriteLine($"after two forward ticks: vitality={vitality[0]} position={positions[0]}");
        Timeline<AnimationTrack, AnimationClip>.Apply(attack, positions, false, vitality); Timeline<DamageTrack, DamageClip>.Apply(attack, positions, false, vitality); Timeline.Advance(attack, positions, false);
        Timeline<AnimationTrack, AnimationClip>.Apply(attack, positions, false, vitality); Timeline<DamageTrack, DamageClip>.Apply(attack, positions, false, vitality); Timeline.Advance(attack, positions, false);
        Console.WriteLine($"after two rewind ticks: vitality={vitality[0]} position={positions[0]}");
    }
}
""";

    const string MixedTimelineJson = """
{
  "name": "attack", "duration": 2, "loop": false,
  "tracks": [
    { "name": "walk-a", "namespace": "Live", "type": "AnimationTrack", "data": { "Code": 1 },
      "clips": [
        { "name": "step", "namespace": "Live", "type": "AnimationClip", "start": 0, "end": 2, "data": { "X": 2, "Y": 1 } },
        { "name": "stride", "namespace": "Live", "type": "AnimationClip", "start": 1, "end": 2, "data": { "X": 6, "Y": 3 } }
      ] },
    { "name": "hurt", "namespace": "Live", "type": "DamageTrack", "data": { "Code": 2 },
      "clips": [ { "name": "hit", "namespace": "Live", "type": "DamageClip", "start": 0, "end": 2, "data": { "Amount": 10 } } ] },
    { "name": "walk-b", "namespace": "Live", "type": "AnimationTrack", "data": { "Code": 3 },
      "clips": [ { "name": "shuffle", "namespace": "Live", "type": "AnimationClip", "start": 0, "end": 2, "data": { "X": 1, "Y": 0 } } ] }
  ]
}
""";

    const string ShowcaseSource = """
using System.Collections.Generic;
using Tl;

namespace Live;

public readonly record struct JumpClip(float Velocity);

public readonly record struct JumpTrack(float Scale) : IBlend<JumpClip>
{
    public void Blend(in JumpClip first, in JumpClip second, float factor, out JumpClip result)
        => result = new JumpClip(first.Velocity + (second.Velocity - first.Velocity) * factor);
}

public readonly struct MoveY : ITrack<JumpTrack, JumpClip>
{
    public static void OnActive(in Frame<JumpTrack, JumpClip> frame, ref float y)
        => y += frame.Direction * frame.Clip.Velocity * frame.Track.Scale;
}

public readonly struct AttachJumping : IBake<MoveY>
{
    public static void Bake(World world, int entity)
        => world.MarkJumping(entity);
}

public sealed class World
{
    public readonly List<int> Jumping = [];
    public void MarkJumping(int entity) => Jumping.Add(entity);
}

public static class Play
{
    public static void Run(byte[] jumpTlb)
    {
        ushort jumpTimeline = TimelineAsset.Load(jumpTlb);
        var ids = new ushort[] { jumpTimeline, jumpTimeline, jumpTimeline, jumpTimeline };
        var tick = new ushort[] { 0, 0, 0, 0 };
        var y = new[] { 0f, 0f, 0f, 0f };

        Console.WriteLine("four characters jump, one call per frame:");
        for (var frame = 1; frame <= 30; frame++)
        {
            Timeline<JumpTrack, JumpClip>.Apply(ids, tick, true, y); Timeline.Advance(ids, tick, true);
            if (frame % 3 == 0)
                Console.WriteLine($"  tick {frame,2}   y = {y[0],4:0.0} m   {new string('#', Math.Max(0, (int)Math.Round(y[0] / 3)))}");
        }

        Console.WriteLine();
        Console.WriteLine("rewind walks the arc back exactly:");
        for (var frame = 0; frame < 30; frame++)
            { Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, tick, false, y); Timeline.Advance(jumpTimeline, tick, false); }
        Console.WriteLine($"  after 30 back ticks: y = {y[0]:0.0} m, tick = {tick[0]}");

        Console.WriteLine();
        var world = new World();
        Timeline.Bake(jumpTimeline, world, 42);
        Timeline.Bake(jumpTimeline, world, 43);
        Console.WriteLine($"Timeline.Bake marked entities {string.Join(", ", world.Jumping)} as jumping; unmarked entities never reach the advance");
    }
}
""";

    public static string Validate()
    {
        var report = new StringBuilder();
        var boss = Execute(Boss);
        Require(boss.Console.Contains("flawless: health=80 position=2"), $"boss receipt line missing: {string.Join(" | ", boss.Console)}");
        report.Append("EXAMPLE PASS boss health=80 position=2; ");

        var shared = Execute(SharedClock);
        Require(shared.Console.Any(line => line.StartsWith("1,000,000 goblins on one shared clock:", StringComparison.Ordinal)), "shared-clock header missing");
        Require(shared.Console.Contains("after 60 ticks: clock=65 effects[0]=150.000"), $"shared-clock receipt line: {shared.Console[^1]}");

        var sync = Execute(SyncCrowd);
        Require(sync.Console.Any(line => line.StartsWith("1,000,000 goblins marching in sync", StringComparison.Ordinal)), "sync header missing");
        Require(sync.Console.Contains("after 60 ticks: clock[0]=65 effects[0]=150.000"), $"sync receipt line: {sync.Console[^1]}");

        var groups = Execute(Groups);
        Require(groups.Console.Length > 1, "groups run printed no console output");
        Require(groups.Console[0].StartsWith("1,000,000 goblins across 100 ability groups:", StringComparison.Ordinal), $"groups header shape: {groups.Console[0]}");
        Require(groups.Console[^1].StartsWith("after 60 ticks: group 7 runs timeline ", StringComparison.Ordinal), $"groups tail line: {groups.Console[^1]}");

        var mixed = Execute(Mixed);
        Require(mixed.Console.Contains("after two forward ticks: vitality=-9 position=2"), $"mixed forward receipt: {mixed.Console[^1]}");
        Require(mixed.Console.Contains("after two rewind ticks: vitality=0 position=0"), $"mixed rewind receipt: {mixed.Console[^1]}");

        var showcase = Execute(Showcase);
        Require(showcase.Console.Length == 14, $"showcase run printed {showcase.Console.Length} lines, expected 14");
        Require(showcase.Console[^2] == "  after 30 back ticks: y = 0.0 m, tick = 0", $"showcase rewind line: {showcase.Console[^2]}");
        Require(showcase.Console[^1] == "Timeline.Bake marked entities 42, 43 as jumping; unmarked entities never reach the advance", $"showcase bake line: {showcase.Console[^1]}");

        report.Append($"EXAMPLE PASS crowd rows={CrowdRows} groups={CrowdRows / CrowdGroupSize} frames={CrowdFrames} shared={shared.Checksum} sync={sync.Checksum} groups={groups.Checksum}; ");
        report.Append($"EXAMPLE PASS samples mixed={mixed.Checksum} showcase={showcase.Checksum}");
        return report.ToString();
    }

    static LiveRun Execute(Example example)
    {
        var compile = LiveAuthoring.Compile(example.Source);
        if (!compile.Ok)
            throw new InvalidOperationException($"example '{example.Id}' failed to compile: {compile.Error}");
        var (packages, error) = LiveAuthoring.BakeAll(compile.Assembly!, example.TimelineJson);
        if (packages.Length == 0)
            throw new InvalidOperationException($"example '{example.Id}' failed to bake: {error}");
        return LiveAuthoring.Run(compile.Assembly!, packages);
    }

    static void Require(bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException($"example validation failed: {detail}");
    }
}
