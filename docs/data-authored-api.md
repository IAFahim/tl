# Data-authored timelines and typed frame queries

Approved by the owner on 2026-09-11. [Issue #56](https://github.com/IAFahim/tl/issues/56) owns implementation and live acceptance status. This document freezes the next consumer API direction. It does not describe implemented alpha.3 functionality, qualify a new release, or freeze a binary ABI.

The owner subsequently paused work; implementation has since resumed on `feat/56-data-authored-api`. Gates 1 through 4 — generated facade and column binding, the TLB1 native format with ownership and safe publication, oracle-traced select/query/execute/commit semantics including A-B-A, opposing orders, blends, crossed frames and reverse movement, complete-row compatibility diagnostics, and the consumer-chain reverse-order tail (kernel-lane phases at `323c85a` and `739bbc8`) — first landed across `548c802022a9e12eb4ab65da4fad6d94d8876b4a`..`739bbc8`. Gate 5 (Unity parity: eligibility policy, Bursted coordinator, IJobEntity parity, delayed dependencies; 22/22 EditMode) is receipted in the extracted tl.unity repository through `fc2d02a`. Gate 6 (flat schema v1, conversion API, TLB1 metadata tail at `bd3c3ca`; deterministic bake cache and memory reports at `4afea8c`) is landed. Gate 7's benchmark record is consolidated in [optimization-verdicts.md](optimization-verdicts.md) bindings 7 and 8. Standalone job discovery without `Define` declarations is implemented and receipted (`ConsumerBindingTests`). The superseded alpha.3 authored surface was removed from the repository under [issue #65](https://github.com/IAFahim/tl/issues/65); remaining owner-gated work is release qualification ([issue #91](https://github.com/IAFahim/tl/issues/91)) after the publication and licensing decisions recorded in [issue #64](https://github.com/IAFahim/tl/issues/64). Under [issue #104](https://github.com/IAFahim/tl/issues/104) the owner then approved the full playback rewrite: the `Timeline.Rows(...).Tick` facade, the `TimelineKernels` kernel catalog (`tlbake --kernel`), and the `TickUnmanaged` seam were removed in favor of the typed playback lane ([typed-playback-lane.md](typed-playback-lane.md)) as the single .NET warm playback surface; the consumer sections below state the shipped shape. The contract body otherwise remains frozen as approved; read the latest issue comments before treating any checklist item as authorization to work.

This document is the only authoring lane. The removed alpha.3 authored API survives only as a frozen design record in [v1.0-alpha-api.md](v1.0-alpha-api.md) and `docs/alpha3`; it is not implemented, shipped, or maintained. A live, embeddable playground over this authoring surface — editable JSON, real bake and playback in the browser — is published at https://iafahim.github.io/tl/ (source: `tools/Tl.Playground/`, issue #116).

## What belongs where

| Owner | Responsibility |
| --- | --- |
| Shared code | Track and clip types, `IBlend<TClip>`, reusable operation signatures and behavior |
| Designer asset | Track type identity, track `data`, clip `data`, clip windows, track order, looping |
| Importer and baker | Validate assets, derive duration and ordered schedules, canonicalize and deduplicate data |
| Generator | Resolve known type pairs, component access, consumer identities and host runners |
| Application | Asset ownership, entity storage, game clock, explicit consumer dependencies |
| Runtime coordinator | Select one step, execute ordered stages, commit once, repeat for signed delta |

Assets require neither a name nor a job binding. The new path requires no handwritten `ITimeline`, catalog, schema marker, `Use<TJob>()`, or generated per-asset C# symbol. Editor labels and file names may exist without becoming runtime identity.

Known executable types are closed at build time. Asset contents may vary within that type universe; introducing executable types requires recompilation. Rebaking timing or ordering must not require writing a timeline declaration in C#. Asset type identifiers resolve through the build's validated type map. Human-readable type names in the JSON below are illustrative; neither language-specific names nor arbitrary local numeric ordinals define the neutral persistence contract.

## Shared data and behavior

These C# 9-compatible domain declarations can be compiled by both hosts. `partial` on component values permits Unity-specific interface declarations in a separate source file.

```cs
using Tl;

public readonly partial struct Resistance
{
    public readonly float Scale;

    public Resistance(float scale) => Scale = scale;
}

public partial struct Health
{
    public float Value;
}

public readonly struct DamageClip
{
    public readonly float Amount;

    public DamageClip(float amount) => Amount = amount;
}

public readonly struct DamageTrack : IBlend<DamageClip>
{
    public readonly float Multiplier;

    public DamageTrack(float multiplier) => Multiplier = multiplier;

    public void Blend(
        in DamageClip first,
        in DamageClip second,
        float factor,
        out DamageClip result)
    {
        result = new DamageClip(
            first.Amount +
            (second.Amount - first.Amount) * factor);
    }
}

public readonly struct ApplyDamage :
    ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        in Resistance resistance,
        ref Health health)
    {
        var amount =
            frame.Clip.Amount *
            frame.Track.Multiplier *
            resistance.Scale;

        health.Value += frame.IsBackward ? amount : -amount;
    }
}
```

`ITimelineJob<TTrack,TClip>` associates reusable behavior with a type pair. It never associates an asset with one particular consumer. The generator derives required read and write access from `Execute`. `Frame.IsBackward` is a proposed flag-derived convenience, not an additional stored direction integer.

For data-authored timelines, `ITimelineJob<TTrack, TClip>` implementations are discovered automatically compilation-wide by the generator. No `Use<TJob>()` declaration, authored `ITimeline`, catalog, or schema marker is required to register a consumer for data-authored consumption. (For alpha.3 authored timelines, the `Use<TJob>()` reference in `ITimeline.Define` remains required as before; the generator binds the union of authored and standalone jobs, deduplicated per job type.)

A pair may have zero, one, or multiple registered consumers. Distinct consumers execute once each for each selected matching occurrence. A handwritten wrapper and its generated counterpart represent the same consumer and must not both run. The compiler must validate that identity explicitly; searching a method body for a call is insufficient proof.

Multi-pairing is supported: one track type may implement `IBlend` for several clip types and each `(TTrack, TClip)` instantiation is a first-class pair (runtime registration and frame keys are pair-scoped). A job type may implement `ITimelineJob<TTrack, TClip>` for several pairings; each pairing is discovered and registered as its own consumer in lexical order of the pair type names — a per-compilation guarantee, since each assembly installs its consumers from its generated `[ModuleInitializer]`, so install order across assemblies follows module-initializer order, deterministic per process but not defined by this contract — and each pairing requires one accessible `Execute` beginning with `in Frame<TTrack, TClip>` for that pair. An authored `Track(settings).Use<TJob>()` resolves the pairing whose track type equals the authored track type. In designer JSON, a track entry whose track type implements `IBlend<>` exactly once may omit `clipType`; a multi-`IBlend` track type must declare `clipType` naming one of its implemented instantiations, and a declared `clipType` that names no such instantiation is a diagnostic.

## Designer asset

Designers author timelines as JSON data conforming to the final flat v1 schema, which the `tlbake` tool converts deterministically into standard TLB1 native bytes. The owner finalized this shape; it replaces every earlier intermediate form (track-level `trackType`/`clipType`, `track`/`payload`, grouping nodes). The contract block below is quoted verbatim from the owner decision:

```json
{
  "name": "boss_phase_one",
  "duration": 64,
  "loop": true,
  "tracks": [
    {
      "name": "main_damage",
      "namespace": "MyGame",
      "type": "DamageTrack",
      "data": { "scale": 2.0 },
      "clips": [
        { "name": "opening_hit", "namespace": "MyGame", "type": "DamageClip", "start": 0,  "end": 12, "data": { "amount": 5 } },
        { "name": "mid_stun",    "namespace": "MyGame", "type": "StunClip", "assembly": "MyGame.Combat", "start": 20, "end": 26, "data": { "seconds": 2 } },
        { "name": "heavy_hit",   "namespace": "MyGame", "type": "DamageClip", "start": 40, "end": 52, "data": { "amount": 9 } }
      ]
    },
    {
      "name": "armor_buff",
      "namespace": "MyGame",
      "type": "DamageTrack",
      "data": { "scale": 0.5 },
      "clips": [
        { "name": "ramp_up", "namespace": "MyGame", "type": "DamageClip", "start": 8, "end": 30, "data": { "amount": 3 } }
      ]
    }
  ]
}
```

Owner rules (FINAL): shape is root(name?) → tracks[] → clips[]. NO grouping nodes anywhere in authoring — no clipGroups, no trackGroups. At BOTH track and clip level: `name` OPTIONAL (pure label, never affects execution/order/keying, no uniqueness law), `namespace` REQUIRED bare name (dotted = diagnostic, `""` = global), `type` REQUIRED bare name, `assembly` OPTIONAL and required when namespace+type matches multiple loaded assemblies (diagnostic names candidates; never guessed), `data` OPTIONAL (missing = zero payload; same field name at both levels). Clips additionally carry `start`/`end`. Type identity is NEVER inherited between levels. GROUPING IS DERIVED AT BAKE: clips on one track sharing the same resolved (namespace, type, assembly) form one sub-lane = one `<TrackType, ClipType>` pair, in first-occurrence order; the track type must `IBlend<>` every distinct clip type among its clips (else diagnostic naming the pairings). Crossfades only WITHIN a derived group; different clip types on the same track overlap freely, never blend, both execute. EXECUTION ORDER = authored clip array order (the sample deliberately interleaves mid_stun between two DamageClips: opening, stun, heavy — A-B-A law unchanged, never type-grouped). Same track type twice with different data/names = separate entries (armor_buff above). The old `trackType`/`clipType` names and the intermediate schema in current docs are REPLACED by this shape.

The public conversion API lives in `tools/Tl.Gen.Tlb` (`TimelineBaker.BakeJson`, `BakerAssemblyResolver`, `TlbMetadata`); its surface is receipt-locked by `tools/Tl.Bake.Tests/Tl.Gen.Tlb.PublicApi.approved.txt`. The `tlbake` CLI (`tools/Tl.Bake`) is a thin front-end:

```sh
tlbake <input.json> <output.tlb> [--assembly <path>]... [--cache <dir>]
tlbake --strip <input.tlb> <output.tlb>
tlbake --report <input.tlb>
```

Type resolution binds names to loaded types at bake time: `namespace` matches the CLR `Type.Namespace` exactly (empty string selects the global namespace), `type` matches `Type.Name`, and `assembly` matches `Assembly.GetName().Name`. Bare names containing `.`, `,`, `+` or `=` are diagnostics. Data objects map field names onto unmanaged struct fields for explicit-width primitives (bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double); unknown fields and wrong-typed values are diagnostics. Assets may contain up to 256 authored track entries. Track array order is semantic. Windows are half-open, duration is the maximum clip end, and an empty asset has duration zero. Within one derived group at most two clips may overlap and they resolve through the group's blender; unsupported overlap fails import.

Converter contract laws:

1. **Determinism**: baking the same JSON input always yields bit-identical TLB1 bytes (including the metadata tail) and identical SHA-256 hot hashes across runs, machines and cultures.
2. **Round trip**: `bake(dump(tlb)) == tlb` holds by construction once a dump emitter lands; baking consumes the same canonical data the binary encodes.
3. **Name binding at bake time**: `namespace`/`type`/`assembly` resolve against the referenced consumer assemblies during baking only; baked assets carry pair keys, never names, so playback and distribution need no type lookup.
4. **Cache**: the opt-in `--cache <dir>` stores bake outputs under a content key derived only from bytes that affect output — the input JSON bytes, each `--assembly` file's SHA-256 in declared order, the `Tl.Gen.Tlb.BakeCacheKey.ToolVersion` string, and the output-kind flags (`--strip`) — so a hit copies byte-identical outputs and never rewrites a destination whose bytes already match (timestamps survive), a miss bakes and stores, and a bake that fails is never cached.
5. **Tool version bump**: any change to bake-affecting code paths bumps `BakeCacheKey.ToolVersion`, invalidating every stored cache entry.

`tlbake --report <input.tlb>` prints a deterministic, culture-invariant `name: value` report parsed from the TLB1 bytes alone: header-derived region sizes (total, hot, metadata, pair table, frame slots), stage and program-step counts, the runtime `TimelineComponent` instance size, and, for full assets, root/track/clip label counts from the metadata tail.

### TLB1 metadata tail

TLB1 gains an OPTIONAL trailing metadata section. The header word at byte offset 40 (previously reserved zero) becomes `metadataOffset`; absent metadata stays encoded as `metadataOffset == 0`. The tail length is `Bytes - metadataOffset`, so the hot prefix layout is otherwise unchanged and pre-tail readers accept both forms. The tail contains: (a) an interned, deduplicated, ordinal-sorted UTF-8 string pool (namespaces, type names, assemblies, labels — each stored once, referenced by index); (b) a type table of `{namespaceIdx, nameIdx, assemblyIdx}` per distinct type plus a pair-type table index-aligned with the hot pairs array, so the hot prefix references no string byte; (c) a names block recording root, track and clip labels with their authored positions. The tick path never reads the tail. Stripping is a legal distribution step: `TlbMetadata.Strip` truncates to `metadataOffset`, zeroes the header word, and rewrites `Bytes`, producing a loadable asset whose tick traces are identical to the full form — the only loss is the pretty dump. (The former kernel-catalog hash binding over the stripped hot view was removed together with the catalog under #104.)

### Migrating alpha.3 authoring

Assets authored for the pre-v1 converter shape are rejected with versioned migration diagnostics, never silently reinterpreted: `removed property 'trackType'` (split into `namespace` + `type`), `removed property 'clipType'` (likewise), `removed property 'payload'` (renamed `data`), and `renamed property 'loops'` (now `loop`); each message names `schema v1` and the fields that replace the removed one. Track and clip elements each carry their full type identity and data — grouping is derived at bake, and execution order is authored clip order. Ship assets stripped (`tlbake --strip`), and audit distribution sizes with `tlbake --report`.

## .NET consumer

Warm playback is the typed playback lane ([typed-playback-lane.md](typed-playback-lane.md)): one call advances every row exactly one frame over caller-owned columns.

```cs
using System.IO;
using Tl;

using var asset = TimelineAsset.Load(
    File.ReadAllBytes("attack.tlb"));

BakedLane<DamageTrack, DamageClip>.Bind(asset);

var positions = new ushort[] { 0, 0 };
var health = new float[] { 100f, 100f };

Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(health);
Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(health);   // catch-up
Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, false).Apply(health);  // rewind
```

`TimelineAsset.Load` is a cold validated import of already baked bytes. Its owner retains the immutable native storage; `Reference` is a borrowed unmanaged reference. All instances and readers must finish before the owner is disposed. The file path locates content and creates no required name field or generated C# identity.

`BakedLane<TTrack, TClip>.Bind(asset)` runs once per (pair, asset) at cold time: it measures the per-position forward and backward float effect of every consumer of the asset through the cold executor with one baseline evaluation per direction, and keeps two native effect tables for the life of the bind. Since #113 the frame view exposes no engine cycle and no game tick, so the former cycle and column-seed purity probes are gone; a consumer that folds the incoming column value into its write bakes its zero-seed baseline instead of being rejected at bind, and must be caught by authoring review. All rows of one `Apply` share the bound asset's movement law.

Lane consumers implement the job shape the generator discovers compilation-wide, write exactly one `ref float` column, and self-invert through `Frame.Direction`:

```cs
public readonly struct FoldDamage :
    ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        ref float health)
    {
        var amount =
            frame.Clip.Amount *
            frame.Track.Multiplier;

        health += frame.Direction * amount;
    }
}
```

`Seek` borrows the caller's position and effect columns for the call only; `Apply` validates lengths and pairwise non-overlap, applies exactly one frame per call (catch-up is repeated calls; rewind is `forward: false`), and allocates nothing. Movement is `TimelineMovement.Advance` exactly, including finite clamping and looping wrap. There is no engine cycle: hosts that need loop counts self-track, either from the movement flags (`FrameFlags.TimelineEnd` marks the wrap tick on looping timelines with `FrameFlags.Reverse` giving the sign; `CompletedAfter`/`CompletedBefore` carry the same information on finite timelines) or, on the lane, from the positions column (a row wrapped when `old == duration - 1 && new == 0` forward, or `old == 0 && new == duration - 1` backward). Consumers that need more than the folded float effect (reading other entity columns, cross-row patterns) belong to the typed frame query below or to the host coordinator, not to the lane fold.

`BakedLane<TTrack, TClip>.Bind` holds one table per closed generic, so several same-pair assets that must coexist (minion and boss variants of one timeline family) use a `TimelineSet<TTrack, TClip>` instead: `Add(asset)` assigns each loaded asset a dense `ushort` id — **ids are assigned at load time, never authored and never baked** — and `Gather(ids).Seek(positions, forward).Apply(effects)` advances the whole mixed crowd in one call, each row through its own timeline's tables, duration, and loop flag (bit-exact with per-asset static lanes). An unbound id throws naming the row; `Dispose` frees the single contiguous native block. Contract and receipts: [typed-playback-lane.md](typed-playback-lane.md).

The typed frame query `Timeline.Query<TTrack, TClip>(in TimelineComponent)` remains the read-only inspection path on .NET: it yields the selected frame for authoring tools, tests, and cold execution without advancing time.

## Unity authoring and direct system consumer

Compile this additional file only in Unity alongside the shared partial component declarations:

```cs
using Unity.Entities;

public readonly partial struct Resistance : IComponentData
{
}

public partial struct Health : IComponentData
{
}
```

These declarations cannot augment types in a separately compiled assembly. Existing gameplay components that already implement `IComponentData` need no adapter declaration.

The library provides `TimelineAuthoring` and its Baker. A designer assigns the data asset; baking creates a `TimelineComponent` referencing immutable baked storage with instance position at zero. The gameplay Baker also supplies the required component data. The game's clock system creates and writes a `TimelineClock` singleton with `uint GameTick` and signed `int Delta` before the timeline group updates. These authoring and runtime adapter types are part of the proposed implementation, not existing alpha.3 types with the same spelling.

```cs
using Tl;
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(TimelineSystemGroup))]
[BurstCompile]
public partial struct DamageSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (timeline, resistance, health) in
            SystemAPI.Query<
                RefRO<TimelineComponent>,
                RefRO<Resistance>,
                RefRW<Health>>())
        {
            foreach (var frame in
                Timeline.Query<DamageTrack, DamageClip>(
                    in timeline.ValueRO))
            {
                ApplyDamage.Execute(
                    in frame,
                    in resistance.ValueRO,
                    ref health.ValueRW);
            }
        }
    }
}
```

The outer query chooses entities and borrows their ECS components. The inner query reads the entity's currently selected stage and yields matching typed frames. It never advances time. An inactive or nonmatching stage yields no frames. It does not return every occurrence of a type from the whole timeline.

## Unity job consumer

Use this `DamageSystem` instead of the preceding direct-loop version. Both call the same shared operation; they are alternative runners, not two simultaneous subscriptions.

```cs
using Tl;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct DamageEntitiesJob : IJobEntity
{
    private void Execute(
        in TimelineComponent timeline,
        in Resistance resistance,
        ref Health health)
    {
        foreach (var frame in
            Timeline.Query<DamageTrack, DamageClip>(in timeline))
        {
            ApplyDamage.Execute(
                in frame,
                in resistance,
                ref health);
        }
    }
}

[UpdateInGroup(typeof(TimelineSystemGroup))]
[BurstCompile]
public partial struct DamageSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency =
            new DamageEntitiesJob()
                .ScheduleParallel(state.Dependency);
    }
}
```

Unity derives the query from `IJobEntity.Execute` parameters and generates a chunk job. TL must materialize any generated wrappers before Unity's Entities generator needs to read them. [Unity IJobEntity documentation](https://docs.unity.cn/Packages/com.unity.entities@1.4/manual/iterating-data-ijobentity.html)

`IJobChunk` uses the same inner query inside an enabled-entity loop. `IJobParallelFor` uses indexed native columns; `IJob` uses a sequential loop. Their container access and dependency declarations remain necessary. A frame or query enumerator is created inside execution and never retained in job fields.

The generator may provide routine host wrappers from the reusable job signature. Custom wrappers retain the same stage and dependency contract. Registration, wrapper replacement, and ordering metadata must be proven without per-asset job selection or accidental duplicate consumers.

## Unity first slice (gate 5, slice 1)

The `unity/com.iafahim.tl` package carries the first Unity host slice: `Runtime/Tl.Runtime.Unity.asmdef` compiles the shared `src/Tl.Core` runtime sources (`Hooks.cs`, `Playback.cs`, `Data.cs`) through symlinks, so `src/Tl.Core` stays the single source of truth for coordinator semantics; a .NET receipt (`tests/Tl.Core.Tests/UnitySharingReceiptTests.cs`) failed when an include stopped being byte-identical. The host extraction at `fccdc22` then materialized those symlinks as verified byte-identical snapshot copies and removed the receipt; the tl.unity README records the provenance. Unity's netstandard 2.1 profile lacks three .NET 6+/7+ surfaces these sources use, so the package carries internal host shims in `Runtime/Profile.cs`: `NativeMemory` (backed by `UnsafeUtility.Malloc`/`Free` with `Allocator.Persistent`), `Interlocked` (Monitor-backed; used only on the cold registration and dispose paths in `Data.cs`), and `MethodImplAttribute`/`MethodImplOptions` with `AggressiveOptimization` (emitted as a plain attribute, so the optimization hints are inert under Mono). `Runtime/FrameAdapter.cs` mirrors `Frame<TTrack, TClip>` with pointers instead of ref fields because `ref` fields are refused on the Unity target runtime (`CS9064`); its public surface is receipt-compared against the source declaration. `Compiled.cs` is excluded: `static abstract` interface members and `ref` fields are both refused on the Unity target runtime (`CS8919`, `CS9064`).

`Runtime/TimelineComponent.cs` defines `Tl.Unity.TimelineComponent : IComponentData` (layout-identical to the shared `Tl.TimelineComponent` — `TimelineRef Reference; ushort Position;` since the #113 slim frame view — so chunk arrays reinterpret directly; the tl.unity snapshot must be re-synced to this shape before the next joint release) and the `TimelineClock` singleton (`uint GameTick; int Delta;`). `Runtime/TimelineCoordinator.cs` defines `TimelineSystemGroup` and the `TimelineCoordinatorSystem` `ISystem`: it completes `state.Dependency`, reads the clock, and drives every chunk through the shared cold coordinator core (the .NET `TimelineQuery.TickCore` seam was removed under #104; the tl.unity snapshot retains its copy) with column pointers registered through `TimelineEcs.Column<T>()`. Both systems carry `[DisableAutoCreation]`, and hosts create them through `TimelineEcs.CreateGroup(world)`, which also registers the coordinator into the group; explicitly created systems are not auto-registered from `[UpdateInGroup]` in this Entities version. Execution is synchronous managed code on the main thread in this slice; Burst qualification of the dispatch is blocked by the managed-convention consumer seam in `src/Tl.Core` (function-pointer consumers plus the static consumer table), and the dependency contract is preserved by completing the incoming dependency before any access and leaving all writes finished before any later system runs. `ComponentSystemGroup` logs and swallows child-system exceptions, so the Unity exception-parity receipt asserts the logged exception (`LogAssert`) together with the observable executed prefix and the uncommitted position.

Fixture TLB1 assets are baked by the `tools/Tl.Bake` CLI with `unity/tools/TlBakeShim`, a .NET project whose assembly name and version match the Unity test assembly (`Tl.Runtime.Unity.Tests`, `0.0.0.0`) and which compiles the same `Tests/Fixtures.cs` sources, so the pair keys baked into the TLB1 equal the keys Unity computes at runtime. The headless proof is:

```sh
unity test tests/Tl.Unity.DataAuthored --mode PlayMode --output test-results.xml
```

with fixtures committed under `tests/Tl.Unity.DataAuthored/Assets/Resources/TlFixtures/*.bytes` and the oracle assertions mirroring the .NET `DataTests` values.

## Unmanaged warm path

The former unmanaged-convention consumer seam (`PairRuntime.ConsumeUnmanaged`, `Timeline.TickUnmanaged`, `UnmanagedTickState`) was removed under #104. The typed playback lane replaces it: its warm path is unmanaged by construction — a `ref struct` lane over caller-borrowed spans, native effect tables plus 8-byte movement records on aligned native blocks, and the function-pointer consumer seam measured once at bind time. Receipts (parity vs `TimelineMovement` law, 0 B warm allocation, capacity folds at 256 pairs/tracks, NativeAOT publish) live in [typed-playback-lane.md](typed-playback-lane.md) and `tests/Tl.Alpha`.

## Typed playback lane (compile lane successor)

The Track B generated-kernel lane (`tlbake --kernel`, `TimelineKernels`) was removed under #104: the lane reaches the same structural specialization without generated per-asset code. `BakedLane<TTrack, TClip>.Bind(asset)` measures each position once per direction through the cold executor — the authored consumer order, blend resolution, and multi-pair folds are captured exactly, forward and backward — and `Timeline<T>.Seek(positions, forward).Apply(effects)` runs the scan/apply over run-length groups (vector compare per 16 rows, singleton fast path, vector fill for positions). Since #113 the frame view is slim: timeline-domain positions and ticks are `ushort` under the single documented 65,535-tick cap (`TLB1` load rejects larger durations), the engine cycle and the game tick are gone from `Frame`, `TickFrame`, `TimelineState`, `TimelineComponent`, and the consumer `Execute`/`Range` seam, movement records halved to 8 bytes, and `Frame.ClipLength`/`Frame.WithinClip` expose the slot's window metadata that data-authored logic actually wants ("halfway through the clip"). The wrap-count question the old cycle column answered is a host concern: flags or position deltas reconstruct it exactly (receipted in `tests/Tl.Alpha`). Receipts: `tests/Tl.Core.Tests/LaneTests.cs` (authored-oracle tables, movement-law parity, rewind exactness, consumer-fault propagation, missing-pair rejection, 0 B), `tests/Tl.Alpha` (movement law at 64 staggered rows and 200 mixed-direction ticks, wrap-count reconstruction on a randomized schedule, blend fold, catch-up linearity, 200k-row capacity, 256-track module capacity), `benchmarks/Alpha --verify`, `benchmarks/TypedPlaybackProto`, and `samples/ManyEntities` (lane vs hand SoA parity).

## Coordinator and observable semantics

For each signed step, the coordinator selects each compatible row once, runs its ordered occurrence stages, and commits once after all required consumers finish. It repeats this for every available crossed frame. Different assets may have different stage counts and opposite type orders. Grouping all occurrences of one type together is invalid for A-B-A.

The Unity group must supply the selection dependency to consumers, collect every scheduled consumer handle, and pass the combined dependency to the next stage or commit. Updating systems in order alone does not establish this chain. Direct main-thread component access must complete required dependencies. The coordinator owns all temporary scheduling storage and reports its bound; the public API requires no guessed caller capacity.

A consumer system may be updated multiple times per game tick. Gameplay effects belong inside the frame query. Other independently scheduled systems must observe the completed timeline dependency before reading affected outputs. Scheduling parallel entity work does not by itself prove SIMD or cross-entity effect independence.

Forward occurrence order is authored order; reverse occurrence order is its exact reverse. Multiple consumers of one occurrence execute in registered chain order (LIFO) on forward frames and in exact reverse chain order on backward frames, making backward traversal the symmetric undo of forward traversal (including floating-point accumulation order). The dependency graph must diagnose cycles or unresolved conflicting order before effects. Cross-row mutation, shared mutable state and external side effects require an explicitly ordered execution policy or proven reduction.

An asset with no consumers for a pair still has valid timing. A registered mandatory consumer whose required columns are missing is a different condition: do not partially execute the other consumers and then commit. Validate complete-row compatibility before any selected effects. Unity baking/configuration reports missing requirements; runtime component removal, enableable components and custom query filters require an explicit eligibility/diagnostic policy before implementation can be accepted. A local no-match iteration must not conceal incomplete execution of an otherwise selected row.

Adopted eligibility policy (Unity adapter, receipted in tl.unity): candidate rows are every entity carrying the timeline component, and the coordinator's row query names only that component, so a removed mandatory column leaves the row visible to the coordinator instead of silently vanishing from an all-in-one query. Presence is chunk-uniform: a chunk whose archetype lacks a registered mandatory column excludes every row in it with reason `MissingMandatoryColumn` before any effect. For enableable mandatory columns the host reads each row's enabled bit on the main thread before any job is scheduled; a present-but-disabled row is excluded with reason `DisabledMandatoryColumn`. Exclusion is a total no-op: no consumer executes and no movement commits — column values and position stay untouched for that tick — while other eligible rows still execute. Eligibility resolves host-side before scheduling: the host emits one entry per maximal contiguous eligible run and the job body contains no eligibility logic. No concealment: every exclusion is recorded with entity, chunk, row index and reason into a preallocated diagnostic sink that throws on exhaustion rather than truncating, so a no-match iteration is always explainable — the consumer record sink shows what executed and the exclusion sink shows why the rest did not. Resolution runs on the main thread with unmanaged temporaries and adds no managed allocation to the warm path; exclusions are cleared and re-evaluated every coordinator update.

The game tick is a host-owned axis and never enters the frame view: hosts that need it carry it beside the component (exotic consumers can pack it into the seam's host-owned state). Timeline-domain positions are `ushort` under the single documented 65,535-tick cap. New instances begin at local zero regardless of the external game tick. Default empty state, gaps, empty assets, zero delta and finite completion are total no-ops. Loop counts are host-derived from the movement flags, exactly as before via `CompletedAfter`/`CompletedBefore` and the looping `TimelineEnd` wrap tick (sign from `Reverse`).

On .NET, an operation exception propagates, leaves any already executed effect prefix, and prevents that step's commit. This is not rollback. Burst jobs require validated nonthrowing execution; the API cannot promise transactional recovery from arbitrary faults. Reversing traversal does not automatically undo arbitrary effects, mutable inputs, clamping or floating-point rounding. The damage example illustrates directional behavior, not a proof of bit-exact inverse arithmetic.

## Implementation gates

Mutable tasks and checkpoints live in issue #56. Implement in this order:

1. Prove generated .NET facade/column binding and direct stage-query consumption on a small executable fixture.
2. Define the canonical neutral asset format, stable type mapping, layout/version checks, native ownership, safe publication and reader reclamation.
3. Implement select/query/execute/commit with exact oracle traces for heterogeneous assets, A-B-A, opposing orders, multiple consumers, blends, every crossed frame, reverse movement, defaults and loop boundaries.
4. Prove complete-row compatibility, role ambiguity diagnostics, writable alias rules, consumer ordering and manual/generated runner identity.
5. Prove Unity direct-system and IJobEntity parity, enabled masks and missing components, with delayed jobs showing no early overwrite or commit. Keep compiler assemblies out of players.
6. Add designer import/baking, diagnostics, deterministic cache and generation/static/native/per-instance memory reports. Asset data must be reusable through .NET and Unity adapters without duplicating selection semantics.
7. Benchmark full public workloads against alpha.3; report scalar latency separately from throughput, allocations, native retention, code size and cache behavior. Validate NativeAOT and Burst. Then update API approvals, migration documentation and the next release gate.

Production source plus UTF-8 relative paths remains bounded by 300,000 bytes, and the runtime data path is unmanaged: no managed arrays, registries, or caches on or reachable from playback, query, or asset storage — the runtime must stay loadable by ECS/Burst job compilation. Baking, import, and generator tooling may use managed types; their output must be unmanaged. Runtime-loaded assets and their ownership are new proof obligations; the old process-lifetime static-data proof does not cover them. No warm allocation, hidden reflection or runtime compilation is authorized by this design. Preknown type signatures remove wiring overhead but do not prove that a data-driven lookup is faster than a fully specialized per-asset kernel. The existing measured <3 ns results do not transfer to this architecture without new benchmarks. C catalog implementation remains deferred; keep its future backend language-neutral.
