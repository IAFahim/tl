# v1 signed seek semantics

This contract applies to the generated C# v1 alpha path. Released v0.6 behavior remains archived under [verification/v0.6](verification/v0.6/README.md).

## Definitions

A public readonly partial struct implementing `ITimeline` declares one static `Define(scoped Builder builder)` method. The build compiler interprets its supported declarative syntax and emits an immutable kernel. A definition may contain up to 256 closed track/clip kinds and 65,536 authored track instances. A clip occupies a half-open `[start, end)` window with `end > start`; at most two clips overlap on one track.

## Position and time

`Start(gameTick)` creates playback at signed position zero and the supplied simulation tick. Position is a boundary between frames. `GameTick` uses unchecked `uint` arithmetic. Finite positions are valid in `0..Duration`. Looping positions and cycles may be negative.

For current position `P`, game tick `G`, and signed delta `d`:

| Delta | Executed positions | Final position | Final game tick |
| --- | --- | --- | --- |
| `d > 0` | `P, P + 1, ..., P + d - 1` | `P + d` | `G + d` |
| `d < 0` | `P - 1, P - 2, ..., P + d` | `P + d` | `G + d` |
| `d = 0` | none | `P` | `G` |

Every crossed local frame is executed. `TrySeek(5)` and five consecutive `TrySeek(1)` calls have the same ordered effects. `TrySeek(-5)` and five consecutive `TrySeek(-1)` calls have the same ordered effects. A seek never samples only its destination.

For a finite definition, any target outside `0..Duration` rejects before effects. Empty finite definitions accept only zero. A looping nonempty definition maps each executed signed position to a local tick in `0..Duration-1` and a signed cycle using floor-style normalization.

## Ordered effects

At an executed local frame, zero active clips invoke no track operation. One active clip is borrowed directly. Two active clips resolve once through `Blend`; the factor uses their intersection, with `0.5f` for a one-frame intersection. Equal-start clips retain authored order. Active tracks execute in authored order while moving forward and reverse authored order while moving backward.

Forward order is Before hooks, tracks, then After hooks. Reverse order is After hooks reversed, tracks reversed, then Before hooks reversed. This makes the generated schedule structurally invertible; application operations remain responsible for making their state changes invertible.

`FrameFlags` are independent facts. `ClipStart` and `ClipEnd` describe clip boundaries. `TimelineStart` and `TimelineEnd` describe local definition boundaries. `CompletedBefore` and `CompletedAfter` describe finite completion sides. `Looping` marks a looping definition and `Reverse` marks reverse execution. `Frame.Direction` derives `-1` from `Reverse` and otherwise returns `1`.

`Frame.GameTick` is the simulation tick of the executed frame. `Frame.TimelineTick` is its normalized local tick. `Frame.Cycle` is signed. `Frame.TrackIndex` is the stable authored track identity.

## Borrowed data

The generated `Data` and `DynamicData` ref structs borrow playback and every component slot. Callback `in` parameters produce read-only aliases; `ref` and `out` parameters produce writable aliases. The engine stores none of these references. Contexts cannot outlive their caller scope, cross asynchronous suspension, survive ECS structural changes, or be recursively reused with overlapping writable aliases.

An `in` alias does not freeze storage against mutation through another alias. Callers that need a snapshot create separate storage explicitly. User exceptions propagate and preserve the already-executed effect prefix.

## Total failure

The typed facade validates lifecycle, current position, arithmetic, and finite target before effects. The dynamic facade additionally validates live ID, owner, route, and generated schema. Engine rejection returns `false` and preserves playback and component storage. A zero delta still validates its context and playback before succeeding without effects.

`TryStop` requires the matching owner on the dynamic path and is idempotent after a valid stop. Stopped and default playback cannot seek. Definitions and dynamic registry entries are immutable after publication and safe for concurrent reads; caller-owned mutable components retain the caller's synchronization responsibility.

## Allocation and portability

Warm typed and dynamic seek allocate zero managed bytes. Static payload storage, generated source, registry allocation, managed assembly size, NativeAOT image size, and native code size are separate reported quantities.

The generated C# path is verified under .NET JIT and NativeAOT. The portable C backend and Unity/Burst target have independent ABIs and qualification workstreams; their current state is not evidence for this C# contract.
