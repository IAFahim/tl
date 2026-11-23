# tl

**tl** compiles designer-authored timelines into deterministic execution — data first, an authoring step that bakes it, a game system that plays it. tl contains no game behaviour:

| Who | Does | Writes code |
|---|---|---|
| Designer | Builds timelines: places clips on tracks, sets values, loop, next, channel | Never |
| Toolbox programmer | Writes each clip type once: payload struct + consumer code | Once per clip type, never per timeline |
| Gameplay programmer | Decides when an entity starts a timeline; declares systems listing which entity data clip code may touch | A little |
| Tl | Bakes timelines, stores each once, plays them on many entities, calls clip code at the right ticks | n/a |

A timeline never lives on an entity. All timelines live once in the shared library the bootstrap loads. What an entity stores is a short list of **playbacks**: `{timeline, tick}` pairs. Jump id 2 vs slide id 5 just means "manifest index 2 vs 5".

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

## Install for Unity

In Unity the package is enough: newest Unity 7, a **Universal 3D** (URP) project, Package Manager > **+** > **Install package from git URL** >
`https://github.com/IAFahim/tl.unity.git?path=/com.tl.unity#main`. No .NET, no tool, no JSON: designers use Unity's Timeline window. Guide:
[USING-ECS](https://github.com/IAFahim/tl.unity/blob/main/docs/USING-ECS.md).

One pasted line does all of it from a terminal: it creates a URP project on the newest Unity 7 editor (installs one with the Unity CLI if
none), adds the package, imports every demo (Force, Events, Crowd) and the Demo hub, builds their scenes and a **Tl Demos** hub scene, runs
every demo's PlayMode test and prints one line per demo with actual and expected numbers (Force x 4.0333 and back to 0, Events 15 strikes
and dummy health 10, Crowd ns/entity at 10k and 100k with 0 B), then leaves Tl Demos open. `TL_DEMOS=force,events` picks demos,
`--no-tests` (or `TL_NO_TESTS=1`) skips the test run. Needs git with access to `tl.unity` (`gh auth login`, `gh auth setup-git`; the repo
is private for now). Safe to rerun on the same project.

```powershell
$env:TL_PROJECT='C:\path\MyGame'; irm https://raw.githubusercontent.com/IAFahim/tl/main/install/unity.ps1 | iex
```

```sh
curl -sSL https://raw.githubusercontent.com/IAFahim/tl/main/install/unity.sh | bash -s -- <project-path>
```

Verified on Windows 11 (PowerShell script, fresh project). The bash script follows the same steps and has not been run on this version.

## Install

```sh
dotnet add package Tl.CSharp --version 4.0.0-preview.1
dotnet tool install --global Tl.Bake --prerelease     # the tl bake command
```

## Get started

The [Hello Tl example](demos/hello) takes one authored timeline from JSON to playback. A track's `[Pulse]` method computes a value when its clip starts. A kind's `Apply` uses that value to update the entity:

```cs
[TlClip("Hello/Teleport")]
public struct TeleportClip { public float Distance; }

public struct TeleportTrack : ITlTrack<TeleportClip>
{
    [Pulse(typeof(TeleportKind), On = ClipEvent.Start)]
    public static float Jump(in TlFrame<TeleportClip> frame)
        => frame.Direction * frame.Clip.Distance;
}

[TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
public struct TeleportKind
{
    public static void Apply(in float distance, ref Teleported player)
        => player.Offset += distance;
}

[TlState]
public partial struct Teleported { public float Offset; }
```

The host attaches a `KindStore` to its `PlaybackTable`, calls `world.Tick(players)`, and runs the generated system with its own component array:

```cs
using var world = new TimelineWorld(File.ReadAllBytes("game.tlib"));
using var players = new PlaybackTable(entityCapacity: 1);
using var kinds = new KindStore(world, players);
var state = new Teleported[1];
players.Start(0, 1);
for (var frame = 0; frame < 60; frame++)
{
    world.Tick(players);
    new Tl.Generated.TlAuto_HelloTl { C0_Teleported = state }.Run(world, players);
}
```

## Run the full thing

`samples/Runner` is the whole pipeline with the code inline — data, bake, systems, output. Its `GameLogic.cs` declares the toolbox (payloads, tracks and clips, consumers, the `Movement` and `Signals` systems); the console host loads `game.tlib` and steps three entities through it.

**1 · the data** — `samples/Runner/timelines/`, authored by a designer, naming the game's own C# types. `run.json` loops locomotion under the whole game; `jump.json` hands back to `run` through `next`; `slide.json` opens an `InputWindow` that requests `barrel_explode` on a second channel when the player presses Attack mid-slide; `potion_hop.json` stacks channel-free on top of anything:

```json
{"name":"slide","duration":24,"next":"run","channel":"locomotion","tracks":[
{"name":"hands","namespace":"Game","type":"InputTrack","clips":[
{"name":"window","namespace":"Game","type":"InputWindowClip","start":6,"end":14,"data":{"Button":"Attack","OnPress":"barrel_explode"}}]}]}
```

**2 · the bake** — `timelines/tl.project.json` to one canonical `game.tlib`, deterministic on every machine:

```sh
cd samples/Runner
tl library timelines --assembly bin/Release/net10.0/Runner.dll --out game.tlib --ids Timelines.g.cs --namespace Runner
```

**3 · the systems** — `samples/Runner/GameLogic.cs`. `Jump` feeds lift from the clip and live jump power. `Lift.Apply` writes vertical motion. `ChangeStat` sends a pulse when a clip starts. The host reads input-trigger results and queues a request for the next step.

Run it:

```sh
cd samples/Runner
dotnet build -c Release
tl library timelines --assembly bin/Release/net10.0/Runner.dll --out game.tlib --ids Timelines.g.cs --namespace Runner
dotnet build -c Release
dotnet run -c Release --no-build
dotnet run -c Release --no-build -- --verify
```

You see the hero run, jump exactly back to the takeoff height, and hand over to `run`; the slider's mid-slide button press detonates the barrel on the action channel; `--verify` proves `Jump_lands_exactly_where_it_took_off`, exact rewind, and a stable state hash across two 600-step runs.

No game behaviour lives in tl. The jump-arc math is `Jump.Evaluate` (toolbox, once per clip type); pressing Attack to detonate is the host setting one `InputState` cell (gameplay, your ECS); sliding into a barrel needs no slide-specific code — the designer arranged an `InputWindow` clip plus a `Cue`/`Stomp` pair the toolbox already provided.

## Data

Each timeline file is the whole input format — authored by a designer, naming the game's own C# types:

- windows are half-open `[start, end)`; execution order is authored clip order
- `data` fields map onto struct fields by name (`Scale` → `ArcTrack.Scale`); an omitted `data` on a track or clip bakes the type's default state
- an optional `name` on the timeline, each track, and each clip labels the asset for tools and reports
- two clips overlapping on one track blend through the type's `IBlend` with the authored factor (0 on the first tick of the overlap, 1 on its last, 0.5 for a one-tick overlap); overlap on a track whose type lacks `IBlend` fails the bake. A short-form `ITlTrack<TClip>` track (`TlNoSettings<TClip>`) blends through the clip's own `IBlend<TClip>` when the clip implements it and otherwise keeps the first clip; the long form keeps calling the track's `Blend`
- `loop: true` wraps at `duration`; finite timelines without `next` disappear at the end; `hold: true` keeps the last tick with `Ended` instead
- `next` names the timeline that takes over the same slot and channel at tick 0 when this one ends; it is incompatible with `loop: true`
- `channel` names an exclusive lane per entity with replacement semantics; a timeline with no channel stacks with everything
- `markers` are unmanaged structs resolved at marker ticks in either direction
- enum fields take member names case-sensitively (`"Voice": "Boom"`); `TimelineId` fields take timeline names resolved against the manifest (`"OnPress": "barrel_explode"`, unknown names fail the bake)
- unknown keys are rejected; a timeline may mix track and clip types freely; steps resolve per stage, with per-step frame tables built at load
- every baked pair carries a layout fingerprint of its two structs (a hash over field names, field types, and declaration order). The generator folds the pair's field shape from source symbols; a struct edit between bake and bind fails fast with a rebake repair instead of silently misreading

One `tl.project.json` per library: `tickRate` feeds the Unity exporter (seconds to ticks; the runtime counts ticks only), `channels` order sets channel numbers (1-up, 0 = none; unknown channel fails the bake), `timelines` lists the timeline files. The manifest is the timeline names sorted ordinally; `TimelineId` is the dense id in load order (manifest order, id = index + 1, 0 = none) with `Timelines.<PascalCase>` compile-time constants.

## Authoring

`tl library` compiles the data to canonical `game.tlib` bytes — deterministic (same input, same bytes, every machine and culture), content-deduped (each identical timeline stored once), digested end to end:

```sh
tl library <dir> [--assembly Game.dll] --out game.tlib --ids Timelines.g.cs [--namespace Game] [--diagnostics json]
```

1. Bakes every timeline as today (TLB v6 with the JSON v2 fields), resolving types via `--assembly`/discovery/`--auto`, then resolves name fields in a second pass against the manifest.
2. Writes `game.tlib`: header, manifest (names/ids/channels), each timeline's TLB bytes (content-digest dedup), digest over everything. Documented in [docs/library-layout.md](docs/library-layout.md) in the style of [docs/tlb1-layout.md](docs/tlb1-layout.md) (offsets, widths, alignment, endianness scope, rejection contract, fixtures).
3. Writes `Timelines.g.cs` (`PascalCase`, collision fails bake, `const` so Burst reads them) plus `LibraryDigest`.

`tl bake tl.project.json -o game.tlib` is the same pack, but it writes `Timelines.g.cs` and `Kinds.g.cs` only with `--emit-sources <dir>` (`tl library` writes them beside the folder as above); `tl decode game.tlib -o decoded/` (or one `.tlb`) writes the exact JSON back, or fails by name when it cannot. The three JSON documents (timeline, `tl.project.json`, scene), the decode guarantees and the migration notes are in [docs/json-format.md](docs/json-format.md).

`new TimelineWorld(ReadOnlySpan<byte> library)` is the one cold step per library: it validates (rejection contract) + digest, turns TLB structures into native frame tables and a tick-ordered index of clip and marker events. A `KindStore` builds the tables used by the generated kind systems. It publishes a `LibraryView`: an unmanaged versioned pointer struct documented in [docs/library-view-abi.md](docs/library-view-abi.md) with readers for tools.

`tl --json --assembly bin/Release/net10.0/Runner.dll` lists every authorable pair in the given assemblies — the authoritative answer to "what can this JSON name?":

```json
{
  "schemaVersion": 1,
  "assemblies": ["Runner"],
  "pairs": [
    {
      "track": { "namespace": "Game", "name": "ArcTrack", "assembly": "Runner" },
      "clip": { "namespace": "Game", "name": "ArcClip", "assembly": "Runner" },
      "consumers": [{ "name": "Game.Jump", "assembly": "Runner" }]
    }
  ]
}
```

## System

A kind declares the values it accepts and how its system applies them. `[Feeds]` supplies a value while a clip is active. `[Pulse]` supplies a value at a clip boundary or marker. Kind systems borrow columns from the game and run in game order after `Tick`. Each `Tick` applies requests and advances the playbacks once. The game owns the columns, I/O, and downstream systems.

Event-only clip hooks can raise typed `Events<T>` from the frame. The host drains each writer in entity, slot, and raise order. A kind-free host can advance playback with `TimelineWorld.Advance`.

### Migrate legacy .NET consumers

Replace column-writing `ExecuteActive` with a span `[Feeds]` method and the kind's `Apply`. Use `[Pulse]` with `ClipEvent.Start`, `End`, or `Marker` for gameplay effects at those boundaries. A `Fold` can still supply inputs to a feeder. Keep frame-only clip hooks with `ref Events<T>` writers for notifications.

`RunFused`, `SystemDispatch`, and `SystemStep` were removed. Attach a `KindStore`, drive the step through `Tick`, and call the generated system's `Run`. Regenerate exported C# and rebake the library so it includes the kind catalog.

## Control, events and pulses on both hosts

Kinds (`[TlKind]`, `[Feeds]`, `[Pulse]`, `[TlState]`) and event-only methods (`OnClipStart`/`OnClipEnd` taking only the frame and `ref Events<T>`) run the same on .NET and in Unity ([com.tl.unity](https://github.com/IAFahim/tl.unity)). The kind step on .NET is `world.Tick(players)`, then the generated `Tl.Generated.TlAuto_<Assembly>` system's `Run`; Unity runs the same order in `TlSimulationGroup`. Every control verb takes the entity, then the timeline or channel, then the rest:

| Verb | .NET (`PlaybackTable`) | Unity (`DynamicBuffer<TimelineRequest>`, `TlRequests`) |
|---|---|---|
| Start by id, at a tick | `players.Start(entity, timeline, at)` | `requests.Start(timeline, at)` |
| Start by name | `players.Start(entity, world.Find("healing_hand"))` | `requests.Start(library.Find("healing_hand"))` |
| Stop, StopChannel, StopAll | `players.Stop(entity, timeline)`, `StopChannel(entity, channel)`, `StopAll(entity)` | `requests.Stop(timeline)`, `StopChannel(channel)`, `StopAll()` |
| Pause, Resume | `players.Pause(entity, timeline)`, `Resume(entity, timeline)` | `requests.Pause(timeline)`, `Resume(timeline)` |
| Seek, Reverse | `players.Seek(entity, timeline, tick)`, `Reverse(entity, timeline, on)` | `requests.Seek(timeline, tick)`, `Reverse(timeline, on)` |
| Channel verbs | `players.PauseChannel(entity, channel)`, `ResumeChannel`, `SeekChannel`, `ReverseChannel` | `requests.PauseChannel(channel)`, `ResumeChannel`, `SeekChannel`, `ReverseChannel` |
| SetSpeed, SetChannelSpeed | `players.SetSpeed(entity, timeline, sixteenths)`, `SetChannelSpeed(entity, channel, sixteenths)` | `requests.SetSpeed(timeline, sixteenths)`, `SetChannelSpeed(channel, sixteenths)` |
| Reset (row reused by a new entity) | `players.Reset(entity)` | `requests.Reset()` |
| Another entity | `players.Start(target, timeline)` | `lookup[target].Start(timeline)` (`BufferLookup<TimelineRequest>`) |
| One entity's events this step | `drained.Of(entity)` after `var drained = events.Drain()` | `DynamicBuffer<TimelineEvent<T>>` on the entity |
| Game pulse | `damage.To(entity, 5)` after `var damage = DamageKind.Pulses(kinds)`, taken once | `DamageKind.Pulses(writer).To(entity, 5)` per job |
| A start the entity cannot hold | dropped, `players.DroppedStarts` | dropped, `TimelineKindTick.DroppedStarts` |

Any code can send any verb to any entity: a healer's cast event starts "healing_hand" on its target, and the target's kinds apply it. Requests apply at the next tick's request phase in the order sent per entity, so a request written after a step's tick (by code that read that step's events) lands on the following step. Events of one entity come in playback-slot order, then raise order. Checked builds (`Debug`, `-p:TlChecked=true`; Unity scripting define `TL_CHECKED`) throw on a start the entity cannot hold. What still differs between the hosts, and why, is in [playback-control: Host differences](docs/playback-control.md#host-differences).

## Generated reports

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release   # content-stable .g.cs snapshot + manifest
```

The export is content-stable. The KindDomain, IdleDomain, and KindSystems tests compare the emitted source with their checked-in snapshots.

## Numbers

Opt-in lazy slot ticks for kind-free playback, their ownership contract, measured cost and verification requirements are described in [PERF-GLIDE](docs/PERF-GLIDE.md).

<!-- tl-numbers: generated by eng/refresh-numbers from benchmarks/Systems/results/numbers.json; edit the renderer, not this block -->
4.0 systems, one step per call (i9-14900K, 32 cores, .NET 10.0.12, Release; best of 5 in-process runs after per-shape steady-state warm-up (at least 0.5 s and until the best frame is flat across three rounds); hot = consecutive frames with the working set cache-resident, cold = interleaved shapes streaming from DRAM; every shape bit-exact forward and backward, 0 B allocated:

| shape | hot ns/entity | cold ns/entity | warm alloc B |
| --- | ---: | ---: | ---: |
| folded lane through a generated system | 9.42 | 9.46 | 0 |
| folded lane rewound through the same system | 9.45 | 9.50 | 0 |
| fold-fed compose through a generated system | 10.86 | 10.79 | 0 |
| frame-consumer dispatch through a generated system | 10.58 | 10.57 | 0 |
| four pairs through one generated system | 21.84 | 22.03 | 0 |
| one playback per entity (2-playback reference) | 8.88 | 8.94 | 0 |
| two playbacks per entity | 17.26 | 17.21 | 0 |
| 64 consumers, 2 active per tick | 372.47 | 372.19 | 0 |

Two playbacks per entity cost 1.94x one playback on the same run. Committed 3.x LiveBatch baselines (per-entity medians from a different harness and row count) ship in the receipt for reference; the 4.10 gates (folded ≤1.03x, fold-fed ≤0.15x, frame ≤0.25x, four-pair ≤0.70x, 64-consumer ≤1.5x, 2-playback ≤2.2x) are decided by same-harness BenchmarkDotNet runs on the release machine, not by the ratios above. Warm allocation is 0 B on every shape and determinism is verified by identical state hashes across two full runs.
<!-- /tl-numbers -->

These are historical measurements of the removed .NET consumer path. The [KindShape harness](benchmarks/KindShape) measures current playback. The original harness and receipts can be reproduced from [the last legacy revision](https://github.com/IAFahim/tl/tree/7c90cde7/benchmarks/Systems).

## Migrating 3.x → 4.0

4.0.0 ships one blessed playback path with no compatibility shims. Rename or remove directly; rebake every asset (`next`, `hold`, `channel`, and marker data have no 3.x spelling).

| 3.x | 4.0 |
|---|---|
| `TimelineAsset.Load/Of/Dispose`, `ushort` index, `TimelineComponent` | `new TimelineWorld(library)`, `TimelineId` consts, `Reload` |
| `Timeline<TTrack,TClip>.Apply` overloads, `ApplyChunk`, generic `Apply<TIndex,...>`, row-list and shared-clock overloads | `system.Run(world, players)` |
| `Timeline.Advance`, `Timeline<TTrack,TClip>.Advance` overloads | `world.Advance(players)` |
| `ColumnSet` caller-assembled columns | `[TimelineSystem]` struct fields |
| `Prepare`/`Play`/`Release`/`BankState` | removed (systems own the plan) |
| `IBake<T>`, `Timeline.Bake`/`BakeRef` | removed; host code plus systems cover it |
| `Timeline.SealConsumers` | removed; registry immutable after install |
| `PairRuntime` function-pointer registration | `ITlModule` (generator ABI v2) |
| `Frame<,>` ref struct holding refs | `Frame<,>` readonly struct holding values |
| `FrameFlags : byte` | `FrameFlags : ushort` + `Synthetic`/`Interrupted` |
| `TTrack : IBlend<TClip>` required | every track implements `IBlend<TClip>` (blend runs only where clips overlap) |
| `ITimelineLane<T>`, `Timeline<T>` lane facade | removed (kernels internal) |
| `Timeline.Query`, `FrameQuery`, `TickFrame` | removed (bake-time inspection via `TlbInspection`/`SystemReader`; runtime reads `LibraryView`) |
| `TimelineState`/`TimelineMovement`, `SlotView`/`LaneSegment` | internal / `LibraryView` ABI v1 |
| `TrySample`/`SampleClip`/`View` | removed (`Inspection` keeps `Tables` snapshots for bake diagnostics) |

Behaviour change to name (D10): a fold-fed consumer with several active tracks of one pair now runs once per active step with its own track's values (matching frame consumers and authored track order). 3.x ran the composed value once per row. If a timeline overlaps two tracks of one pair feeding one composed consumer, re-tune by ear and re-record the receipt.

Contributing: [AGENTS.md](AGENTS.md) · Security: [SECURITY.md](SECURITY.md) · License: [MIT](LICENSE)
