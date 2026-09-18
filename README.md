# tl

**tl** compiles designer-authored timeline data into deterministic execution — JSON in, baked `.tlb` bytes out, advanced by a small unmanaged runtime on .NET and Unity.

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

Try the [interactive playground/cookbook](https://iafahim.github.io/tl/) — live authoring-JSON editing and timeline playback in the browser, with [embeddable recipe pages](https://iafahim.github.io/tl/cookbook/).

Data and behavior are separate. Designers author tracks, clips, windows, and loop points as JSON; the `tlb` compiler produces canonical TLB1 bytes; typed C# consumers fold what each active `(track, clip)` pair does into a borrowed float column. No reflection, no delegates on warm paths, no runtime compilation, 0 B per frame.

- **Deterministic bake** — same inputs, same bytes, on every machine and culture; content-keyed cache hits preserve timestamps
- **Heterogeneous assets** — mixed track/clip types in one asset; execution order is authored order (A-B-A preserved)
- **One playback API** — `TimelineAsset.Load` returns a `ushort`; everything after that is `Timeline<TTrack,TClip>.Advance`
- **Typed frame queries** — read-only stage views over the currently selected step; they never advance time
- **Fast** — 0.15-0.19 ns/row on grouped rows, 0 B warm (receipts below)
- **NativeAOT-safe** — no generator assemblies in application output; one shared domain file compiles for .NET and Unity

| Environment | Install | Status |
| --- | --- | --- |
| .NET 10 (JIT and NativeAOT) | NuGet packages (below) | receipted |
| Unity 6000+ (Mono, IL2CPP, Burst jobs) | UPM package `com.iafahim.tl` from [tl.unity](https://github.com/IAFahim/tl.unity) | EditMode-receipted |

## Quick Start

### 1. Install

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.9
dotnet tool install --global Tl.Bake --prerelease
```

The bake command is `tlb`. For an offline install, copy the `.nupkg` files into a local `packages/` folder and append `--source ./packages` to both commands. `Tl.CSharp` brings the runtime and the build-time generator, which discovers your consumers on every compilation — including IDE design-time builds — and configures consuming projects automatically.

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended C# install: runtime plus build-time consumer binding |
| `Tl.Runtime` | Small declaration, frame, state, and movement ABI |
| `Tl.Gen.CSharp` | Build-time generator that binds typed consumers |
| `Tl.Bake` | `dotnet tool` (command: `tlb`): JSON to baked TLB1 assets, with watch mode, cache, report, and strip |

### 2. Define the domain

You author unmanaged domain values plus one typed consumer per `(track, clip)` pair. The generator discovers consumers compilation-wide: no registration, catalog, or schema marker.

```cs
using Tl;

namespace Combat
{
    public readonly struct DamageClip
    {
        public readonly float Amount;
        public DamageClip(float amount) => Amount = amount;
    }

    public readonly struct DamageTrack : IBlend<DamageClip>
    {
        public readonly float Multiplier;
        public DamageTrack(float multiplier) => Multiplier = multiplier;

        public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
            => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
    }

    public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
    {
        public static void Execute(in Frame<DamageTrack, DamageClip> frame, ref float health)
        {
            var amount = frame.Clip.Amount * frame.Track.Multiplier;
            health += frame.Direction * amount;
        }
    }
}
```

Track values hold immutable settings, clip values immutable authored payload. A consumer writes exactly one `ref float` effect column and self-inverts through `Frame.Direction`, so rewind is bit-exact.

### 3. Author and bake one timeline

`boss.json` — flat schema v1. `namespace`+`type` name the C# types above; `data` field names map onto struct fields; windows are half-open `[start, end)`; execution order is authored clip order. Two clips overlapping on one track blend through `IBlend<TClip>` with the authored factor:

```json
{
  "name": "boss_phase_one",
  "duration": 8,
  "loop": true,
  "tracks": [
    {
      "name": "main_damage",
      "namespace": "Combat",
      "type": "DamageTrack",
      "data": { "Multiplier": 2.0 },
      "clips": [
        { "namespace": "Combat", "type": "DamageClip", "start": 0, "end": 5, "data": { "Amount": 5 } },
        { "namespace": "Combat", "type": "DamageClip", "start": 3, "end": 8, "data": { "Amount": 9 } }
      ]
    }
  ]
}
```

Bake with `tlb`, pointing `--assembly` at the compiled domain DLL so the baker resolves the authored type names. **The assembly name must match the assembly that ships those types at runtime** — pair keys hash assembly-qualified names. Baking is deterministic; cache hits preserve timestamps:

```sh
dotnet build -c Release
tlb boss.json boss.tlb --assembly bin/Release/net10.0/MyApp.dll --cache ~/.tlbcache
```

| invocation | purpose |
| --- | --- |
| `tlb <input.json> <output.tlb> [--assembly <dll>]... [--cache <dir>]` | bake one timeline |
| `tlb --watch <input.json> <output.tlb> [--assembly <dll>]... [--debounce <ms>]` | re-bake on save |
| `tlb --watch <input-dir> [<output-dir>] [--assembly <dll>]...` | watch every `*.json` in a directory |
| `tlb --json --assembly <dll>...` | print the authorable `(track, clip)` pairs the assemblies expose |
| `tlb --report <input.tlb>` | byte and section size audit |
| `tlb --strip <input.tlb> <output.tlb>` | drop authoring metadata for distribution |

**Watch mode** is the designer loop: bake every input at startup, re-bake on save (debounced, hash-skipped), one JSON event line per action (`ready`, `rebuild`, `skip`, `diagnostic`); a bake error stays in the loop as a `diagnostic` event until the JSON is fixed. `tlb --json --assembly ...` lists every blendable unmanaged pair with member names, so authoring tools fill type lists automatically — the Blender bridge (`tools/Tl.Blender`) uses it exactly that way.

The bake stores each distinct string once in a single ordinal-sorted pool, each distinct type pair once, and (TLB1 v3) each distinct track/clip payload once per pair in fixed-width `ushort`-indexed value pools — repeated payloads cost one pool entry. More than 65,535 unique values in one pool is a bake diagnostic naming the type. Assets from earlier alphas (v1/v2 layouts) are rejected at load with a diagnostic; re-bake them.

### 4. Play it back — the fast path first

#### Load once

`TimelineAsset.Load` is the whole acquisition step: a cold validated import that interns the bytes content-addressedly and returns the timeline index — a dense `ushort` that is the timeline's playback identity everywhere. The same bytes always land on the same index; each `Load` holds one acquisition; releasing the last one reclaims the native block under the next load. `TimelineAsset.Of(index)` is the metadata/dispose view.

```cs
ushort boss = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
```

#### Fastest: advance one timeline's rows per call

`Timeline<TTrack, TClip>` is the only public playback API. Resolution is implicit and lazy: the **first typed use** of an index for a pair folds every registered consumer's effect for every position into the pair's process-lifetime bank; later calls are a folded-table read. The most performant shape is one call per distinct timeline index over its grouped rows — the natural ECS query-loop shape — at **0.18-0.19 ns/row**:

```cs
var positions = new ushort[] { 0 };
var health    = new float[] { 100f };

Timeline<DamageTrack, DamageClip>.Advance(boss, positions, true, health);   // one frame forward
Timeline<DamageTrack, DamageClip>.Advance(boss, positions, true, health);   // catch-up: repeat calls
Timeline<DamageTrack, DamageClip>.Advance(boss, positions, false, health);  // rewind: exact inverse
```

Every advance moves each row exactly one frame: finite assets clamp at duration, looping assets wrap, rows sharing a position collapse into one vector run.

#### Jumping, catching up, rewinding

The clock is your column — jumping is writing it. Effects are a function of position, so a teleport changes future deltas without replaying intermediate frames; catch-up is repeated single-frame calls so every system observes every tick; rewind is `forward: false` and returns columns bit-exactly.

```cs
positions[0] = 40;                                                    // jump row 0 to frame 40 (loops wrap at duration)
while (lagFrames-- > 0)                                               // catch-up, one tick per call
    Timeline<DamageTrack, DamageClip>.Advance(boss, positions, true, health);
Timeline<DamageTrack, DamageClip>.Advance(boss, positions, false, health);   // rewind one tick
```

Loop and completion counts come from the movement ABI (`FrameFlags.TimelineEnd` on wrapping ticks, sign in `Reverse`, `CompletedAfter`/`CompletedBefore` on finite ones) or trivially from the position column (`old == duration - 1 && new == 0` forward) — receipted in `tests/Tl.Alpha` (`WrapCounts`). There is deliberately **no multi-frame step parameter** and never will be: a 50-tick skip would hide 49 intermediate states from other systems, and repeated folds stay bit-exact where a precomputed K-frame sum can round differently (owner decision).

#### Mixed crowds: many timelines, one call

Several timelines of the same pair play side by side through one per-pair bank: every index folds in on first use, all tables live in contiguous native blocks, and one call advances the whole crowd against a per-row index column. Rows grouped by index run at the fast-path cost; ids alternating per row are the slowest shape (~5x) — group rows by timeline when you can (archetype order usually does):

```cs
ushort boss  = TimelineAsset.Load(File.ReadAllBytes("boss-jump.tlb"));
ushort grunt = TimelineAsset.Load(File.ReadAllBytes("grunt-jump.tlb"));
ushort elite = TimelineAsset.Load(File.ReadAllBytes("elite-jump.tlb"));

var timelineIds = new ushort[] { boss, grunt, grunt, elite };
var lastTick    = new ushort[] { 0, 0, 2, 5 };
var health      = new float[4];

Timeline<DamageTrack, DamageClip>.Advance(timelineIds, lastTick, true, health);  // fused crowd call
Timeline<DamageTrack, DamageClip>.Seek(timelineIds, lastTick, true).Apply(health); // two-step form

var bossTicks  = new ushort[] { 0, 2 };   // boss's rows only — no index column needed
var bossHealth = new float[2];
Timeline<DamageTrack, DamageClip>.Advance(boss, bossTicks, true, bossHealth);
```

Indices come only from `TimelineAsset.Load`; equal-length index, position, and effect columns are the whole crowd call.

#### Attach host markers with `Timeline.Bake`

Consumers stay pure and host-agnostic; host wiring is declared, not registered. A bake declaration is a struct implementing `IBake<TConsumer, ...TContext>` — the first argument names the consumer, the remaining zero to four are host context types (the consumer ABI's pointer-slot cap; only `IBake`1..`5` exist), on its own struct or on the consumer itself:

```cs
public readonly struct ApplyDamageBake : IBake<ApplyDamage, World, Entity>
{
    public static void Bake(ApplyDamage consumer, World world, Entity entity)
        => entity.Add<DamageTag>();
}

public readonly struct Heal : ITrack<HealTrack, HealClip>, IBake<Heal, World> { ... }
```

The generator discovers every closed `IBake` instantiation, validates the shape (one accessible static `void Bake`, consumer by value, then one by-value context parameter per declared type, in declared order — TLGEN70-73 diagnose every invalid shape), and installs the dispatch into the cold unmanaged bake table at module init. No reflection, no runtime compilation, nothing GC-visible; the warm path is untouched. Then one type-agnostic call attaches the markers of a loaded timeline:

```cs
ushort boss = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
Timeline.Bake(boss, world, entity);
```

`Timeline.Bake(id, args...)` walks the timeline's pair keys in baked order and runs every installed bake whose declared context list the arguments satisfy: subset satisfaction (every declared context type must appear among the argument types), first-argument binding (declaration order wins over caller order), exact `TypeKey` identity (no assignability), zero-context bakes run unconditionally, missing contexts keep the bake silent, extra argument types are ignored. Repeated calls are deterministic; dead indices throw the intern-table diagnostic.

Playback then stays marker-gated — absence is structurally impossible inside the loop, because a marker exists only if this timeline was baked with that pair:

```cs
foreach (var chunk in world.Chunks<DamageTag, TimelineIndex, Tick, Health>())
    Timeline<DamageTrack, DamageClip>.Advance(chunk.Indices, chunk.Ticks, forward, chunk.Health);
```

The engine deliberately has **no absence API**: a pair's columns contain only rows that have the pair (the host's own structure), and the first typed use of an index lacking the pair is a loud located diagnostic — a host wiring error, never designer data. One pair may be served by several consumers (registered through `PairRuntime<TTrack, TClip>.Consume`, folded in registration order — receipted by `BakedLaneFoldsConsumersInRegisteredOrder`); cross-pair order is the host's call order. The matching matrix and end-to-end flow are pinned by `BakeWalkTests` (`tests/Tl.Core.Tests`) and the generator fixture in `tests/Tl.Gen.CSharp.Consumer.Tests`.

#### Read without advancing: typed frame queries

The same bytes drive a read-only stage view of a row's currently selected step. It never advances `Position`; repeated queries return identical frames; gaps and clamped-completed positions yield nothing; reverse movement re-observes stages in reverse; frames exist only during `Execute` — never retain their borrowed references.

```cs
using var asset = TimelineAsset.Of(boss);

foreach (var frame in Timeline.Query<DamageTrack, DamageClip>(
             new TimelineComponent(asset.Reference) { Position = positions[0] }))
    ApplyDamage.Execute(in frame, ref health[0]);
```

**Migrating from the two-path alpha** (one public playback API, [#174](https://github.com/IAFahim/tl/issues/174)):

| alpha.8 spelling | index spelling |
| --- | --- |
| `BakedLane<T,C>.Bind(asset)` / `Timeline<T,C>.Bind(asset)` | `ushort index = TimelineAsset.Load(bytes)` — resolves on first typed advance |
| `Timeline<T,C>.Slot(asset)` / `Slot(asset, measured)` | removed with [#180](https://github.com/IAFahim/tl/issues/180): load returns the index; resolution is implicit |
| `Timeline<T,C>.Advance(asset, positions, fwd, effects)` | `Timeline<T,C>.Advance(index, positions, fwd, effects)` — or the `TimelineAsset.Of(index)` scalar view |
| `TimelineSet<T,C>` public surface | internalized; `Timeline<T,C>.Seek/Advance(indices, ...)` is the crowd spelling |
| `Timeline<BakedLane<T,C>>.Seek(positions, fwd).Apply(effects)` | `Timeline<T,C>.Advance(index, positions, fwd, effects)` |

## Generated reports

Generated sources live inside the generator during normal compilation. For a standalone, content-stable snapshot (review, another pipeline, size inspection):

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release
```

The export writes generated `.g.cs`, a manifest, and `TlGenCompile.report.txt` under `obj/Release/<tfm>/TlGenCompile`; a repeated identical invocation is a cache hit and preserves timestamps.

## Performance

The hot path is allocation-free after warmup; `benchmarks/Alpha --verify` checks lane, data-authored, and allocation receipts. Throughput below is one million rows per call, one frame per call (Intel i9-14900K, .NET 10, Release; best of 5 reps of 20 frames over 3 interleaved rounds; every shape bit-exact against per-asset lanes forward and backward, 0 B warm) — ordered fast to slow. `benchmarks/TypedPlaybackProto` prints checksum-verified parity at 100k and 1M rows; `samples/ManyEntities` reproduces consumer-facing numbers.

| workload | ms/frame | ns/row |
| --- | ---: | ---: |
| static hand lane, waves of 100 | 0.14 | 0.13 |
| static hand lane, uniform clocks | 0.16 | 0.15 |
| **one timeline index, grouped/waves of 100** | **0.19** | **0.18** |
| 100 timelines, id blocks of 100, waves of 100 | 0.21 | 0.20 |
| one looping timeline, staggered clocks | 0.24 | 0.22 |
| finite timeline, staggered clocks | 0.34 | 0.32 |
| plain floor (`effects += 1; positions += 1`) | 0.41 | 0.39 |
| 16 timelines, id blocks of 16, waves of 100 | 0.50 | 0.48 |
| ids alternating per row, staggered clocks | 1.80 | 1.72 |

Crowd-shape envelopes from `benchmarks/PairHandles` (100k rows, [#180](https://github.com/IAFahim/tl/issues/180) rerun): 0.18-0.19 ns/row one index for the batch; 0.32 with eight variants grouped; 0.86 with eight variants alternating per row; wave positions 0.17-0.18 grouped, 0.20 in index/position blocks of 100, 0.67-0.69 alternating. The retired single-bound statics gold path measured 0.16 ns/row. **How to stay fast: group rows by timeline index** — ECS archetypes cluster identical rows for free; the per-index advance is the product shape.

**Bake and load** (20 MB authoring corpus, `tools/Tl.Bake.Bench`, same host): bake **179.7 ms** end to end (read 2.8, parse 149.1, bake 24.5) — the legacy DOM path measured 3,393 ms (18.9x); managed allocation 111 MB (was 189). `TimelineAsset.Load` of the 15.8 MB result: 2.9 ms. One `MeasuredLanes.Measure` of the 65,500-tick corpus asset: 3.9 ms, once per load.

**Memory.** The host owns 8 B/row of caller columns (index 2 B, position 2 B, effect 4 B — 8 MiB at one million rows); `TimelineState` is 8 B, `TimelineComponent` 16 B, each movement record 8 B. Each timeline's measured tables live in the pair bank's one contiguous native block: `28 * (duration + 1) + 48` bytes — 1,868 B at duration 64, 1.75 MiB at the 65,535-tick cap. The warm path allocates 0 B in every lane; a 256-track module folds to one 34,688-effect column per tick (`tests/Tl.Alpha --module-capacity`). The repository enforces a 300,000-byte budget over production source; generated source, static data, and per-entity state are measured separately.

## tl in an ECS host

The warm path is column-native, so an archetype ECS (Unity DOTS, Frent, any chunk- or SoA-based job system) embeds tl without adapters: entities are rows, component arrays are the columns, and the runtime keeps no per-row state of its own. Prototype history with a real ECS host: [IAFahim/FrentFun](https://github.com/IAFahim/FrentFun).

Lifecycle: **bake once** (JSON → `.tlb`; duration above the 65,535-tick cap is rejected at bake) → **load** (interned `ushort` index) → **attach markers** (`Timeline.Bake(index, contexts...)`) → **resolve lazily** (first typed use folds the pair bank; a timeline lacking the pair is a loud diagnostic) → **advance per frame**:

```cs
foreach (var chunk in world.Chunks<TimelineIndex, Tick, Health>())
    Timeline<DamageTrack, DamageClip>.Advance(chunk.Indices, chunk.Ticks, forward, chunk.Health);
```

- The three columns are the host's own storage (archetype chunk arrays or SoA), borrowed as spans for the call only; the lane never moves, copies, or retains row data; 0 B warm.
- Row order never matters — each row carries its own index and clock, so archetype splits and moving entities are just rows. An asset declaring more than 256 pairs is rejected at fold; an unknown or unloaded index throws naming the row; `Apply` validates column lengths and pairwise non-overlap.
- `Apply` accumulates deltas into the effect column and commits the next position in place; skipped rows (past-end finite, boundary positions) are total no-ops.
- Behavior that needs more than the folded effect reads the typed frame query — a function of position only: no game tick, no other entities' columns.
- One registry exists by owner decision ([#150](https://github.com/IAFahim/tl/issues/150), [#180](https://github.com/IAFahim/tl/issues/180)): the cold, load-time intern table mapping baked bytes to dense `ushort` indices — process-lifetime unmanaged state, never on the warm path. Everything the playback kernels touch is unmanaged, which is what keeps tl loadable by ECS/Burst job compilation.

## Unity ECS

Install the UPM package (Package Manager → *Add package from git URL*): `https://github.com/IAFahim/tl.unity.git?path=com.iafahim.tl`. Unity compiles the shared `src/Tl.Core` sources directly (NuGet for .NET, UPM for Unity); the full walkthrough lives in [tl.unity's END-TO-END.md](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md). A coordinator advances `TimelineComponent` rows and your system consumes typed frames:

```cs
foreach (var (timeline, resistance, health) in
         SystemAPI.Query<RefRO<TimelineComponent>, RefRO<Resistance>, RefRW<Health>>())
    foreach (var frame in TimelineEcs.Query<DamageTrack, DamageClip>(in timeline.ValueRO))
        ApplyDamage.Execute(in frame, in resistance.ValueRO, ref health.ValueRW);
```

## Scope

- Track and clip types are closed at build time; introducing executable types requires recompilation
- Asset track/clip `namespace` names are a single bare segment; dotted namespaces diagnose
- Half-open clip windows `[start, end)`; execution order is authored clip order
- Unmanaged track settings, clip payloads, and component slots
- Arbitrary looping deltas perform every observable effect and are proportional to the requested work
- Designer GUI authoring, C asset consumption, and runtime-loaded arbitrary schemas are deferred

## Repository map

| Path | Role |
| --- | --- |
| `src/Tl.Core` | Runtime declarations, asset import, frames, state, movement, and the typed playback lane |
| `src/Tl.Gen.CSharp` | C# consumer discovery, typed binding, and export tool |
| `src/Tl.CSharp` | One-package C# installation |
| `samples/Mixed` | Data-authored timeline sample on the typed lane |
| `samples/NuGetQuickStart` | Runnable quick start consuming the published nuget.org packages; CI runs it on every build |
| `samples/ManyEntities` | Typed lane vs hand SoA lanes with per-entity clocks; `dotnet run -c Release` prints the ns/row parity sweep |
| `tools/Tl.Gen.Tlb` | Baking library: TLB1 writer, metadata pools, cache keys, size reports |
| `tools/Tl.Bake` | `tlb` CLI: JSON-to-TLB1 bake, `--watch`, `--json`, `--strip`, `--cache`, `--report` |
| `tools/Tl.Bake.Bench` | Bake-path receipts: corpus generator, byte-identity parity, per-stage timing |
| `tools/Tl.Blender` | Blender >= 5.0 NLA bridge: exports the flat schema and bakes through the `tlb` CLI |
| `tools/Tl.Playground` | Source of the live playground linked above |
| `tests/Tl.Alpha` | Typed-lane, data-authored, and allocation receipts |
| `tests/Tl.PackageConsumer` | Isolated package-only JIT and NativeAOT consumer |
| `benchmarks/Alpha` | Oracle and verification evidence for data-authored lane shapes |
| `benchmarks/PairHandles` | Pair lane receipts: parity vs per-asset lanes, throughput across crowd shapes |
| `benchmarks/TypedPlaybackProto` | Typed lane parity and throughput at 100k-1M rows |

The Unity host package (`com.iafahim.tl`) lives in [tl.unity](https://github.com/IAFahim/tl.unity), published under the MIT license decided in issue #64. Standalone probes under `benchmarks/` (FusedAdvance, ValuePoolFormat) document shipped surfaces; every other `benchmarks/*` directory is commit-scoped history of removed surfaces.

## Documentation

This README is the documentation. Unity walkthrough: [tl.unity](https://github.com/IAFahim/tl.unity). Contributor protocol: [AGENTS.md](AGENTS.md). Vulnerability reporting: [SECURITY.md](SECURITY.md).

## Contributing

Read [AGENTS.md](AGENTS.md) before changing code. Open an issue first for any public ABI, plan-schema, semantic, package-boundary, or benchmark-fixture change so the invariant and migration cost are visible. Keep pull requests focused on one observable result, stating trigger, previous behavior, resulting behavior, validation, size delta, and target limitations; performance work includes exact receipts and a same-machine baseline/candidate comparison. Run the validation block in AGENTS.md before requesting review.

## License

[MIT](LICENSE) — decided by the repository owner in [issue #64](https://github.com/IAFahim/tl/issues/64).
