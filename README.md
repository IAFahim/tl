# tl

Compile gameplay timelines into tiny, allocation-free playback kernels. Author an attack, animation, status effect, cutscene, or simulation as normal typed C#; `tl` folds its known data and calls directly into your component operations.

- Heterogeneous tracks and clips in one timeline
- Generated `in`/`ref`/`out` contexts with no handwritten plumbing
- 0 B warm playback allocation and NativeAOT support
- `ushort` runtime IDs for up to 65,536 compiled timelines
- Explicit Before/After hooks and timeline composition
- 1.383 ns simple and 2.188 ns heterogeneous sequential public calls on the reference machine

**Status: v1.0.0-alpha.2 candidate.** This is a breaking alpha for .NET 10 and C# 14.

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended one-package C# installation; contains the build-only compiler and depends on the runtime |
| `Tl.Runtime` | The compact runtime ABI, timeline IDs, playback state, authoring syntax, and frame contract |
| `Tl.Gen.CSharp` | Standalone automatic C# frontend and build-time kernel generator |
| `Tl.Compiler` | Versioned language-neutral immutable plan for backend and tooling authors |
| `Tl.Gen.C` | Portable C11 backend with a fixed-width ABI |
| `Tl.Unity` | Native UPM runtime for C# 9, Unity ECS, and Burst |

## Install

```sh
dotnet add package Tl.CSharp --prerelease
```

`Tl.CSharp` contains the compiler needed during the build and pulls in `Tl.Runtime`. Only `Tl.Runtime` enters the game output; the verified package consumer output contains no `Tl.Gen`, `Tl.Compiler`, `Tl.Gen.C`, or Roslyn assembly. Install `Tl.Runtime` and `Tl.Gen.CSharp` separately only when a toolchain needs that split.

## Build a combo attack

```cs
using Tl;

public readonly record struct FighterPose(float SwordAngle, float Lunge);
public readonly record struct CombatStats(float Health, float Stagger);
public readonly record struct SwingClip(float SwordAngle, float Lunge);
public readonly record struct HitClip(float Damage, float Stagger);

public readonly struct SwordTrack : ITrack<SwingClip>
{
    public void Blend(in SwingClip first, in SwingClip second, float factor, out SwingClip result)
        => result = new(
            first.SwordAngle + (second.SwordAngle - first.SwordAngle) * factor,
            first.Lunge + (second.Lunge - first.Lunge) * factor);

    public static void Forward(in Frame<SwordTrack, SwingClip> frame, in FighterPose currentPose, out FighterPose nextPose)
        => nextPose = new(currentPose.SwordAngle + frame.Clip.SwordAngle, currentPose.Lunge + frame.Clip.Lunge);

    public static void Backward(in Frame<SwordTrack, SwingClip> frame, in FighterPose currentPose, out FighterPose nextPose)
        => nextPose = new(currentPose.SwordAngle - frame.Clip.SwordAngle, currentPose.Lunge - frame.Clip.Lunge);
}

public readonly struct HitTrack : ITrack<HitClip>
{
    public void Blend(in HitClip first, in HitClip second, float factor, out HitClip result)
        => result = new(
            first.Damage + (second.Damage - first.Damage) * factor,
            first.Stagger + (second.Stagger - first.Stagger) * factor);

    public static void Forward(in Frame<HitTrack, HitClip> frame, ref CombatStats boss)
        => boss = new(boss.Health - frame.Clip.Damage, boss.Stagger + frame.Clip.Stagger);

    public static void Backward(in Frame<HitTrack, HitClip> frame, ref CombatStats boss)
        => boss = new(boss.Health + frame.Clip.Damage, boss.Stagger - frame.Clip.Stagger);
}

public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var sword = builder.Track(new SwordTrack());
        var hit = builder.Track(new HitTrack());
        builder.Clip(in sword, new SwingClip(-25f, 0.1f), 0u, 16u);
        builder.Clip(in sword, new SwingClip(90f, 1f), 8u, 24u);
        builder.Clip(in hit, new HitClip(120f, 35f), 12u, 13u);
    }
}
```

The generator discovers `ITimeline` declarations from the project’s normal compile items. Normal builds and IDE design-time compilations run the same deterministic pass; unchanged inputs are cache hits that preserve generated file timestamps. Editors decide when to schedule design-time compilation, so generation follows that background compile rather than each raw keystroke. There is no `Build`, `Compile`, `InMemory`, `Bind`, source item list, reflection, boxing, or runtime authoring graph.

`Frame<TTrack,TClip>` exposes `Track`, resolved `Clip`, effective `Tick`, `State`, and stable `TrackIndex`. Callback parameters after the frame define any finite set of `in`, `ref`, and `out` component slots. The generator derives the timeline’s complete input and output schema.

## Advance it in the game loop

```cs
var currentPose = new FighterPose(0f, 0f);
var nextPose = currentPose;
var boss = new CombatStats(1_000f, 0f);

var input = new Attack.Input(currentPose: in currentPose);
var output = new Attack.Output(nextPose: ref nextPose, boss: ref boss);

if (Timeline.TryStart(Attack.Id, out var playback)
    && Timeline.TryForward(
        Attack.Id,
        in playback,
        10u,
        in input,
        ref output,
        out var nextPlayback))
{
    playback = nextPlayback;
}
```

`Timeline.All[id]` provides the equivalent indexed facade. Scalar forward/backward methods stay scalar. Span overloads hoist validation and route selection across a batch.

The generated contexts are stack-only ref structs. They retain managed byrefs correctly when component storage lives in an object or array, while unmanaged component values work naturally with ECS-style storage. The engine retains no input, output, frame, or playback reference.

## Unity ECS target

Install the native UPM package through Unity Package Manager:

```text
https://github.com/IAFahim/tl.git?path=/src/Tl.Unity
```

The ECS ownership boundary is one `Playback` value per entity. Query the required components directly, construct the borrowed generated contexts inside `Execute`, advance once, and write the returned playback back only when the operation succeeds. The included Burst Combat sample uses this complete call site:

```cs
using Tl;
using Unity.Burst;
using Unity.Entities;

public struct AttackPlayback : IComponentData
{
    public Playback Value;
    public uint Tick;
}

public struct CurrentPose : IComponentData
{
    public FighterPose Value;
}

public struct NextPose : IComponentData
{
    public FighterPose Value;
}

public struct BossCombatStats : IComponentData
{
    public CombatStats Value;
}

[BurstCompile]
public partial struct AdvanceAttackJob : IJobEntity
{
    private void Execute(
        ref AttackPlayback state,
        in CurrentPose currentPose,
        ref NextPose nextPose,
        ref BossCombatStats boss)
    {
        var playback = state.Value.Has(PlaybackFlags.Started)
            ? state.Value
            : Timeline.Start(Attack.Id);

        var input = new Attack.Input(currentPose: in currentPose.Value);
        var output = new Attack.Output(
            nextPose: ref nextPose.Value,
            combat: ref boss.Value);

        if (!Timeline.TryForward(
                Attack.Id,
                in playback,
                state.Tick,
                in input,
                ref output,
                out var nextPlayback))
            return;

        state.Value = nextPlayback;
        state.Tick++;
    }
}

[BurstCompile]
public partial struct AttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
        => state.Dependency = new AdvanceAttackJob().ScheduleParallel(state.Dependency);
}
```

`CurrentPose` and `NextPose` are separate component types because an entity cannot carry two components of the same type. A `ref` callback parameter aliases the queried component storage; an `in` parameter reads it without creating mutable output state. Generated contexts must be created and consumed inside the job invocation, never stored in a component, captured, or retained across a structural change.

`Tl.Unity` is a separate C# 9 assembly with an unmanaged 12-byte playback value, pointer-backed generated contexts, direct scalar kernels, and BlobAsset-compatible inspection data. It passed EditMode, PlayMode, Burst AOT, a Standalone Linux player build, and a 1,048,576-call zero-allocation receipt on Unity 6000.7.0a5, Entities 6.7.0, Collections 6.7.0, and Burst 2.0.0. Unity 6000.0 with Entities 1.4.3 is the declared stable compatibility floor and remains a separate CI lane. NuGetForUnity cannot run the Tl build generator and is not the supported install path. See the [Unity package guide](docs/unity.md) and Unity's [Burst type support](https://docs.unity3d.com/Packages/com.unity.burst%401.8/manual/csharp-type-support.html) documentation.

## Emit portable C11

`Tl.Compiler` carries only numeric identities, operations, payload IDs, and half-open windows. `Tl.Gen.C` maps each operation ID to explicit C symbols and returns the source plus a byte-accurate report.

```cs
using Tl.Compiler;
using Tl.Gen.C;

var damage = new OperationId("game.damage");
var plan = new TimelinePlan(
    "attack",
    runtimeId: 7,
    loops: false,
    tracks: [new TrackPlan(0, 10, damage)],
    clips: [new ClipPlan(0, 120, 12, 13)]);
var binding = new CBinding(
    "attack",
    "attack.h",
    [new COperationBinding(damage, "damage_forward", "damage_backward")]);
var emission = CEmitter.Generate(plan, binding);
```

The emitted C11 owns no heap memory. `tl_playback` is 12 bytes, `tl_frame` is 24 bytes, and the header asserts both layouts. Consumer operations receive a borrowed `void*` context and `const tl_frame*`. Arbitrary C# callback bodies are never claimed to be portable.

## Composition

`builder.Include<TTimeline>()` composes another build-visible definition. `builder.Before<THook>()` and `builder.After<THook>()` register generated hook calls without editing the included timeline. Include graphs are cycle-checked and lowered in deterministic authored order.

## Know what the compiler made

Every cache miss prints the number and UTF-8 size of generated C# files. Every build leaves `obj/<configuration>/<tfm>/TlGenCompile/TlGenCompile.report.txt` with one row per timeline and artifact. Cache hits preserve generated contents and timestamps and point to the same report.

```text
TlGenCompile: 1 timeline(s), 3 source file(s), 27,094 UTF-8 B; report obj/Release/net10.0/TlGenCompile/TlGenCompile.report.txt
```

The generated timeline exposes its exact authored shape and runtime-sized static payload storage. The core reports actual process-wide unmanaged registry allocation.

```cs
Console.WriteLine($"tracks: {Attack.TrackCount}");
Console.WriteLine($"clips: {Attack.ClipCount}");
Console.WriteLine($"regions: {Attack.RegionCount}");
Console.WriteLine($"static payloads: {Attack.StaticDataBytes} B");
Console.WriteLine($"timeline registry: {Timeline.RegistryRetainedBytes} B");
```

Generated source bytes, static payload bytes, shared dispatch values, registry allocation, managed assembly size, NativeAOT binary size, and JIT native code are separate quantities. The report does not pretend source length is runtime memory.

## Performance

The alpha benchmark measures a full public call and complete receipts with 65,536 operations, 16 warmups, 12 measured iterations, 250 ms iterations, and MemoryDiagnoser:

| Workload | Pattern | Handwritten oracle | `Timeline.TryForward` | Public batch 8 |
| --- | --- | ---: | ---: | ---: |
| One-track sum | Sequential | 0.609 ns/tick | 1.383 ns/tick | 1.384 ns/tick |
| Two-kind combat | Sequential | 1.512 ns/tick | 2.188 ns/tick | 2.033 ns/tick |
| One-track sum | Random | 3.530 ns/tick | 4.786 ns/tick | 4.854 ns/tick |
| Two-kind combat | Random | 6.826 ns/tick | 7.015 ns/tick | 6.633 ns/tick |

Every measured arm allocates 0 B. Scalar public values are the median of three independent alpha.2 run medians on an i9-14900K with .NET 10.0.12; oracle and batch values are the alpha.1 reference until the alias-safe batch kernel is remeasured. Each benchmark uses one runtime-loaded ID in its hot stream. Random seeking costs more because region selection and branch prediction are real work; a stream switching among many compatible IDs is not part of this alpha claim. Exact alpha.1 reports, disassembly, environment, and limits remain in [the historical verification record](docs/verification/v1.0-alpha.1/README.md); alpha.2 candidate measurements are recorded in [the release notes](docs/v1.0-alpha.2.md).

## Limits

- IDs cover all 65,536 `ushort` values and are never reused within a process.
- Each generated module contains at most 256 routes; modules compose to the full ID space.
- A definition supports at most 256 distinct track/clip kinds and 65,536 track instances.
- Clip windows are half-open `[start, end)`; at most two clips overlap on one track.
- A generated batch accepts at most 256 ticks and uses at most 1,024 bytes of stack storage to make aliased tick input stable before effects.
- Compiled definitions live for the process lifetime.
- Runtime topology mutation and arbitrary managed plugin loading are outside this alpha.
- Context routing and `Include` resolve definitions from the same compilation. Cross-assembly schema routing is outside this alpha.
- Timeline declarations emitted by another source generator are not visible to this pre-compilation generator pass.
- NativeAOT is supported by `Tl.CSharp`. Unity ECS and Burst use the separate `Tl.Unity` UPM package.

The [API contract](docs/v1.0-alpha-api.md), [migration guide](docs/v1.0-alpha-migration.md), [implementation plan](plan.md), [architecture](docs/architecture.md), and [extension contract](docs/extending.md) contain the complete design and verification rules.

## Repository map

| Path | Role |
| --- | --- |
| `src/Tl.Core` | Compact runtime ABI, playback state, frame contract, and ID registry |
| `src/Tl.Gen` | Compiler cache, C# semantic frontend, lowering, and source backend |
| `src/Tl.Compiler` | Language-neutral immutable plan |
| `src/Tl.Gen.C` | Portable C11 backend and ABI reporting |
| `src/Tl.CSharp` | One-package C# installation |
| `src/Tl.Unity` | Native UPM runtime, Entities blob boundary, and Burst sample |
| `samples/Mixed` | Small heterogeneous attack that builds and runs generated code |
| `benchmarks/Alpha` | Public-path BenchmarkDotNet suite, exact oracle, and disassembly inputs |
| `tests/Tl.Alpha` | JIT and NativeAOT behavioral, capacity, allocation, and lifetime receipts |
| `tests/Tl.Core.Tests` | Approved public runtime surface |
| `tests/Tl.Gen.Tests` | Semantic analysis, generation, routing, caching, and diagnostics |
| `tests/Tl.Unity.Project` | Unity EditMode, PlayMode, Burst, player, and package-isolation gates |
| `docs/verification` | Frozen measurements, environment identity, decisions, and limits |

## Build

```sh
dotnet build tl.slnx -c Release -m:1
dotnet test tl.slnx -c Release --no-build
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```
