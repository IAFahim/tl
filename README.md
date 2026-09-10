# tl

`tl` compiles immutable gameplay timelines into allocation-free playback kernels. A timeline may contain different track and clip types, and every generated call borrows the exact component data its operations require.

- Signed seek replays every crossed frame in deterministic order
- Generated typed facade for the fastest known-timeline path
- Generated dynamic facade for runtime-selected `ushort` IDs
- Any finite set of `in`, `ref`, and `out` component slots
- Zero managed allocation after warmup
- .NET JIT and NativeAOT package-consumer verification
- Language-neutral compiler plan with separate C# and C backends

**Status: v1.0.0-alpha.2 candidate.** This is a breaking alpha for .NET 10 and C# 14.

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended C# package; adds the build-only compiler and runtime |
| `Tl.Runtime` | Playback ABI, declarations, frames, and dynamic registry |
| `Tl.Gen.CSharp` | C# frontend and generated-kernel backend |
| `Tl.Compiler` | Language-neutral immutable plan, validation, region lowering, and extension contract |
| `Tl.Gen.C` | Independently versioned portable C11 backend |
| `Tl.Unity` | C# 9 UPM runtime for checked-in Unity ECS and Burst kernels |

## Install

```sh
dotnet add package Tl.CSharp --prerelease
```

Normal builds and IDE design-time builds discover `ITimeline` declarations through the incremental Roslyn generator. The compiler owns generated sources and reuses structurally equal inputs across edits. Run `dotnet msbuild -t:TlGenExport` when deterministic standalone source files and a generation report are needed under `obj/<configuration>/<tfm>/TlGenCompile`. Applications ship `Tl.Runtime`; the package-only checks prove that the generator, compiler, and Roslyn assemblies do not enter managed or NativeAOT output.

## Build a combo attack

```cs
using Tl;

public readonly record struct Pose(float X, float Y);
public readonly record struct AnimationSettings(float Weight);
public readonly record struct AnimationClip(float X, float Y);
public readonly record struct Health(float Value);
public readonly record struct DamageSettings(float Multiplier);
public readonly record struct DamageClip(float Amount);

public readonly struct AnimationTrack : ITrack<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);

    public static void Seek(
        in Frame<AnimationTrack, AnimationClip> frame,
        in Pose currentPose,
        in AnimationSettings animationSettings,
        out Pose nextPose)
        => nextPose = new(
            currentPose.X + frame.Direction * frame.Clip.X * animationSettings.Weight,
            currentPose.Y + frame.Direction * frame.Clip.Y * animationSettings.Weight);
}

public readonly struct DamageTrack : ITrack<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    public static void Seek(
        in Frame<DamageTrack, DamageClip> frame,
        in Health currentHealth,
        in DamageSettings damageSettings,
        out Health nextHealth)
        => nextHealth = new(
            currentHealth.Value - frame.Direction * frame.Clip.Amount * damageSettings.Multiplier);
}

public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var animation = builder.Track(new AnimationTrack());
        var damage = builder.Track(new DamageTrack());
        builder.Clip(animation, new AnimationClip(2f, 1f), 0u, 40u);
        builder.Clip(animation, new AnimationClip(6f, 3f), 20u, 60u);
        builder.Clip(damage, new DamageClip(10f), 10u, 11u);
        builder.Clip(damage, new DamageClip(20f), 40u, 41u);
    }
}
```

`Frame<TTrack,TClip>` exposes borrowed `Track` and resolved `Clip` values plus `GameTick`, `TimelineTick`, signed `Cycle`, `TrackIndex`, `Flags`, and the derived `Direction` value `1` or `-1`. The callback parameters after the frame declare the timeline's component schema. The generator produces the borrowed context and direct calls.

## Run the typed facade

```cs
var currentPose = new Pose(0f, 0f);
var animationSettings = new AnimationSettings(1f);
var currentHealth = new Health(100f);
var damageSettings = new DamageSettings(1f);
var nextPose = currentPose;
var nextHealth = currentHealth;
var playback = Attack.Start(20_000u);
var data = new Attack.Data(
    ref playback,
    in animationSettings,
    in currentHealth,
    in currentPose,
    in damageSettings,
    ref nextHealth,
    ref nextPose);

if (!Attack.TrySeek(ref data, 11))
    return;

if (!Attack.TrySeek(ref data, -6))
    return;
```

`Start(20_000u)` starts timeline position zero at game tick 20,000. `TrySeek(ref data, 11)` executes local frames 0 through 10. The following `TrySeek(ref data, -6)` executes local frames 10 through 5 in reverse order. The resulting playback is at position 5 and game tick 20,005. A delta is an amount of simulation, not a destination sample.

`Attack.Data` is a stack-only collection of borrowed references. `in` callback slots become read-only references and `ref` or `out` slots become writable references. It copies no component payload and the engine retains no context, component, frame, or playback reference.

## Run a runtime-selected timeline

```cs
ushort id = Attack.Id;

if (!Timeline.TryStart(id, 20_000u, out var playback))
    return;

var data = new Attack.DynamicData(
    ref playback,
    in animationSettings,
    in currentHealth,
    in currentPose,
    in damageSettings,
    ref nextHealth,
    ref nextPose);

if (!Timeline.TrySeek(id, ref data, 1))
    return;

if (!Timeline.TryStop(id, in playback, out var stopped))
    return;

playback = stopped;
```

Use `Attack.Start` and `Attack.TrySeek` when the definition is known in source. Use `Attack.Id`, `Timeline.TryStart`, `Attack.DynamicData`, and `Timeline.TrySeek` when an ID is selected at runtime. The dynamic route validates identity and schema before effects. It cannot infer a missing component from an ID.

## Unity ECS target

Unity projects consume the separate `Tl.Unity` UPM package and checked-in generated kernels. An ECS job owns one 16-byte `Playback<TTimeline>` value per entity, borrows queried components through a generated ref struct for one invocation, calls the generated signed `TrySeek`, and writes the playback back after success. The package contains no .NET 10 runtime, Tl compiler or generator, Roslyn assembly, managed registry, runtime compilation, or bundled sample source. The external `tests/Tl.Unity.Project` project owns the canonical Burst Combat sample and its receipts.

The installed Unity 6000.7.0a5 preview editor passes EditMode, PlayMode, Burst AOT, zero-allocation, player-content, and Standalone Linux player gates. Unity 6000.0 with Entities 1.4.3 remains the declared compatibility floor and was unavailable on the validation machine. IL2CPP has not been qualified. See the [Unity guide](docs/unity.md) for the package contract and commands.

## Composition

`builder.Include<TTimeline>()` composes a source-visible definition. `builder.Before<THook>()` and `builder.After<THook>()` add ordered hooks without editing the included definition. Include graphs are cycle-checked and lowered in deterministic authored order.

## Generated size and retained memory

Every cache miss prints generated C# file count and UTF-8 bytes. Every build writes `TlGenCompile.report.txt` with one row per timeline and artifact. Generated timelines expose `TrackCount`, `ClipCount`, `RegionCount`, and `StaticDataBytes`; `Timeline.RegistryRetainedBytes` reports process-wide unmanaged registry allocation.

```cs
Console.WriteLine(Attack.TrackCount);
Console.WriteLine(Attack.ClipCount);
Console.WriteLine(Attack.RegionCount);
Console.WriteLine(Attack.StaticDataBytes);
Console.WriteLine(Timeline.RegistryRetainedBytes);
```

Generated source bytes, static payload bytes, registry allocation, managed assembly size, NativeAOT image size, and native code size are reported independently. See the [signed seek evidence](benchmarks/Alpha/results/signed-seek/README.md) for exact correctness, latency, allocation, code-size, assembly, and PMU receipts.

## Limits

- IDs cover all 65,536 `ushort` values and do not reuse a published slot.
- Each generated module contains at most 256 dynamic routes.
- A definition supports at most 256 distinct track/clip kinds and 65,536 track instances.
- Clip windows are half-open `[start, end)` and at most two clips overlap on one track.
- Finite playback positions remain within `0..Duration`; looping positions and cycles are signed.
- Definitions and registry routes are immutable after publication.
- Cross-assembly schema routing and declarations produced by another generator are outside this alpha.
- C ABI v2 and Unity/Burst have separate signed-seek packages and qualification receipts.

The [API contract](docs/v1.0-alpha-api.md), [semantics](docs/semantics.md), [migration guide](docs/v1.0-alpha-migration.md), [architecture](docs/architecture.md), and [implementation plan](plan.md) define the complete boundary.

## Repository map

| Path | Role |
| --- | --- |
| `src/Tl.Core` | Runtime ABI, playback, frame, declaration surface, and registry |
| `src/Tl.Compiler` | Language-neutral immutable plan and validated region schedule |
| `src/Tl.Gen.CSharp` | C# frontend and build-time kernel generator |
| `src/Tl.Gen.C` | Portable C11 backend package |
| `src/Tl.CSharp` | One-package C# installation |
| `src/Tl.Unity` | C# 9 Unity package with ECS, Burst, and blob boundaries |
| `samples/Mixed` | Complete heterogeneous signed-seek example |
| `benchmarks/Alpha` | Public-path benchmarks, oracle, PMU harness, and evidence |
| `tests/Tl.Alpha` | Generated JIT and NativeAOT correctness receipts |
| `tests/Tl.Gen.CSharp.Tests` | C# frontend, emitter, cache, and public API receipts |
| `tests/Tl.PackageConsumer` | Isolated typed and dynamic package-consumer gate |
| `tests/Tl.Unity.Project` | Unity EditMode, PlayMode, Burst, and player gate |

## Validate

```sh
python3 benchmarks/source_budget.py
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project samples/Mixed -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```
