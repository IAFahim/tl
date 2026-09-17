# tl

**tl** compiles designer-authored timeline data into deterministic execution — JSON in, baked `.tlb` bytes out, advanced by a small unmanaged runtime on .NET and Unity.

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

Try the [interactive cookbook/playground](https://iafahim.github.io/tl/) — live authoring-JSON editing and timeline playback in the browser, with [embeddable recipe pages](https://iafahim.github.io/tl/cookbook/).

Timeline data and timeline behavior are separate. Designers author tracks, clips, windows, and loop points as JSON; a deterministic `tlbake` compile produces canonical TLB1 assets; typed C# consumers (`ITimelineJob<TTrack,TClip>`) fold what one active `(track, clip)` pair does into a borrowed float effect column. The runtime has no reflection, no delegates on warm paths, no runtime compilation, and no warm-path allocation — the same baked bytes drive .NET and Unity.

- **Deterministic bake** — same inputs, same bytes, on every machine and culture; content-keyed cache hits preserve timestamps
- **Heterogeneous assets** — tracks and clips of different types in one asset; execution order is authored order (A-B-A preserved)
- **Typed playback lane** — `Timeline<T>.Seek(positions, forward).Apply(effects)` advances every row exactly one frame over run-length groups; catch-up is repeated calls, rewind is `forward: false`
- **Bind-time effect tables** — consumers are measured once per (pair, asset); no kernel catalog, no interpreter tier, one execution path
- **Typed frame queries** — a read-only stage view over the row's currently selected step; never advances time
- **NativeAOT-safe** — no generator assemblies in application output; one shared domain file compiles for both .NET and Unity
- **Flawless install** — `dotnet add package` and run; package targets configure consuming projects automatically

## Supported environments

| Environment | Install | Status |
| --- | --- | --- |
| .NET 10 (JIT and NativeAOT) | NuGet packages (below) | receipted |
| Unity 6000+ (Mono, IL2CPP, Burst jobs) | UPM package `com.iafahim.tl` from [tl.unity](https://github.com/IAFahim/tl.unity) | EditMode-receipted |

The packages are development prereleases on [nuget.org](https://www.nuget.org/).

## Quick Start

### 1. Install

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.7
dotnet tool install --global Tl.Bake --prerelease --version 1.0.0-alpha.7
```

For an offline install, copy the `.nupkg` files into a local `packages/` folder and append `--source ./packages` to both commands.

`Tl.CSharp` brings the runtime and the build-time generator, which discovers your consumers on every compilation — including IDE design-time builds — and sets consuming projects up automatically.

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended C# install: runtime plus build-time consumer binding |
| `Tl.Runtime` | Small declaration, frame, state, and movement ABI |
| `Tl.Gen.CSharp` | Build-time generator that binds typed consumers |
| `Tl.Bake` | `dotnet tool` (command: `tlbake`): JSON to baked TLB1 assets |

### 2. Define the domain

You author the domain values and one typed consumer per `(track, clip)` pair. `Tl` supplies `IBlend<TClip>`, `ITimelineJob<TTrack,TClip>`, and `Frame<TTrack,TClip>`; the generator discovers consumers compilation-wide, so there is no registration, catalog, or schema marker.

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

    public readonly struct ApplyDamage : ITimelineJob<DamageTrack, DamageClip>
    {
        public static void Execute(
            in Frame<DamageTrack, DamageClip> frame,
            ref float health)
        {
            var amount = frame.Clip.Amount * frame.Track.Multiplier;
            health += frame.Direction * amount;
        }
    }
}
```

Track values hold immutable settings. Clip values hold immutable authored payload. A lane consumer writes exactly one `ref float` effect column and self-inverts through `Frame.Direction`, so rewind is exact; `BakedLane.Bind` measures the fold once at cold time; position purity is an authoring contract, not a bind-time check (removed with the #113 purity probes).

### 3. Author and bake one timeline

`boss.json` — flat schema v1. Track/clip `namespace`+`type` name the C# types above; `data` field names map onto struct fields; windows are half-open `[start, end)`; execution order is authored clip order:

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
        { "namespace": "Combat", "type": "DamageClip", "start": 0, "end": 3, "data": { "Amount": 5 } },
        { "namespace": "Combat", "type": "DamageClip", "start": 4, "end": 8, "data": { "Amount": 9 } }
      ]
    }
  ]
}
```

Bake with the `tlbake` tool, pointing `--assembly` at the compiled domain DLL so the baker resolves the authored type names. **The assembly name must match the assembly that ships those types at runtime** — pair keys hash assembly-qualified names. Baking is deterministic; cache hits preserve timestamps:

```sh
dotnet build -c Release
tlbake boss.json boss.tlb --assembly bin/Release/net10.0/MyApp.dll --cache ~/.tlbcache
```

`tlbake --report boss.tlb` audits sizes and `tlbake --strip boss.tlb boss.dist.tlb` trims authoring metadata for distribution.

### 4. Bind, advance, and query

The application owns the position and effect columns and the game clock. `TimelineAsset.Load` is a cold validated import; `BakedLane.Bind` measures the per-position effect tables once; dispose the asset after all readers are done.

```cs
using var asset = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
BakedLane<DamageTrack, DamageClip>.Bind(asset);

var positions = new ushort[] { 0 };
var health    = new float[] { 100f };

Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(health);   // one frame
Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(health);   // catch-up call
Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, false).Apply(health);  // rewind
```

Every `Apply` advances each row exactly one frame: rows that would cross duration clamp on finite assets, looping assets wrap, and skipped rows leave their columns untouched. Rows sharing a position form one run — the per-row cost collapses toward a vector add on grouped storage. There is no engine cycle: hosts that need loop counts track wraps from the movement flags (`TimelineEnd` on looping timelines, `CompletedAfter`/`CompletedBefore` on finite ones, sign from `Reverse`) or from the position column, exactly as before — receipted in `tests/Tl.Alpha`.

Several timelines of the **same** pair play side by side through a `TimelineSet`: ids are assigned at load time by `Add` (never authored, never baked), all tables live in one contiguous native block, and one call advances a whole mixed crowd — minions and boss together.

```cs
using var minionAsset = TimelineAsset.Load(File.ReadAllBytes("minion-jump.tlb"));
using var bossAsset   = TimelineAsset.Load(File.ReadAllBytes("boss-jump.tlb"));
var jumps    = new TimelineSet<DamageTrack, DamageClip>();
var minionId = jumps.Add(minionAsset);
var bossId   = jumps.Add(bossAsset);

var timelineIds = new ushort[] { minionId, minionId, bossId };
var lastTick    = new ushort[] { 0, 0, 2 };
var health      = new float[3];

jumps.Gather(timelineIds).Seek(lastTick, true).Apply(health);   // whole crowd, one frame
```

There is deliberately **no multi-frame step parameter** and never will be. A game runs thousands of systems that must all observe every timeline tick — a 50-tick skip would hide 49 intermediate states from them. Lag catch-up is repeated single-frame calls, which also keeps every float fold bit-exact (a precomputed K-frame sum can round differently from K sequential folds). This is an owner decision; see `docs/typed-playback-lane.md`.

The same bytes drive the typed query lane, which reads a row's currently selected stage without advancing it:

```cs
foreach (var frame in Timeline.Query<DamageTrack, DamageClip>(
             new TimelineComponent(asset.Reference) { Position = positions[0] }))
    ApplyDamage.Execute(in frame, ref health[0]);
```

The query is a read-only stage view: it never advances `Position`, so repeated queries return identical frames. A gap or clamped-completed position yields no frames; reverse movement re-observes the same stages in reverse; multiple occurrences of one pair in a step appear in authored order. Frames exist only during `Execute`; consumers must not retain their borrowed references.

## Generated reports

Normal compilation owns generated sources inside the generator. Run the explicit export target when a standalone, content-stable snapshot is useful for review, another build pipeline, or size inspection:

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release
```

The export writes generated `.g.cs`, a manifest, and `TlGenCompile.report.txt` under `obj/Release/<tfm>/TlGenCompile`. A repeated identical invocation is a cache hit and preserves generated content.

## Performance contract

The hot path is allocation-free after warmup. `benchmarks/Alpha --verify` checks lane, data-authored, and allocation receipts; scalar latency and multi-entity throughput are reported separately and never inferred from a partial inner loop.

The repository enforces a 300,000-byte budget over production source contents plus relative UTF-8 paths. Generated source, static data, per-entity state, managed/native output, scratch, and allocations are measured separately.

## Performance

Throughput is the typed lane's product shape; receipts and methodology in
[docs/typed-playback-lane.md](docs/typed-playback-lane.md). `benchmarks/TypedPlaybackProto`
prints checksum-verified parity against a hand lane at 100k and 1M rows, and the runnable sample
under `samples/ManyEntities` reproduces consumer-facing numbers with checksum-verified effects.

One million rows per call, one frame per call, positions and timeline ids distributed as named
(Intel Core i9-14900K, .NET 10, Release; best of 5 reps of 20 frames over 3 interleaved rounds;
every set shape is bit-exact against per-asset static lanes, forward and backward, and allocates
0 B on the warm path):

| workload | ms/frame | ns/row |
| --- | ---: | ---: |
| plain floor (`effects += 1; positions += 1`) | 0.41 | 0.39 |
| static lane, uniform clocks | 0.16 | 0.15 |
| static lane, waves of 100 | 0.14 | 0.13 |
| static lane, staggered singles | 0.19 | 0.18 |
| static lane, uniform, backward | 0.16 | 0.16 |
| set, one timeline, waves of 100 | 0.19 | 0.18 |
| set, 100 timelines, id blocks of 100, waves of 100 | 0.21 | 0.20 |
| set, 64 timelines, id blocks of 64, waves of 100 | 0.28 | 0.27 |
| set, 16 timelines, id blocks of 16, waves of 100 | 0.50 | 0.48 |
| set, one looping timeline, staggered clocks | 0.24 | 0.22 |
| set, ids alternating per row, staggered clocks | 1.80 | 1.72 |
| set, finite timeline, staggered clocks | 0.34 | 0.32 |
| set, one timeline, waves of 100, backward | 0.20 | 0.19 |
| set, one looping timeline, staggered clocks, backward | 0.25 | 0.24 |

Measured before the #113 frame slim (cycles column present); the #113 A/B in
[docs/typed-playback-lane.md](docs/typed-playback-lane.md) re-measured the affected shapes at
parity or better on the same host family (finite staggered 0.38 -> 0.19 ns/row, static
staggered 0.19 -> 0.15-0.17, alternating ids 1.67 -> 1.22), and the cycle column no longer
exists in any shape.

The static lane binds per-tick movement records at load: that trades +3.6% on static waves of
100 for 22.9x/21.7x on static staggered forward/backward
([#104 receipt](https://github.com/IAFahim/tl/issues/104#issuecomment-5692056006)).

Staggered clocks on one looping timeline hold 0.22 ns/row — 1.2x the static lane's own
baked-record staggered path (0.18) on the same ticks — because a 64-row probe routes staggered
chunks to a gather applier folding the effect table over 16 rows per `vgatherps`; backward takes
the same route at the same cost. Crowds whose
ids switch every few rows run the mixed scanner and pay for the id-switch rate, not the crowd
size; grouping rows by timeline id — the ECS norm — keeps every row on a table-shaped path.

Memory (same shapes, after the #113 frame slim): the host owns 8 B/row of caller columns for
a set — timeline id 2 B, position 2 B, effect 4 B (8 MiB at one million rows) — with no cycle
column in any shape; the static lane uses 6 B/row. `TimelineState` is 8 bytes (was 16),
`TimelineComponent` 16 (was 24), and each baked movement record 8 bytes (was 16). Each
timeline's measured tables live in the set's one contiguous native block:
28 * (duration + 1) + 48 bytes — 1,868 B at duration 64, 28,748 B at 1,024, 1.75 MiB at the
65,535-tick cap (was 44 * (duration + 1) + 48). The warm path allocates 0 B in every lane; a
256-track module folds to one 34,688-effect column per tick (`tests/Tl.Alpha --module-capacity`).

## tl in an ECS host

The warm path is column-native, so an archetype ECS (Unity DOTS, Frent, any chunk- or SoA-based
job system) embeds tl without adapters: entities are rows, component arrays are the columns, and
the runtime keeps no per-row state of its own. Prototype history with a real ECS host:
[IAFahim/FrentFun](https://github.com/IAFahim/FrentFun); the shipped contract and receipts:
[docs/typed-playback-lane.md](docs/typed-playback-lane.md).

### Lifecycle: bake once, bind once, advance per frame

1. **Author and bake.** Flat-schema JSON in, deterministic `.tlb` bytes out (`tlbake`, Quick
   Start above). The [Blender bridge](tools/Tl.Blender/README.md) exports the same JSON from NLA
   scenes and bakes through the same CLI, receipted byte-identical to hand authoring. Duration
   above the 65,535-tick `ushort` position cap is rejected at bake with a diagnostic naming both.
2. **Load.** `TimelineAsset.Load(bytes)` is a cold validated import; the owner retains the
   immutable native storage and disposes it after all readers finish.
3. **Measure once per asset.** `MeasuredLanes.Measure(asset)` folds every position's forward and
   backward float effect once each through the cold executor into aligned native tables —
   131,000 evaluations for the 65,500-tick asset of the #108 corpus receipt, 3.9 ms cold, once
   per load. `TimelineSet.Add(asset)` measures internally when the measurement is not shared.
4. **Bind.** `TimelineSet<TTrack,TClip>.Add(asset)` — or `Add(asset, measured)` to reuse one
   measurement across several sets (dispose it after the last `Add`) — appends the effect tables
   plus 8-byte per-tick movement records into the set's one contiguous native block and returns
   the timeline's dense `ushort` id, assigned at load time, never authored, never baked. The
   asset may be disposed once `Add` returns. One asset per pair that never coexists with another
   can skip the set and bind `BakedLane<TTrack,TClip>` instead.
5. **Advance.** One call moves every row exactly one frame over host columns:

```cs
var jumps  = new TimelineSet<DamageTrack, DamageClip>();
var bossId = jumps.Add(bossAsset);            // dense id, assigned by Add at load time

// one loop per component set; rows map to entities in any order
foreach (var chunk in world.Chunks<TimelineId, Tick, Health>())   // your host's chunk iteration
    jumps.Gather(chunk.Ids)                   // ReadOnlySpan<ushort>: each row's timeline
         .Seek(chunk.Ticks, forward)          // Span<ushort>: per-row clock, committed in place
         .Apply(chunk.Health);                // Span<float>: effect deltas accumulate per row
```

Catch-up is repeated single-frame calls; rewind is `forward: false`, and the backward table is
the measured inverse, so forward-then-backward returns columns bit-exactly. There is no
multi-frame step parameter and never will be (owner decision): every system observes every tick,
and repeated folds stay bit-exact where a precomputed K-frame sum can round differently.

### What the host owns

- The three columns are the host's own storage — archetype chunk arrays or SoA — borrowed as
  spans for the `ref struct` call only. The lane never moves, copies, or retains row data, and
  warm playback allocates 0 B. Per row that is 8 B of host state: timeline id 2 B, position 2 B,
  effect 4 B.
- Row order never matters; each row carries its own id and clock, so archetype splits and moving
  entities are just rows. Per timeline, the set holds `28 * (duration + 1) + 48` bytes of tables
  and movement records in its block (1,868 B at duration 64, 1.75 MiB at the 65,535-tick cap);
  `TimelineComponent` on coordinator hosts is 16 B.
- No registry and no process-global row identity sit on the warm path: ids are per-set ordinals,
  tables live in the set's native block, and the runtime data path is unmanaged — the property
  that keeps tl loadable by ECS/Burst job compilation. The lane above is the .NET throughput
  path; Unity DOTS hosts use the UPM package's coordinator and typed-query shape (next section).
- Skipped rows (finite timelines past the end, boundary positions) are total no-ops: no fold, no
  movement, columns untouched. `Apply` validates column lengths and pairwise non-overlap; an
  unbound id throws naming the row; an asset declaring more than 256 pairs is rejected at bind.

### What the host does with outputs

- `Apply` accumulates deltas into the effect column and commits the next position in place.
  There is no cycle column (removed in #113): a host that wants loop counts derives them from
  the movement flags — `FrameFlags.TimelineEnd` marks the wrap tick on looping timelines with
  the sign in `FrameFlags.Reverse`, and `CompletedAfter`/`CompletedBefore` carry it on finite
  ones — or from the position column (`old == duration - 1 && new == 0` forward). Receipted
  against the removed engine cycle on a randomized schedule in `tests/Tl.Alpha` (`WrapCounts`).
- The fold is a function of position only — no game tick, no other entities' columns. Behavior
  that needs more reads the typed frame query (`Timeline.Query`), a read-only view of the row's
  selected stage that never advances time; its consumers take `in` reads and `ref` writes, and
  the generator derives the roles from `Execute`.
- Lane consumers declare `static void Execute(in Frame<TTrack,TClip>, ref float effect)`. The
  build discovers them compilation-wide (`Tl.Gen.CSharp`, inside `Tl.CSharp`) — no
  registration, no catalog, no schema marker.

### What adoption costs

Warm numbers are rows of the [Performance](#performance) table above (one million rows,
i9-14900K, best of 5 over interleaved rounds); cold numbers are the #108 receipt in
[docs/typed-playback-lane.md](docs/typed-playback-lane.md). Warm cost follows the data shape —
clock distribution and id grouping — not the authoring front-end:

| adoption step | what you add | measured warm cost (table above) |
| --- | --- | --- |
| playback only: a hand-written `ITimelineLane<T>` | one closed-form lane type; no JSON, no bake, no generator | 0.13-0.18 ns/row (static lane rows) |
| authored JSON assets | `tlbake`, domain structs, typed consumers, `TimelineSet` ids | 0.18-0.20 ns/row with ids grouped in waves or blocks of 100; 0.22 staggered on one looping timeline; 0.32 finite staggered; 0.48 at 16-row id blocks |
| Blender authoring | the bridge addon; same schema, same `.tlb` bytes | unchanged from authored JSON |
| generated C# job binding | consumers discovered compilation-wide and installed at build by `Tl.Gen.CSharp` | unchanged — every front-end drives the same measured tables |

Cold cost arrives once per asset load, not per frame: on the #108 20 MB corpus, load 0.9 ms, one
`Measure` 3.9 ms, seven `Add`s 1.11 ms combined. The host floor is the table's plain floor row
(0.39 ns/row) — any system writing 6 B/row pays it, and uniform clocks under the lane beat it
because run-length groups collapse the per-row work. Grouping rows by timeline id — the ECS
norm, since component-sorted archetypes already cluster identical rows — keeps a crowd on the
table-shaped rows; ids alternating every few rows pay the mixed scanner (1.72 ns/row), and fully
staggered clocks fragment runs (the ManyEntities sweep: 4.46 vs 0.76 ns/row for the hand SoA
lane).

## Unity ECS

Install the UPM package in Unity (Package Manager → *Add package from git URL*):

```
https://github.com/IAFahim/tl.unity.git?path=com.iafahim.tl
```

Unity is not a NuGet consumer here — the runtime is a net10.0 library with `ref` fields, so the UPM package compiles the shared `src/Tl.Core` sources directly (the same model MagicOnion uses: NuGet for .NET, UPM for Unity). The full walkthrough from baking to typed queries lives in the tl.unity repository: [END-TO-END.md](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md). A coordinator advances `TimelineComponent` rows, and your own system consumes typed frames:

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
| `samples/NuGetQuickStart` | Runnable quick start that consumes the published nuget.org packages; CI runs it on every build |
| `samples/ManyEntities` | Typed lane vs hand SoA lanes with per-entity clocks; CI runs it |
| `tools/Tl.Bake` | `tlbake` JSON-to-`TLB1` baker with cache, report, and strip |
| `tests/Tl.Alpha` | Typed-lane, data-authored, and allocation receipts |
| `tests/Tl.PackageConsumer` | Isolated package-only JIT and NativeAOT consumer |
| `benchmarks/Alpha` | Oracle and verification evidence for data-authored lane shapes |
| `benchmarks/TypedPlaybackProto` | Typed lane parity and throughput at 100k-1M rows |

The data-authored Unity host package (`com.iafahim.tl`) lives in the [tl.unity](https://github.com/IAFahim/tl.unity) repository, published under the MIT license decided in issue #64.

## Documentation

- [Data-authored API contract](docs/data-authored-api.md) — the frozen design contract
- [Typed playback lane](docs/typed-playback-lane.md) — the shipped playback surface, contract, receipts, and verdicts
- [Execution semantics](docs/semantics.md) — select, execute, commit, and movement laws of the removed alpha.3 catalog surface
- [Architecture](docs/architecture.md) — package and boundary map
- [Unity end-to-end](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md) — JSON bake to Unity ECS typed queries

## License

[MIT](LICENSE) — decided by the repository owner in [issue #64](https://github.com/IAFahim/tl/issues/64).
