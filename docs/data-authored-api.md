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

## Designer asset

The editor writes data of this shape. This example is not a finalized interchange schema or a required handwritten JSON workflow.

```json
{
  "looping": false,
  "tracks": [
    {
      "type": "DamageTrack",
      "data": { "Multiplier": 2 },
      "clips": [
        {
          "start": 5,
          "end": 6,
          "data": { "Amount": 10 }
        },
        {
          "start": 12,
          "end": 13,
          "data": { "Amount": 20 }
        }
      ]
    }
  ]
}
```

Track type identity determines the clip type and blend implementation. Assets may contain up to 256 authored tracks, including heterogeneous and repeated type pairs. Track array order is semantic. Windows are half-open, duration is the maximum clip end, and an empty asset has duration zero. Two overlapping clips on a track resolve through its blender; unsupported overlap fails import. Canonical data deduplication never merges distinct authored occurrences or changes their order.

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

## Coordinator and observable semantics

For each signed step, the coordinator selects each compatible row once, runs its ordered occurrence stages, and commits once after all required consumers finish. It repeats this for every available crossed frame. Different assets may have different stage counts and opposite type orders. Grouping all occurrences of one type together is invalid for A-B-A.

The Unity group must supply the selection dependency to consumers, collect every scheduled consumer handle, and pass the combined dependency to the next stage or commit. Updating systems in order alone does not establish this chain. Direct main-thread component access must complete required dependencies. The coordinator owns all temporary scheduling storage and reports its bound; the public API requires no guessed caller capacity.

A consumer system may be updated multiple times per game tick. Gameplay effects belong inside the frame query. Other independently scheduled systems must observe the completed timeline dependency before reading affected outputs. Scheduling parallel entity work does not by itself prove SIMD or cross-entity effect independence.

Forward occurrence order is authored order; reverse occurrence order is its exact reverse. Multiple consumers of one occurrence need an explicit observable order and a corresponding reverse order. The dependency graph must diagnose cycles or unresolved conflicting order before effects. Cross-row mutation, shared mutable state and external side effects require an explicitly ordered execution policy or proven reduction.

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
