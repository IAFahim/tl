# tl

`tl` turns typed C# timeline declarations into direct playback kernels during the normal build. One timeline can contain different track and clip types. The runtime API uses a `ushort` ID, generated borrowed contexts, and caller-owned unmanaged state.

**Status: v1.0.0-alpha.1.** This is a breaking alpha for .NET 10 and C# 14.

| Package | Purpose |
| --- | --- |
| `Tl.Runtime` | The compact runtime ABI, timeline IDs, playback state, authoring syntax, and frame contract |
| `Tl.Gen.CSharp` | The automatic C# frontend and build-time kernel generator |

## Define

```cs
using Tl;

public readonly record struct Pose(float X, float Y);
public readonly record struct AnimationClip(float X, float Y);
public readonly record struct Health(float Value);
public readonly record struct DamageClip(float Amount);

public readonly struct AnimationTrack : ITrack<AnimationClip>
{
    public void Blend(in AnimationClip first, in AnimationClip second, float factor, out AnimationClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);

    public static void Forward(in Frame<AnimationTrack, AnimationClip> frame, in Pose current, out Pose next)
        => next = new(current.X + frame.Clip.X, current.Y + frame.Clip.Y);

    public static void Backward(in Frame<AnimationTrack, AnimationClip> frame, in Pose current, out Pose next)
        => next = new(current.X - frame.Clip.X, current.Y - frame.Clip.Y);
}

public readonly struct DamageTrack : ITrack<DamageClip>
{
    public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);

    public static void Forward(in Frame<DamageTrack, DamageClip> frame, ref Health health)
        => health = new(health.Value - frame.Clip.Amount);

    public static void Backward(in Frame<DamageTrack, DamageClip> frame, ref Health health)
        => health = new(health.Value + frame.Clip.Amount);
}

public readonly partial struct Attack : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var animation = builder.Track(new AnimationTrack());
        var damage = builder.Track(new DamageTrack());
        builder.Clip(in animation, new AnimationClip(2f, 1f), 0u, 40u);
        builder.Clip(in damage, new DamageClip(10f), 10u, 11u);
    }
}
```

The generator discovers `ITimeline` declarations from the project’s normal compile items. There is no `Build`, `Compile`, `InMemory`, `Bind`, source item list, reflection, boxing, or runtime authoring graph.

`Frame<TTrack,TClip>` exposes `Track`, resolved `Clip`, effective `Tick`, `State`, and stable `TrackIndex`. Callback parameters after the frame define any finite set of `in`, `ref`, and `out` component slots. The generator derives the timeline’s complete input and output schema.

## Run

```cs
var currentPose = new Pose(0f, 0f);
var nextPose = currentPose;
var health = new Health(100f);

var input = new Attack.Input(current: in currentPose);
var output = new Attack.Output(next: ref nextPose, health: ref health);

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

## Unity ECS

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
    public Pose Value;
}

public struct NextPose : IComponentData
{
    public Pose Value;
}

public struct HealthComponent : IComponentData
{
    public Health Value;
}

[BurstCompile]
public partial struct AdvanceAttackJob : IJobEntity
{
    private void Execute(
        ref AttackPlayback state,
        in CurrentPose currentPose,
        ref NextPose nextPose,
        ref HealthComponent health)
    {
        var playback = state.Value;
        if (!playback.Has(PlaybackFlags.Started)
            && !Timeline.TryStart(Attack.Id, out playback))
            return;

        var input = new Attack.Input(current: in currentPose.Value);
        var output = new Attack.Output(
            next: ref nextPose.Value,
            health: ref health.Value);

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

This is the required Unity integration shape, but `v1.0.0-alpha.1` itself is not Unity-compatible. It targets .NET 10/C# 14, while Unity 6 documents .NET Standard 2.1 or .NET Framework profiles and C# 9, and the current runtime registry has not passed Burst compilation. A production Unity backend must emit C# 9-compatible kernels into the Unity compilation, store immutable timeline data in Burst-supported native storage, remove the generic static-interface bridge, and pass Burst/IL2CPP correctness and performance gates. See Unity's [API compatibility](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html), [C# compiler](https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html), [Entities `ISystem`](https://docs.unity.cn/Packages/com.unity.entities%401.2/manual/systems-isystem.html), and [Burst type support](https://docs.unity3d.com/Packages/com.unity.burst%401.8/manual/csharp-type-support.html) documentation.

## Composition

`builder.Include<TTimeline>()` composes another build-visible definition. `builder.Before<THook>()` and `builder.After<THook>()` register generated hook calls without editing the included timeline. Include graphs are cycle-checked and lowered in deterministic authored order.

## Performance

The alpha benchmark measures a full public call and complete receipts with 65,536 operations, 16 warmups, 12 measured iterations, 250 ms iterations, and MemoryDiagnoser:

| Workload | Pattern | Handwritten oracle | `Timeline.TryForward` | Public batch 8 |
| --- | --- | ---: | ---: | ---: |
| One-track sum | Sequential | 0.609 ns/tick | 1.382 ns/tick | 1.384 ns/tick |
| Two-kind combat | Sequential | 1.512 ns/tick | 2.239 ns/tick | 2.033 ns/tick |
| One-track sum | Random | 3.530 ns/tick | 4.964 ns/tick | 4.854 ns/tick |
| Two-kind combat | Random | 6.826 ns/tick | 6.974 ns/tick | 6.633 ns/tick |

Every measured arm allocates 0 B. Public values are the median of three independent run medians on an i9-14900K with .NET 10.0.12; oracle values come from the complete final run. Each benchmark uses one runtime-loaded ID in its hot stream. Random seeking costs more because region selection and branch prediction are real work; a stream switching among many compatible IDs is not part of this alpha claim. Exact reports, disassembly, environment, and limits are kept in [the verification record](https://github.com/IAFahim/tl/blob/v1.0.0-alpha.1/docs/verification/v1.0-alpha.1/README.md).

## Limits

- IDs cover all 65,536 `ushort` values and are never reused within a process.
- Each generated module contains at most 256 routes; modules compose to the full ID space.
- A definition supports at most 256 distinct track/clip kinds and 65,536 track instances.
- Clip windows are half-open `[start, end)`; at most two clips overlap on one track.
- Compiled definitions live for the process lifetime.
- Runtime topology mutation and arbitrary managed plugin loading are outside this alpha.
- Context routing and `Include` resolve definitions from the same compilation. Cross-assembly schema routing is outside this alpha.
- Timeline declarations emitted by another source generator are not visible to this pre-compilation generator pass.
- NativeAOT is supported. Unity Burst requires its own backend and qualification.

The [API contract](https://github.com/IAFahim/tl/blob/v1.0.0-alpha.1/docs/v1.0-alpha-api.md), [migration guide](https://github.com/IAFahim/tl/blob/v1.0.0-alpha.1/docs/v1.0-alpha-migration.md), [implementation plan](https://github.com/IAFahim/tl/blob/v1.0.0-alpha.1/plan.md), and [release notes](https://github.com/IAFahim/tl/blob/v1.0.0-alpha.1/docs/v1.0-alpha.1.md) contain the complete design and verification rules.

## Build

```sh
dotnet build tl.slnx -c Release -m:1
dotnet test tl.slnx -c Release --no-build
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```
