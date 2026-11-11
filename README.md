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

Three structs and one JSON file — a first timeline in a minute. Put the structs and `timelines/jump.json` in a console project:

```cs
public readonly struct ArcClip { public readonly float Velocity; public ArcClip(float velocity) { Velocity = velocity; } }

public readonly struct ArcTrack : IBlend<ArcClip>
{
    public readonly float Scale;
    public ArcTrack(float scale) { Scale = scale; }
    public void Blend(in ArcClip first, in ArcClip second, float factor, out ArcClip result)
        => result = new ArcClip(first.Velocity + ((second.Velocity - first.Velocity) * factor));
}

public readonly struct Rise : ITrack<ArcTrack, ArcClip>
{
    public static void Fold(in Frame<ArcTrack, ArcClip> frame, out float lift)
        => lift = frame.Direction * frame.Clip.Velocity * frame.Track.Scale;

    public static void ExecuteActive(in Frame<ArcTrack, ArcClip> frame, in float lift, ref Motion m)
        => m.Y += lift;

    public static void OnClipEnd(in Frame<ArcTrack, ArcClip> frame)
    {
    }
}
```

```json
{
  "name": "jump", "duration": 30, "next": "run", "channel": "locomotion",
  "tracks": [
    { "namespace": "Game", "type": "ArcTrack", "data": { "Scale": 1.0 },
      "clips": [ { "namespace": "Game", "type": "ArcClip", "start": 0, "end": 15, "data": { "Velocity": 2.0 } },
                 { "namespace": "Game", "type": "ArcClip", "start": 15, "end": 30, "data": { "Velocity": -2.0 } } ] }
  ]
}
```

Build, then bake the whole library with `tl library` — it packs every timeline into one `game.tlib` and writes the stable ids to `Timelines.g.cs`:

```sh
dotnet build -c Release
tl library timelines --assembly bin/Release/net10.0/Runner.dll --out game.tlib --ids Timelines.g.cs --namespace Runner
```

Play it — systems run every step, then one `Advance` moves every clock; `Run` never moves ticks. After the full loop the arc has risen and fallen and `next` has handed the entity back to `run`. Put the loop in `Program.cs` and `dotnet run`:

```cs
using var world = new TimelineWorld(File.ReadAllBytes("game.tlib"));
using var players = new PlaybackTable(entityCapacity: 1, slotsPerEntity: 4);
var motion = new Motion[1];
var power = new JumpPower[1];
players.Start(0, Timelines.Jump);
for (var frame = 0; frame < 30; frame++)
{
    world.ApplyRequests(players);
    new Movement { Motion = motion, JumpPower = power }.Run(world, players);
    world.Advance(players);
}
```

## Run the full thing

`samples/Runner` is the whole pipeline with the code inline — data, bake, systems, output. Its `GameLogic.cs` is the shared toolbox both hosts compile (payloads, tracks and clips, consumers, the `Movement` and `Combat` systems); the console host loads `game.tlib` and steps three entities through it.

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

**3 · the systems** — `samples/Runner/GameLogic.cs`. `Jump` folds the pure arc once per tick and composes it with live per-jumper power every step; `InputWindow` requests `barrel_explode` while Attack is held; `ChangeStat` spends one `JumpCounter` on clip start; `Stomp` and `Cues` raise typed events:

```cs
public readonly struct Jump : ITrack<ArcTrack, ArcClip>
{
    public static void Fold(in Frame<ArcTrack, ArcClip> frame, out float lift)
        => lift = frame.Direction * frame.Clip.Velocity * frame.Track.Scale;

    public static void ExecuteActive(in Frame<ArcTrack, ArcClip> frame, in float lift, ref Motion m, in JumpPower power)
        => m.Y += lift * power.Value;

    public static void OnClipEnd(in Frame<ArcTrack, ArcClip> frame)
    {
    }
}
```

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

No game behaviour lives in tl. The jump-arc math is `Jump.Fold` (toolbox, once per clip type); pressing Attack to detonate is the host setting one `InputState` cell (gameplay, your ECS); sliding into a barrel needs no slide-specific code — the designer arranged an `InputWindow` clip plus a `Cue`/`Stomp` pair the toolbox already provided.

## Data

Each timeline file is the whole input format — authored by a designer, naming the game's own C# types:

- windows are half-open `[start, end)`; execution order is authored clip order
- `data` fields map onto struct fields by name (`Scale` → `ArcTrack.Scale`); an omitted `data` on a track or clip bakes the type's default state
- an optional `name` on the timeline, each track, and each clip labels the asset for tools and reports
- two clips overlapping on one track blend through the type's `IBlend` with the authored factor; overlap on a track whose type lacks `IBlend` fails the bake
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

`new TimelineWorld(ReadOnlySpan<byte> library)` is the one cold step per library: it validates (rejection contract) + digest, turns TLB structures into native blocks, and builds the derived tables — per-step fold lanes (every tick both directions; pure functions of the frame), frame tables per (timeline, step, direction), an event index (sorted tick/kind/step/marker plus 1 bit per tick), and per (system, timeline, stage) step lists in authored order so dispatch costs active steps, not clip types in the game. It publishes a `LibraryView`: an unmanaged versioned pointer struct documented in [docs/library-view-abi.md](docs/library-view-abi.md) with readers for tools.

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

The structs in the run above are the whole game side — the clip payload, the track settings with its blend, and the consumers that read entity columns plus shared context. `ITrack<TTrack, TClip>` consumers are discovered compilation-wide — no registration, no catalog. `frame.Direction` is +1 forward and −1 backward, which is why rewind is exact.

A **system** is a struct the gameplay programmer declares once. It lists entity columns plus shared context (spatial index, event writer). The generator connects each consumer to the system holding its parameters and writes the loop. A game has a handful of systems and any number of timelines and clip types:

```cs
[TimelineSystem]
public ref partial struct Movement
{
    public Span<Motion> Motion;
    public ReadOnlySpan<JumpPower> JumpPower;
    public Span<Stats> Stats;
    public ReadOnlySpan<InputState> Input;
    public Requests Requests;
    public Events<Cue> Cues;
}
```

`Span`/`ReadOnlySpan` fields are columns (one element per entity); anything else is context; all column and context types are unmanaged. One consumer belongs to exactly one system (all params covered); fitting two systems without `[TimelineSystemMember(typeof(S))]` is an error; fitting none warns and never runs. `Run` never moves ticks: any number of systems run per step, ticks move once in `Advance`.

All consumer methods are static, optional, in any combination, found by name and shape:

| Method | Called | Receives | Use |
|---|---|---|---|
| `Fold(in Frame, out R...)` | at load, every tick both directions; must be pure | frame only | authored-data numbers |
| `ExecuteActive(...)` | every step, once per active step of this pair per playback | frame **or** leading fold results, then columns, then context | per-entity effects |
| `ExecuteBatch(...)` | once per block of playbacks instead of `ExecuteActive` | gathered folds + column spans | hand SIMD |
| `OnClipStart(in Frame, ...)` | first tick of clip either direction (`Direction` says which); mid-clip start gives `Synthetic` | frame, columns, context | one-shots, stat changes |
| `OnClipEnd(in Frame, ...)` | last tick either direction; mid-clip stop/replace gives `Interrupted` | frame, columns, context | cleanup |
| `OnMarker(in MarkerFrame, ...)` on `struct X : IMarker<T>` | at marker tick either direction | marker frame, columns, context | footsteps, spawns |

Rewind is exact (tied to ticks): `ChangeStat` applies `+delta` forward and `-delta` back across the same tick. Binding: `ref/in T` column type = this entity's element; `ref/in T` context type = shared value; `ref Requests` = start/stop for this entity applied next request phase; `ref Events<T>` = raises with `EntityRef`+slot; fold results = leading `in R` in declaration order. Caps are unchanged: 10 folds, widths 1/2/4/8, 30 live columns per consumer, 40-slot row, same-type duplicate-column rejection.

A **playback** is one timeline playing on an entity: `{Timeline, Tick}` plus channel and flags. `Tick` is the next tick to play forward. Storage is a native table behind a managed owner with inline slots per entity (slot order = start order, O(1) start/stop, removal keeps order). Operations: `Start(entity, id, at = 0)` (channel replace: active clips of replaced playbacks get `OnClipEnd`+`Interrupted`; `at > 0` gives `OnClipStart`+`Synthetic` next step), `Stop`/`StopChannel`/`StopAll` (actives get `Interrupted` ends), `IsPlaying`/`Find(channel)`. Games with per-skill loadouts declare `[TimelineSlotKeys] enum Skill { Jump, Slide, Hurt }` and swap each entity's slot table (key → `TimelineId`, see `slots` in `samples/Runner/Program.cs`) so a potion swaps the jump without touching entities.

One whole step, both hosts:

```cs
using var world = new TimelineWorld(File.ReadAllBytes("game.tlib"));
var players = new PlaybackTable(entityCapacity: 4096, slotsPerEntity: 4);
players.Start(0, Timelines.Run);
void Step() {
    world.ApplyRequests(players);
    new Movement { Motion = motion, JumpPower = power, Stats = stats, Input = input, Requests = world.Requests(players), Cues = cues.Writer }.Run(world, players);
    new Combat { Position = position, Heads = heads, Hits = hits.Writer }.Run(world, players);
    world.Advance(players);
}
```

Step order: 1. Requests (last step's, then host's). 2. Systems in game order. 3. `Advance` per playback (++/--, wrap if loop, `next` handover same slot+channel at tick 0 + `StartPending`, else `hold` → `Ended` else removal; `Paused`/`Ended` don't move). 4. Events drained in (entity, slot, raise) order. Determinism: no hash-ordered containers; every tick played exactly once per step, no skipping; float order fixed by consumer order (entities row order; per entity slots in slot order; per playback steps in authored track order, reversed backward; per step consumers in ordinal consumer-name order, reversed backward; event methods after `ExecuteActive` of the same playback+tick).

Game owns column arrays plus result systems (integrate motion, apply damage, present cues). Tl requires none of that.

## Generated reports

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release   # content-stable .g.cs snapshot + manifest
```

Checked-in systems snapshots are byte-exact emission goldens. Regenerate them
only with the commands below and check in the result; the matching tests fail
on any drift:

- `samples/Runner/Generated/TlSystemsRunner.g.cs` — regenerate via
  `Tl.Systems.Tests` `RunnerSnapshotTests` with `TL_UPDATE_SNAPSHOT=1`
  (emits the `Game.Logic` toolbox with id `Runner`; the Runner sample and
  `LoaderRunTests` consume it).
- `benchmarks/Systems/Generated/TlSystemsBench.g.cs` — regenerate via
  `Tl.Systems.Tests` `BenchGenTests` with `TL_UPDATE_BENCH=1`
  (emits the bench domain with id `Bench`; the Systems benchmarks consume it).
- `tests/Tl.Systems.Tests/LoaderLibrary/game.tlib` — rebake with
  `tl library tests/Tl.Systems.Tests/LoaderLibrary --assembly <Tl.Systems.Tests.dll> --out game.tlib`
  (the `LoaderRunTests` loader-built proof consumes it; ids are positional).

## Numbers

The opt-in kind-free playback glide, its unsafe ownership contract and verification requirements are described in [PERF-GLIDE](docs/PERF-GLIDE.md).

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

The table is rendered from [benchmarks/Systems](benchmarks/Systems) receipts — every shape bit-exact forward and backward with 0 B warm — and CI fails if the receipt and this section disagree. Re-measure on the release machine with `eng/refresh-numbers`, which rebuilds the harness, re-runs every shape, and re-renders this block from the fingerprinted receipt. The 4.10 gates (folded ≤1.03x, fold-fed ≤0.15x, frame ≤0.25x, four-pair ≤0.70x, 64-consumer ≤1.5x, 2-playback ≤2.2x, 0 B warm, parity) run against these rows plus the committed 3.x baselines; ratios below are indicative until a same-harness release-machine run records the verdicts.

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
