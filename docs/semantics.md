# Playback semantics

This is the behavior shared by runtime-authored and compiled timelines.

## Time and work

Ticks are `uint`. A clip occupies `[start, end)` with `end > start`. Runtime
duration is the final end cut, or zero for empty content. Clip, track and
registry counts have their own checked capacities.

For looping timelines with nonzero duration, effective tick is raw tick modulo
duration. Hooks receive effective ticks; `Playback.Tick` retains the supplied
raw destination. Duration zero must never cause division by zero.

Each supplied destination tick updates playback. A non-empty destination invokes
one directional `ITrack` operation per active track in authored order. Empty
destinations invoke no operation. A large jump does not replay all crossed clips:
only work active at the destination is exposed. Multi-tick input is processed in
supplied order, not sorted or implicitly expanded.

A track has at most two simultaneous clips. One active clip is passed directly;
two are resolved by the authored track's `IBlend<TClip>.Blend` once before the operation.
The overlap factor uses the intersection window, with factor 0.5 for a one-tick
overlap. Preserve the existing orientation and arithmetic order. More than two
simultaneous clips must fail authoring validation.

## Per-work state

The operation's `ClipState` is `Enter`, `Stay` or `Exit`:

- **Exit:** destination is the last active frame in the requested direction:
  forward `end - 1`, backward `start`. This is positional and takes precedence.
- **Enter:** the movement crosses the work's entry edge: forward through `start`,
  backward through `end`, including the defined wrapping/full-cycle rules.
- **Stay:** neither of the above.

A resolved blend's work state uses the pair's outer window: minimum start and
maximum end. Its blend factor still uses the intersection. One-frame work is
therefore Exit, including when a jump lands on it. Repeat visits can also be Exit.

Stateless calls and repeated positions do not report Enter. Stateless sampling
still reports positional Exit. Starting silently at tick zero and then sampling
that same zero produces Stay for a multi-tick clip starting there; it is not a
movement across the entry edge.

The engine does not decide whether Enter, Stay or Exit applies a value. The
README consumer applies only Stay as an example. Arbitrary hooks can observe all
states. Backward hooks implement application behavior; the engine cannot undo
arbitrary user side effects automatically.

## Playback and lifetime

`Playback` remains 8 bytes: `uint Tick`, `ushort Cycles`, `PlaybackFlags : ushort`.
Flags are `Started`, `Stopped`, `LastLoopFrame` and `Completed`; clip movement
facts do not live in the flags word.

`Start(index, at)` positions silently and sets Started. `Stop` requires a started
playback, preserves its other facts and is idempotent. Stateful playback rejects
default/unstarted and stopped values before invoking hooks. A new Start clears
Stopped. An empty tick span preserves valid playback.

Non-looping completion is positional: forward at/after the final active tick,
backward at zero; an empty timeline completes on advancement. Looping timelines
report LastLoopFrame at effective `duration - 1`. Forward cycle accumulation
throws before the affected tick's hook if the `ushort` count overflows; backward
cycle subtraction saturates at zero. Keep the existing directional wrap rules
and test both raw and normalized destinations, including multi-cycle jumps.

`Playback` contains no timeline ID. Callers pair it with the intended timeline;
it is not a serialized, ownership-checked handle. Global runtime IDs use one
`ushort` space, reserve `Timeline.None`, and are not reused after Destroy. They
are process-local identifiers, not durable asset identifiers.

Each playback owner keeps its own result and optional `Cursor`.
The cursor is only a navigation hint: wrong owner or starting tick must fall back
to searching.

Track and clip values are passed by readonly reference. No work view or resolved
blend buffer exists. Building and binding may allocate; warmed library playback
must not allocate. Consumer operations and blend implementations are responsible
for their own allocations and side effects.

Callback exceptions preserve mutations already made through caller references.
A failed call returns no `Playback`; the caller's previous value remains unchanged.
Cursor publication happens after a successful walk, so a failed cursor call leaves
the caller's cursor unchanged. Runtime and generated batch paths follow this rule.
