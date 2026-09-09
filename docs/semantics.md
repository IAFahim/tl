# v1 timeline semantics

This contract applies to the generated v1 alpha path. The released v0.6 behavior is preserved under [verification/v0.6](verification/v0.6/README.md).

## Definitions

A public readonly partial struct implementing `ITimeline` declares one static `Define(scoped Builder builder)` method. The build compiler interprets the supported declarative statements; it never runs the method. Each definition is immutable after compilation and receives one process-local `ushort` ID. IDs cover 0 through 65,535, never reuse, and remain live for the process.

A definition can contain up to 256 closed track/clip kinds and 65,536 authored track instances. A `TrackRef<TTrack>` belongs to the defining builder expression. Default, foreign, or mismatched handles are rejected during generation. A clip occupies a half-open `[start, end)` window where `end > start`. At most two clips may overlap on one track.

Normal compile items are discovered automatically. No runtime authoring graph, `Build`, `Compile`, `InMemory`, `Bind`, or destruction operation exists. Include and compatible runtime routing require their definitions in the same compilation in this alpha.

## Time and work

Ticks are `uint`. Duration is the largest clip end or zero for empty content. For a looping nonempty timeline, the effective tick is the supplied tick modulo duration. `Playback.Tick` retains the supplied destination and `Frame.Tick` receives the effective tick. A zero-duration definition never divides by zero.

Each supplied tick updates playback and invokes one directional track operation for every active authored track in stable authored order. A gap invokes no track operation. A large jump samples the destination and movement facts; it does not replay every crossed clip. A span processes ticks in supplied order without sorting or expansion.

One active clip is supplied directly. Two active clips resolve once through the track's `Blend` operation. The factor uses their intersection window and is `0.5f` for a one-tick intersection. Equal-start clips preserve authored order, so blend orientation and floating-point evaluation are deterministic.

## Frame state

`Frame<TTrack,TClip>` borrows the authored track and resolved clip and carries effective tick, stable 16-bit `TrackIndex`, and `ClipState`:

- `Exit` is positional at `end - 1` while moving forward and at `start` while moving backward.
- `Enter` means movement crossed the work's entry edge: `start` forward or `end` backward, including defined loop crossings.
- `Stay` means neither rule applies.

Exit takes precedence. A blended work uses the pair's outer window for state and its intersection for the blend factor. A one-tick work is therefore Exit. Starting at a position is silent; sampling that same position does not create an entry crossing.

Track code decides what each state means for the application. Backward callbacks are explicit application behavior; the engine cannot reverse arbitrary effects automatically.

## Context and effects

Static track callbacks begin with the exact frame kind. Remaining parameters are explicit `in`, `ref`, or `out` component slots. The generator forms the union of those slots and emits separate stack-only borrowed `Input` and `Output` contexts. Matching uses parameter identity, exact type, and access mode; it never uses reflection, boxing, implicit conversion, or an object array.

An `in` slot is a read-only alias through that path. It is not a snapshot against another writable alias. A `ref` slot aliases caller storage directly. An `out` callback overwrites its slot only when that callback runs; gaps and rejected operations preserve caller storage. Managed object-field and array-element references remain valid through compacting GC because generated contexts retain managed byrefs.

Hooks use the same component-slot rules. Before hooks run after validation and before track work. After hooks run after successful track work and are not finally handlers. Accepted gaps still run hooks. An outer include runs outer Before hooks, included Before hooks, included tracks, included After hooks, then outer After hooks. Registration order within a phase is stable in both directions.

User exceptions propagate and preserve prior effects. Try methods convert only engine validation failure to `false`; they do not catch callbacks or roll back component writes.

## Playback and failure

`Playback` is a 12-byte unmanaged value containing raw `Tick`, `Cycles`, owner ID, and flags. Flags are `Started`, `Stopped`, `LastLoopFrame`, and `Completed`. Owner authentication prevents a playback from one definition executing another.

`TryStart` creates started state for a live ID. `TryStop` requires the same owner and is idempotent after a successful stop. Default, unstarted, stopped, wrong-owner, invalid-ID, incompatible-context, and cycle-overflow executions return `false` before hooks or track effects. A valid empty span preserves playback and output.

Non-looping completion is positional at the final active forward frame or zero backward. Looping cycles are checked against `ushort` capacity before effects; backward cycle subtraction saturates at zero. Batch validation precedes user effects for every engine-detectable failure.

The runtime registry and compiled definition metadata are immutable after publication and safe for concurrent reads. Caller-owned mutable components require the caller's normal synchronization. Contexts must not outlive their stack scope, cross asynchronous suspension, survive ECS structural changes, or be used recursively with the same writable aliases.

## Allocation and portability

Warmed exact-schema scalar and batch playback allocate zero managed bytes. The registry is paged unmanaged storage. The engine retains no context, component, frame, or playback reference.

The generated runtime is verified under .NET JIT and NativeAOT. Unity Burst is a separate backend target because its supported C# and runtime subset differs. Arbitrary managed plugin loading is not available in a published NativeAOT process.
