# tl

Compile gameplay timelines into tiny, allocation-free playback kernels. Author an attack, animation, status effect, cutscene, or simulation as normal typed C#; `tl` folds its known data and calls directly into your component operations.

- Heterogeneous tracks and clips in one timeline
- Generated `in`/`ref`/`out` contexts with no handwritten plumbing
- 0 B warm playback allocation and NativeAOT support
- `ushort` runtime IDs for up to 65,536 compiled timelines
- Explicit Before/After hooks and timeline composition
- 1.396 ns simple and 2.247 ns heterogeneous sequential public calls on the reference machine

**Status: v1.0.0-alpha.2 candidate.** This is a breaking alpha for .NET 10 and C# 14.

| Package | Purpose |
| --- | --- |
| `Tl.CSharp` | Recommended one-package C# installation; contains the build-only compiler and depends on the runtime |
| `Tl.Runtime` | The compact runtime ABI, timeline IDs, playback state, authoring syntax, and frame contract |
| `Tl.Gen.CSharp` | Standalone automatic C# frontend and build-time kernel generator |
| `Tl.Compiler` | Versioned language-neutral immutable plan for backend and tooling authors |
| `Tl.Gen.C` | Portable C11 backend with a fixed-width ABI |

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

The ECS ownership boundary is one `Playback` value per entity. Query the required components directly, construct the borrowed generated contexts inside `Execute`, advance once, and write the returned playback back only when the operation succeeds. A Unity backend should make this job the complete call site:

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
        var playback = state.Value;
        if (!playback.Has(PlaybackFlags.Started)
            && !Timeline.TryStart(Attack.Id, out playback))
            return;

        var input = new Attack.Input(currentPose: in currentPose.Value);
        var output = new Attack.Output(
            nextPose: ref nextPose.Value,
            boss: ref boss.Value);

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
        => new AdvanceAttackJob().ScheduleParallel();
}
```

`CurrentPose` and `NextPose` are separate component types because an entity cannot carry two components of the same type. A `ref` callback parameter aliases the queried component storage; an `in` parameter reads it without creating mutable output state. Generated contexts must be created and consumed inside the job invocation, never stored in a component, captured, or retained across a structural change.

This is the required Unity integration shape, but the current package is not Unity-compatible. It targets .NET 10/C# 14, while Unity uses a different API and compiler profile, and the runtime registry has not passed Burst compilation. NuGetForUnity can copy NuGet assemblies into a Unity project, but it cannot make this target framework or the MSBuild generator Burst-compatible. A production `Tl.Unity` UPM package must emit Unity-compatible kernels, store immutable timeline data in Burst-supported native storage, and pass Burst, Entities, IL2CPP, player-content, and allocation gates. See Unity's [API compatibility](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html), [C# compiler](https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html), [Entities `ISystem`](https://docs.unity.cn/Packages/com.unity.entities%401.2/manual/systems-isystem.html), and [Burst type support](https://docs.unity3d.com/Packages/com.unity.burst%401.8/manual/csharp-type-support.html) documentation.

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
| One-track sum | Sequential | 0.609 ns/tick | 1.396 ns/tick | 1.436 ns/tick |
| Two-kind combat | Sequential | 1.512 ns/tick | 2.247 ns/tick | 2.870 ns/tick |
| One-track sum | Random | 3.530 ns/tick | 4.972 ns/tick | 5.206 ns/tick |
| Two-kind combat | Random | 6.826 ns/tick | 7.085 ns/tick | 7.999 ns/tick |

Every measured arm allocates 0 B. Public scalar and batch values are the median of three independent alpha.2 run medians on an i9-14900K with .NET 10.0.12; handwritten oracle values are the unchanged alpha.1 reference. Batch values are throughput per tick across eight-tick calls. Each benchmark uses one runtime-loaded ID in its hot stream. Random seeking costs more because region selection and branch prediction are real work; a stream switching among many compatible IDs is not part of this alpha claim. Exact alpha.2 raw JSON, logs, environment, generated hashes, PMU counters, assembly, and limitations are retained in [the performance evidence](benchmarks/Alpha/results/v1.0.0-alpha.2/README.md). The [historical verification record](docs/verification/v1.0-alpha.1/README.md) preserves alpha.1.

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
- NativeAOT is supported. Unity Burst requires its own backend and qualification.

The [API contract](docs/v1.0-alpha-api.md), [migration guide](docs/v1.0-alpha-migration.md), [implementation plan](plan.md), [architecture](docs/architecture.md), and [extension contract](docs/extending.md) contain the complete design and verification rules.

## Repository map

| Path | Role |
| --- | --- |
| `src/Tl.Core` | Compact runtime ABI, playback state, frame contract, and ID registry |
| `src/Tl.Gen` | Compiler cache, C# semantic frontend, lowering, and source backend |
| `src/Tl.Compiler` | Language-neutral immutable plan |
| `src/Tl.Gen.C` | Portable C11 backend and ABI reporting |
| `src/Tl.CSharp` | One-package C# installation |
| `samples/Mixed` | Small heterogeneous attack that builds and runs generated code |
| `benchmarks/Alpha` | Public-path BenchmarkDotNet suite, exact oracle, and disassembly inputs |
| `tests/Tl.Alpha` | JIT and NativeAOT behavioral, capacity, allocation, and lifetime receipts |
| `tests/Tl.Core.Tests` | Approved public runtime surface |
| `tests/Tl.Gen.Tests` | Semantic analysis, generation, routing, caching, and diagnostics |
| `docs/verification` | Frozen measurements, environment identity, decisions, and limits |

## Build

```sh
dotnet build tl.slnx -c Release -m:1
dotnet test tl.slnx -c Release --no-build
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```
