# Data-authored timelines and typed frame queries

Approved by the owner on 2026-09-11. [Issue #56](https://github.com/IAFahim/tl/issues/56) owns implementation and live acceptance status. This document freezes the next consumer API direction. It does not describe implemented alpha.3 functionality, qualify a new release, or freeze a binary ABI.

The owner subsequently paused work; implementation has since resumed on `feat/56-data-authored-api`. Gates 1 through 4 receipts — generated facade and column binding, the TLB1 native format with ownership and safe publication, oracle-traced select/query/execute/commit semantics including A-B-A, opposing orders, blends, crossed frames and reverse movement, and complete-row compatibility diagnostics — plus the gate-7 first-pass shape benchmarks have landed at commit `548c802022a9e12eb4ab65da4fad6d94d8876b4a`. Remaining work: the consumer-chain reverse order tail of gate 4, Unity parity (gate 5), designer import (gate 6), standalone job discovery, and removal of the superseded alpha.3 surface. The contract body below remains frozen as approved; read the latest issue comments before treating any checklist item as authorization to work.

The [alpha.3 API](v1.0-alpha-api.md) remains the shipped contract. New APIs below require a generator and runtime implementation with executable receipts before being advertised as available.

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

Multi-pairing is supported: one track type may implement `IBlend` for several clip types and each `(TTrack, TClip)` instantiation is a first-class pair (runtime registration and frame keys are pair-scoped). A job type may implement `ITimelineJob<TTrack, TClip>` for several pairings; each pairing is discovered and registered as its own consumer in lexical order of the pair type names, and each pairing requires one accessible `Execute` beginning with `in Frame<TTrack, TClip>` for that pair. An authored `Track(settings).Use<TJob>()` resolves the pairing whose track type equals the authored track type. In designer JSON, a track entry whose track type implements `IBlend<>` exactly once may omit `clipType`; a multi-`IBlend` track type must declare `clipType` naming one of its implemented instantiations, and a declared `clipType` that names no such instantiation is a diagnostic.

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
tlbake <input.json> <output.tlb> [--assembly <path>]...
tlbake <input.json> <output.tlb> --kernel <out.g.cs>
tlbake --strip <input.tlb> <output.tlb>
```

Type resolution binds names to loaded types at bake time: `namespace` matches the CLR `Type.Namespace` exactly (empty string selects the global namespace), `type` matches `Type.Name`, and `assembly` matches `Assembly.GetName().Name`. Bare names containing `.`, `,`, `+` or `=` are diagnostics. Data objects map field names onto unmanaged struct fields for explicit-width primitives (bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double); unknown fields and wrong-typed values are diagnostics. Assets may contain up to 256 authored track entries. Track array order is semantic. Windows are half-open, duration is the maximum clip end, and an empty asset has duration zero. Within one derived group at most two clips may overlap and they resolve through the group's blender; unsupported overlap fails import.

Converter contract laws:

1. **Determinism**: baking the same JSON input always yields bit-identical TLB1 bytes (including the metadata tail) and identical SHA-256 hot hashes across runs, machines and cultures.
2. **Round trip**: `bake(dump(tlb)) == tlb` holds by construction once a dump emitter lands; baking consumes the same canonical data the binary encodes.
3. **Name binding at bake time**: `namespace`/`type`/`assembly` resolve against the referenced consumer assemblies during baking only; baked assets carry pair keys, never names, so playback and distribution need no type lookup.

### TLB1 metadata tail

TLB1 gains an OPTIONAL trailing metadata section. The header word at byte offset 40 (previously reserved zero) becomes `metadataOffset`; absent metadata stays encoded as `metadataOffset == 0`. The tail length is `Bytes - metadataOffset`, so the hot prefix layout is otherwise unchanged and pre-tail readers accept both forms. The tail contains: (a) an interned, deduplicated, ordinal-sorted UTF-8 string pool (namespaces, type names, assemblies, labels — each stored once, referenced by index); (b) a type table of `{namespaceIdx, nameIdx, assemblyIdx}` per distinct type plus a pair-type table index-aligned with the hot pairs array, so the hot prefix references no string byte; (c) a names block recording root, track and clip labels with their authored positions. The tick path never reads the tail. Stripping is a legal distribution step: `TlbMetadata.Strip` truncates to `metadataOffset`, zeroes the header word, and rewrites `Bytes`, producing a loadable asset whose tick traces are identical to the full form — the only loss is the pretty dump.

Kernel hash binding: the compiled-kernel catalog binds the metadata-stripped form of the asset. The emitter hashes the stripped bytes, and the runtime hashes exactly the loaded `Bytes` region; a stripped asset therefore keeps its kernel binding, while a full metadata-bearing asset hashes extra tail bytes and silently keeps the interpreter. Ship kernel-bound assets stripped. Receipts: same JSON bakes byte-identically twice; stripped JSON bakes equal equivalent code-authored `Baker` output byte for byte; full and stripped forms produce identical tick traces forward and backward; `KernelEmitter.Emit(full) == Emit(stripped)`; spy-kernel dispatch hits stripped bytes and not full bytes.

## .NET consumer

```cs
using System.IO;
using Tl;

using var asset = TimelineAsset.Load(
    File.ReadAllBytes("attack.tlb"));

var timelines = new[]
{
    new TimelineComponent(asset.Reference),
    new TimelineComponent(asset.Reference)
};

var resistance = new[]
{
    new Resistance(1f),
    new Resistance(0.5f)
};

var health = new[]
{
    new Health { Value = 100f },
    new Health { Value = 100f }
};

var query = Timeline.Rows(timelines)
    .Read(resistance)
    .Write(health);

query.Tick(gameTick: 200_000u, delta: 1);
query.Tick(gameTick: 200_001u, delta: 5);
query.Tick(gameTick: 200_006u, delta: -2);
```

`TimelineAsset.Load` is a cold validated import of already baked bytes. Its owner retains the immutable native storage; `Reference` is a borrowed unmanaged reference. All instances and readers must finish before the owner is disposed. The file path locates content and creates no required name field or generated C# identity.

`Timeline.Rows` borrows caller-owned state and aligned component columns. `Read` exposes read-only access and `Write` exposes writable access. Construction checks lengths, complete required component sets, supported asset types, and prohibited writable aliasing before effects. A row can reference a different asset from its neighbors. Replacing an asset or changing membership must pass compatibility validation before subsequent effects.

The concise column syntax applies when component roles are unambiguous. Multiple roles of the same value type must never be guessed from argument position or local variable names. Until explicit role binding is designed and proven, an ambiguous declaration or binding receives a build diagnostic. Arbitrary unmanaged input/output arity remains the goal; silently narrowing behavior is forbidden.

The generator supplies the type-specific execution path behind this facade. A source generator cannot add members to a type in an already compiled assembly. The prototype must prove where the facade, generated binding, and public runtime primitives live before production implementation proceeds.

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

The `unity/com.iafahim.tl` package carries the first Unity host slice: `Runtime/Tl.Runtime.Unity.asmdef` compiles the shared `src/Tl.Core` runtime sources (`Hooks.cs`, `Playback.cs`, `Data.cs`) through symlinks, so `src/Tl.Core` stays the single source of truth for coordinator semantics; a .NET receipt (`tests/Tl.Core.Tests/UnitySharingReceiptTests.cs`) fails when an include stops being byte-identical. Unity's netstandard 2.1 profile lacks three .NET 6+/7+ surfaces these sources use, so the package carries internal host shims in `Runtime/Profile.cs`: `NativeMemory` (backed by `UnsafeUtility.Malloc`/`Free` with `Allocator.Persistent`), `Interlocked` (Monitor-backed; used only on the cold registration and dispose paths in `Data.cs`), and `MethodImplAttribute`/`MethodImplOptions` with `AggressiveOptimization` (emitted as a plain attribute, so the optimization hints are inert under Mono). `Runtime/FrameAdapter.cs` mirrors `Frame<TTrack, TClip>` with pointers instead of ref fields because `ref` fields are refused on the Unity target runtime (`CS9064`); its public surface is receipt-compared against the source declaration. `Compiled.cs` is excluded: `static abstract` interface members and `ref` fields are both refused on the Unity target runtime (`CS8919`, `CS9064`).

`Runtime/TimelineComponent.cs` defines `Tl.Unity.TimelineComponent : IComponentData` (`TimelineRef Reference; uint Position; long Cycle;` — layout-identical to the shared `Tl.TimelineComponent`, so chunk arrays reinterpret directly) and the `TimelineClock` singleton (`uint GameTick; int Delta;`). `Runtime/TimelineCoordinator.cs` defines `TimelineSystemGroup` and the `TimelineCoordinatorSystem` `ISystem`: it completes `state.Dependency`, reads the clock, and drives every chunk through the shared `TimelineQuery.TickCore` with column pointers registered through `TimelineEcs.Column<T>()`. Both systems carry `[DisableAutoCreation]`, and hosts create them through `TimelineEcs.CreateGroup(world)`, which also registers the coordinator into the group; explicitly created systems are not auto-registered from `[UpdateInGroup]` in this Entities version. Execution is synchronous managed code on the main thread in this slice; Burst qualification of the dispatch is blocked by the managed-convention consumer seam in `src/Tl.Core` (function-pointer consumers plus the static consumer table), and the dependency contract is preserved by completing the incoming dependency before any access and leaving all writes finished before any later system runs. `ComponentSystemGroup` logs and swallows child-system exceptions, so the Unity exception-parity receipt asserts the logged exception (`LogAssert`) together with the observable executed prefix and the uncommitted position.

Fixture TLB1 assets are baked by the `tools/Tl.Bake` CLI with `unity/tools/TlBakeShim`, a .NET project whose assembly name and version match the Unity test assembly (`Tl.Runtime.Unity.Tests`, `0.0.0.0`) and which compiles the same `Tests/Fixtures.cs` sources, so the pair keys baked into the TLB1 equal the keys Unity computes at runtime. The headless proof is:

```sh
unity test tests/Tl.Unity.DataAuthored --mode PlayMode --output test-results.xml
```

with fixtures committed under `tests/Tl.Unity.DataAuthored/Assets/Resources/TlFixtures/*.bytes` and the oracle assertions mirroring the .NET `DataTests` values.

## Unmanaged-convention consumer seam (gate 5, slice 2 preparation)

`src/Tl.Core` now carries an unmanaged-convention consumer seam beside the managed one.

- `PairRuntime<TTrack, TClip>.ConsumeUnmanaged` installs `delegate* unmanaged` bind/execute entries into a second native table with the same layout, capacity laws, and LIFO chain discipline. Managed and unmanaged consumers of one pair coexist, and each tick arm dispatches only its own table while preserving authored order, mirrored backward consumer order, blend-once resolution, and independent clamping.
- `Timeline.TickUnmanaged` borrows caller-owned rows (`Tl.TimelineComponent*`), column base pointers, a zeroed `UnmanagedTickState` (the materialized-asset stamp plus chain and column scratch), and a caller-materialized column-type key buffer, then drives the pure-pointer `UnmanagedTick.TickCoreUnmanaged` walk over native memory using the shared `TimelineMovement` math. When the stamp equals the row asset address the per-asset binding is already materialized; otherwise the caller re-materializes the keys before the call.
- Unmanaged consumers are non-throwing by contract: exceptions cannot cross the unmanaged-convention boundary, so exception propagation and throwing missing-column diagnostics remain properties of the managed arm.
- Unity/Burst qualification of the Bursted job coordinator that calls this core lands in the next atom. The seam itself is proven on .NET by parity, zero-allocation, dual-convention, and compacting-GC receipts, and it must keep passing NativeAOT publish.

## Compile lane (Track B, phase 1)

Baked assets can be bound to a generated source kernel that replaces only the structural walk (header loads, stage search, step addressing) with compile-time constants; movement math, consumer tables, bind state and identity state stay the shared interpreter machinery, so kernel and interpreter are identical by construction rather than merely tested-equal.

- Emission: `tlbake <input.json> <output.tlb> ... --kernel <out.g.cs>` bakes with the existing staging code and emits, for exactly those bytes, an internal `TimelineKernel_<sha256>` class whose `Tick(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)` is generated from the TLB1 header. Duration, looping, stage bounds, per-step slot offsets and pair indexes are literals; single-stage assets emit the flat path with no header load. Movement goes through the public `TimelineMovement.Select` with constant duration and loop arguments; consumer execution goes through `TimelineKernels.Chain`, which walks the same `PairTable` fn-ptr chain in the same LIFO order forward and the exact mirrored order backward. Generated files are deterministic, culture-independent and byte-identical for identical bytes; committed fixture regeneration is receipt-tested.
- Binding: the generated class registers hash-to-kernel through `TimelineKernels.Register` in a `[ModuleInitializer]` (cold, managed statics beside `PairTable`'s managed registration). `TimelineQuery` captures the kernel once per query at its cold build: rows must reference one asset address with at most 16 pairs, then the asset block (byte-identical to the baked bytes, so its SHA-256 equals the bake-time hash) is hashed and looked up in the catalog. A miss keeps the interpreter silently; that is the fallback receipt. The catalog lives in a .NET-host static probed through a null function pointer, so hosts that compile `Data.cs` without the catalog keep the interpreter unchanged.
- Dispatch: the warm `Tick` path pays one null-check branch; a bound query ticks the kernel after re-validating that every row still references the captured asset address, falling back to the interpreter otherwise.
- Phase-1 scope: structure constants only. Consumers still execute through the registered fn-ptr seam and read track, clip and blend data from the asset slots, so blend factors stay slot-resident; folding consumer calls and payloads into the kernel is phase 2.
- Rule: one query dispatches the kernel only when all of its rows reference the asset the kernel was captured for. A query whose rows span different assets, contain default components, or carry more than 16 pairs uses the interpreter for its whole lifetime.
- Receipts (tests/Tl.Core.Tests/KernelTests.cs): parity on identical-structure bytes for forward/backward/clamp, looping cycles and uint wrap, A-B-A mirrored consumers, blend-once including the span-1 window, zero-consumer and empty assets, multi-row two-pass commit; spy-kernel dispatch on exact bytes; hash-miss fallback; warm-path 0 B allocation over a 100k-tick window; compacting-GC column continuity. `benchmarks/Alpha --verify` proves OneTrack kernel-bound facade hashes equal the interpreter and compiled lanes.

## Coordinator and observable semantics

For each signed step, the coordinator selects each compatible row once, runs its ordered occurrence stages, and commits once after all required consumers finish. It repeats this for every available crossed frame. Different assets may have different stage counts and opposite type orders. Grouping all occurrences of one type together is invalid for A-B-A.

The Unity group must supply the selection dependency to consumers, collect every scheduled consumer handle, and pass the combined dependency to the next stage or commit. Updating systems in order alone does not establish this chain. Direct main-thread component access must complete required dependencies. The coordinator owns all temporary scheduling storage and reports its bound; the public API requires no guessed caller capacity.

A consumer system may be updated multiple times per game tick. Gameplay effects belong inside the frame query. Other independently scheduled systems must observe the completed timeline dependency before reading affected outputs. Scheduling parallel entity work does not by itself prove SIMD or cross-entity effect independence.

Forward occurrence order is authored order; reverse occurrence order is its exact reverse. Multiple consumers of one occurrence execute in registered chain order (LIFO) on forward frames and in exact reverse chain order on backward frames, making backward traversal the symmetric undo of forward traversal (including floating-point accumulation order). The dependency graph must diagnose cycles or unresolved conflicting order before effects. Cross-row mutation, shared mutable state and external side effects require an explicitly ordered execution policy or proven reduction.

An asset with no consumers for a pair still has valid timing. A registered mandatory consumer whose required columns are missing is a different condition: do not partially execute the other consumers and then commit. Validate complete-row compatibility before any selected effects. Unity baking/configuration reports missing requirements; runtime component removal, enableable components and custom query filters require an explicit eligibility/diagnostic policy before implementation can be accepted. A local no-match iteration must not conceal incomplete execution of an otherwise selected row.

`Tick(G,+N)` emits game ticks `G` through `G+N-1`; `Tick(G,-N)` emits `G-1` through `G-N`. Game-tick arithmetic wraps as `uint`; local position and signed cycle remain separate. New instances begin at local zero regardless of the external game tick. Default empty state, gaps, empty assets, zero delta and finite completion are total no-ops. Loops retain independent per-instance cycles.

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
