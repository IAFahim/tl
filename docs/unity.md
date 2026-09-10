# Unity ECS and Burst

## Install

Add the native package through Unity Package Manager:

```text
https://github.com/IAFahim/tl.git?path=/src/Tl.Unity
```

The .NET `Tl.CSharp` package targets .NET 10 and runs its generator through MSBuild. Unity projects consume `Tl.Unity`, a C# 9 UPM package with checked-in generated kernels. The player does not contain `Tl.Compiler`, a Tl generator, Roslyn, reflection-based binding, or runtime compilation.

## Compatibility

| Lane | Editor | Entities | Collections | Burst | Status |
| --- | --- | --- | --- | --- | --- |
| Stable floor | 6000.0 | 1.4.3 | resolved by Entities | resolved by Entities | Required; editor unavailable on the current machine |
| Preview | 6000.7.0a5 (`a15235a53881`) | 6.7.0 | 6.7.0 | 2.0.0 | Passed locally |

The package declares Unity 6000.0 and Entities 1.4.3. Passing the installed preview editor does not establish the stable compatibility floor.

## Runtime shape

Each generated timeline exposes its own `Start(gameTick)`, `TrySeek(ref data, delta)`, and `TryStop` facade. `Start` anchors signed local position zero to the caller's external game tick. A positive delta executes every crossed frame in ascending order, a negative delta executes every crossed frame in descending order, and zero preserves playback and component storage.

`Playback<TTimeline>` is a 16-byte unmanaged value containing signed `Position`, `GameTick`, and lifecycle flags. Timeline identity lives in its generic type, so one generated facade cannot accept another timeline's playback. Finite kernels reject an invalid origin, overflow, or a target outside `0..Duration` before effects. Looping kernels normalize negative positions into a local tick and signed cycle. Explicit unchecked game-tick arithmetic wraps as unsigned simulation time, including when generated assemblies use checked compilation.

One direction-aware track operation receives `Frame<TTrack,TClip>`. Its `Direction` is `1` or `-1`. `FrameFlags` independently records clip start, clip end, timeline start, timeline end, completion side, looping, and reverse movement. An interior frame has neither clip boundary flag.

Generated data values are byref-like ref structs containing typed pointers to playback and caller-owned component fields. Create and consume one inside the same `IJobEntity.Execute` call. The type system prevents storing it in a component, class, array, or boxed value. Do not return it or retain it across a callback, structural change, job boundary, or storage relocation. Read-only input pointers stay live: when input and output alias, later ordered frames observe earlier writes.

The scalar kernel uses immediate constants and direct static calls. It does not traverse the optional `TimelineBlob`, allocate, box, reflect, use a delegate, or consult a managed registry. The blob mirrors the language-neutral track and clip plan for ECS storage and tooling.

## Verification

The committed project at `tests/Tl.Unity.Project` consumes only the local package and owns the canonical external Burst Combat sample. The sample starts with a nonzero delta and its own ECS system runs forward and backward in PlayMode. The complete PlayMode suite covers Burst ECS execution, forward and reverse replay, zero delta, finite rejection, looping with negative cycles, checked-build game-tick wrap, aliasing, ABI widths, blob layout, and zero managed allocation across 1,048,576 warm calls. Its EditMode suite freezes modifiers, layouts, constraints, field order, and ref returns, and inspects the player compilation graph for compiler, generator, and Roslyn assemblies.

```sh
unity --no-banner --format json test tests/Tl.Unity.Project --mode EditMode --output /tmp/tl-unity-editmode.xml --timeout 600
unity --no-banner --format json test tests/Tl.Unity.Project --mode PlayMode --output /tmp/tl-unity-playmode.xml --timeout 600
```

The standalone Linux gate builds `Assets/TlUnityPlayer.unity`, rejects forbidden toolchain assemblies, verifies Burst symbols for the gate and canonical sample, and runs until the player prints `TL_UNITY_PLAYER_OK`:

```sh
eng/test-unity-player /tmp/tl-unity-player
```

The local preview receipt passed all 4 EditMode tests and all 9 PlayMode tests. The Linux player contained `lib_burst_generated.so`, exported the Burst-compiled `AdvanceGateJob` and `AdvanceAttackJob` symbols, contained none of the forbidden compiler, generator, or Roslyn assemblies, and printed the expected marker. This receipt does not cover IL2CPP or the unavailable stable editor.
