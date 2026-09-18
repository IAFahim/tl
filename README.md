# tl

**tl** compiles designer-authored timeline data into deterministic execution — JSON in, baked `.tlb` bytes out, advanced by a small unmanaged runtime on .NET and Unity.

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

Try the [interactive cookbook/playground](https://iafahim.github.io/tl/) — live authoring-JSON editing and timeline playback in the browser, with [embeddable recipe pages](https://iafahim.github.io/tl/cookbook/).

Timeline data and timeline behavior are separate. Designers author tracks, clips, windows, and loop points as JSON; a deterministic `tlb` compile produces canonical TLB1 assets; typed C# consumers (`ITimeline<TTrack,TClip>`) fold what one active `(track, clip)` pair does into a borrowed float effect column. The runtime has no reflection, no delegates on warm paths, no runtime compilation, and no warm-path allocation — the same baked bytes drive .NET and Unity.

- **Deterministic bake** — same inputs, same bytes, on every machine and culture; content-keyed cache hits preserve timestamps
- **Heterogeneous assets** — tracks and clips of different types in one asset; execution order is authored order (A-B-A preserved)
- **Typed playback lane** — `Timeline<T>.Seek(positions, forward).Apply(effects)` advances every row exactly one frame over run-length groups; catch-up is repeated calls, rewind is `forward: false`
- **Resolve-time effect tables** — consumers are measured once per (pair, asset) at slot resolve; no kernel catalog, no interpreter tier, one execution path
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
dotnet add package Tl.CSharp --version 1.0.0-alpha.8
dotnet tool install --global Tl.Bake --prerelease
```

The bake command is `tlb`. Releases published up to `1.0.0-alpha.7` install the same tool under the name `tlbake`; if `tlb` is not found, update the tool (`dotnet tool update --global Tl.Bake --prerelease`).

For an offline install, copy the `.nupkg` files into a local `packages/` folder and append `--source ./packages` to both commands.

`Tl.CSharp` brings the runtime and the build-time generator, which discovers your consumers on every compilation — including IDE design-time builds — and sets consuming projects up automatically.

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended C# install: runtime plus build-time consumer binding |
| `Tl.Runtime` | Small declaration, frame, state, and movement ABI |
| `Tl.Gen.CSharp` | Build-time generator that binds typed consumers |
| `Tl.Bake` | `dotnet tool` (command: `tlb`): JSON to baked TLB1 assets, with watch mode, cache, report, and strip |

### 2. Define the domain

You author the domain values and one typed consumer per `(track, clip)` pair. `Tl` supplies `IBlend<TClip>`, `ITimeline<TTrack,TClip>`, and `Frame<TTrack,TClip>`; the generator discovers consumers compilation-wide, so there is no registration, catalog, or schema marker.

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

    public readonly struct ApplyDamage : ITimeline<DamageTrack, DamageClip>
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

Track values hold immutable settings. Clip values hold immutable authored payload. A lane consumer writes exactly one `ref float` effect column and self-inverts through `Frame.Direction`, so rewind is exact; `Timeline<TTrack, TClip>.Slot` measures the fold once at attach time; position purity is an authoring contract, not a resolve-time check (removed with the #113 purity probes).

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

Bake with the `tlb` tool, pointing `--assembly` at the compiled domain DLL so the baker resolves the authored type names. **The assembly name must match the assembly that ships those types at runtime** — pair keys hash assembly-qualified names. Baking is deterministic; cache hits preserve timestamps:

```sh
dotnet build -c Release
tlb boss.json boss.tlb --assembly bin/Release/net10.0/MyApp.dll --cache ~/.tlbcache
```

### The `tlb` command

| invocation | purpose |
| --- | --- |
| `tlb <input.json> <output.tlb> [--assembly <dll>]... [--cache <dir>]` | bake one timeline |
| `tlb --watch <input.json> <output.tlb> [--assembly <dll>]... [--debounce <ms>]` | re-bake on save |
| `tlb --watch <input-dir> [<output-dir>] [--assembly <dll>]...` | watch every `*.json` in a directory |
| `tlb --json --assembly <dll>...` | print the authorable `(track, clip)` pairs the assemblies expose |
| `tlb --report <input.tlb>` | byte and section size audit |
| `tlb --strip <input.tlb> <output.tlb>` | drop authoring metadata for distribution |

**Watch mode** is the designer loop: it bakes every input once at startup, then re-bakes on save. Saves are debounced (default 100 ms, `--debounce` to tune), content that did not change is skipped by hash, and every action prints one JSON line — `ready`, `rebuild`, `skip`, or `diagnostic`. A bake error stays in the loop as a `diagnostic` event; fix the JSON, save, and it rebuilds. Editors and tooling can parse the event stream directly; `Ctrl+C` exits.

`tlb --json --assembly ...` introspects the compiled domain and lists every blendable unmanaged `(track, clip)` pair with member names, so authoring tools can offer type lists without hand-maintaining them — the Blender bridge uses it to fill type names automatically.

### What the baker deduplicates

Authored game data repeats itself — the same namespace and type names on every track and clip. The bake stores each distinct **string once** in a single ordinal-sorted pool (timeline, track, and clip names; namespaces and type names; assembly names) and references pool entries by index from the type and label tables, so `"namespace": "TlPlayer"` written on a track and on all of its clips costs one pool entry. Each distinct track/clip **type pair** is stored once and shared by every track that uses it; two tracks of the same pair draw their values from one shared per-pair pool. Deduplication is per asset: one `.tlb` is self-contained, and identical strings in separately baked assets are stored per asset.

Repeated **payloads are deduplicated too** (TLB1 v3): each pair's distinct track values and clip payloads are stored once in a per-pair value pool of unique whole structs, and every frame slot references them by fixed-width `ushort` index — track data repeated across the track's slots and a clip spanning several stages each cost one pool entry. More than 65,535 unique values in one pool is a bake diagnostic naming the type. Windows and blend-factor bounds stay inline in the 24 B slot row (`u16` pool indices, `u8` track index, `u32` window/factor bounds, 8-aligned). Data member names (`"Velocity"`, `"Multiplier"`) are matched against the assembly at bake time and never stored in the asset at all. Assets baked by earlier alphas carry the v1 or v2 layout and are rejected at load with a diagnostic; re-bake them with the current `tlb`. `tlb --report` shows the split: `tlb/metadata-bytes` is the pooled string/type/label tail, `tlb/hot-bytes` the execution region, and the `pool/*/unique-count` fields report the value pools.

### 4. Resolve, advance, and query

The application owns the position and effect columns and the game clock. `TimelineAsset.Load` is a cold validated import; `Timeline<TTrack, TClip>.Slot(asset)` resolves the asset into the pair's bank at attach time and measures the per-position effect tables once; dispose the asset after all readers are done.

```cs
using var asset = TimelineAsset.Load(File.ReadAllBytes("boss.tlb"));
var boss = Timeline<DamageTrack, DamageClip>.Slot(asset);       // attach-time resolve, dense ushort slot

var positions = new ushort[] { 0 };
var health    = new float[] { 100f };

Timeline<DamageTrack, DamageClip>.Advance(asset, positions, true, health);   // one frame
Timeline<DamageTrack, DamageClip>.Advance(asset, positions, true, health);   // catch-up call
Timeline<DamageTrack, DamageClip>.Advance(asset, positions, false, health);  // rewind
```

`Timeline<TTrack, TClip>` is the only public playback API. `Slot` is the attach-time resolve: cold, once per asset, it folds every registered consumer's effect for every position into the pair's process-lifetime bank and returns a dense `ushort` slot (never a pointer, never authored, never baked); repeated resolves of the same asset return the same slot, and distinct assets take consecutive slots. Two advance spellings share one kernel path: the scalar `Advance(asset, positions, forward, effects)` advances a whole column on one asset's slot, and the crowd `Advance(slots, positions, forward, effects)` advances a mixed crowd against a per-row slot column; `Seek(...).Apply(...)` remains the two-step form of the crowd spelling. The interned load handle of [#150](https://github.com/IAFahim/tl/issues/150) lands next and swaps the `TimelineAsset` parameter of `Slot` and the scalar advance for a `ushort` handle mechanically — `forward` stays pinned immediately after the handle in every advance signature.

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

The pair-typed spelling makes that same crowd a one-liner when every timeline shares one `(T, C)` pair — the common designer reality where each character's timeline is a slight variation. `Timeline<TTrack, TClip>` owns the per-pair bank for the process: `Slot` returns a dense `ushort` slot per asset (never a pointer, never authored, never baked), and one crowd call advances every row against its own timeline.

```cs
var boss   = Timeline<DamageTrack, DamageClip>.Slot(TimelineAsset.Load(File.ReadAllBytes("boss.tlb")));
var grunt  = Timeline<DamageTrack, DamageClip>.Slot(TimelineAsset.Load(File.ReadAllBytes("grunt.tlb")));
var elite  = Timeline<DamageTrack, DamageClip>.Slot(TimelineAsset.Load(File.ReadAllBytes("elite.tlb")));

var slots    = new ushort[] { boss, grunt, grunt, elite };
var lastTick = new ushort[] { 0, 0, 2, 5 };
var health   = new float[4];

Timeline<DamageTrack, DamageClip>.Seek(slots, lastTick, true).Apply(health);   // whole crowd, one frame
Timeline<DamageTrack, DamageClip>.Advance(slots, lastTick, true, health);      // fused spelling
Timeline<DamageTrack, DamageClip>.Advance(bossAsset, lastTick, true, health);  // scalar: whole column, one asset
```

Slots come only from `Slot` at attach time; a slot column, a position column, and an effect column of equal length are the whole crowd call. The warm path is the same measured-table lane as the set (grouped rows still collapse into vector runs; 0 B), receipted bit-exact against per-asset lanes in `tests/Tl.Core.Tests` and in `benchmarks/PairHandles` — 0.18-0.19 ns/row with one slot for the batch, 0.32 with eight variants grouped (~2x), 0.86 with eight variants alternating per row (~5x), and wave positions at 0.17-0.18 grouped, 0.20 in slot/position blocks of 100, 0.67-0.69 alternating. The #167 guard-free mixed singleton walk — a fused vector probe sends chunks whose positions all sit below the set's minimum duration, with no adjacent slot/position repeat, to a singleton walk of one record load, one slot-record chain, and the two column writes — carries the alternating shapes; the retired single-bound statics gold path measured 0.16 ns/row, and the #174 re-pointed uniform gold arm (one bank slot, constant column) sits on the uniform bank path within noise of the one-slot shape. Use `TimelineSet` when you want scoped ownership and disposal of the bank; use `Timeline<TTrack, TClip>` when the pair's timelines live for the process.

**Structural membership.** A pair's packed columns contain only rows that have the pair. Membership is the host's own structure: an ECS host attaches a per-pair marker component and builds the pair's slot/position/effect columns from marked rows only, so an entity whose asset lacks the pair never reaches the timeline call at all. The engine deliberately has no absence API: `Slot` reports an asset that lacks the pair as a loud located diagnostic at attach time (a host wiring error, never designer data), and the hot path carries no per-frame absence handling — no sentinel rows, no filtering, no branch.

**Consumer order.** One pair may be served by several consumers: each registers through `PairRuntime<TTrack, TClip>.Consume`, and the resolve folds them in registration order — a deterministic chain receipted by `BakedLaneFoldsConsumersInRegisteredOrder` in `tests/Tl.Core.Tests`. Cross-pair order is the host's call order: one advance per pair per frame, sequenced by the host.

**Migrating from the two-path alpha** (one public playback API, [#174](https://github.com/IAFahim/tl/issues/174)):

| alpha.8 spelling | one-API spelling |
| --- | --- |
| `BakedLane<T,C>.Bind(asset)` | `Timeline<T,C>.Slot(asset)` |
| `Timeline<T,C>.Bind(asset)` / `Bind(asset, measured)` | `Timeline<T,C>.Slot(asset)` / `Slot(asset, measured)` |
| `Timeline<BakedLane<T,C>>.Seek(positions, fwd).Apply(effects)` | `Timeline<T,C>.Advance(asset, positions, fwd, effects)` — or `Seek(slots, positions, fwd).Apply(effects)` with a slot column |
| `Timeline<BakedLane<T,C>>.Advance(positions, fwd, effects)` | same substitution as above |

There is deliberately **no multi-frame step parameter** and never will be. A game runs thousands of systems that must all observe every timeline tick — a 50-tick skip would hide 49 intermediate states from them. Lag catch-up is repeated single-frame calls, which also keeps every float fold bit-exact (a precomputed K-frame sum can round differently from K sequential folds). This is an owner decision.

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

Throughput is the typed lane's product shape. `benchmarks/TypedPlaybackProto`
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

Measured before the #113 frame slim (cycles column present); the #113 A/B re-measured the affected shapes at
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
[IAFahim/FrentFun](https://github.com/IAFahim/FrentFun); the shipped receipts are recorded in
issues [#104](https://github.com/IAFahim/tl/issues/104) and
[#108](https://github.com/IAFahim/tl/issues/108).

### Lifecycle: bake once, resolve once, advance per frame

1. **Author and bake.** Flat-schema JSON in, deterministic `.tlb` bytes out (`tlb`, Quick
   Start above). The Blender bridge (`tools/Tl.Blender`) exports the same JSON from NLA
   scenes and bakes through the same CLI, receipted byte-identical to hand authoring. Duration
   above the 65,535-tick `ushort` position cap is rejected at bake with a diagnostic naming both.
2. **Load.** `TimelineAsset.Load(bytes)` is a cold validated import; the owner retains the
   immutable native storage and disposes it after all readers finish.
3. **Measure once per asset.** `MeasuredLanes.Measure(asset)` folds every position's forward and
   backward float effect once each through the cold executor into aligned native tables —
   131,000 evaluations for the 65,500-tick asset of the #108 corpus receipt, 3.9 ms cold, once
   per load. `TimelineSet.Add(asset)` measures internally when the measurement is not shared.
4. **Resolve.** `Timeline<TTrack,TClip>.Slot(asset)` — or `Slot(asset, measured)` to reuse one
   measurement across several pairs (dispose it after the last resolve) — appends the effect
   tables plus 8-byte per-tick movement records into the pair bank's one contiguous native block
   and returns the timeline's dense `ushort` slot, assigned at attach time, never authored, never
   baked. The asset may be disposed once `Slot` returns, and repeated resolves of the same asset
   return the same slot. `TimelineSet<TTrack,TClip>.Add(asset)` remains the scoped-ownership
   spelling of the same bank shape for hosts that create and dispose banks dynamically.
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
  unbound id throws naming the row; an asset declaring more than 256 pairs is rejected at resolve.

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
i9-14900K, best of 5 over interleaved rounds); cold numbers are the #108 receipt. Warm cost follows the data shape —
clock distribution and id grouping — not the authoring front-end:

| adoption step | what you add | measured warm cost (table above) |
| --- | --- | --- |
| playback only: a hand-written `ITimelineLane<T>` | one closed-form lane type; no JSON, no bake, no generator | 0.13-0.18 ns/row (static lane rows) |
| authored JSON assets | `tlb`, domain structs, typed consumers, `TimelineSet` ids | 0.18-0.20 ns/row with ids grouped in waves or blocks of 100; 0.22 staggered on one looping timeline; 0.32 finite staggered; 0.48 at 16-row id blocks |
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
| `samples/ManyEntities` | Typed lane vs hand SoA lanes with per-entity clocks; CI runs it — `dotnet run -c Release` in the folder prints the sweep/pulse/churn ns-per-row parity table (typed lane wins where rows group; the hand lane wins the fully staggered sweep) |
| `tools/Tl.Gen.Tlb` | Baking library: TLB1 writer, string/type metadata pool, bake cache keys, size reports |
| `tools/Tl.Bake` | `tlb` CLI: JSON-to-TLB1 bake, `--watch`, `--json`, `--strip`, `--cache`, `--report` |
| `tools/Tl.Bake.Bench` | Bake-path receipts: deterministic corpus generator plus byte-identity parity and per-stage timing across the legacy, string, and UTF-8 bytes bake entries |
| `tools/Tl.Blender` | Blender >= 5.0 NLA bridge: exports flat-schema JSON and bakes through the `tlb` CLI; type names come from `tl_nla` custom properties, preferences, or the single introspected pair; receipts in `tests/test_tl_blender.py` |
| `tools/Tl.Playground` | Source of the live cookbook/playground linked above; `dotnet run --project tools/Tl.Playground/Playground.Native -c Release` prints the SMOKE receipt; the site is the `Playground` publish output deployed to gh-pages |
| `tests/Tl.Alpha` | Typed-lane, data-authored, and allocation receipts |
| `tests/Tl.PackageConsumer` | Isolated package-only JIT and NativeAOT consumer |
| `tests/tlb_cli` | `tlb` CLI scenario harness (`config.json`); run via `python3 -m unittest discover -s tests -p test_tlb_cli.py` |
| `benchmarks/Alpha` | Oracle and verification evidence for data-authored lane shapes |
| `benchmarks/PairHandles` | Pair-typed lane receipts: parity against per-asset lanes and throughput across the one-slot uniform gold arm and the grouped, alternating, and wave-position shapes |
| `benchmarks/TypedPlaybackProto` | Typed lane parity and throughput at 100k-1M rows |

The solution carries three benchmark projects, `benchmarks/Alpha`, `benchmarks/PairHandles`, and `tools/Tl.Bake.Bench`; `benchmarks/FusedAdvance`, `benchmarks/TypedPlaybackProto`, and `benchmarks/ValuePoolFormat` are standalone probes documenting shipped surfaces (the fused-`Advance` verdict, typed-lane parity, the TLB1 v3 value pools); every other `benchmarks/*` directory and its result receipts are commit-scoped history of removed surfaces — their numbers apply only to the source and contract named in each local report.

The data-authored Unity host package (`com.iafahim.tl`) lives in the [tl.unity](https://github.com/IAFahim/tl.unity) repository, published under the MIT license decided in issue #64.

## Documentation

This README is the documentation. The Unity walkthrough lives in the tl.unity repository ([END-TO-END.md](https://github.com/IAFahim/tl.unity/blob/main/END-TO-END.md)); the contributor protocol in [AGENTS.md](AGENTS.md); vulnerability reporting in [SECURITY.md](SECURITY.md). Design documents and benchmark receipt prose removed from the tree remain in git history.

## Contributing

Read [AGENTS.md](AGENTS.md) before changing code. Open an issue first for any public ABI, plan-schema, semantic, package-boundary, or benchmark-fixture change so the invariant and migration cost are visible. Keep pull requests focused on one observable result, stating the trigger, previous behavior, resulting behavior, validation, size delta, and target limitations; performance work includes exact receipts and a same-machine baseline/candidate comparison. Run the validation block in [AGENTS.md](AGENTS.md) before requesting review, and never commit generated scratch, credentials, or machine-local configuration.

## License

[MIT](LICENSE) — decided by the repository owner in [issue #64](https://github.com/IAFahim/tl/issues/64).
