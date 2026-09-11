# tl

`tl` turns immutable gameplay timelines into typed C# programs. One timeline can contain many track and clip types, and one generated query advances thousands of entities through the same ordered schedule without reflection, delegates, runtime compilation, or warm-path allocation.

**Status: v1.0.0-alpha.3 prerelease.** The C# API is a breaking prerelease for .NET 10 and C# 14. Unity ECS is qualified on the stable and preview lanes listed below.

- Heterogeneous tracks and clips in one timeline
- Each authored track binds one typed operation to its `(track, clip)` pair
- Any finite set of unmanaged `in` and `ref` component slots
- Deterministic forward and reverse occurrence order
- Generated schema queries over borrowed component columns
- Total finite completion and explicit looping
- Automatic incremental generation during normal and IDE builds
- NativeAOT-safe runtime output with no compiler assemblies
- Language-neutral immutable schedule below the C# frontend

## Packages

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended C# install: runtime plus automatic compiler |
| `Tl.Runtime` | Small declaration, frame, state, and movement ABI |
| `Tl.Gen.CSharp` | C# declaration reader and generated query backend |
| `Tl.Compiler` | Language-neutral validated ordered schedule |
| `Tl.Gen.C` | Existing portable C11 ABI v2 backend; alpha.3 catalogs are not emitted yet |
| `Tl.Unity` | Unity ECS/Burst adapter, packaged and qualified independently |

## Install

Download `Tl.CSharp.1.0.0-alpha.3.nupkg` and `Tl.Runtime.1.0.0-alpha.3.nupkg` from the [GitHub prerelease](https://github.com/IAFahim/tl/releases/tag/v1.0.0-alpha.3) into `packages`, then install from that local source:

```sh
dotnet add package Tl.CSharp --version 1.0.0-alpha.3 --source ./packages
```

The packages are not published to nuget.org.

The package generator runs whenever Roslyn compiles the project, including supporting IDE design-time builds. There is no `Build`, `Compile`, `InMemory`, `Bind`, registry, or interpreted fallback in the alpha.3 execution path.

## Define gameplay operations

The quick start separates code you author from code the compiler generates. You author the domain values, jobs, timeline `Attack`, schema marker `CombatRows`, and catalog `Combat`. `Tl` supplies `IBlend<TClip>`, `ITimelineJob<TTrack,TClip>`, `Frame<TTrack,TClip>`, `ITimeline`, `Builder`, `ITimelineCatalog`, and `CatalogBuilder`. Compilation then completes the two `partial` declarations and creates the catalog API used below.

Track values hold immutable settings. Clip values hold immutable authored payload. A job describes what one active `(track, clip)` pair does to borrowed component storage.

```cs
using Tl;

public readonly record struct Pose(float X, float Y);
public readonly record struct Health(float Value);
public readonly record struct Resistance(float Scale);
public readonly record struct AnimationClip(float X, float Y);
public readonly record struct DamageClip(float Amount);

public readonly record struct AnimationTrack(int Order) : IBlend<AnimationClip>
{
    public void Blend(
        in AnimationClip first,
        in AnimationClip second,
        float factor,
        out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);
}

public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
{
    public void Blend(
        in DamageClip first,
        in DamageClip second,
        float factor,
        out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public readonly struct AnimationJob : ITimelineJob<AnimationTrack, AnimationClip>
{
    public static void Execute(
        in Frame<AnimationTrack, AnimationClip> frame,
        ref Pose pose)
        => pose = new(
            pose.X + frame.Direction * frame.Clip.X,
            pose.Y + frame.Direction * frame.Clip.Y);
}

public readonly struct DamageJob : ITimelineJob<DamageTrack, DamageClip>
{
    public static void Execute(
        in Frame<DamageTrack, DamageClip> frame,
        in Resistance resistance,
        ref Health health)
        => health = new(
            health.Value - frame.Direction * frame.Clip.Amount
                * frame.Track.Multiplier * resistance.Scale);
}
```

`in` declares a borrowed read-only component column. `ref` declares a borrowed writable column. If the same named, typed slot is read by one job and written by another, the generated schema exposes it as writable. `out` is intentionally unsupported in alpha.3 because a skipped occurrence cannot satisfy C# definite assignment without inventing a value.

## Author one heterogeneous timeline

`Attack` is an authored partial timeline declaration. `Use<TJob>()` binds behavior to a track. Authored order is semantic, so the following timeline executes animation, damage, animation at every frame where all three are active. The compiler completes `Attack` with immutable timeline data, count and size metadata, and the internal selection, execution, and commit code.

```cs
public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var opening = builder.Track(new AnimationTrack(1)).Use<AnimationJob>();
        var impact = builder.Track(new DamageTrack(2f)).Use<DamageJob>();
        var followThrough = builder.Track(new AnimationTrack(3)).Use<AnimationJob>();

        builder.Clip(opening, new AnimationClip(2f, 1f), 0u, 20u);
        builder.Clip(impact, new DamageClip(10f), 0u, 20u);
        builder.Clip(followThrough, new AnimationClip(1f, 0f), 0u, 20u);
    }
}
```

Two clips may overlap on one track. The generated kernel calls `Blend` once and passes one resolved frame to that track's job. `builder.Before<THook>()`, `builder.After<THook>()`, `builder.Include<TTimeline>()`, and `builder.Looping()` add explicit composition and lifecycle semantics.

## Declare a catalog and use its generated .NET query

`CombatRows` and `Combat` are authored declarations. A schema is a user-named empty unmanaged marker such as `CombatRows`; it does not implement an interface, and there is no `ITimelineSchema`. The catalog gives each schema a closed set of valid timelines, and the generator derives its component columns from the jobs in those timelines.

```cs
public readonly struct CombatRows;

public readonly partial struct Combat : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<CombatRows>().Asset<Attack>();
    }
}
```

`Asset<Attack>()` closes `CombatRows` membership at compile time and causes the compiler to create a catalog-local route for `Attack`. It does not allocate an asset or register one at runtime.

Together, the declarations above form `Timelines.cs`; the following generated API use can live in `Program.cs`. Compilation adds `Combat.Asset` with `None` and `Attack` routes, `Combat.State` for each row's playback state, `Combat.Query`, and its `CombatRows(...)` query-construction method. The application owns the state and component arrays passed to that generated query.

```cs
var states = new[]
{
    new Combat.State(Combat.Asset.Attack),
    new Combat.State(Combat.Asset.Attack),
    new Combat.State(Combat.Asset.None),
};
var poses = new Pose[states.Length];
var resistance = new[]
{
    new Resistance(1f),
    new Resistance(0.5f),
    new Resistance(1f),
};
var health = new[]
{
    new Health(100f),
    new Health(100f),
    new Health(100f),
};

var query = new Combat.Query().CombatRows(states, poses, resistance, health);
query.Tick(gameTick: 200_000u, delta: 3);
query.Tick(gameTick: 200_003u, delta: -3);
```

`Tick(G, +N)` emits game ticks `G` through `G + N - 1`. `Tick(G, -N)` emits `G - 1` through `G - N`. The game tick is supplied by the caller; timeline state stores only catalog-local asset, position, cycle, and internal pending selection.

For each simulation step, the query selects every row once, executes compatible rows stage by stage, and commits each selected row once after all stages. This preserves A→B→A for one entity while allowing each typed operation stage to process every compatible entity. Reverse playback executes the exact reversed occurrence order. Finite assets stop contributing when complete; other rows continue. Asset `None` is empty, and default state is valid empty state.

Schema construction checks equal column lengths, route membership, and prohibited writable overlap. `Tick` rechecks mutable routes before effects. Configuration errors throw before callbacks; ordinary completion is a no-op. A job exception propagates, retains its already-executed effect prefix, and prevents state commit for that step.

## Frame data

`Frame<TTrack,TClip>` borrows the immutable track and resolved clip. It also exposes:

| Member | Meaning |
| --- | --- |
| `GameTick` | External simulation tick for this emitted frame |
| `TimelineTick` | Local normalized tick in the asset |
| `Cycle` | Signed loop cycle of the emitted frame |
| `TrackIndex` | Stable authored track index, `0..255` |
| `Flags` | Independent clip, timeline, completion, loop, and reverse facts |
| `Direction` | `-1` when `Reverse` is present; otherwise `1` |

Frames exist only during `Execute`. Jobs must not retain their borrowed references.

## Generated reports

Normal compilation owns generated sources inside the compiler. Run the explicit export target when a standalone, content-stable snapshot is useful for review, another build pipeline, or size inspection:

```sh
dotnet msbuild -t:TlGenExport -p:Configuration=Release
```

The export writes generated `.g.cs`, a manifest, and `TlGenCompile.report.txt` under `obj/Release/<tfm>/TlGenCompile`. A repeated identical invocation is a cache hit and preserves generated content. Reports include timeline, catalog, track, clip, operation, slot, region, occurrence, unique schedule, unique payload, neutral byte, generated source byte, static data, and state counts.

Generated catalogs expose `AssetCount`, `StateBytes`, and `StaticDataBytes`. Generated timelines expose `Duration`, `Loops`, `TrackCount`, `ClipCount`, `MaxStageCount`, and `StaticDataBytes`.

## Performance contract

The hot path is allocation-free after warmup. The source generator specializes region boundaries, payload storage, blend facts, operation calls, stage order, and schema routing. The release benchmark compares the full generated query against an independent direct oracle with identical observable work. Scalar latency and multi-entity throughput are reported separately; the below-3-ns goal applies only to its named hot workload and is never inferred from a partial inner loop.

The repository enforces a 250,000-byte budget over production source contents plus relative UTF-8 paths. Generated source, static data, per-entity state, managed/native output, scratch, and allocations are measured separately.

## Unity ECS

Unity uses the same authored jobs and neutral ordered schedule with host-specific storage and scheduling. Generated Unity selectors and typed operation jobs operate over ECS columns, followed by one state commit. Unity source is materialized before Unity script compilation so Entities can generate its own jobs. The qualified stable lane is Unity 6000.0.83f1, Entities 1.4.3, and Burst 1.8.30; the preview lane is Unity 6000.7.0a5, Entities 6.7.0, Collections 6.7.0, and Burst 2.0.0. EditMode and PlayMode pass 4/4 on both lanes. Stable Mono and IL2CPP players execute the generated Burst jobs, print the expected marker, contain Burst symbols, and exclude compiler, generator, and Roslyn assemblies. The 10,000-row stable fixture measures 18.006 ns/entity-step, 20 scheduled jobs/step, and 0 main-thread managed B after warmup. See the [Unity guide](docs/unity.md).

## Limits

- At most 256 authored tracks per timeline
- At most two active clips on one track
- Half-open clip windows `[start, end)` with constant `uint` bounds
- Unmanaged track settings, clip payloads, and component slots
- `in` and `ref` operation slots; no `out` slots in alpha.3
- Row-local effects only in the parallel scheduling model
- Arbitrary looping deltas perform every observable effect and are proportional to the requested work
- Designer GUI authoring, C catalog emission, cross-generated declarations, and runtime-loaded arbitrary schemas are outside alpha.3

## Repository map

| Path | Role |
| --- | --- |
| `src/Tl.Core` | Runtime declarations, borrowed frames, state, and total movement |
| `src/Tl.Compiler` | Language-neutral ordered schedule and validation |
| `src/Tl.Gen.CSharp` | C# frontend, generated query backend, and export tool |
| `src/Tl.CSharp` | One-package C# installation |
| `src/Tl.Gen.C` | Existing portable C11 backend |
| `src/Tl.Unity` | Qualified Unity ECS/Burst runtime package |
| `samples/Mixed` | Heterogeneous A→B→A catalog/query sample |
| `tests/Tl.Alpha` | Generated behavior, schema, scale, and allocation receipts |
| `tests/Tl.PackageConsumer` | Isolated package-only JIT and NativeAOT consumer |
| `benchmarks/Alpha` | Full-query oracle, latency, throughput, assembly, and PMU evidence |

Read the [API contract](docs/v1.0-alpha-api.md), [execution semantics](docs/semantics.md), [migration guide](docs/v1.0-alpha-migration.md), [architecture](docs/architecture.md), and [implementation plan](plan.md).

## Validate

```sh
python3 benchmarks/source_budget.py
python3 -m unittest discover -s benchmarks -p test_collect.py
python3 -m unittest discover -s tests -p test_release_artifacts.py
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project samples/Mixed -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```
