using System.Globalization;
using System.Text;

namespace Play;

public sealed record Example(string Id, string Label, string Description, string Source, string TimelineJson);

public static class Examples
{
    public const int CrowdRows = 1_000_000;
    public const int CrowdGroupSize = 10_000;
    public const int CrowdDuration = 1024;
    public const int CrowdFrames = 60;

    public static string CrowdTimelineJson(float scale, int split) => string.Create(
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

    public static readonly string GroupsTimelineJson = BuildGroupsTimelineJson();

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

    public static readonly Example Boss = new(
        "boss",
        "nuget quick start · boss",
        "the NuGet quick-start boss: two ticks of a 4-tick timeline push 5 damage per tick through a ×2 DamageTrack — health 100 → 80, position 2",
        BossSource,
        BossTimelineJson);

    public static readonly Example SharedClock = new(
        "shared-clock",
        "numbers · one clock for a million goblins",
        "1,000,000 goblins on one 1024-tick looping timeline (rise at tick 0, fall at tick 600), one shared ushort clock: Apply folds the whole crowd, Advance moves the single clock. README receipt on the reference host: 0.08 ms per frame hot, 0.17 cold — 0.08 ns per goblin",
        SharedClockSource,
        CrowdTimelineJson(scale: 2f, split: 600));

    public static readonly Example SyncCrowd = new(
        "sync",
        "numbers · raid in sync, ids+clocks columns",
        "1,000,000 goblins, one 1024-tick looping timeline, every row carries its own id and clock column with all clocks parked on tick 5 — a raid jumping in sync. README receipt: 0.17 ms per frame hot, 0.37 cold — 0.17 ns per goblin",
        SyncCrowdSource,
        CrowdTimelineJson(scale: 2f, split: 600));

    public static readonly Example Groups = new(
        "groups",
        "numbers · 100 timelines, 100 ability groups",
        "1,000,000 goblins in 100 groups of 10,000: group i runs its own 1024-tick timeline (rise/fall split at tick 300 + 7i, scale 1 + 0.25i), ids[i] = i / 10,000, clocks[i] = i % 1024. README receipt: 0.35 ms per frame hot, 0.50 cold — 0.35 ns per goblin",
        GroupsSource,
        GroupsTimelineJson);

    public static readonly Example Raw = new(
        "raw",
        "raw binding (advanced)",
        "function-pointer consumer with no ITrack and no generator binding — the exact shape benchmarks/Numbers measures",
        LiveAuthoring.RawBindingSource,
        LiveAuthoring.RawBindingTimelineJson);

    public static readonly Example[] All = [Readme, Boss, SharedClock, SyncCrowd, Groups, Raw];

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
        report.Append($"EXAMPLE PASS crowd rows={CrowdRows} groups={CrowdRows / CrowdGroupSize} frames={CrowdFrames} shared={shared.Checksum} sync={sync.Checksum} groups={groups.Checksum}");
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
