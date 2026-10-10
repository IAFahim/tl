# tl

**tl** compiles designer-authored timeline data into deterministic execution — data first, an authoring step that bakes it, a game system that plays it. No reflection, no runtime compilation, 0 B per frame.

[![ci](https://github.com/IAFahim/tl/actions/workflows/ci.yml/badge.svg)](https://github.com/IAFahim/tl/actions/workflows/ci.yml)

Live playground: [iafahim.github.io/tl](https://iafahim.github.io/tl/)

## Install

```sh
dotnet add package Tl.CSharp --version 1.3.0
dotnet tool install --global Tl.Bake     # the tlb bake command
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
    public static void ExecuteActive(in Frame<JumpTrack, JumpClip> frame, ref float y)
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

Play it — `Apply` folds every row's effect and `Advance` advances every clock one frame; `Apply` is pure over the clock and never moves it, the one `Advance` call after every `Apply` does. After the full loop `y` is back at 0, the arc risen and fallen. Put the loop in `Program.cs` and `dotnet run`:

```cs
ushort jumpTimeline = TimelineAsset.Load(File.ReadAllBytes("jump.tlb"));
var tick = new ushort[1];
var y = new float[1];
for (var frame = 0; frame < 30; frame++)
{
    Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, tick, true, y);
    Timeline.Advance(jumpTimeline, tick, true);
}
```

A crowd that shares one clock — the raid jumping in sync — can drop the clock column entirely and hold that clock once: `Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, clock, true, jumpFx)` folds the whole crowd in one broadcast pass, and `Timeline<JumpTrack, JumpClip>.Advance(jumpTimeline, ref clock, true)` advances the single clock ([Shared clocks](#system)).

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
    public static void ExecuteActive(in Frame<JumpTrack, JumpClip> frame, ref float y)
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
    Timeline.Advance(ids, tick, true);
    if (frame % 3 == 0)
        Console.WriteLine($"  tick {frame,2}   y = {y[0],4:0.0} m   {new string('#', Math.Max(0, (int)Math.Round(y[0] / 3)))}");
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

Migration from earlier packages: add an `Advance` call after every `Apply` — `Apply` gathers effects and never moves the clock, and its `positions` parameter is now `ReadOnlySpan` (existing `Span` callers compile unchanged; the per-entity overload takes the position `in`, so call sites spelling `ref` drop it); the consumer method `Execute` became `OnActive` in 1.0.0; 2.0.0 renames `OnMemo` to `Fold`, `OnActive` to `ExecuteActive`, `ApplyLanes` to `ApplyChunk`, and folds the `(fx, inputs)` Apply overload into the `ColumnSet` form — `Apply(ids, clocks, forward, fx)` for one effect column, `Apply(ids, clocks, forward, in set)` for any columns, `ApplyChunk(ids, clocks, forward, fx0, fx1)` for two typed fold lanes; the advance method `Step` is now `Advance` (pre-1.0 renames ship without compatibility aliases).

## Data

The JSON above is the whole input format — one file per timeline, authored by a designer, naming the game's own C# types:

- windows are half-open `[start, end)`; execution order is authored clip order
- `data` fields map onto struct fields by name (`Scale` → `JumpTrack.Scale`); an omitted `data` on a track or clip bakes the type's default state — the same payload as `"data": {}`
- an optional `name` on the timeline, each track, and each clip labels the asset for tools and reports; `--strip` drops the labels
- two clips overlapping on one track blend through the type's `IBlend` with the authored factor
- `loop: true` wraps at `duration`; finite timelines clamp
- a timeline may mix track and clip types freely; caps are 65,535 ticks and 256 pairs per asset
- every baked pair carries a layout fingerprint of its two structs (a hash over field names, field types, and declaration order). The C# generator owns the value end to end: it folds the pair's field shape from source symbols, emits a `PairRuntime<,>.VerifyLayout(fingerprint)` registration in the generated module initializer, and publishes the same constant as an assembly attribute that the baker copies into the asset. The one-time typed fold compares the two constants and fails fast with a rebake repair, so an asset baked against an older field layout of the same type names is rejected at bind instead of silently misread. Because both constants come from one generation of one assembly, an unedited rebuild can never disagree; a struct edit between bake and load changes the fold deterministically and the guard fires. The check is a single ulong compare on the cold fold — never on the warm path — and the runtime computes no fingerprint itself, which keeps it free of reflection under NativeAOT and trimming. Raw function-pointer consumers without a generated binding (and assets baked without one, layout 0) skip the comparison; hand-written consumers may register manually

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

`--watch` bakes at startup, re-bakes on save (debounced, hash-skipped), and prints one JSON event per action (`ready`, `rebuild`, `skip`, `diagnostic`) — a broken file stays in the loop as a `diagnostic` until fixed. More: `tlb --report` audits asset sizes, `tlb --inspect <input.tlb>` emits the baked asset as deterministic, schema-versioned structural JSON — header, pairs with pool widths, stages, and every program step's window and blend factors — the contract for visual tooling; `tlb --live --assembly <game.dll> [--asset <file.tlb>]... [--resolve]` hosts the assembly Unity-style in an isolated context (the engine version wins), snapshots the process tables — intern/pair/bake occupancy — and every folded bank with byte totals, dedupe hits, and per-lane value-shape summaries as deterministic JSON through the public `Tl.Inspection` surface (`--summary` for compact text; `--resolve` runs module initializers and folds every view, degrading to unresolved output on engine skew); `tlb --exec --assembly <game.dll> --method Game.Dumps.Playbook [--asset <file.tlb>] [--arg key=value ...]` is the Unity `-executeMethod` analog: it runs a public static `int Method()` or `int Method(TimelineAsset)` in the hosted assembly — the return value becomes the exit code, an unhandled exception exits 4 with the stack, and `--arg` pairs ride along in `Environment.GetCommandLineArgs()`; `tlb --strip` drops authoring metadata for distribution. The Blender NLA bridge (`tools/Tl.Blender`) exports this same JSON and bakes through the same CLI. The TLB1 container's full byte layout — for out-of-process parsers that never run the CLI — is specified in [docs/tlb1-layout.md](docs/tlb1-layout.md), with committed golden fixtures as the parser test corpus.

Cold editor tooling can use [`Inspection.CopyLanes`](docs/inspection-lanes.md) to copy an already folded pair's decoded forward/backward raw words. It performs no fold or live execution and returns owned storage; the host synchronizes with load and disposal. This additive API is available from the issue #432 source revision and belongs to the next package version after 3.0.0.

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
- `consumers` — discovered `ITrack<track, clip>` jobs and the effect columns they write: the live `ExecuteActive` ref columns plus the `Fold` results that fold into typed lanes (`outputs`)

Each `pairs` entry maps onto one track and its clips: the track entry names `(track.namespace, track.name)`, each clip entry names `(clip.namespace, clip.name)`, and `data` sets exactly the listed `fields`. With `--auto` the namespaces can be left out entirely; use this listing to check spellings, pick among same-named types, and see which consumers fold into a pair.

## System

The three structs in the run above are the whole game side — the clip payload, the track settings with its blend, and the consumer that writes one effect column. `ITrack<TTrack, TClip>` consumers are discovered compilation-wide — no registration, no catalog. `frame.Direction` is +1 forward and −1 backward, which is why rewind is exact. Loading is one call that returns the timeline's index — a dense `ushort`, the whole acquisition step; the first typed use folds the pair's measured tables once, every later call is a table read.

Hooking a timeline into game state — marking entities, spawning effects, notifying systems — is the one-shot bake surface. A bake is a marker struct implementing `IBake<TConsumer>` with a static `Bake` method whose signature is the contract: any parameters, each by value, `in`, or `ref`, of any type (including `Span<T>`/`ReadOnlySpan<T>`); a parameter typed exactly `TConsumer` is bound to `default(TConsumer)` — consumers are static, so the parameter participates in the per-pair signature match — and every other parameter is host state you pass to `Timeline.Bake`. The generator reads the declared signature, infers the forwarding dispatch, and registers each bake per `(track, clip)` pair in the same generated binding — "add whatever parameter you want and it source gens it for me":

```cs
public readonly struct AttachJumping : IBake<MoveY>
{
    public static void Bake(World world, int entity)
        => world.MarkJumping(entity);
}

Timeline.Bake(jumpTimeline, world, 42);
Timeline.Bake(jumpTimeline, world, 43);
```

`ref` parameters write through to the caller's variable, `in` parameters avoid copies, and a `Span<Entity>` parameter bakes a whole spawned wave in one call. Bakes are host-timed — attach and transition effects, never per-frame work — so managed state is legal at bake time. The dispatch is a cold pass over the asset's pairs; the warm path never sees a bake.

Per frame, three caller-owned columns — timeline index, clock, effect — and two calls: `Timeline<Track, Clip>.Apply` folds every row's effect at its current clock and never writes the clock, then one `Timeline.Advance` advances every clock one frame. Finite timelines clamp, looping ones wrap, rows sharing a clock collapse into vector runs. Because `Apply` is read-only on the clock, several pair systems may consume the same column in one frame — `Advance` moves it exactly once, and a per-system `Advance` multiplies the frame. A single system that owns its clock column outright may fuse the pair into `Apply(ids, clocks, next, forward, fx)` — `next` may be the same array for in-place — one pass, same result as `Apply` + `Advance`. Rewind is `forward: false` and applies each tick's backward contribution in reverse order: integer and enum lanes return bit-exactly, and float lanes return bit-exactly whenever every partial sum is representable (the ±2.0 arc above). Arbitrary float contributions accumulate IEEE rounding in both directions — a lane of 0.1 and −0.333 steps measured 40 ticks out and back from 0 lands on −3.1e−07 — so hosts that need exact replay from arbitrary float data keep the effect in an integer or fixed-point lane, or snapshot the column (issue #439). There is no multi-frame skip parameter, ever: every system observes every tick, and sequential folds stay bit-exact (owner decision). Loop counts come from `FrameFlags.TimelineEnd` or the position column.

```cs
// shared clock column — safe to fan out to every pair system, advance once:
Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, true, jumpFx);
Timeline<HealTrack, HealClip>.Apply(ids, clocks, true, healFx);
Timeline.Advance(ids, clocks, true);

// or one owned column — fused, single pass:
Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, clocks, true, jumpFx);
```

A whole crowd on one clock needs no clock column at all: the clock is one `ushort`, `Apply(index, clock, forward, fx)` folds the span in one broadcast pass — no per-row position read, no per-row clock write, no ids scan, effects bit-identical to the per-row path at the same clock value — and `Advance(index, ref clock, forward)` advances that one clock in O(1). A clock past the end of a finite timeline skips the whole span, exactly like the per-row law. At one million rows this is the shipped floor: 0.08 ns per character hot, 0.19 cold (the `shared-clock` row above).

```cs
// whole crowd on one clock — the clock is a scalar:
Timeline<JumpTrack, JumpClip>.Apply(raid, clock, true, jumpFx);
Timeline<HealTrack, HealClip>.Apply(raid, clock, true, healFx);
Timeline<JumpTrack, JumpClip>.Advance(raid, ref clock, true);
```

Entities that never form a crowd — a sparse set scattered across the host's own storage — pay the per-call contract once per batch instead of once per entity: `Timeline<JumpTrack, JumpClip>.Apply(rows, ids, tick, true, jumpY)` folds the entities at `rows[i]`, reading each row's id, clock, and effect straight from the host's columns (`ids[rows[i]]`, `tick[rows[i]]`, `jumpY[rows[i]]`), and `Timeline<JumpTrack, JumpClip>.Advance(rows, ids, tick, true)` moves those clocks. The result is bit-identical to calling the per-entity overload once per row, with no scratch columns and 0 B allocated: the columns stay the host's own (any 2-byte index and position struct, any 4-byte effect struct), unbound ids resolve lazily inside the batch, and a checked build validates column coherence and row bounds once per call.

```cs
// sparse set of jumpers, scattered rows of the host's component columns:
Timeline<JumpTrack, JumpClip>.Apply(jumperRows, jumperIds, jumperTicks, true, jumpY);
Timeline<JumpTrack, JumpClip>.Advance(jumperRows, jumperIds, jumperTicks, true);
```

```cs
for (var frame = 0; frame < 30; frame++)
{
    Timeline<JumpTrack, JumpClip>.Apply(jumpTimeline, tick, false, y);
    Timeline.Advance(jumpTimeline, tick, false);
}
```

More systems on the same pair just declare the marker again — no registration, no chaining. Every consumer of `(JumpTrack, JumpClip)` of the same kind runs inside the same one `Apply` call — the single-result Apply family plays Fold lane 0 and `ApplyChunk` plays typed lanes by result type, the `ColumnSet` Apply runs every live `ExecuteActive` — folding its contribution into the effect column after the consumers before it:

```cs
public readonly struct ScreenShake : ITrack<JumpTrack, JumpClip>
{
    public static void ExecuteActive(in Frame<JumpTrack, JumpClip> frame, ref float shake)
    {
        if (frame.Has(FrameFlags.TimelineEnd))
            shake += 1f;
    }
}
```

Consumers fold in consumer-name order (`MoveY` before `ScreenShake`) — ordinal, culture-independent, deterministic on every machine; rename a consumer to move it. Receipts: `TandemFirstJob` and `TandemSecondJob` in `tests/Tl.Alpha` both run from generated installs, and the fold order is pinned by `tests/Tl.Core.Tests`. Order across different pairs is the host's call order.

### The two consumer methods

A consumer declares one or both of two static methods, and the method name carries the lane contract:

`Fold(in Frame<TTrack, TClip> frame, out T0 r0, out T1 r1, ...)` runs once per tick, forward and backward, at the pair's first typed use — the fold, `duration × 2` invocations — and each `out` result freezes into its own per-tick, per-direction native table. The contract is the measured consumer's, by name now:

- **Pure**: a function of the frame (`frame.Clip`, `frame.Track`, `frame.TimelineTick`, `frame.Flags`) — no side effects, no live state reads; live state read at fold freezes at fold time.
- **Unmanaged results only, at most 10 of at most 8 bytes each** (TLGEN79): `bool`, `byte`, `short`, `int`, `long`, `float`, `double`, `char`, and enums — one lane per result, with 8-byte results folding into an adjacent 4-byte lane pair. `string` and other managed types are illegal as memo outputs (TLGEN76/79) — rich labels resolve to codes in the memo and map to text host-side or in `ExecuteActive`.
- **Write-only results**: every probe starts from `0f` — accumulate, never read the incoming value. `out` results get private lanes in declaration order; `ref` results join the shared accumulate pool by type.
- Direction comes from `frame.Direction` (`+1` forward, `−1` backward); the backward tables are measured too, so a sign-blind memo makes rewind wrong rather than absent.
- Side effects fire `duration × 2` times at fold and never during playback; changed data means a new `Load` — a folded `(asset, pair)` never re-folds.

Fold-only consumers play through the measured Apply family — `Apply(ids, clocks, forward, fx)` for one result, `ApplyChunk(ids, clocks, forward, fx0, fx1)` for several typed results — with zero per-frame consumer cost after the fold.

`ExecuteActive` runs per frame per row, live, in three shapes:

- `ExecuteActive(in Frame<TTrack, TClip> frame)` — pure dispatch: audio cues, markers, logging. The effects-less `Apply(ids, positions, forward)` executes it in row order.
- `ExecuteActive(in Frame<TTrack, TClip> frame, ref T fx, in T a, in T b, ...)` — **live columns**: the feeding call binds the columns by type through a caller-assembled `ColumnSet` — `Apply(ids, clocks, forward, in set)` with `set.Add<T>(span)` per column — and the runtime executes the consumer once per row per frame at the row's current tick, `ref` read-write, `in` read-only.
- `ExecuteActive(in T0 r0, in T1 r1, ..., ref T fx, in T a, ...)` — **compose**: the leading `in` parameters are the Fold results, fed positionally (type-checked at generation) from their frozen tables at the row's current position — direction-correct — while the `ref`/`in` columns come from the same Apply call. Precompute the pure math once, compose with live inputs per row.

Legacy `ExecuteActive(in Frame<TTrack, TClip> frame, ref float y)` keeps working as sugar for a single-result `Fold` — same fold, same frozen table, same measured Apply — and `Fold` is the canonical spelling in new code. The consumer ABI reserves 40 pointer slots per registered consumer, and live `ExecuteActive` columns — memo feeds, `ref` columns, `in` columns — total at most 30 per consumer (TLGEN68). A required column that is not passed is a loud located throw naming the pair, consumer, and parameter — `Timeline<JumpTrack, JumpClip> consumer 'MoveY' ExecuteActive requires a column of type int (multiplier); none was passed.` — at first play of a column-carrying Apply. The plain float-column Apply family drives only the Fold lanes and never dispatches live consumers, so a pair with a live `ExecuteActive` must also be driven by its column-carrying overload; a pair with no Fold lane at all, or whose first lane holds an integer, enum, `double`, or other non-float result (the generator marks integer-arithmetic Fold results in the consumer's key metadata), throws a located `ArgumentException` at first float play instead of silently adding nothing or misreading bits, and the generic `Apply<TIndex, TPosition, TEffect>` with an `int`, `uint`, or enum effect reads the matching typed lane with integer arithmetic exactly like `ApplyChunk` (issue #439). Live `ExecuteActive` columns bind through a caller-assembled `ColumnSet` — `Add<T>(span)` per column, cold assembly, resolved by `TypeKey` at first play while the warm read stays the direct pointer row — so distinct `in`/`ref` column types compose freely up to the 30-column bound. Same-type duplicates are TLGEN81 (TypeKey binding cannot distinguish two columns of one type), and a partially fed pair names the column actually left unfed.

The owner's jump case, first-class — the pure arc folds once, the per-jumper power composes live every frame:

```cs
public readonly struct JumpMove : ITrack<JumpTrack, JumpClip>
{
    // fold: the pure arc, frozen per tick and direction
    public static void Fold(in Frame<JumpTrack, JumpClip> frame, out float arc)
        => arc = frame.Direction * frame.Clip.Height * frame.Track.Scale;

    // per frame per row: arc arrives from the frozen table, multiplier is live host state
    public static void ExecuteActive(in float arc, ref float y, in int multiplier)
        => y += arc * multiplier;
}

var y = new float[crowd];
var power = new int[crowd];   // per-jumper, changes whenever gameplay says so
var player = new ColumnSet();
player.Add(y);
player.Add(power);
Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, forward, in player);
```

Rewind replays the backward tables: `forward: false` feeds the same `in` values from the backward tables (bit-exact under the representability rule in [System](#system)), and changing `power` between frames changes the applied effect — the input column is read fresh every call. Rule of thumb: **Fold carries the number — a pure per-tick contribution, frozen and shared by every row; ExecuteActive carries the effect on the world — live, per row, composed against the frozen number.**

Fold takes up to 10 results of any legal width, and every one of them composes into ExecuteActive beside the live player columns — four results of four widths here, `ref JumpY` and `in JumpPower` live on the same consumer:

```cs
public readonly struct JumpWideMove : ITrack<JumpTrack, JumpClip>
{
    // fold: four frozen results, one direction-correct table each (byte/short/int/float)
    public static void Fold(in Frame<JumpTrack, JumpClip> frame, out float arc, out int kind, out short phase, out byte style)
    {
        arc = frame.Direction * frame.Clip.Height * frame.Track.Scale;
        kind = frame.Clip.Height;
        phase = (short)(frame.Clip.Height / 2);
        style = (byte)(frame.Clip.Height % 7 + 1);
    }

    // per frame per row: the four feeds arrive from their tables at the row's position,
    // direction-correct, beside the live player columns
    public static void ExecuteActive(in float arc, in int kind, in short phase, in byte style, ref JumpY y, in JumpPower power)
        => y.Value += arc * power.Lift + kind + phase + style;
}

var y = new JumpY[crowd];
var power = new JumpPower[crowd];
var player = new ColumnSet();
player.Add(y);
player.Add(power);
Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, forward, in player);
```

Results of 8 bytes (`long`, `ulong`, `double`, wide enums) fold the same way — the lane is an adjacent pair, the read reassembles the exact bits, and `double` is added with IEEE semantics in both directions. Distinct gameplay column types compose through the same `ColumnSet`, assembled once per system run and reused across calls. The set holds GC-tracked references to its columns and pins them only for the duration of each `Apply`, so a compacting collection between assembly and play — or one triggered inside a consumer — never strands a write (issue #437); a `stackalloc` column therefore needs a `scoped ColumnSet`, which the compiler enforces:

```cs
var caller = new ColumnSet();
caller.Add(power);
caller.Add(wind);
Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, forward, caller);
```

```cs
public readonly struct JumpAudio : ITrack<JumpTrack, JumpClip>
{
    // dispatch: live per row — the clip boundary cue fires when a row crosses it
    public static void ExecuteActive(in Frame<JumpTrack, JumpClip> frame)
    {
        if (frame.Has(FrameFlags.ClipStart)) Audio.Cue(frame.Clip.Sound);
    }
}
```

Variation is authored, measured, or composed: short and tall jumps are separate assets that each fold their own table, continuous scaling folds a unit asset and multiplies through a live column or host-side after `Apply`, and live modifiers — wind, bounce, authority — are one live `in` column away from a folded arc.

Host wiring is declared, not registered — implement `IBake<TConsumer>` with the `Bake` signature you want and one type-agnostic call attaches your markers at load time:

```cs
public readonly struct AttachJumping : IBake<MoveY>
{
    public static void Bake(World world, int entity)
    {
        world.MarkJumping(entity);
    }
}

var world = new World();
Timeline.Bake(jumpTimeline, world, 42); Timeline.Bake(jumpTimeline, world, 43);
```

`Timeline.Bake(id, args...)` walks the timeline's pairs and runs every bake whose declared parameter types all appear among the argument types — exact type match, and repeated types bind positionally: each parameter consumes the next not-yet-bound argument of its type, so two `Entity` parameters receive the first and second `Entity` arguments rather than the same one twice. Declaration order does not matter, parameterless bakes run on every call, a missing type keeps the bake silent. Every bake registered to one `(track, clip)` pair must declare an identical parameter list (TLGEN74 names both signatures otherwise); bakes on different pairs of the same asset may differ and still dispatch in one call. Pass an lvalue for a parameter the bake mutates by `ref` — the write lands in your variable, exactly like any `ref` API. The host overloads take at most four state arguments per call. Discovered, signature-checked, and validated at build time (TLGEN70-74), installed into an unmanaged table, warm path untouched. A timeline that lacks the pair is a loud located diagnostic at first typed use — host wiring error, never designer data. Reading without advancing: `Timeline.Query<TTrack, TClip>(in TimelineComponent)` is a read-only stage view that never moves the clock.

The proofs owed before the bake dispatch turns caller storage into raw pointers:

- **Lifetime** — every host overload takes each state argument `in T` (or `ref T`), so the argument slots alias caller-owned storage that structurally outlives the synchronous `Bake` call: the pinned slot row lives in `Dispatch`'s frame, the per-bake invoke buffer is a `stackalloc` beneath it, nothing escapes the call, and the generated thunks dereference slots only inside it. A parameter typed as the consumer is `default` and consumes no argument slot, so the registration's parameter count bounds every slot access against the host's four-entry buffer.
- **Aliasing** — every host overload hands its arguments to `Dispatch` as GC-tracked `ref`s, and `Dispatch` pins all four with `fixed` for the whole call before deriving the raw `nint` slots the generated thunks read, so an argument living inside a heap object (a class field, an array element) stays put even when an earlier bake allocates and compacts the heap; pinning a stack lvalue is free, and an interior pin holds its object only for the one cold call (issue #443). The boxed dispatch of earlier alphas had no such slots.
- **Alignment** — there is no packing anywhere in the path: rehydrated addresses are the caller's own `T` lvalues, pinned in place rather than copied (naturally aligned, including `Span<T>` at pointer width), so `Unsafe.AsRef<T>` never sees misaligned storage.
- **Concurrency** — the unmanaged table is installed only from the generated module initializer under its gate. `Install` publishes each entry's key with release semantics before the entry is linked; entries and their `ParamKeys` are immutable once linked; `Dispatch` only reads. No reader-visible byte is written after publication, and the process-lifetime `ParamKeys` allocation lives exactly as long as the table itself.

Package consumers: pack the local repos first (`dotnet pack src/Tl.Core src/Tl.Gen.CSharp src/Tl.CSharp -c Release -o artifacts/packages`) and restore against that folder with an isolated `NUGET_PACKAGES` — otherwise the stale nuget.org package wins the cache and the build fails with misleading TLGEN66 `ExecuteActive` errors.

## Bank blocks and stable views

Every pair bank — the per-`(Track, Clip)` storage behind `Timeline<Track, Clip>` — allocates one immutable block per bound timeline index: a 72-byte `SlotView` header followed by the segmented encoding — per-lane direction directories (one `uint` per 64-tick bucket), the 16-byte run pool, the dense pool, flat tables for directions that escaped segmentation (`TableTicks` below 2049, or an encoding wider than the flat tables), and 8-byte lane keys — 64-aligned. Movement records and the backward-by-position table are derived at read time (`backward[p == 0 ? duration - 1 : p - 1]`, arithmetic next positions), never stored (#405). Blocks never move for the bank's lifetime; the id directory, motion words, and absent markers grow by doubling and retire superseded arrays onto a retired chain that is freed only when the bank is disposed. Nothing duplicates clip or track values per consumer, per entity, or per index: the bank dedupes on the encoding — each bind hashes `(duration, looping, lane count, encoded runs, dense bytes, flat bytes, lane keys)` and shares an existing block when a decoded value-by-value comparison confirms the hash, which every content-identical asset is — and per-entity state stays `(index id, position)` columns.

Hosts and other language runtimes acquire a view — a 72-byte `SlotView` copy — and may keep it for the bank's lifetime:

| field | offset | meaning |
| --- | ---: | --- |
| `Directory`, `Segments`, `Dense` | 0 / 8 / 16 | segmented encoding: 4 B directory bucket per 64 ticks per lane direction (bit 31 set = forward scan), 16 B runs (`Start`, `End` inclusive, `Kind` constant/ramp/dense, `V0`/`V1`), dense float pool |
| `Forward`, `Backward` | 24 / 32 | flat effect tables for escaped directions, `TableTicks` IEEE-754 binary32 elements each; null when that direction is segmented |
| `LaneKeys` | 40 | 8 B per lane |
| `Duration`, `Looping`, `Absent` | 48 / 50 / 52 | baked duration in ticks, wrap flag, bound-but-pair-less marker |
| `TableTicks` | 56 | element count of each table (`max(1, duration) + 1`) |
| `ResultCount`, `AbiVersion` | 60 / 62 | lane count; `3` — consumers reject unknown versions, a layout change is a new version, never silent drift |
| `Generation` | 64 | bank publication counter at bake time, diagnostics only |

`Timeline<JumpTrack, JumpClip>.View(jumpTimeline)` returns the copy. Acquisition resolves a pending index first; a swept-absent index returns `Absent = 1` with null table pointers and `TableTicks = 0`; a never-bound index throws `ArgumentException`. The copy is byte-compatible with the runtime's internal slot layout, so the playback kernels and a host mirror read the same offsets. Movement output columns mark a row with no next position using `SlotView.Skipped` (`0xFFFF`); next positions are derived arithmetic, never stored per tick. The bank holds 65,536 dense ids per pair, 0-based with no sentinel; the 65,537th add throws `InvalidOperationException`.

### Playback misuse contract

The playback path (`Apply`, `Advance`, `View`, the typed lanes) carries no managed diagnostics in shipped bits: valid inputs throw nothing, and the warm path allocates 0 B. Column length, position-domain (`position` beyond the asset's `Duration`), and overlap checks, and the disposed-state check, are compiled only into checked builds — every build where the `TL_CHECKED` define is present (Debug by default, or `dotnet build -p:TlChecked=true`); the C# `[Conditional]` mechanism removes the call and its argument evaluation from the shipped Release assembly, so the guards cost zero instructions there. In a checked build misuse fails fast with a located `ArgumentException` or `ObjectDisposedException`. In the shipped Release package the same misuse is undefined behaviour, not a guaranteed fault: the record paths still fail fast through span bounds checks and null dereferences, but the vector kernels store through pointers, where a wrong-width or short column writes into neighbouring memory and a disposed bank dereferences a freed directory — nothing faults. Authoring and bind time keep located diagnostics regardless of configuration (invalid definitions, wrong pair, unbound ids, foreign `MeasuredLanes`, over-capacity adds, baked layout skew). Consume the checked configuration while developing; CI runs a checked lane beside the shipped one, and an IL scan proves the shipped assembly contains zero guard call sites.

The proofs owed before a pointer leaves the runtime:

- **Publication** — a block becomes visible only after its tables are baked and its header written; the directory entry is the publication point, written with release semantics under a bank gate that also serializes bind, absent-marking, and dispose (closing the concurrent-bind race). Readers are lock-free: published blocks are immutable, a stale directory snapshot keeps reading a retired array whose every entry is untouched — the retired chain is linked in each array's private tail pad, outside reader-visible bytes — and a read that misses a just-published index falls back to resolve, which re-checks under the gate. Allocation precedes every counter commit, so a failed allocation leaves the bank untouched.
- **Ownership** — the bank owns all blocks, directories, retired arrays, and the content-dedupe table; views and every captured pointer borrow. Disposing the bank invalidates every view and pointer with no callback or keepalive; use after dispose is a usage error with the same standing as every other raw-pointer borrow in the runtime.
- **Identity** — a slot's identity is `(pair key, dense ushort index)`; index to content is fixed at first fold, a re-bake is a new index, and there is no silent rebind: binding a folded index returns it unchanged, and content-identical binds share the same block. An index whose asset is later disposed keeps its folded tables, because the bank copies at bind and the asset is refcounted independently. Identity inherits the intern table's 128-bit collision tolerance.
- **Safe reclamation** — immovability is the reclamation proof: per-index blocks, the live arrays, retired directories, motion words, and dedupe-table arrays are all freed in `Dispose` — nothing bank-attributable stays allocated after it — so there is no late reclamation, no graveyard, and no epoch a consumer must track.

Host guidance for Unity/Burst and C consumers: resolve at load and capture `SlotView`s per index into component or chunk metadata — never resolve or bounds-check inside a hot loop. Run a pair system over a whole chunk back-to-back while it is cache-resident (the interleaved 56 MB working set measured 0.36–0.39 ns/row DRAM-bound against 0.22 hot), parallelise across chunks in the host (8 P-cores each holding an L2-resident slice), and prefer per-index calls for grouped archetypes. The runtime ships one deterministic single-thread fold and takes no lock on the warm path. What cannot be promised stays unpromised: staggered per-row clocks at 1M rows floor at ~0.14–0.17 ns/row single-core (gather-bound), and DRAM-cold frames at ~0.30 (bandwidth-bound), on the reference host. Warm kernels dispatch once per segment across register-table permute (durations at most 8 ticks), vector gather over the flat or segmented tables, and run-coalescing scalar tiers (#244, #405); movement is derived arithmetic, never a stored record. Adopting the view is additive: existing `Apply`/`Advance` code is unchanged, and `SlotView` is plain data with no managed object, function pointer, or serialization story — process-local native memory, never valid across processes or an endianness boundary.

## Generated reports

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release   # content-stable .g.cs snapshot + manifest
```

## Numbers

<!-- tl-numbers: generated by eng/refresh-numbers from benchmarks/Numbers/results/numbers.json; edit the renderer, not this block -->
One million characters, one frame per call (i9-14900K, .NET 10, Release; best of 5 × 3 rounds after a per-scenario steady-state warm-up (at least 0.5 s and until the best frame is flat across three rounds); hot = consecutive frames with the crowd cache-resident, cold = the 9 crowds interleaved, re-read from memory each frame; every shape bit-exact forward and backward, 0 B warm):

| scenario | hot ms/frame | hot ns/character | cold ms/frame | cold ns/character |
| --- | ---: | ---: | ---: | ---: |
| whole crowd on one timeline (a raid jumping in sync) | 0.17 | 0.17 | 0.36 | 0.36 |
| crowd on one clock: shared-clock Apply + scalar Advance | 0.08 | 0.08 | 0.16 | 0.16 |
| 100 timelines, crowds of 10,000 each (per-ability groups) | 0.25 | 0.25 | 0.42 | 0.42 |
| one looping timeline, every character on its own clock | 0.21 | 0.21 | 0.34 | 0.34 |
| one-shot finite timeline, staggered clocks | 0.09 | 0.09 | 0.18 | 0.18 |
| hand-written scalar loop (`effects[i] += 1f`) | 0.18 | 0.18 | 0.26 | 0.26 |
| hand-written SIMD loop (`Vector<float>` add, scalar tail) | 0.08 | 0.08 | 0.17 | 0.17 |
| small squads: 16 timelines × 16 characters | 0.53 | 0.53 | 0.69 | 0.69 |
| worst case: unsorted rows, a different timeline each | 1.35 | 1.35 | 1.62 | 1.62 |

A single-timeline crowd floors at 0.08 ns per character hot and 0.16 cold — the hot column is the steady state with the crowd cache-resident, the cold column is the same frame with the 9 crowds interleaved so the working set streams from DRAM. The hand-written SIMD row is the traffic floor of this machine (0.08 hot, 0.17 cold); the shared-clock crowd sits on it and the per-row-clock crowds carry 4 more bytes per character. Grouping rows by timeline keeps every crowd on the fast rows (ECS archetypes cluster identical rows for free). Authoring a full game's data — 19.3 MB of JSON — bakes in 55 ms and loads in 1.8 ms. Memory: 8 B per character of host columns, `8 * (duration + 1)` bytes of flat tables per timeline or a segmented run encoding below that (72-byte header; movement records and backward-by-position derived, never stored), 0 B allocated per frame at any crowd size.
<!-- /tl-numbers -->

### The code that gets each row

Every number in the table is one frame of playback over the same million rows. The call shapes below are written against the quick-start raid for readability; the receipt harness ([benchmarks/Numbers/Domain.cs](benchmarks/Numbers/Domain.cs)) runs exactly these shapes on longer data — a looping 1024-tick `gold` lane (Amount 1.25 until tick 600, then −0.5, Scale 2), a finite copy of it for row 5, and 100 variants (Scale 1 + 0.25k, split at tick 300 + 7k) for the multi-timeline rows — with clocks seeded `i % 1024` where the rows below write `i % 30` (`i % 512` for the finite row), and each per-row-clock frame is the fused `Apply(ids, clocks, clocks, forward, fx)`, one pass equal to `Apply` + `Advance` (row 2 times its shared-clock `Apply` + scalar `Advance`, rows 6–7 their hand-written loops). The readable setup, baked once:

```cs
public readonly record struct JumpClip(float Velocity);

public readonly record struct JumpTrack(float Scale) : IBlend<JumpClip>
{
    public void Blend(in JumpClip first, in JumpClip second, float factor, out JumpClip result)
        => result = new JumpClip(first.Velocity + (second.Velocity - first.Velocity) * factor);
}

public readonly struct MoveY : ITrack<JumpTrack, JumpClip>
{
    public static void ExecuteActive(in Frame<JumpTrack, JumpClip> frame, ref float y)
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

```sh
tlb raid.json raid.tlb --auto
```

```cs
ushort arc = TimelineAsset.Load(File.ReadAllBytes("raid.tlb"));   // the arc's dense baked index
var ids = new ushort[1_000_000];
var clocks = new ushort[1_000_000];
var y = new float[1_000_000];                                    // one jump height per character
```

**Row 1 — whole crowd on one timeline (a raid jumping in sync): 0.17 ns/character hot, 0.36 cold.** A million goblins, every row on the arc and every clock cell on tick 5 — one timeline, one clock value, a clock column that still exists per row:

```cs
for (int i = 0; i < ids.Length; i++) { ids[i] = arc; clocks[i] = 5; }

Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, true, y);
Timeline.Advance(ids, clocks, true);   // one frame, both calls: 0.17 ms hot · 0.36 cold (9 crowds interleaved)
```

**Row 2 — crowd on one clock: shared-clock Apply + scalar Advance: 0.08 hot, 0.16 cold.** The same raid, but the clock column disappears — the crowd shares one scalar clock and folds in one broadcast pass:

```cs
ushort clock = 5;                                // every goblin on tick 5 together, mid-rise

Timeline<JumpTrack, JumpClip>.Apply(arc, clock, true, y);        // 0.08 ms per frame hot — this machine's floor
Timeline<JumpTrack, JumpClip>.Advance(arc, ref clock, true);     // one tick, O(1)
```

**Row 3 — 100 timelines, crowds of 10,000 each (per-ability groups): 0.25 hot, 0.42 cold.** One hundred abilities, each with its own baked copy of the arc; ability k owns rows 10,000k–10,000k+9,999, so identical timelines cluster into long runs:

```cs
for (int i = 0; i < ids.Length; i++) { ids[i] = (ushort)(i / 10_000); clocks[i] = (ushort)(i % 30); }

Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, true, y);
Timeline.Advance(ids, clocks, true);   // one frame: 0.25 ms hot · 0.42 cold
```

**Row 4 — one looping timeline, every character on its own clock: 0.21 hot, 0.34 cold.** A million guards on the arc, each seeded at its own spawn tick — thirty different table slots gathered every frame:

```cs
for (int i = 0; i < ids.Length; i++) { ids[i] = arc; clocks[i] = (ushort)(i % 30); }

Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, true, y);
Timeline.Advance(ids, clocks, true);   // one frame: 0.21 ms hot · 0.34 cold
```

**Row 5 — one-shot finite timeline, staggered clocks: 0.09 hot, 0.18 cold.** The same arc baked with `"loop": false` — a door-open. Doors staggered over the rise; a row past the end clamps at tick 30 and folds nothing:

```cs
ushort once = TimelineAsset.Load(File.ReadAllBytes("raid-once.tlb"));   // raid.json with "loop": false
for (int i = 0; i < ids.Length; i++) { ids[i] = once; clocks[i] = (ushort)(i % 15); }

Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, true, y);
Timeline.Advance(ids, clocks, true);   // one frame: 0.09 ms hot · 0.18 cold
```

**Row 6 — hand-written scalar loop (`effects[i] += 1f`): 0.18 hot, 0.26 cold.** The baseline the tl rows race — the whole frame is one plain C# add per character:

```cs
for (var i = 0; i < y.Length; i++)
    y[i] += 1f;                           // one frame: 0.18 ms hot · 0.26 cold
```

**Row 7 — hand-written SIMD loop (`Vector<float>` add, scalar tail): 0.08 hot, 0.17 cold.** The same add by hand, `Vector<float>` wide, and the traffic floor of this machine:

```cs
var ones = new Vector<float>(1f);
int i = 0;
for (; i <= y.Length - Vector<float>.Count; i += Vector<float>.Count)
    (Vector.LoadUnsafe(ref y[i]) + ones).StoreUnsafe(ref y[i]);
for (; i < y.Length; i++)
    y[i] += 1f;                           // one frame: 0.08 ms hot · 0.17 cold
```

**Row 8 — small squads: 16 timelines × 16 characters: 0.53 hot, 0.69 cold.** Sixteen squads of sixteen, tiled to a million rows; the warm kernel re-dispatches every 16 rows, so short crowds pay the fixed cost per segment (short runs gather straight from the flat table without the segment probes, issue #441):

```cs
for (int i = 0; i < ids.Length; i++) { ids[i] = (ushort)(i / 16 % 16); clocks[i] = (ushort)(i % 30); }

Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, true, y);
Timeline.Advance(ids, clocks, true);   // one frame: 0.53 ms hot · 0.69 cold
```

**Row 9 — worst case: unsorted rows, a different timeline each: 1.35 hot, 1.62 cold.** `ids[i] = i % 100` — no two neighbouring rows share a timeline, so nothing clusters and every row gathers from its own tables (the call-free mixed tier of issue #441 recovered most of the #405 cost: 7.15 → 1.36 hot):

```cs
for (int i = 0; i < ids.Length; i++) { ids[i] = (ushort)(i % 100); clocks[i] = (ushort)(i % 30); }

Timeline<JumpTrack, JumpClip>.Apply(ids, clocks, true, y);
Timeline.Advance(ids, clocks, true);   // one frame: 1.35 ms hot · 1.62 cold
```

Receipts: [benchmarks/Numbers](https://github.com/IAFahim/tl/tree/main/benchmarks/Numbers) (generates this section; `eng/refresh-numbers` re-measures and re-renders it from a fingerprinted receipt, and CI fails if the two disagree), [benchmarks/PairHandles](https://github.com/IAFahim/tl/tree/main/benchmarks/PairHandles), [benchmarks/Alpha](https://github.com/IAFahim/tl/tree/main/benchmarks/Alpha), [tests/Tl.Alpha](https://github.com/IAFahim/tl/tree/main/tests/Tl.Alpha) — parity, allocation, and throughput evidence, run in CI on every push.

Contributing: [AGENTS.md](AGENTS.md) · Security: [SECURITY.md](SECURITY.md) · License: [MIT](LICENSE)
