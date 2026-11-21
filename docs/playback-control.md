# Playback control

`Playback` remains eight unmanaged bytes. Control requests change a playback or an entity's channel; library replacement keeps the playback's current position.

When a live library is replaced, both hosts call `KindRules.Rebind`. The rule removes entries belonging to the old playback, clamps its tick to the replacement timeline, and preserves pause, direction and held state. An active playback keeps its tick for the replacement step and re-enters only the active spans, marked synthetic on that step. Clip-start, clip-end and marker pulses are not repeated. A playback that has not started yet still begins once. A stopping playback is removed.

A span feeder sees `Synthetic` on the step its playback began mid-clip or re-entered after a replacement or a seek, and not on the later ticks of that clip: spans come from the library's load-time table, not from per-entity entries that carried the flag.

`PlaybackFlags.ReenterPending` is transient host state used for that replacement step. An `IKindRows` storage adapter must implement `Purge(row, slot, timeline)` to remove all old entries for exactly that playback, without removing other playbacks or game pulses. Entry and playback pointers are borrowed only for the current row operation. Hosts finish all jobs using the old library before replacing it; the rule retains no pointers.

`PlaybackRules.Rebind` owns the position and flag rule, including timelines without kinds. Plain tables check the library generation when applying requests or advancing; an attached `KindStore` does the same check before its tick. Legacy consumers still execute their active body at the replacement position, but suppress start, end and marker callbacks on that synthetic step. The following step resumes normal playback.

The public API approval includes the new flag, `KindRules.Rebind` and `IKindRows.Purge`. This is an unreleased source change; no package version, tag or NuGet release is created.

## Control verbs

One request vocabulary in Core (`RequestOp`), written by plain code (`PlaybackTable`, `Requests`) and by Burst jobs (`TlRequests` in com.tl.unity). Requests apply at the start of the step, before advance, in submission order per entity. The law is recorded on IAFahim/tl#577.

- `Seek(timeline, tick)` and `SeekChannel(channel, tick)` land without crossing: the tick is clamped (a looping timeline wraps), the playback's entries are purged, held is cleared, pause and direction are kept, and active spans re-enter synthetically on the next step with no clip-start, clip-end or marker pulse. Seeking a playback that has not begun only moves where it begins.
- `Reverse(timeline, on)` and `ReverseChannel(channel, on)` set the playback's direction. A held playback leaves its hold when the new direction faces away from the end it is parked at: it begins again at that tick. A world-wide rewind (`forward = false`) leaves held playbacks parked.
- `PauseChannel(channel)` and `ResumeChannel(channel)` pause or resume the channel's live playback. Channel 0 means no channel; channel verbs on it do nothing.
- A paused playback does not move with the step in either direction; it moves only by `Resume` or `Seek`.
- `Reset(entity)` (`PlaybackTable.Reset`, `Requests.Reset`, `RequestOp.Reset`) says "this row is a new entity": it zeroes the entity's slots, purges its kind rows, drops its queued game pulses and adds nothing to the closing list, so no end pulse of the old occupant reaches the column the new one now uses. `StopAll` keeps its meaning (interrupt, with end pulses). Order in one step: `Reset(row)`, write the new column value, `Start(row, timeline)`, `Tick`. Fixes #594.

## Speed

`SetSpeed(timeline, sixteenths)` and `SetChannelSpeed(channel, sixteenths)` set how many ticks a playback crosses per step: `n / 16` ticks (16 and 0 mean 1x; 4 is 0.25x, 64 is 4x, at most 255). The step crosses `(Phase + n) >> 4` ticks and keeps `Phase = (Phase + n) & 15`, so 1.5x crosses 1, 2, 1, 2 ticks. Seek clears `Phase`. A playback that crosses no tick in a step contributes nothing that step.

Over any number of steps, a playback at any speed applies exactly what 1x applies over the same crossed ticks, in order: spans, pulses (`[Pulse]` on clip start, clip end and markers), `Each`-mode kinds, handover to the next timeline, loops and holds, forward and backward. Mixed speeds on one entity meet in substep 0, where game pulses (`KindPulses.To`) also land. Scenes can set speed (`"op": "SetSpeed"`, `"speed": 2`, a multiple of 1/16; `docs/json-format.md`).

What is exact: kinds and `[Pulse]`. Tests: `KindSpeedTests`, `SpeedRunTests`, `TwoAssemblyRateTests` (rates 0.5x, 1.5x, 2x, 2.5x, 3x, 4x across a handover, a loop, a foreign feeder and a world rewind equal a 1x run bit for bit), and the Inventory scenes `inventory-fast` and `inventory-mixed`, whose hashes `tl run`, Unity and the IL2CPP and CoreCLR players agree on.

Known limit in Unity: `ITlTrack` event consumers (`OnClipStart`, `OnClipEnd`, `ExecuteActive` with `Events<T>`) run once per step at the playback's final tick. At speeds above 1x a clip boundary crossed in an earlier tick of the same step raises no event. Use `[Pulse]` and kinds when a clip boundary must be seen at every speed.

## Derived kinds

`Sum<T>`, `Set<T>` and `While<T>` name a kind whose value type is the column type `T`, so the clip value, the column and the kind need no mirror struct. Use them in `[Feeds]` or `[Pulse]`: `[Pulse(typeof(Sum<Teleported>), On = ClipEvent.Start)] static Teleported Jump(in TlFrame<TeleportClip> frame) => ...`.

| Marker | Accepts | Combine | Apply | Idle |
|---|---|---|---|---|
| `Sum<T>` | spans and pulses | field-wise `+` | `column += total` field-wise | none |
| `Set<T>` | spans and pulses | last entry wins | `column = total` | none |
| `While<T>` | spans | `\|` for bool and integers, `max` for floats | `column = total` | `column = default` |

The generator writes an internal `[TimelineKind]` struct `Tl.Derived.<Template>_<Namespace>_<Type>` for each `(template, T)` it meets, and the library names that kind the same way, so a derived kind and a hand-written Combine/Apply/Idle kind are interchangeable and leave identical column bytes (`DerivedDifferentialTests`: 600 ticks, rewind and every request verb). Against an `Each`-mode kind the result differs only by float addition order when two pulses meet on one row in one tick. Errors: `TLGEN110` (a `Sum<T>` field that is not numeric, or a value type that is not an unmanaged struct), `TLGEN111` (a `While<T>` field that cannot be combined), `TLGEN112` (a `Set<T>` or `While<T>` column that has another writer). The kind and its feeders must be in one assembly.

## Unconnected fields

`TLGEN113` is a warning for a `[TlClip]` field that no track of the clip reads (`frame.Clip.<Field>` never appears; a clip passed whole to a helper counts as read). `TLGEN114` warns for a `[TlState]` field whose name appears nowhere else in the assembly. Both are ordinary warnings: silence them with `NoWarn` or `#pragma`.

## Finding a timeline by name

`world.Find("name")` binary-searches the sorted manifest of the loaded library and returns the timeline id, or 0 when no timeline has that (ordinal) name. Generated `Timelines` id constants stay available and are now optional: a project that calls `Find` compiles without them and without a library file on disk.

## One verb list on both hosts

Every verb takes the entity first, then the timeline (or channel), then the rest. On .NET the entity is a row of the `PlaybackTable`; in Unity it is the entity whose `DynamicBuffer<TimelineRequest>` you write (`TlRequests` in com.tl.unity). Both write the same `RequestOp` and run the same `PlaybackRules`.

| Verb | .NET | Unity |
|---|---|---|
| Start by id, at a tick | `players.Start(entity, timeline, at)` | `requests.Start(timeline, at)` |
| Start by name | `players.Start(entity, world.Find("healing_hand"))` | `requests.Start(library.Find("healing_hand"))` |
| Stop | `players.Stop(entity, timeline)` | `requests.Stop(timeline)` |
| StopChannel | `players.StopChannel(entity, channel)` | `requests.StopChannel(channel)` |
| StopAll | `players.StopAll(entity)` | `requests.StopAll()` |
| Pause, Resume | `players.Pause(entity, timeline)`, `Resume` | `requests.Pause(timeline)`, `Resume` |
| Seek | `players.Seek(entity, timeline, tick)` | `requests.Seek(timeline, tick)` |
| Reverse | `players.Reverse(entity, timeline, on)` | `requests.Reverse(timeline, on)` |
| PauseChannel, ResumeChannel, SeekChannel, ReverseChannel | `players.PauseChannel(entity, channel)`, ... | `requests.PauseChannel(channel)`, ... |
| SetSpeed | `players.SetSpeed(entity, timeline, sixteenths)` | `requests.SetSpeed(timeline, sixteenths)` |
| SetChannelSpeed | `players.SetChannelSpeed(entity, channel, sixteenths)` | `requests.SetChannelSpeed(channel, sixteenths)` |
| Reset | `players.Reset(entity)` | `requests.Reset()` |

- `timeline` is the library id: a `Timelines.<Name>` constant, `world.Find(name)` on .NET, `TlLibrary.Find(name)` in Unity (main thread, the timeline's own name, also for timelines streamed in later). Unity also takes a `TlTimeline` asset a designer dragged into a clip or component. Unity numbers timelines in the order they stream in, so `Timelines.g.cs` constants match only a library loaded with `TlLibrary.FromBytes`; `Find` or a `TlTimeline` works in both cases.
- A `Requests` system field (`ref Requests` in a legacy consumer) is the .NET-only form; it is bound to the consumer's own entity, and `For(entity, slot)` retargets it.

## Starting a timeline on another entity

Any code may send any verb to any entity in the same step: the healer's cast event starts "healing_hand" on its target, and the target's kinds apply it. A request applies at the next request phase, which is the start of the next tick, in the order sent per entity. A request written after a step's tick (for example by code that reads that step's events) applies on the following step; a request written before the tick applies in that tick.

```csharp
world.Tick(players);
new Tl.Generated.TlAuto_Game { C0_Health = health, E0_Cast = casts.Writer }.Run(world, players);
var drained = casts.Drain();
for (var caster = 0; caster < players.EntityCapacity; caster++)
    foreach (var cast in drained.Of(caster))
        players.Start(aim[caster], (ushort)cast.Value.Spell);
```

In Unity the same reader is a job in `TlSimulationGroup` with `OrderLast = true` that reads `DynamicBuffer<TimelineEvent<Cast>>` and writes the target's buffer through a `BufferLookup<TimelineRequest>`. Schedule it single-threaded (`Schedule`, not `ScheduleParallel`), or record into an `EntityCommandBuffer` that plays back before the next tick: two casters may aim at one target. Tests: `CrossEntityControlTests` (.NET), `KindHashTests.AWhistleOnOneRowStartsAStompOnTheRowItAimsAtWhichHitsTwoTicksLater` and the `CrossPinned` hash that the ECS host and the plain .NET host in com.tl.unity share.

## Events: one order on both hosts

Each step, every entity's events of one type form one run: ordered by playback slot, then by the order the methods raised them in that step.

- .NET: `events.Drain()` returns all runs sorted by entity; `drained.Of(entity)` (`EventRecords.Of`) returns one entity's run without allocating.
- Unity: the entity's `DynamicBuffer<TimelineEvent<T>>` holds that run, cleared and refilled every tick. Read it in `TlSimulationGroup` (`OrderLast = true`); a reader at frame rate misses ticks when a frame runs more than one.
- `Slot`, `Value` and the position in the run are the same on both hosts. `EventRecord.Entity` is the row on .NET and unused in Unity, and `Sequence` counts across the whole drain on .NET but restarts per entity in Unity, so compare runs by entity, slot, value and position.

## Game pulses

Each kind that accepts pulses gets a generated `<Kind>Kind` class on both hosts, in the kind's namespace (`TlUnityGenerated` for the global namespace):

| | .NET | Unity |
|---|---|---|
| Sender | `var damage = DamageKind.Pulses(kinds);` | `DamageKind.Pulses(writer)` from `TimelineKindTick.BeginPulses(n)` |
| Send | `damage.To(entity, 5)` before `world.Tick` | `.To(entity, 5)` before the tick |
| To an entity without the kind | counted in `KindStore.DroppedPulses` | counted in `TimelineKindTick.DroppedPulses` |

On .NET take the sender once per `KindStore` and keep it: `Pulses` allocates a small handle. The string form `kinds.Pulses<T>("Ns.Kind")` stays for tools. On .NET a kind nested in a type and a derived kind (`Sum<T>`, `Set<T>`) get no class (send with `kinds.Pulses<T>("Tl.Derived.Sum_Ns_T")`), and Unity writes none for a grouped kind (groups do not run there).

## A start the entity cannot hold

When an entity's kind set misses a kind the timeline (or its `next` chain) feeds, both hosts drop the start, count it (`PlaybackTable.DroppedStarts`, `TimelineKindTick.DroppedStarts`) and leave the entity as it was: a playback on the same channel keeps playing. Checked builds throw instead: on .NET (`Debug`, or `-p:TlChecked=true`) `Tick` and `ApplyRequests` throw an `ArgumentException` before any request of that batch applies; in Unity, with the scripting define `TL_CHECKED`, `TlCheckedStartsSystem` (in `TlSimulationGroup`, last) throws an `InvalidOperationException` at the end of the step that dropped it, which Unity logs as an exception.

## Host differences

Only what still differs, and why:

| | .NET | Unity | Why |
|---|---|---|---|
| Entity | row index | `Entity` and its buffers | ECS storage |
| Timeline handle | id | id, or a `TlTimeline` asset | designers drag assets |
| Timeline ids | name order of the baked library | stream order (`FromBytes` keeps the library's) | Unity assembles the library as SubScenes load |
| Request storage | one queue per `PlaybackTable` | one buffer per entity | ECS storage; writes to another entity's buffer need a single-threaded job or an ECB |
| Pulse sender | taken once per `KindStore` | taken per job | the .NET sender holds a managed handle |
| Generated sender for a derived kind (`Sum<T>`, `Set<T>`) | none; `kinds.Pulses<T>("Tl.Derived.<Name>")` | `Tl.Derived.<Name>Kind.Pulses(writer)` | src budget |
| A pulse to a row outside the table | throws | n/a (an `Entity` without the kind is counted) | |
| Event record fields | `Entity` = row, `Sequence` across the drain | `Entity` unused, `Sequence` per entity | per-entity buffers |
| Event methods above 1x speed | see Speed | once per step, at the last tick | Unity limit; use `[Pulse]` |
| Legacy consumers (`Fold`, `ExecuteActive` with columns, `ref Requests`) | run | not run (TLUNITY9105) | out of scope for #656 |
| Kind groups | run | skipped (TLUNITY9104) | |
| Starts of timelines not baked onto the object | n/a | dropped with a warning (Also Plays) | per-object kind archetypes, #655 |
| Checked builds | `Debug` or `TlChecked=true` | scripting define `TL_CHECKED` | |

## Migration: `ExecuteBatch` removed (#659)

Unreleased source change; the package version stays `4.0.0-preview.1`. The public API approval lists every change.

- The `ExecuteBatch(in Frame, ReadOnlySpan<BatchItem<R>>, Span<T>...)` consumer form and `Tl.BatchItem<R>` are removed. Unity never ran it (TLUNITY9105). A consumer that still declares `ExecuteBatch` fails with TLGEN95. Move the loop body into `ExecuteActive(in Frame, in R fold, ref T column)`, which runs once per playback per step with the same fold value and the entity's own column element, or feed a kind. A consumer whose only methods are `Fold` and `ExecuteActive` is read as a timeline job (TLGEN65 rules), not a system consumer; keep its `OnClipStart`/`OnClipEnd` (an empty one is enough) or a `Requests`/`Events` parameter to stay in the system.
- The source budget pays for #655's precombined spans with this deletion (src stays under 520,000 B).

## Migration: one control, event and pulse API (#656)

Unreleased source change; the package version stays `4.0.0-preview.1`. The public API approval lists every change.

- A start an entity cannot hold no longer throws in Release builds: it is dropped and counted in the new `PlaybackTable.DroppedStarts`. Checked builds still throw. Code that caught the `ArgumentException` reads the counter instead.
- `PlaybackRules.Start` checks the entity's kinds before it replaces a channel's playback, so a dropped start leaves that playback running (both hosts).
- New `EventRecords.Of(drained, entity)`: one entity's events from `Events<T>.Drain()`.
- `RequestQueue.Sort(items, count)`, which allocated its own scratch, is removed; pass the scratch: `RequestQueue.Sort(items, count, scratch)` (the source budget pays for this change).
- The generator writes `<Kind>Kind.Pulses(KindStore)` for every kind that accepts pulses (`TlSystems<id>.Pulses.g.cs`). A type of yours named `<Kind>Kind` in the kind's namespace now collides (CS0101); rename it.
- com.tl.unity (IAFahim/tl.unity): `TlRequests` takes a `ushort` id for every timeline verb and gains `Reset()`; `TlLibrary.Find(name)`; `TlCheckedStartsSystem` (created only with `TL_CHECKED`); `TimelineJobs.ScheduleAdvance` takes the step direction and `TimelineAdvanceJob` has `Backward`, so kind-free playbacks follow a world rewind.

## Migration: spans from the load-time table (#652)

This is an unreleased source change after `v4.0.0-preview.1`; the package version stays `4.0.0-preview.1`, and no tag or NuGet release is created. The public API approval lists every change.

- Regenerate systems with `Tl.Gen.CSharp`. Generated kind loops read `KindStore.Score` and call `KindRules.Due(entry, substep, run, count)` and `KindRules.Depth(copies, samples)`.
- `IKindRows` adapters: remove `RemoveSpan`, `Sample` and `Unsample`, and implement `Sampled(row, samples)`. It records how many rated copies the step wrote after the row's slots (0 when none); a kind loop walks those copies by substep.
- Removed with the span entries: `KindTable.RemoveSpan`, `Sample`, `Unsample`, `KindEntryFlags.Sample`, `KindRules.Reenter`, `IsSampled`, `SampleDepth` and `KindRules.Marker`. To count live spans, take `cell = score.CellOf(timeline, tick, kind)`; when `cell >= 0`, read `score.Start[cell + 1] - score.Start[cell]` (a cell of -1 means the timeline does not feed that kind).
- `KindTable.RemovePulses` is gone: every entry a kind row holds is a pulse (timeline pulses, game pulses and a foreign feeder's span values), and an owner clears every row it visited once it has applied it (a grouped kind drops only its own members' entries). A host that appends its own entries with `KindTable.Append` must expect them to be consumed by the owner's next run.
- A foreign feeder must run before its owner in every step. The first step rejects the opposite order (`KindStore.Join`); a host that reorders its systems later gets values one step late.
- A host that does not use `KindStore` builds its own table with `KindScore.Build(in tables, ref steps)`, where `steps` implements `IKindSteps.StepOf(timeline, window, tick)`, and calls `Free` when the library is replaced. `tables` must come from a validated kind section (`Build` throws when a feeder's kind is outside its timeline's kind mask), and the score borrows that section's timeline rows, so the library bytes must outlive it.
- Behaviour: last-wins kinds combine rated copies first (by source slot, then crossing), then the other slots in slot order, each in authored pair order, and a span feeder sees `Synthetic` only on the step its playback began mid-clip or re-entered (see above).

## Precombined spans (#655)

A kind opts in with `Associative = true` on `[TimelineKind]` or `[TlKind]`. That declares its `Combine` associative; tl never infers it from code. The generator then precombines the kind in its system when all of these hold:

- every `[Feeds]` method of the kind in that system takes only the frame (`Frame<,>` or `TlFrame<>`) and its own fold results;
- none of them, nor the `Fold` whose results they take, reads `Synthetic` or `Interrupted`. Reading `frame.Flags`, calling `frame.Has` with anything but a constant without those bits, or passing the frame on counts as a read;
- the kind is not `Mode = KindMode.Each`, and the system does not feed it from another assembly.

A precombined kind reads one value per playback slot and step: `Combine` over the cell's spans for that (timeline, tick, direction), in pair order, plus a presence bit. `Apply`, pulses, game pulses, rated copies and a foreign feeder's values work as before. Per-entity inputs belong in `Apply`, which runs once per entity: `Lift.Apply(in float lift, in JumpPower power, ref Motion motion)`. All precombined kinds of a system share one table: a row per (timeline, tick) holds a 32-bit head with three bits per kind (listed, forward, backward) and each kind's two values. When at least two of them are contiguous in the system's kind order, have no `Idle`, take no context in `Apply` and every entity reaches them, the generated `Fused` method walks each entity's playbacks once for all of them, then runs each kind's pulses and `Apply` in kind order; otherwise each kind walks the shared table alone.

- The values are built on the first run after a library loads, by generated code through `KindStore.SpanValues(kind, part, ref fill)`, and are freed with the library. `KindStore.SpanValueBytes(kind)` reports the kind's share of the table bytes (the owner, the kind at `Shift` 0, also carries the head, padding and row index); it is 0 while the kind is not precombined. A host without `KindStore` owns the memory: `KindBuild.Of(in score, timelines, views, row, byFeeder, ref fill)` lays out every kind the generated `IKindFill` adds into `views` (indexed by kind) and fills the per-timeline row bases into `row` (one `int` per timeline); the host allocates `Rows * Stride` zeroed bytes and calls `Fill(data, ref fill)`. `byFeeder` picks whether routes are indexed by the span's feeder (com.tl.unity's `KindIndex.Routes`) or by its ordinal (the .NET join). com.tl.unity does this in `KindIndex.SpanValues` with `UnsafeUtility` memory, so it runs under Burst.
- With one playback per entity the result is bit for bit the per-span walk, floats included. With several playbacks, `Combine` regroups by slot, so float sums can differ in the last bits; integer and pick kinds stay exact (`PrecombineTests`). Members of one `Group` route independently: one can be precombined while another keeps the per-span walk, and they still share one entry table.
- Memory and concurrency of the table:
  - **Lifetime:** built once per library, owned by the `KindStore` (.NET) or the `KindIndex` (Unity), and freed with it (`Release`, reload, `KindIndex.Dispose`); nothing else keeps a pointer past the step that reads it.
  - **Aliasing:** read-only after it is published. No step writes it. The kinds of a system share `Row` and `Data`, each at its own `Front`, `Back` and `Shift`; only the owner frees them.
  - **Alignment:** blocks are 64-byte aligned on .NET (16-byte on Unity's `UnsafeUtility`). Each kind aligns its offset to `min(64, alignof(T))` (`alignof(T)` measured as `sizeof({ byte; T }) - sizeof(T)`), with `Stride` rounded up to `max(4, max align)`. `alignof(T)` divides both the offset and `sizeof(T)`, so `Front`, `Back` and `Stride` keep every value naturally aligned up to the block alignment (`Int128`, `UInt128` and `Vector128<T>` are 16-aligned on 64-bit .NET, `Vector256<T>` 32 on x64; a `bool` kind has `Front` 4, `Back` 5, `Stride` 8, a three-`float` kind `Front` 4, `Back` 16, `Stride` 28, an `Int128` kind `Front` 16, `Back` 32, `Stride` 48; `KindValuesLayoutTests`).
  - **Publication:** on .NET every worker's range calls `KindStore.SpanValues` before its row loop; the first caller builds the table under the join lock and publishes it with a volatile write of `Data` after every other field, and the others either see `Data` through a volatile read or wait on the lock and then see it. In Unity the generated `Schedule` builds it on the main thread before it schedules the job that reads it.
- `TLGEN115` is an info diagnostic for an opted-in kind that keeps the per-span walk, naming the first reason. It is not reported for a kind the system feeds from another assembly, which always keeps the per-span walk. Derived `Sum<T>`, `Set<T>` and `While<T>` kinds keep the per-span walk for now.
