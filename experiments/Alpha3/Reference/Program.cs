using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnifiedTimelineProof;

var timelines = new TimelineState[10_000];
var resistance = new Resistance[timelines.Length];
var health = new Health[timelines.Length];
var poses = new Pose[timelines.Length];
Reset();
var query = new CombatQuery(timelines, resistance, health, poses);
query.Tick(200_000, 4);
for (var entity = 0; entity < timelines.Length; entity++)
{
    Check(health[entity].Value == ((entity & 1) == 0 ? 90 : 80), "damage");
    Check(poses[entity].X == ((entity & 1) == 0 ? 380 : 360), "stage order");
    Check(timelines[entity].Playback.Position == 4, "completion");
}
query.Tick(200_004, -4);
for (var entity = 0; entity < timelines.Length; entity++)
    Check(health[entity].Value == 100 && poses[entity].X == 0 && timelines[entity].Playback.Position == 0, "mirror");

query.Tick(200_000, int.MaxValue);
Check(health[0].Value == 90 && poses[0].X == 380, "forward overshoot");
query.Tick(200_000, int.MinValue);
Check(health[0].Value == 100 && poses[0].X == 0, "reverse overshoot");
default(CombatQuery).Tick(0, int.MinValue);
Reset();
var first = new CombatQuery(timelines.AsSpan(0, 1), resistance.AsSpan(0, 1), health.AsSpan(0, 1), poses.AsSpan(0, 1));
first.Tick(200_000, 4);
query.Tick(200_004, 1);
Check(timelines[0].Playback.Position == 4 && timelines[1].Playback.Position == 1 && poses[1].X == 100, "completed row does not block others");
timelines[0] = default;
first.Tick(200_005, 100);
Check(timelines[0].Playback.Position == 0 && health[0].Value == 90, "empty asset");
timelines[0] = new TimelineState(LoopingAttack.Asset);
health[0].Value = 100;
poses[0].X = 0;
first.Tick(200_000, 12);
Check(timelines[0].Playback.Position == 0 && health[0].Value == 70, "looping");
first.Tick(200_012, -12);
Check(timelines[0].Playback.Position == 0 && health[0].Value == 100 && poses[0].X == 0, "loop mirror");
var selected = Timeline.Select(in timelines[0], 200_000, 1);
Check(selected.Playback.Position == 0 && selected.Selection.GameTick == 200_000, "selection preserves committed playback");
var again = Timeline.Select(in selected, 200_000, 1);
Check(again.Selection.TimelineTick == selected.Selection.TimelineTick, "selection idempotent");
var completed = Timeline.Complete(in selected);
Check(Timeline.Complete(in completed).Playback.Position == completed.Playback.Position, "completion idempotent");

var rejectedLength = false;
try { _ = new CombatQuery(timelines, resistance.AsSpan(1), health, poses); }
catch (ArgumentException) { rejectedLength = true; }
Check(rejectedLength, "bad storage diagnosed at construction");
var rejectedAlias = false;
try { _ = new CombatQuery(timelines, resistance, health, MemoryMarshal.Cast<Health, Pose>(health)); }
catch (ArgumentException) { rejectedAlias = true; }
Check(rejectedAlias, "alias diagnosed at construction");
Reset();
for (var warmup = 0; warmup < 10; warmup++)
{
    query.Tick(200_000, 4);
    query.Tick(200_004, -4);
}
var before = GC.GetAllocatedBytesForCurrentThread();
for (var round = 0; round < 32; round++)
{
    query.Tick(200_000, 4);
    query.Tick(200_004, -4);
}
var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
Check(allocated == 0, "no warm allocations");
Console.WriteLine($"10,000 entities; default playback, mixed completion, clamping, empty assets, loops, signed order, and setup validation passed; {allocated} B over 2,560,000 entity-ticks.");
Console.WriteLine($"Playback {Unsafe.SizeOf<Playback>()} B; Selection {Unsafe.SizeOf<Selection>()} B; TimelineState {Unsafe.SizeOf<TimelineState>()} B.");

void Reset()
{
    for (var entity = 0; entity < timelines.Length; entity++)
    {
        timelines[entity] = new TimelineState((entity & 1) == 0 ? Attack.Asset : HeavyAttack.Asset);
        resistance[entity].Scale = 0.5f;
        health[entity].Value = 100;
        poses[entity].X = 0;
    }
}

static void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException(name);
}
