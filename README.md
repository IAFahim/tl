# tl

**tl** compiles designer-authored timeline data into deterministic execution — data first, an authoring step that bakes it, a game system that plays it. No reflection, no runtime compilation, 0 B per frame.

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

Live playground: [iafahim.github.io/tl](https://iafahim.github.io/tl/)

## Install

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.10
dotnet tool install --global Tl.Bake --prerelease     # the tlb bake command
```

## Get started

Three structs and one JSON file — a first timeline in a minute. Put the structs and `jump.json` in a console project:

```cs
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
```

```json
{
  "duration": 30, "loop": true,
  "tracks": [
    { "type": "JumpTrack", "data": { "Scale": 1.0 },
      "clips": [ { "type": "JumpClip", "start": 0, "end": 15, "data": { "Velocity": 2.0 } },
                 { "type": "JumpClip", "start": 15, "end": 30, "data": { "Velocity": -2.0 } } ] }
  ]
}
```

Build, then bake with `--auto` — namespaces are inferred from the built assembly, so the JSON above needs none:

```sh
dotnet build -c Release
tlb jump.json jump.tlb --auto
```

Play it — one call advances every row one frame; after the full loop `y` is back at 0, the arc risen and fallen. Put the loop in `Program.cs` and `dotnet run`:

```cs
ushort jumpTimeline = TimelineAsset.Load(File.ReadAllBytes("jump.tlb"));
var tick = new ushort[1];
var y = new float[1];
for (var frame = 0; frame < 30; frame++)
    Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, tick, true, y);
    Timeline.Step(jumpTimeline, tick, true);
```

A crowd that shares one clock — the raid jumping in sync — can drop the clock column entirely and hold that clock once: `Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, clock, true, jumpFx)` folds the whole crowd in one broadcast pass, and `Timeline<JumpTrack, JumpClip>.Step(jumpTimeline, ref clock, true)` advances the single clock ([Shared clocks](#system)).

`--auto` is opt-in and never guesses silently: exactly one loaded type of that bare name fills the `namespace`; zero or several stop the bake naming every candidate. `tlb --json --assembly bin/Release/net10.0/YourGame.dll` lists every authorable pair with its namespace, fields, and consumers — the source for filling tracks and clips by hand ([Type discovery](#type-discovery)). "Run the full thing" below is the same shape with four characters, rewind, and host wiring.

## Run the full thing

`samples/Showcase` is the whole pipeline with the code inline — data, bake, system, output.

**1 · the data** — `jump.json`, authored by a designer, naming the game's own C# types:

```json
{
  "name": "jump", "duration": 30, "loop": true,
  "tracks": [
    {
      "name": "arc", "namespace": "Showcase", "type": "JumpTrack", "data": { "Scale": 1.0 },
      "clips": [
        { "name": "rise", "namespace": "Showcase", "type": "JumpClip", "start": 0, "end": 15, "data": { "Velocity": 2.0 } },
        { "name": "fall", "namespace": "Showcase", "type": "JumpClip", "start": 15, "end": 30, "data": { "Velocity": -2.0 } }
      ]
    }
  ]
}
```

**2 · the bake** — JSON to canonical TLB1 bytes, deterministic on every machine:

```sh
tlb jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll
```

**3 · the system** — `Program.cs`, three structs and one call per frame:

```cs
public readonly record struct JumpClip(float Velocity);

public readonly record struct JumpTrack(float Scale) : IBlend<JumpClip>
{
    public void Blend(in JumpClip first, in JumpClip second, float factor, out JumpClip result)
    {
        result = new JumpClip(first.Velocity + (second.Velocity - first.Velocity) * factor);
    }
}

public readonly struct MoveY : ITrack<JumpTrack, JumpClip>
{
    public static void OnActive(in Frame<JumpTrack, JumpClip> frame, ref float y)
    {
        y += frame.Direction * frame.Clip.Velocity * frame.Track.Scale;
    }
}

ushort jumpTimeline = TimelineAsset.Load(File.ReadAllBytes("jump.tlb"));
var ids = new ushort[] { jumpTimeline, jumpTimeline, jumpTimeline, jumpTimeline };
var tick = new ushort[4];
var y = new float[4];

for (var frame = 1; frame <= 30; frame++)
{
    Timeline<JumpTrack, JumpClip>.Apply(ids, tick, true, y);
    Timeline.Step(ids, tick, true);
    if (frame % 3 == 0)
        Console.WriteLine($"  tick {frame,2}   y = {y[0],4:0.0} m   {new string('#', (int)Math.Round(y[0] / 3))}");
}
```

Run it:

```sh
cd samples/Showcase
dotnet build -c Release
tlb jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll
dotnet run -c Release --no-build
```

You see:

```
four characters jump, one call per frame:
  tick  3   y =  6.0 m   ##
  tick  6   y = 12.0 m   ####
  tick  9   y = 18.0 m   ######
  tick 12   y = 24.0 m   ########
  tick 15   y = 30.0 m   ##########
  tick 18   y = 24.0 m   ########
  tick 21   y = 18.0 m   ######
  tick 24   y = 12.0 m   ####
  tick 27   y =  6.0 m   ##
  tick 30   y =  0.0 m

rewind walks the arc back exactly:
  after 30 back ticks: y = 0.0 m, tick = 0

Timeline.Bake marked entities 42, 43 as jumping; unmarked entities never reach the advance
```

Migration from earlier packages: the consumer method `Execute` is now `OnActive`.

## Data

The JSON above is the whole input format — one file per timeline, authored by a designer, naming the game's own C# types:

- windows are half-open `[start, end)`; execution order is authored clip order
- `data` fields map onto struct fields by name (`Scale` → `JumpTrack.Scale`); an omitted `data` on a track or clip bakes the type's default state — the same payload as `"data": {}`
- an optional `name` on the timeline, each track, and each clip labels the asset for tools and reports; `--strip` drops the labels
- two clips overlapping on one track blend through the type's `IBlend` with the authored factor
- `loop: true` wraps at `duration`; finite timelines clamp
- a timeline may mix track and clip types freely; caps are 65,535 ticks and 256 pairs per asset

## Authoring

`tlb` compiles the data to canonical TLB1 bytes — deterministic (same input, same bytes, every machine and culture), pooled (each distinct string, type, and payload stored once; repeated designer copy shrinks on the way in), and cached (content-keyed hits preserve timestamps):

```sh
tlb jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll
```

`--assembly` names the DLL that ships the JSON's types (pair keys hash assembly-qualified names). The explicit form always wins; the lazy forms fill in what is unambiguous:

```sh
tlb jump.json          # output defaults to jump.tlb beside the input; assembly discovered
tlb                    # exactly one *.json in the directory; more than one names the candidates
tlb jump.json jump.tlb --auto   # namespace inferred where absent (see below)
```

Discovery sweeps the DLLs under the launch directory (skipping `.git`, `obj`, and friends) and keeps the ones defining every `(namespace, type)` pair the JSON references — exactly one is used, several fail naming them, zero fails naming the missing types and swept roots. `tlb.db` in the launch directory remembers the last resolution per JSON and is reused while the recorded DLL still exists and still defines the types; it is machine-local, gitignored, and rewritten on every re-sweep.

`--auto` (opt-in, bake and `--watch`) infers a missing `namespace` at authoring time: the loaded assemblies — narrowed by an authored `assembly` when present — are searched for types whose bare name equals the authored `type`. Exactly one match fills the namespace; zero or several fail with a diagnostic naming every candidate and the repair. Inference runs only where the property is absent, per track and per clip — an authored `namespace` always wins, and `"namespace": ""` still selects the global namespace. Inferred bakes are byte-identical to writing the namespaces by hand; namespace written after `clips` still wins, and clip identity is never taken from the track.

The designer loop:

```sh
tlb --watch jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll
```

`--watch` bakes at startup, re-bakes on save (debounced, hash-skipped), and prints one JSON event per action (`ready`, `rebuild`, `skip`, `diagnostic`) — a broken file stays in the loop as a `diagnostic` until fixed. More: `tlb --report` audits asset sizes, `tlb --strip` drops authoring metadata for distribution. The Blender NLA bridge (`tools/Tl.Blender`) exports this same JSON and bakes through the same CLI.

### Type discovery

`tlb --json --assembly bin/Release/net10.0/Showcase.dll` lists every authorable pair in the given assemblies — the authoritative answer to "what can this JSON name?", captured live from `samples/Showcase`:

```json
{
  "schemaVersion": 1,
  "assemblies": [
    "Showcase"
  ],
  "pairs": [
    {
      "track": {
        "namespace": "Showcase",
        "name": "JumpTrack",
        "assembly": "Showcase",
        "size": 4,
        "fields": [
          {
            "name": "Scale",
            "type": "float"
          }
        ]
      },
      "clip": {
        "namespace": "Showcase",
        "name": "JumpClip",
        "assembly": "Showcase",
        "size": 4,
        "fields": [
          {
            "name": "Velocity",
            "type": "float"
          }
        ]
      },
      "blendable": true,
      "unmanaged": true,
      "trackPairings": [
        "Showcase.JumpClip"
      ],
      "consumers": [
        {
          "name": "Showcase.MoveY",
          "assembly": "Showcase",
          "outputs": [
            "float"
          ]
        }
      ]
    }
  ]
}
```

- `schemaVersion` — output contract version; tooling pins it before reading anything else
- `assemblies` — simple names of the assemblies the pairs were collected from
- `pairs` — every `(track, clip)` combination the bake accepts, ordered by track then clip
- `track` / `clip` — the authoring identity: `namespace` is what the JSON's `namespace` field spells, `name` is the `type`, `assembly` is the simple name `--assembly` takes; write the pair's `data` from `fields` (name and value type)
- `size` — struct byte size; omitted for managed types, which never bake
- `blendable` — the track implements `IBlend<this clip>`; only blendable pairs produce lanes
- `unmanaged` — both structs are unmanaged; a `false` pair fails the bake
- `trackPairings` — every clip type this track can blend (the full `IBlend<>` set)
- `consumers` — discovered `ITrack<track, clip>` jobs and the effect columns their `OnActive` writes (`outputs`)

Each `pairs` entry maps onto one track and its clips: the track entry names `(track.namespace, track.name)`, each clip entry names `(clip.namespace, clip.name)`, and `data` sets exactly the listed `fields`. With `--auto` the namespaces can be left out entirely; use this listing to check spellings, pick among same-named types, and see which consumers fold into a pair.

## System

The three structs in the run above are the whole game side — the clip payload, the track settings with its blend, and the consumer that writes one effect column. `ITrack<TTrack, TClip>` consumers are discovered compilation-wide — no registration, no catalog. `frame.Direction` is +1 forward and −1 backward, which is why rewind is exact. Loading is one call that returns the timeline's index — a dense `ushort`, the whole acquisition step; the first typed use folds the pair's measured tables once, every later call is a table read.

Hooking a timeline into game state — marking entities, spawning effects, notifying systems — is the one-shot bake surface. A bake is a marker struct implementing `IBake<TConsumer>` (up to four context types, `IBake<TConsumer, TContext0..TContext3>`) with a static `Bake` method; `TConsumer` names the `ITrack` consumer it hangs off. The generator discovers bakes compilation-wide alongside consumers and registers each per `(track, clip)` pair in the same generated binding. `Timeline.Bake` walks the loaded asset's pairs and fires every bake whose declared context types all match the arguments you pass, type by type:

```cs
public readonly struct AttachJumping : IBake<MoveY, World, int>
{
    public static void Bake(MoveY consumer, World world, int entity)
        => world.MarkJumping(entity);
}

Timeline.Bake(jumpTimeline, world, 42);
Timeline.Bake(jumpTimeline, world, 43);
```

Bakes are host-timed — attach and transition effects, never per-frame work. The dispatch is a cold pass over the asset's pairs; the warm path never sees a bake.

Per frame, three caller-owned columns — timeline index, clock, effect — and two calls: `Timeline<Track, Clip>.Apply` folds every row's effect at its current clock and never writes the clock, then one `Timeline.Step` advances every clock one frame. Finite timelines clamp, looping ones wrap, rows sharing a clock collapse into vector runs. Because `Apply` is read-only on the clock, several pair systems may consume the same column in one frame — `Step` moves it exactly once. A single system that owns its clock column outright may fuse the pair into `Apply(ids, clocks, next, forward, fx)` — `next` may be the same array for in-place — one pass, same result as `Apply` + `Step`. Rewind is `forward: false` and returns columns bit-exactly. There is no multi-frame skip parameter, ever: every system observes every tick, and sequential folds stay bit-exact (owner decision). Loop counts come from `FrameFlags.TimelineEnd` or the position column.

```cs
// shared clock column — safe to fan out to every pair system, step once:
Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, true, jumpFx);
Timeline<HealTrack, HealClip>.Apply(ids, clocks, true, healFx);
Timeline.Step(ids, clocks, true);

// or one owned column — fused, single pass:
Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, clocks, true, jumpFx);
```

A whole crowd on one clock needs no clock column at all: the clock is one `ushort`, `Apply(index, clock, forward, fx)` folds the span in one broadcast pass — no per-row position read, no per-row clock write, no ids scan, effects bit-identical to the per-row path at the same clock value — and `Step(index, ref clock, forward)` advances that one clock in O(1). A clock past the end of a finite timeline skips the whole span, exactly like the per-row law. At one million rows this is the shipped floor: 0.08 ns per character hot, 0.17 cold (the `shared-clock` row above).

```cs
// whole crowd on one clock — the clock is a scalar:
Timeline<JumpTrack, JumpClip>.Apply(raid, clock, true, jumpFx);
Timeline<HealTrack, HealClip>.Apply(raid, clock, true, healFx);
Timeline<JumpTrack, JumpClip>.Step(raid, ref clock, true);
```

```cs
for (var frame = 0; frame < 30; frame++)
    Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, tick, false, y);
    Timeline.Step(jumpTimeline, tick, false);
```

More systems on the same pair just declare the marker again — no registration, no chaining. Every consumer of `(JumpTrack, JumpClip)` runs inside the same one `Apply` call, folding its contribution into the effect column after the consumers before it:

```cs
public readonly struct ScreenShake : ITrack<JumpTrack, JumpClip>
{
    public static void OnActive(in Frame<JumpTrack, JumpClip> frame, ref float shake)
    {
        if (frame.Has(FrameFlags.TimelineEnd))
            shake += 1f;
    }
}
```

Consumers fold in consumer-name order (`MoveY` before `ScreenShake`) — ordinal, culture-independent, deterministic on every machine; rename a consumer to move it. Receipts: `TandemFirstJob` and `TandemSecondJob` in `tests/Tl.Alpha` both run from generated installs, and the fold order is pinned by `tests/Tl.Core.Tests`. Order across different pairs is the host's call order.

Host wiring is declared, not registered — implement `IBake<TConsumer, ...TContext>` (zero to four context types) and one type-agnostic call attaches your markers at load time:

```cs
public readonly struct AttachJumping : IBake<MoveY, World, int>
{
    public static void Bake(MoveY consumer, World world, int entity)
    {
        world.MarkJumping(entity);
    }
}

var world = new World();
Timeline.Bake(jumpTimeline, world, 42); Timeline.Bake(jumpTimeline, world, 43);
```

`Timeline.Bake(id, args...)` walks the timeline's pairs and runs every bake whose declared context types appear among the argument types — exact type match, first argument of that type wins, declaration order is the parameter order, zero-context bakes run on every call, a missing context keeps the bake silent. Discovered and validated at build time (TLGEN70-73), installed into an unmanaged table, warm path untouched. A timeline that lacks the pair is a loud located diagnostic at first typed use — host wiring error, never designer data. Reading without advancing: `Timeline.Query<TTrack, TClip>(in TimelineComponent)` is a read-only stage view that never moves the clock.

## Bank blocks and stable views

Every pair bank — the per-`(Track, Clip)` storage behind `Timeline<Track, Clip>` — allocates one immutable block per bound timeline index: a 64-byte `SlotView` header followed by the five measured tables, 64-aligned, `64 + 28 * (max(1, duration) + 1)` bytes per block. Blocks never move for the bank's lifetime; the id directory, motion words, and absent markers grow by doubling and retire superseded arrays onto a retired chain that is freed only when the bank is disposed. Nothing duplicates clip or track values per consumer, per entity, or per index: the bank dedupes on folded values — each bind hashes `(duration, looping, forward, backward)` and shares an existing block when the measured bytes compare equal, which every content-identical asset is — and per-entity state stays `(index id, position)` columns.

Hosts and other language runtimes acquire a view — a 64-byte `SlotView` copy — and may keep it for the bank's lifetime:

| field | offset | meaning |
| --- | ---: | --- |
| `Forward`, `Backward`, `BackwardByPosition` | 0 / 8 / 16 | effect tables, `TableTicks` IEEE-754 binary32 elements each |
| `ForwardRecords`, `BackwardRecords` | 24 / 32 | movement-record walks, `TableTicks` elements at `RecordBytes` stride |
| `Duration`, `Looping`, `Absent` | 40 / 42 / 44 | baked duration in ticks, wrap flag, bound-but-pair-less marker |
| `TableTicks` | 48 | element count of each table (`max(1, duration) + 1`) |
| `RecordBytes` | 52 | stride of one `LaneMovementRecord` (8: `Effect` float, `Next` ushort) |
| `AbiVersion` | 54 | 1; consumers reject unknown versions — a layout change is version 2, never silent drift |
| `Generation` | 56 | bank publication counter at bake time, diagnostics only |

`Timeline<JumpTrack, JumpClip>.View(jumpTimeline)` returns the copy. Acquisition resolves a pending index first; a swept-absent index returns `Absent = 1` with null table pointers and `TableTicks = 0`; a never-bound index throws `ArgumentException`; a disposed bank throws `ObjectDisposedException`. The first 48 bytes are byte-compatible with the runtime's internal slot layout, so the playback kernels and a host mirror read the same offsets. The bank holds 65,536 dense ids per pair, 0-based with no sentinel; the 65,537th add throws `InvalidOperationException`.

The proofs owed before a pointer leaves the runtime:

- **Publication** — a block becomes visible only after its tables are baked and its header written; the directory entry is the publication point, written with release semantics under a bank gate that also serializes bind, absent-marking, and dispose (closing the concurrent-bind race). Readers are lock-free: published blocks are immutable, a stale directory snapshot keeps reading a retired array whose every entry is untouched — the retired chain is linked in each array's private tail pad, outside reader-visible bytes — and a read that misses a just-published index falls back to resolve, which re-checks under the gate. Allocation precedes every counter commit, so a failed allocation leaves the bank untouched.
- **Ownership** — the bank owns all blocks, directories, retired arrays, and the content-dedupe table; views and every captured pointer borrow. Disposing the bank invalidates every view and pointer with no callback or keepalive; use after dispose is a usage error with the same standing as every other raw-pointer borrow in the runtime.
- **Identity** — a slot's identity is `(pair key, dense ushort index)`; index to content is fixed at first fold, a re-bake is a new index, and there is no silent rebind: binding a folded index returns it unchanged, and content-identical binds share the same block. An index whose asset is later disposed keeps its folded tables, because the bank copies at bind and the asset is refcounted independently. Identity inherits the intern table's 128-bit collision tolerance.
- **Safe reclamation** — immovability is the reclamation proof: per-index blocks, the live arrays, retired directories, motion words, and dedupe-table arrays are all freed in `Dispose` — nothing bank-attributable stays allocated after it — so there is no late reclamation, no graveyard, and no epoch a consumer must track.

Host guidance for Unity/Burst and C consumers: resolve at load and capture `SlotView`s per index into component or chunk metadata — never resolve or bounds-check inside a hot loop. Run a pair system over a whole chunk back-to-back while it is cache-resident (the interleaved 56 MB working set measured 0.36–0.39 ns/row DRAM-bound against 0.22 hot), parallelise across chunks in the host (8 P-cores each holding an L2-resident slice), and prefer per-index calls for grouped archetypes. The runtime ships one deterministic single-thread fold and takes no lock on the warm path. What cannot be promised stays unpromised: staggered per-row clocks at 1M rows floor at ~0.14–0.17 ns/row single-core (gather-bound), and DRAM-cold frames at ~0.30 (bandwidth-bound), on the reference host. Which intrinsic tier a host mirrors per CPU class is pending #244; every tier reads only the view's five tables. Adopting the view is additive: existing `Apply`/`Step` code is unchanged, and `SlotView` is plain data with no managed object, function pointer, or serialization story — process-local native memory, never valid across processes or an endianness boundary.

## Generated reports

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release   # content-stable .g.cs snapshot + manifest
```

## Numbers

<!-- tl-numbers: generated by eng/refresh-numbers from benchmarks/Numbers/results/numbers.json; edit the renderer, not this block -->
One million characters, one frame per call (i9-14900K, .NET 10, Release; best of 5 × 3 rounds after a per-scenario steady-state warm-up (at least 0.5 s and until the best frame is flat across three rounds); hot = consecutive frames with the crowd cache-resident, cold = the 8 crowds interleaved, re-read from memory each frame; every shape bit-exact forward and backward, 0 B warm):

| scenario | hot ms/frame | hot ns/character | cold ms/frame | cold ns/character |
| --- | ---: | ---: | ---: | ---: |
| whole crowd on one timeline (a raid jumping in sync) | 0.18 | 0.18 | 0.36 | 0.36 |
| crowd on one clock: shared-clock Apply + scalar Step | 0.08 | 0.08 | 0.16 | 0.16 |
| 100 timelines, crowds of 10,000 each (per-ability groups) | 0.36 | 0.36 | 0.53 | 0.53 |
| one looping timeline, every character on its own clock | 0.21 | 0.21 | 0.37 | 0.37 |
| one-shot finite timeline, staggered clocks | 0.10 | 0.10 | 0.20 | 0.20 |
| hand-written loop for comparison (`effects += 1`) | 0.35 | 0.35 | 0.39 | 0.39 |
| small squads: 16 timelines × 16 characters | 0.54 | 0.54 | 0.63 | 0.63 |
| worst case: unsorted rows, a different timeline each | 1.06 | 1.06 | 1.12 | 1.12 |

A single-timeline crowd floors at 0.08 ns per character hot and 0.16 cold — the hot column is the steady state with the crowd cache-resident, the cold column is the same frame with the 8 crowds interleaved so the working set streams from DRAM. Grouping rows by timeline keeps every crowd on the fast rows (ECS archetypes cluster identical rows for free). Authoring a full game's data — 19.3 MB of JSON — bakes in 58 ms and loads in 1.6 ms. Memory: 8 B per character of host columns, `28 * (duration + 1) + 64` bytes of tables per timeline, 0 B allocated per frame at any crowd size.
<!-- /tl-numbers -->

Receipts: `benchmarks/Numbers` (generates this section; `eng/refresh-numbers` re-measures and re-renders it from a fingerprinted receipt, and CI fails if the two disagree), `benchmarks/PairHandles`, `benchmarks/Alpha`, `tests/Tl.Alpha` — parity, allocation, and throughput evidence, run in CI on every push.

Contributing: [AGENTS.md](AGENTS.md) · Security: [SECURITY.md](SECURITY.md) · License: [MIT](LICENSE)
