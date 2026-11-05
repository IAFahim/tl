# Tl roadmap: implementation plan

Version 2, 2026-11-05. For AI agents working in `IAFahim/tl` (and `IAFahim/tl.unity` for P5-P6).

`AGENTS.md` governs process (issues, claims, atoms, receipts, validation). This plan governs scope.
A product-contract change in this plan takes effect only after the owner approves the matching decision in section 3.
Where this file and `Tl next from authoring to a running game.md` differ, this file wins.

## 0. How to work this plan

1. Read `AGENTS.md`, `README.md`, this file, issue #56 (data-authored contract) and issue #356 (consumer caps) before editing.
2. Open one parent issue ("Ship the Tl 4 playback model", sections 1-4 + section 3 checklist) and one child issue per phase in section 5. Copy each phase's atoms and acceptance receipts into its issue.
3. Run phases in order. Do not start a phase while a decision it depends on is still open.
4. Every atom ends green on the core gate in `AGENTS.md` (Required validation) plus `python3 benchmarks/source_budget.py` and the receipts the atom names.
5. Budget: `src/` measured 353,025 of 354,000 bytes on 2026-11-05 (975 left). Delete code before adding it. Record each atom's byte delta in its issue.
6. Production code carries no explanatory comments (`AGENTS.md`). Reasoning goes in issues and docs.
7. Do not re-measure what section 2 already proves. Start from it.

## 1. What Tl is (read first)

Tl plays designer-authored timelines on game entities, deterministically and fast. Tl contains no game behaviour.

| Who | Does | Writes code |
|---|---|---|
| Designer | Builds timelines: places clips on tracks, sets values, loop, next, channel | Never |
| Toolbox programmer | Writes each clip type once: payload struct + consumer code | Once per clip type, never per timeline |
| Gameplay programmer | Decides when an entity starts a timeline; declares systems listing which entity data clip code may touch | A little |
| Tl | Bakes timelines, stores each once, plays them on many entities, calls clip code at the right ticks | n/a |

Three confusion killers from the chat that motivated this plan:

- The `Rules`/`CommandKind` switch (jump on press, potion = boost row) is **sample game code, not Tl**. Your ECS does this your own way.
- A timeline never lives on an entity. All timelines live once in the shared library the bootstrap loads. What an entity stores is a short list of **playbacks**: `{timeline, tick}` pairs (section 4.6). Jump id 3 vs slide id 4 just means "manifest index 3 vs 4".
- The slide-into-barrel works with no slide-specific code. The designer adds an `InputWindow` clip (`Attack` -> `barrel_explode`) plus a `Cue`/`Stomp` clip on the `barrel_explode` timeline. Those are general clip types a programmer wrote once (Appendix B). You only write new code when a clip type does not exist yet.

Must work with no timeline-specific code:

- **Jump with counter.** Jump timeline has an arc clip plus a `ChangeStat` clip (`JumpCounter, -1`) on another track. Designer arranges existing types.
- **Sliding into a barrel.** Slide has `InputWindow` tick 6-14 with `OnPress = "barrel_explode"`. Pressing inside the window requests `barrel_explode` on the same entity.
- **Stomp.** While active, clip code checks the entity's feet against a spatial index the game builds each step, raises a `StompHit` event.

Runtime picture:

```text
game.tlib   shared, read-only: every timeline, loaded once
entity      playback slots: [{jump, tick 7}, {potion_hop, tick 2}] + own columns (Motion, Stats, Input, Position, ...)
each step   1. apply start/stop requests
            2. systems run clip code of every active clip on every playback
            3. advance: ticks move, loops wrap, finished timelines hand over to `next` or end
```

A **system** is a struct the gameplay programmer declares once. It lists entity columns plus shared context (spatial index, event writer). The generator connects each consumer to the system holding its parameters and writes the loop. A game has a handful of systems and any number of timelines and clip types.

## 2. Where Tl is today (facts; do not re-measure before starting)

Measured 2026-11-05 against `ecff1af` unless noted. Scouts returned file:line proof; trust it.

### 2.1 Identity, storage, overloads

- `TimelineId` does not exist. Identity is plain `ushort index` / `ReadOnlySpan<ushort> indices` plus `TimelineAsset` (`src/Tl.Core/Data.cs:273`), `TimelineRef` (`Data.cs:98`), `TimelineComponent` (`Data.cs:312`).
- Process-wide statics, no per-world isolation: `TimelineTable` (`src/Tl.Core/InternTable.cs:8`), `PairTable` (`src/Tl.Core/Data.cs:340`), `BakeTable` (`src/Tl.Core/BakeTable.cs:29`), per-pair fold bank `Timeline<TTrack,TClip>._bank` (`src/Tl.Core/TimelinePair.cs:47`, via `Bank()`/`FloatBank()`), `LaneTable<TTrack,TClip>` + `LaneAccelerator<T>` (`src/Tl.Core/Lane.cs:1107,~1082`). Burst cannot reach them; server+client in one process share them.
- Overload sprawl: `Timeline.Advance` = 6 (`src/Tl.Core/Timeline.cs:17-102`), `Timeline.Bake/BakeRef` = 9 (`Timeline.cs:124-166`), `Timeline<TTrack,TClip>.Apply*` = 12 incl. `ApplyChunk` (`src/Tl.Core/TimelinePair.cs:52-411`), `Advance*` = 4 (`:253-743`), `Timeline<T>.Apply/Advance` = 2+2 (`src/Tl.Core/Lane.cs:22-38`). Plus `Prepare`/`Play`/`Release`/`BankState` (`TimelinePair.cs:20,426,532,587`) and `ColumnSet` (`:911`).
- `Frame<TTrack,TClip>` is a `readonly ref struct` with `ref readonly` track/clip fields (`src/Tl.Core/Compiled.cs:13-48`). `FrameFlags : byte` (`src/Tl.Core/Playback.cs:7`). `TTrack : IBlend<TClip>` required shape via `IBlend<TClip>.Blend` (`src/Tl.Core/Hooks.cs:3-7`).

### 2.2 Generator (what it emits today)

- One file per compilation: `TlConsumerBinding[{id}].g.cs`, `internal static unsafe class TlConsumerBinding[{id}]` + per-consumer unmanaged thunks (`src/Tl.Gen.CSharp/JobEmitter.cs`). Thunk ABI: `(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)`.
- Does **not** emit `ColumnSet` (caller-assembled `ref struct` in Tl.Core) or `Run`. `TimelineGeneration.Run` (`src/Tl.Gen.CSharp/TimelineGeneration.cs:12-60`) is the host-chaining entry, not emitted code.
- Consumer shapes: `Fold(in Frame<T,C>, out/ref ...)` (<=10 results, widths 1/2/4/8) and `ExecuteActive([in Frame<T,C>,] in/ref ...)` (<=30 live columns incl. memo feeds, 40-slot row). Caps from `Analysis/TypeAnalyzer.cs:44-46,57-63` + `Model/Jobs.cs`; `SlotRow = 40` (`Analysis/JobReader.cs:9`).
- Frameworks: analyzer `netstandard2.0` (Roslyn 4.3.1), CLI `net10.0` (Roslyn 5.9.0) (`src/Tl.Gen.CSharp/Tl.Gen.CSharp.csproj:4-37`). `src/Tl.Core` is `net10.0`-only (`src/Tl.Core/Tl.Core.csproj:4`). `src/Tl.CSharp` is the `net10.0` metapackage. All samples are `net10.0`. `LangVersion 14` (`Directory.Build.props:3`).
- No Unity/Burst references in `src/**` or `samples/**`. Unity host lives in extracted `tl.unity`. `UnityJobEmitter` (`src/Tl.Gen.CSharp/UnityJobEmitter.cs:5-8`) is a same-content alias under a fixed filename. `--backend unity-entities` (`GeneratorCli.cs`) selects it.

### 2.3 Bake / load split today

- `tlb` bakes JSON to TLB1 (`docs/tlb1-layout.md`); `--auto` namespace inference is opt-in. Bake-time `Fold`/`Blend` runs through `BakeRuntime<,>` (`src/Tl.Core/BakeTable.cs:7-18`) registered by generated `Bake_{N}`; load-time first-use fold goes through the per-pair bank (`TimelinePair.cs:47,~897`). No `game.tlib` multi-timeline library, no manifest, no stable ids, no `next`/`hold`/`channel`/`markers` keys.
- LiveBatch prototypes (the speed numbers in the sister doc) matched Tl bit-for-bit over 97 frames but are hand-written thunks, not the real generator path. P0 re-ports them onto BenchmarkDotNet through the real generator.

### 2.4 Known defects to fix, not work around

1. One full pass over ids+clocks per (track, clip) pair per frame, including rows whose timeline lacks the pair.
2. No entity model (who plays what, overlap, exclusivity, finishing, transitions are host code). No events; clip start/end are per-row per-tick flag checks.
3. `TrySample`/`FrameQuery` return only the first matching step of a pair; two overlapping tracks of one pair with a fold-fed `ExecuteActive` disagree between `Apply` (adds lane once per tick) and `Play` (runs compose once per active step with the same value). P0 adds the two-track test; D10 settles it.
4. `src/` has 975 bytes of headroom. Additions land only after deletions.

## 3. Decisions (owner approves; agents implement the recommended column unless changed)

| # | Decision | Recommended | Why | Contract touched |
|---|---|---|---|---|
| D1 | Timeline identity | `TimelineWorld` loads a whole library. Ids dense per world, manifest order (names sorted ordinally, id = index+1, 0 = none), `Timelines.Jump` compile-time constants. Content dedup stays inside a world. | Stable ids across runs/hosts/client-server; kills process-wide table | #56: "no ids in authored or baked data" |
| D2 | When folding happens | Eager per library at load, per world. Bake-time folding is a later option (P7). | Tl.Core runs inside Unity (D4); folding stays a runtime step | #56 minor (lazy first-use -> eager per library) |
| D3 | Removed public API | Appendix A removed list, in 4.0.0, with migration notes, no shims | One blessed path; frees budget | public API, #56 |
| D4 | Target frameworks | Tl.Core multi-targets `netstandard2.1;net10.0`. SIMD under `#if NET`. Public surface compiles from C# 9. | Unity today is C# 9 on .NET Standard 2.1 | none |
| D5 | `Frame` shape | Plain `readonly struct` holding track+clip by value + tick data. `FrameFlags` widens to `ushort`. | C# 9 + Burst | consumer ABI |
| D6 | JSON v2 | Adds `next`, `hold`, `channel`, `markers`. Enum fields take member names, `TimelineId` fields take timeline names. Unknown keys still rejected. | Transitions, exclusivity, timeline refs become data | #56 authoring contract |
| D7 | Consumer surface | Adds `ExecuteBatch`, `OnClipStart`, `OnClipEnd`, marker consumers, context params, `Requests`, `Events<T>`. #356 caps unchanged (10 folds widths 1/2/4/8, 30 live cols, 40-slot row, duplicate-column rejection). | Events + cross-timeline starts without per-row polling | #356 amendment |
| D8 | Playback model | Per-entity playback slots, channels, one `Advance` per step. `Run` never moves ticks. | Several systems read one timeline; overlap/exclusivity become data | movement contract |
| D9 | Source budget | Delete first. After P3 owner sets the new number from measured bytes. | 975 bytes headroom | budget |
| D10 | Fold-fed consumer + several active tracks of one pair | Fold lanes kept per step (track); consumer runs once per active step with its own track's values | Matches frame consumers + authored track order | consumer semantics |
| D11 | Unity authoring | Unity Timeline window, one generic Tl track class + one generic Tl clip class (type picked in inspector). Export JSON v2. Bundled self-contained `tlb` bakes. | Designers know the window; no per-type asset codegen | tl.unity |
| D12 | Registration | Generated `ITlModule` per assembly (direct calls inside). No function-pointer tables; explicit `TlGenerated.Install()` fallback. | IL2CPP/Burst-safe; load-time fold needs no `delegate*` | generator ABI v2 |
| D13 | netstandard2.1 performance parity | Same kernels, same 0 B warm, same gates on **both** TFMs. SIMD/AVX2 is a net10-only bonus; scalar paths must be identical. | Unity players and servers run the 2.1 build; a slow 2.1 build ships a slow game | perf contract (new) |

D13 detail (binding): P1/P3/P5 benchmarks run every shape on `net10.0` **and** `netstandard2.1` (net10 host). Gates in 4.10 apply to both; the 2.1-vs-10 ratio gate is `<=1.20x` on scalar shapes A-D (see 4.10). A 2.1 miss blocks the phase like any gate miss. `System.Runtime.CompilerServices.Unsafe` is banned from the 2.1 build unless Unity resolution is verified; use pointers. `BitOperations`/`ModuleInitializer`/`SkipLocalsInit`/`IsExternalInit` via polyfills; `NativeAlloc` wraps `NativeMemory.AlignedAlloc` (net10) / aligned `Marshal.AllocHGlobal` (2.1); `IncrementalHash` replaces `SHA256.TryHashData`; scalar + `System.Numerics.Vector<T>` fallbacks under `#else`.

## 4. Target design

### 4.1 Packages

| Package | Repo | Targets | Holds |
|---|---|---|---|
| Tl.Core | tl | netstandard2.1, net10.0 | `TimelineWorld`, library loader, derived tables, `LibraryView` + readers, `PlaybackTable`, `Playback`, `Frame`, `ITrack`, `IBlend`, `IMarker`, attributes, `Requests`, `Events<T>`, `EntityRef`; SIMD kernels net10-only |
| Tl.Gen.CSharp | tl | netstandard2.0 (Roslyn 4.3.x per Unity 6), net10.0 CLI | consumer discovery, system assignment, kernels, `Run`, `ITlModule` registration, diagnostics |
| Tl.Bake (`tlb`) | tl | net10.0 + self-contained single-file win-x64/osx-arm64/linux-x64 | JSON v2 to `.tlb`; `tlb library` writes `game.tlib` + `Timelines.g.cs` |
| Tl.Unity (`com.tl.unity`) | tl.unity | Unity 6.x, Entities 1.x, Burst | library singleton, playback buffers, system groups, advance/requests/events systems, gameplay API |
| Tl.Gen.Unity | tl.unity | netstandard2.0, Roslyn 4.3.x | Unity-only emission: column components, Burst jobs, `TimelineJobs` entry points; reuses Tl.Gen.CSharp analysis |
| Tl.Unity.Editor | tl.unity | Unity 6.x editor | Tl track/clip/marker assets, library asset, JSON exporter, bake runner, entity authoring, validation |

Game side (not Tl; samples + Appendix B): `Game.Logic` shared source both hosts compile (payloads, consumers, systems, plain rules); `Game.Unity` holds authoring/ordering/presentation. Delete `src/Tl.Gen.CSharp/UnityJobEmitter.cs` once Tl.Gen.Unity exists.

### 4.2 Authoring data: JSON v2

Everything v1 keeps its meaning: root `name/duration/loop/tracks`; track `name/namespace/type/assembly/data/clips`; clip `name/namespace/type/assembly/start/end/data`; `--auto` inference; max two overlapping clips per track blended linearly.

New keys: `next` (string, timeline name; incompatible with `loop:true`), `hold` (bool default false; finite without `next` stays at end with `Ended` instead of removal), `channel` (string; exclusive lane per entity, replacement semantics; no channel = stacks), `markers` (`{type, namespace?, assembly?, tick, data}` unmanaged marker structs), enum fields as member names (case-sensitive), `TimelineId` fields as timeline names resolved against the manifest (unknown name fails bake).

One `library.json` per library: `{ tickRate, channels[], timelines(glob|list) }`. Channel order sets channel numbers (1-up, 0 = none; unknown channel fails bake). Manifest = timeline names sorted ordinally. `tickRate` feeds the Unity exporter (seconds->ticks); runtime counts ticks only.

### 4.3 Library: bake and load

`tlb library <dir> [--assembly Game.Logic.dll] --out game.tlib --ids Timelines.g.cs [--namespace Game] [--diagnostics json]`:

1. Bakes every timeline as today (TLB v6 with JSON v2 fields), resolving types via `--assembly`/discovery/`--auto`, then resolves name fields in a second pass against the manifest.
2. Writes `game.tlib`: header, manifest (names/ids/channels), each timeline's TLB bytes (content-digest dedup), digest over everything. Documented in `docs/library-layout.md` in the style of `docs/tlb1-layout.md` (offsets, widths, alignment, endianness scope, rejection contract, fixtures).
3. Writes `Timelines.g.cs` (`PascalCase`, collision fails bake; `const` so Burst reads them) plus `LibraryDigest`.

`new TimelineWorld(ReadOnlySpan<byte> library)` (cold, once per library): validates (rejection contract) + digest; turns TLB structures into native blocks; builds fold lanes per (timeline, step) whose pair has `Fold` consumers (D10, reuses `MeasuredLanes` logic through generated `ITlModule`); frame tables per (timeline, step, direction) — per-tick rows to 2,048 ticks, else per-stage + `ushort` tick-to-stage map, blends evaluated per tick only inside blend windows; event index (sorted tick/kind/step/marker + 1 bit per tick); per (system, timeline, stage) filtered step lists in authored order (dispatch proportional to active steps, not clip types in game). Publishes `LibraryView`: unmanaged versioned pointer struct, documented in `docs/library-view-abi.md` with lifetime/aliasing/alignment/concurrency proofs. `Reload(span)` swaps between steps; playbacks keep ids (by name) + ticks clamped; removed names stop.

### 4.4 Consumer model

`Frame<TTrack,TClip>` becomes a plain `readonly struct` (track+clip by value, `Tick/ClipLength/WithinClip/TrackIndex/Flags`; `Direction`; `Has()`). New `MarkerFrame<TMarker>`, `FrameFlags : ushort` (+`Synthetic/Interrupted`), `IBlend` optional (overlap on a track lacking it fails bake), `IMarker<T>`.

All consumer methods static, optional, in any combination, found by name+shape:

| Method | Called | Receives | Use |
|---|---|---|---|
| `Fold(in Frame, out R...)` | at load, every tick both directions; must be pure | frame only | authored-data numbers |
| `ExecuteActive(...)` | every step, once per active step of this pair per playback | frame **or** leading fold results, then columns, then context | per-entity effects |
| `ExecuteBatch(...)` | once per block of playbacks instead of `ExecuteActive` | gathered folds + column spans | hand SIMD |
| `OnClipStart(in Frame, ...)` | first tick of clip either direction (`Direction` says which); mid-clip start gives `Synthetic` | frame, columns, context | one-shots, stat changes |
| `OnClipEnd(in Frame, ...)` | last tick either direction; mid-clip stop/replace gives `Interrupted` | frame, columns, context | cleanup |
| `OnMarker(in MarkerFrame, ...)` on `struct X : IMarker<T>` | at marker tick either direction | marker frame, columns, context | footsteps, spawns |

Rewind is exact (tied to ticks): `ChangeStat` applies `+delta` forward, `-delta` back across the same tick.

Binding: `ref/in T` column type = this entity's element; `ref/in T` context type = shared value; `ref Requests` = start/stop for this entity applied next request phase; `ref Events<T>` = raises with `EntityRef`+slot; fold results = leading `in R` in declaration order. Order: entities row order; per entity slots in slot order; per playback steps in authored track order (reversed backward); per step consumers in ordinal consumer-name order (reversed backward); event methods after `ExecuteActive` of same playback+tick. TLGEN9x diagnostics for: param type in no system; fits two systems without `[TimelineSystemMember]`; folds on event methods; managed param; `Requests`/`Events<T>` by `in`; both `ExecuteActive`+`ExecuteBatch`; missing `IBlend` where bake found overlap (baker reports).

### 4.5 Systems

```csharp
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

`Span/ReadOnlySpan` field = column (one element per entity); anything else = context; all column+context types unmanaged. One consumer belongs to exactly one system (all params covered); two-system fit without `[TimelineSystemMember(typeof(S))]` is an error; no-system fit warns (never runs). Per system the generator emits: a C# 9 kernel over raw pointers + `LibraryView` + `PlaybackView` (direct calls, switch dispatch over per-stage step lists, then event dispatch), a .NET wrapper `Run(TimelineWorld, PlaybackTable, bool forward)` that checks column lengths/pins/calls, and `ITlModule` system registration. `Run` never moves ticks (D8): any number of systems run per step, ticks move once in `Advance`. Same kernel source compiles for Burst (Tl.Gen.Unity path).

### 4.6 Playback model (how entities interact with timelines)

```csharp
public readonly struct TimelineId { public readonly ushort Value; }
[StructLayout(Sequential, Size = 8)]
public struct Playback { public ushort Timeline; public ushort Tick; public byte Channel; public PlaybackFlags Flags; public ushort Reserved; }
[Flags] public enum PlaybackFlags : byte { None=0, Reverse=1, Paused=2, StartPending=4, Ended=8 }
public readonly struct EntityRef { public readonly int Index; public readonly int Version; }
```

`Tick` = next tick to play forward (today's position meaning). `Channel` copied from timeline at start. `Reserved` held for later time-scale work. `EntityRef` matches Unity `Entity` layout so Unity reinterprets in place; .NET host uses row index + `Version = 0`.

Storage: .NET `PlaybackTable` (native behind managed owner, inline slots per entity default 4, slot order = start order, O(1) start/stop, removal keeps order); Unity `DynamicBuffer<Playback>` (`[InternalBufferCapacity(4)]`). Both reach kernels through one `PlaybackView` (pointer/counts/stride) so semantics match.

Operations (same names .NET `PlaybackTable` / Unity `TimelinePlayer`): `Start(entity, id, at=0)` (channel replace: active clips of replaced get `OnClipEnd`+`Interrupted`; `at>0` gives `OnClipStart`+`Synthetic` next step); `Stop(entity,id)` / `StopChannel` / `StopAll` (actives get `Interrupted` ends); `IsPlaying` / `Find(channel)`. `Requests` (from consumers) offers the same ops for the current entity, deferred to next request phase in (entity, slot, raise) order.

Named skills: game declares `[TimelineSlotKeys] enum Skill { Jump, Slide, Hurt }`; each entity carries a slot table (key -> `TimelineId`) so player A jumps `jump_heavy` while enemy B jumps `jump_small`. Gameplay calls `Start(entity, slots.Get(Skill.Jump))`. A potion swaps the slot entry. No child entities per skill.

Step order (both hosts): 1. Requests (last step's, then host's). 2. Systems in game order (`system.Run(...)` / `TimelineJobs.<S>(...)`). 3. `Advance` per playback (++/--, wrap if loop, `next` handover same slot+channel at tick 0 + `StartPending`, else `hold` -> `Ended` else remove; `Paused`/`Ended` don't move). 4. Events drained in (entity, slot, raise) order. Determinism: no hash-ordered containers; parallel jobs write per-chunk streams merged in chunk order; float order fixed by 4.4 order; every tick played exactly once per step, no skipping.

### 4.7 .NET host (one whole step)

```csharp
using var world = new TimelineWorld(File.ReadAllBytes("game.tlib"));
var players = new PlaybackTable(world, entityCapacity: 4096, slotsPerEntity: 4);
players.Start(entity: 0, new TimelineId(Timelines.Run));
void Step() {
    world.ApplyRequests(players);
    new Movement { Motion = motion, JumpPower = power, Stats = stats, Input = input, Requests = world.Requests(players), Cues = cues.Writer }.Run(world, players);
    new Combat { Position = position, Heads = heads, Hits = hits.Writer }.Run(world, players);
    world.Advance(players);
}
```

Game owns column arrays + result systems (integrate motion, apply damage, present cues). Tl requires none of that.

### 4.8 Unity runtime (tl.unity)

Components: `Playback` (`IBufferElementData`, inline 4), `TimelineSlot{ushort Key; TimelineId Timeline;}`, `TimelineRequest` buffers, `TimelineLibrary{LibraryView View;}` unmanaged singleton + managed owner holding `TimelineWorld`. Generated per game by Tl.Gen.Unity: column components (`XData : IComponentData { X Value; }`, reinterpreted as `X*` in place — no hand copies), one Burst `IJobChunk` per system over `Playback` + its columns (enabled masks honoured, shared kernel), `TimelineJobs.<System>(ref SystemState, context...)` entry points. Groups: `InitializationSystemGroup: TimelineLibrarySystem` (build from blob, editor hot reload, calls `TlGenerated.Install()`); `FixedStepSimulationSystemGroup: TimelineSimulationGroup(TimelineRequestSystem[OrderFirst], <game systems>, TimelineAdvanceSystem[OrderLast, Burst parallel])`; `PresentationSystemGroup: TimelineEventsSystem` (merge per-chunk streams; `TimelineEvents.Drain<T>`). Library at runtime: `TimelineLibraryAuthoring` in a subscene bakes `game.tlib` bytes into a blob; system builds `TimelineWorld` + publishes `LibraryView`; editor watches the `.tlib` and reloads between steps. Gameplay API (Burst-safe statics): `TimelinePlayer.Start/Stop/StopChannel/IsPlaying`, `TimelineSlots.Get`, e.g. input system queries `(RefRO<InputStateData>, DynamicBuffer<TimelineSlot>, DynamicBuffer<Playback>)` and starts `TimelineSlots.Get(slots, (ushort)Skill.Jump)`.

### 4.9 Unity editor authoring (tl.unity editor)

1. Create -> Tl -> Timeline Library (once): tick rate, channels, outputs (JSON folder, `game.tlib`, `Timelines.g.cs` path+namespace).
2. Create -> Timeline (Unity asset) per timeline: add **Tl Track**s (inspector picks track type from `ITrack<,>` `TTrack`s, fills fields), add clips (inspector offers clip types paired with that track, fills fields), add **Tl Marker**s. Hand-written one-class-each assets (`TlLibraryAsset`, `TlTrackAsset : TrackAsset` with `TrackType+DataJson`, `TlClipAsset : PlayableAsset` with `ClipType+DataJson`, `TlMarker : Marker` with `MarkerType+DataJson`); inspectors draw fields by reflection (numbers/bools plain, enums dropdown, `TimelineId` library-name dropdown, nested structs nested, fixed buffers read-only v1). Stored JSON is exactly JSON v2 `data`. One generic class each because Unity asset classes need a `.cs` per `ScriptableObject`; P6 atom 1 verifies on target Unity, fallback = editor command writing per-type `.cs` files.
3. Add timeline to library list (name defaults to asset name; loop/next dropdown/hold/channel dropdown). Save exports JSON v2 + bakes (on save debounced, Tl -> Bake Library menu, before every player build). Errors point at asset/track/clip.
4. In a subscene add **Timeline Player**: slot list (slot-enum key -> timeline name) + start-with list + game's own authoring components for data clips touch.

Exporter mapping (`TlJsonExporter`): entry name -> `name`; `duration*tickRate` rounded (override wins) -> `duration`; loop/next/hold/channel same keys; non-muted `TlTrackAsset`s in `GetOutputTracks()` order (groups flattened) -> `tracks[]` (authored order = execution order); `TrackType+DataJson` -> `type(+namespace)+data`; clips by start -> `clips[]` (`start=round(s*rate)`, `end=max(start+1,round(e*rate))`); two overlaps kept (linear blend; custom mix curve warns); `clipIn!=0`/`timeScale!=1`/extrapolation/>2 overlaps = error; `TlMarker` -> `markers[]`; optional `TlSubTimelineClip` (P7) inlined/shifted/cut. Exporter writes one deterministic JSON per timeline into the library folder; check them in (diffable; CI bakes from them). Bake runner (`TlBakeRunner`): runs bundled `tlb` (`Packages/com.tl.unity/Tools~/tlb/<os>/tlb`) as `tlb library <json> --assembly Library/ScriptAssemblies/Game.Logic.dll --out <tlib> --ids <ids.cs> --namespace <ns> --diagnostics json`; maps diagnostics to assets by exported names. Entity authoring (`TimelinePlayerAuthoring`+baker): library + slots + start-with fields; baker adds `Playback`/`TimelineSlot`/`TimelineRequest` buffers; validation baking system collects reachable timelines (start-with, slots, `next` closure, `TimelineId` fields in clips), maps pairs->consumers->systems->required columns, and every missing component is a baking error naming GameObject+timeline+track+component. This answers "what do I bake on this entity".

### 4.10 Performance targets (both TFMs — D13 binding)

Same machine as baseline; LiveBatch shapes ported to BenchmarkDotNet through the real generator; every shape measured on `net10.0` **and** `netstandard2.1` (net10 host); SIMD fast path net10-only.

| Shape | Gate (each TFM) | 2.1-vs-10 ratio gate |
|---|---|---|
| A: folded | no regression beyond 3% | <=1.10x |
| B: fold-fed compose, new kernel | <=0.15x today's B | <=1.20x |
| C: frame consumer | <=0.25x today's C | <=1.20x |
| D: four pairs, one system | <=0.70x today's D | <=1.20x |
| 64 consumers, 2 active/tick | <=1.5x same system with 2 consumers | <=1.20x |
| 2 playbacks/entity | <=2.2x 1 playback | same |
| Warm allocation | 0 B both TFMs | n/a |
| Parity | bit-exact vs today's Tl A-D, fwd+bwd, 97 frames | 2.1 == 10 bit-exact |

Unity (tl.unity, Performance Testing package): Burst kernel within 1.5x .NET kernel per playback same machine; state hash identical to .NET Runner over 600 steps.

## 5. Phases

### P0: Record decisions, port LiveBatch receipts

- Depends: none. Owns: parent+child issues, `benchmarks/LiveBatch/`.
- Atoms: open parent issue (sections 1-4 + D1-D13 checklist); port `LiveBatch.zip` to `benchmarks/LiveBatch` on BenchmarkDotNet (shapes A, B via ColumnSet `Apply` + `Prepare`/`Play`, C, D, B per 128-row chunk; `--verify` parity arm, raw JSON, PMU where available; consumers via real generator); add two-tracks-of-one-pair test (2.4) and record today's behaviour; measure `src/` byte cost per Appendix A removal.
- Acceptance: receipts committed, parity PASS, owner answered D1-D13.

### P1: Run Tl from a TimelineWorld on .NET Standard 2.1 (+2.1 perf parity foundations)

- Depends: P0 + D3, D4, D9, D12, D13. Owns: `src/Tl.Core/**`, `src/Tl.Gen.CSharp/**` (registration), `tests/Tl.Core.Tests/**`, `Directory.Build.props`.
- Atoms: delete removable surface needing nothing later (`IBake`, `BakeTable`, `BakeRuntime`, `Timeline.Bake/BakeRef`, `ApplyChunk`, generic `Apply<TIndex,...>`, row-list overloads, `ITimelineLane<T>` facade; update tests/benches/README users); introduce `TimelineWorld` owning intern/bank/derived tables (keep static `Timeline<,>` forwards to a default world until P4); generated `ITlModule` per assembly (direct calls) via module initializer + idempotent `TlGenerated.Install()`; multi-target `netstandard2.1;net10.0` (polyfills, `NativeAlloc`, `IncrementalHash`, SIMD `#if NET` + scalar/`Vector<T>` fallbacks, struct-generic dispatch replacing static abstracts, no `allows ref struct`, overloads over `params Span`, no `Unsafe` in 2.1); test suite green on **both** TFMs (2.1 via net10 host `SetTargetFramework`); add `LangVersion 9` project compiling Appendix B payloads against 2.1 build; run shapes A-D on **both** TFMs and post the ratio table.
- Acceptance: core gate green both frameworks; NativeAOT publish passes; shape A within 3% on net10 **and** 2.1-vs-10 ratio <=1.10x; scalar shapes B-D ratios recorded (gate enforced P3, no silent regression vs P0); byte delta posted.

### P2: Build a timeline library with stable ids

- Depends: P1 + D1, D2, D6, D10. Owns: `tools/Tl.Gen.Tlb/**`, `tools/Tl.Bake/**`, `tools/Tl.Tlb/**`, loader+derived tables in `src/Tl.Core`, `docs/tlb1-layout.md`, `docs/library-layout.md`, `docs/library-view-abi.md`.
- Atoms: JSON v2 parse (`next/hold/channel/markers`, enum names, `TimelineId` names second pass vs manifest; located diagnostics + goldens); TLB v6 (header next/channel/hold + marker section; update layout doc/fixtures/`TimelineRef.Validate`); `tlb library` verb (manifest, packing+dedup, `game.tlib` format doc, `Timelines.g.cs`, `--diagnostics json`, `--namespace`); self-contained single-file `tlb` per OS as CI artifacts; loader fixture (Game.Logic DLL built against 2.1 Tl.Core bakes); `new TimelineWorld(library)` per-world manifest-order ids (D1) + `Reload`; derived tables at load (per-step fold lanes D10, frame tables per-tick to 2048 else per-stage+map, event index+bits, per-(system,timeline,stage) step lists); `LibraryView` ABI + readers (C# 9, unmanaged, no statics) with required proofs; fuzz library format like `tests/Tl.Fuzz` covers TLB.
- Acceptance: library bytes deterministic cross-OS (golden); frame tables equal `TrySample` per tick+step every fixture both directions; event index equals clip boundaries+markers; lanes equal today's `View` lanes except D10 deltas; corrupt libraries rejected with located diagnostics.

### P3: Play timelines through generated systems (2.1 parity enforced)

- Depends: P2 + D5, D7, D8, D13. Owns: `src/Tl.Gen.CSharp/**` (new emitters), `src/Tl.Core` playback model, tests+benchmarks.
- Atoms: plain-struct `Frame`, `ushort FrameFlags`+`Synthetic`/`Interrupted`, optional `IBlend`, `IMarker/MarkerFrame`; `Playback/PlaybackFlags/PlaybackTable` (native inline slots; Start/Stop/StopChannel/StopAll/IsPlaying/Find + channel/synthetic/interrupt rules); `Requests`+`Events<T>` (native append, deterministic drain) + `EntityRef`; `TimelineWorld.ApplyRequests/Advance` (loop/next/hold/remove/reverse/paused); generator (consumer forms in 4.4, `[TimelineSystem]` structs, assignment, TLGEN9x, C# 9 kernels with per-stage switch dispatch + event dispatch, `Run` wrappers, `ITlModule` system registration); `ExecuteBatch` path (gather folds per block into stack spans); net10 fast path (pure-add fold + 1 playback/entity in order reuses `LaneOps` SIMD); benchmarks through `Run` (LiveBatch shapes + 64-consumer/2-active, 2-playbacks/entity, event-heavy, alternating timelines) on **both** TFMs; Appendix B toolbox as fixtures (stat rewind exact, barrel fires only in window, channel replace interrupts, `next` hands over in one step, two systems reading one timeline advance once).
- Acceptance: every 4.10 gate met **on both TFMs incl. ratio gates**; parity A-D; state hash identical twice + both frameworks; 0 B warm both; NativeAOT passes; checked builds reject mismatched columns; `LangVersion 9` project compiles Appendix B fully.

### P4: Ship Tl 4.0 with one playback API

- Depends: P3 + D3. Owns: old-API removal, README/docs/samples/approvals/release notes, `benchmarks/source_budget.py`.
- Atoms: delete all Appendix A removals + tests/benches (port/retire Numbers+friends with receipts); rewrite README (Data JSON v2, Authoring `tlb library`, System consumers/systems/playbacks/step order, Numbers via `eng/refresh-numbers`); add `samples/Runner` (Game.Logic shared source on Appendix B toolbox + console host + determinism test incl. `Jump_lands_exactly_where_it_took_off`); 3.x->4.0 migration guide (name D10 behaviour change); apply D9.
- Acceptance: full release gate (`AGENTS.md`) incl. package-only consumer validation; samples run.

### P5: Run Tl timelines in Unity ECS with Burst (tl.unity)

- Depends: P4 (or P3 if tl.unity tracks a prerelease). Owns: tl.unity runtime, Tl.Gen.Unity, tests.
- Atoms: `com.tl.unity` layout (Runtime/Editor/Tests/`Tools~`) on 2.1 Tl.Core; `Playback`/`TimelineSlot`/`TimelineRequest` buffers, `TimelineLibrary` singletons, blob baking, `TimelineLibrarySystem` + editor hot reload; `TimelineSimulationGroup` + `TimelineRequestSystem` + `TimelineAdvanceSystem` (Burst parallel/chunk) + `TimelineEventsSystem` + `TimelineEvents.Drain<T>`; Tl.Gen.Unity (column components, Burst `IJobChunk`/system calling shared kernel with masks, `TimelineJobs.<S>` + context params); `TimelinePlayer`+`TimelineSlots` API; module-initializer verification Mono+IL2CPP (call `TlGenerated.Install()` anyway); tests (play-mode per 4.6 rule, Burst compile, Unity Runner hash == .NET Runner 600 steps, perf receipts).
- Acceptance: Unity gates in 4.10 met; IL2CPP sample runs on one desktop platform.

### P6: Author and bake Tl timelines in the Unity editor (tl.unity)

- Depends: P5 + D11. Owns: Tl.Unity.Editor, authoring components, editor tests, sample assets.
- Atoms: `TlLibraryAsset/TlTrackAsset/TlClipAsset/TlMarker` + reflection inspectors + type dropdowns (verify one-class assumption on target Unity; fallback = per-type `.cs` writer); `TlJsonExporter` (4.9 mapping + `.playable` goldens); `TlBakeRunner` (bundled `tlb`, diagnostics mapping, save/menu/build triggers); `TimelinePlayerAuthoring`+baker+validation baking system (missing-column errors); sample: fully editor-authored Runner whose exported JSON bakes in CI to byte-identical `game.tlib`.
- Acceptance: 4.9 workflow start-to-finish in clean project; validation errors name GameObject+timeline+track+component; Unity bake == CI bake bytes.

### P7: Later (separate issues; not started here)

Editor preview scrub, fixed-point clock/time-scale in `Playback.Reserved`, bake-time folding alternative to D2, rollback/networking snapshot helpers, `TlSubTimelineClip` flattening, Blender bridge to JSON v2.

## 6. What proves what

| Property | Proof | Phase |
|---|---|---|
| Same results as today | LiveBatch parity A-D fwd+bwd | P0, P3 |
| Frame tables right | per-tick+per-step equality with `TrySample` every fixture both directions | P2 |
| Events right | event index == clip boundaries; stat `+d` then rewind returns to start | P2, P3 |
| Determinism | state hash twice, both frameworks, .NET vs Unity | P3, P5 |
| 2.1 parity | 4.10 ratio gates both TFMs; 2.1 == 10 bit-exact | P1, P3, P5 |
| No warm allocation | MemoryDiagnoser 0 B both TFMs | P3 |
| Burst compat | Burst compile tests for kernels + `LibraryView` readers | P5 |
| Unity compiles game code | `LangVersion 9` project compiles Appendix B vs 2.1 Tl.Core | P1, P3 |
| Library robustness | fuzzing + rejection tests | P2 |
| Authoring round trip | Unity Runner == CI JSON bake byte-for-byte | P6 |
| Entity setup | missing columns are baking errors | P6 |

## 7. Risks

- Source budget: additions cannot land before deletions. P3 overrun -> file D9 with measured delta, do not compress code.
- Per-entity indirection may lose to dense columns for 1-playback/entity crowds; P3 fast-path atom covers it, gated by shape A.
- D10 changes behaviour for multi-track fold-fed consumers; P0 test records today, migration guide names it.
- Unity specifics: module initializers under IL2CPP (mitigated by `Install()`), `ScriptableObject` assumption in 4.9, `tlb` loading Unity-compiled Game.Logic (P2 fixture). Verify before relying.
- Non-Burst-safe consumer code (managed/exceptions) is the game's responsibility; Tl.Gen.Unity reports what it can at generation time.
- 2.1 parity risk: `Vector<T>`/scalar fallbacks may trail AVX2 on wide shapes; the ratio gate binds scalar shapes, SIMD stays a net10-only bonus documented in Numbers, never presented as latency.

## 8. Glossary

Timeline = designer data (duration/loop/next/hold/channel/tracks/markers). Track/clip = row / timed payload segment. Pair = (track, clip) combo. Consumer = game code for a pair/marker. Toolbox = game's clip types + consumers. Library = `game.tlib` (all timelines + manifest). `TimelineId` = dense id in loaded library (manifest order). Playback = one timeline playing on one entity. Channel = exclusive lane per entity (none stacks). Column = per-entity data (`Span<T>` in system). Context = shared data (non-span field). System = `[TimelineSystem]` struct. Step = requests, systems, advance. Fold lane = per-tick precomputed results at load. Frame table = resolved clip data per tick/stage+step at load.

## Appendix A: API today -> 4.0

| Today | 4.0 |
|---|---|
| `TimelineAsset.Load/Of/Dispose/Index/Reference` | `new TimelineWorld(library)`, `TimelineId` consts, `Reload` |
| `Timeline<TTrack,TClip>.Apply` all overloads, `ApplyChunk`, generic `Apply<TIndex,...>`, row-list + shared-clock overloads | `system.Run(world, players)` |
| `Timeline.Advance`, `Timeline<TTrack,TClip>.Advance` all overloads | `world.Advance(players)` |
| `ColumnSet` | `[TimelineSystem]` fields |
| `Prepare/Play/Release/BankState` | removed |
| `IBake<T>`, `Timeline.Bake/BakeRef`, `BakeRuntime<,>` | removed; host code + systems cover it |
| `Timeline.SealConsumers` | removed; registry immutable after install |
| `PairRuntime<,>` function-pointer registration | `ITlModule` (generator ABI v2) |
| `Frame<,>` ref struct | `Frame<,>` readonly struct |
| `FrameFlags : byte` | `FrameFlags : ushort` + `Synthetic/Interrupted` |
| `TTrack : IBlend<TClip>` required | `IBlend` optional |
| `ITimelineLane<T>`, `Timeline<T>` lane facade | removed (kernels internal) |
| `TimelineComponent/FrameQuery/Timeline.Query/TickFrame` | `world.Sample<TTrack,TClip>(id, tick, step)` for tools |
| `TimelineState/TimelineMovement` | internal |
| `SlotView/LaneSegment` | `LibraryView` ABI v1 |
| `TrySample/SampleClip/View/Inspection` | kept, moved onto `TimelineWorld` |

Counts above match the scout audit (Advance 6, Bake/BakeRef 9, Apply* 12, lane Apply/Advance 2+2).

## Appendix B: example toolbox (game side; samples + tests use it)

Payloads: `Motion/Position/JumpPower/Stats(+Stat enum)/InputState(+Button)/Sound/Anim/Foot/Cue/StompHit/HeadIndex(simple loop index)/Skill([TimelineSlotKeys] Jump/Slide/Hurt)`; tracks+clips `Move/MoveClip, Arc(+IBlend)/ArcClip, Stat/StatClip, Input/InputWindowClip(Button+TimelineId OnPress), Stomp/StompClip(Radius), Cue/CueClip, FootstepMarker`. Consumers: `Move` (adds `Direction*clip`), `Jump` (`Fold lift=Direction*Velocity*Scale`, `ExecuteActive m.Y+=lift*power`), `ChangeStat` (`OnClipStart` adds `Direction*Delta`), `InputWindow` (`ExecuteActive` requests `OnPress` if pressed forward), `Stomp` (index lookup -> `Events<StompHit>`), `Cues` (`OnClipStart` -> `Events<Cue>`), `Footsteps : IMarker` (`OnMarker` -> cue). Systems `Movement(Motion/JumpPower/Stats/Input/Requests/Cues)` + `Combat(Position/Heads/Hits)`; `Stomp` lands in Combat only. Runner timelines: run 40 loop locomotion (Move/Cue/markers); jump 30 next=run locomotion (Arc/Stat-1/Cue); slide 24 next=run locomotion (Move/InputWindow Attack->barrel_explode/Cue); barrel_explode 20 action (Cue Boom/Stomp); potion_hop 12 no-channel (Arc). Full struct listing lives in the parent issue; the `LangVersion 9` project must compile it against the 2.1 build.
