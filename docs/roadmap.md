# tl roadmap

The benchmark phase answered the design questions; nothing in `src/` is real
yet. This plan turns the validated shapes in [benchmarks.md](benchmarks.md)
into an engine, one phase at a time, keeping the discipline that produced
those verdicts: every performance claim gets a BenchmarkDotNet arm, every
semantic claim gets a receipt check under `--verify`, and runs are pinned by
`benchmarks/run.py` (CPU affinity, environment, source hashes in
`environment.json`).

## What is already decided

From [benchmarks.md](benchmarks.md), the contract is fixed and must not drift.
The consumer surface is the final "work in Tracks" redesign: exactly THREE
interfaces, a process-global timeline registry, and per-work movement states.

The three interfaces (signature order is the user's convention: `ref data`
FIRST, then `in` payloads, tick LAST):

```cs
public interface IBlend<TClip>
{
    void Blend(in TClip first, in TClip second, float t, out TClip result);
}

public interface IForward<TTrack, TClip, TData>
{
    void Forward(ref TData data, in Tracks<TTrack, TClip> tracks, in uint tick);
}

public interface IBackward<TTrack, TClip, TData>
{
    void Backward(ref TData data, in Tracks<TTrack, TClip> tracks, in uint tick);
}
```

- Ticks are `uint`; multi-tick calls are `params ReadOnlySpan<uint>`. The
  hook receives the EFFECTIVE tick (normalized on looping timelines — the
  position the view's works describe); `Playback.Tick` keeps the raw
  authored destination.
- One callback per NON-EMPTY tick through `IForward`/`IBackward`: the frame
  is never materialized, and empty ticks fire NO callback (Playback still
  updates). `Tracks`/`TrackWork` are `ref struct` views over the CSR tables.
- Blending is resolved before the consumer sees it: `IBlend<TClip>.Blend`
  collapses an overlapping pair into one clip, fused into enumerator
  `MoveNext`, written into a single per-call `stackalloc` sized
  `MaxActiveTracks`.
- Static-abstract table fetches (`ITrackTables`) are hoisted once per call.
- Hooks are abstract-only: no default interface methods (10.9–11.4× and
  silent mutation loss), no delegates (4×), no boxing (4–4.5×).
- Region navigation stays data-driven (cursor/CSR), not generated code.
- Authoring is a `Timeline<TTrack,TClip>.Build` callback over a stack-scoped
  `TimelineBuilder` (tracks mint unstoreable `TrackRef` handles; `Clip(in
  TrackRef, in TClip, uint start, uint end)`; `Looping()`), plus a
  `Build<TSource>(source, build)` overload for GUI-style flows.
- The non-generic `Timeline` is a PROCESS-GLOBAL registry across every
  `(TTrack, TClip)` closure: ONE `ushort` index space (`None` reserved,
  capacity 65,535, tombstones never reused, `Destroy(ushort)`), with
  `Start(index, at)` / `Stop(index, in pb)` / `Forward`/`Backward`
  (Playback-carrying and stateless) / `IsValid` / `Duration` /
  `IsLooping`. Dispatch is per-(entry, `TData`) function-pointer bridges:
  a per-`TData` static token id (`DataToken<TData>`, a CAS counter), each
  entry lazily binding and caching `delegate*` pointers created from a
  bridge generic instantiation of the closure's engine (`ref data` rides
  through as a stack-pinned `void*`, rehydrated with `Unsafe.AsRef`; zero
  allocation, no boxing, one reflection bind per (entry, `TData`) pair).
  The hub's `TData` cannot be constrained against an unknown closure at a
  non-generic call site — the constraints live on the bridge, validated at
  bind time. All lifecycle and bounds checks run BEFORE the pointer call.
- `GeneratedTimeline<TTrack,TClip>` survives as the compiled-tables flavor
  (same new hooks, per-closure, no global registry) for the frozen/receipts
  path.
- Playback is an 8-byte blittable value (`Sequential`: `uint Tick`,
  `ushort Cycles`, `PlaybackFlags Flags`): `uint Tick` + `ushort Cycles`
  was pinned over the design's `ushort Tick` + `uint Cycles` swap — same
  8 bytes, no 65,535-tick ceiling (the 65,536-op benchmark fixture
  literally exceeds it). The flags word carries only lifecycle and
  completion facts: `Started`, `Stopped`, `LastLoopFrame`, `Completed`.
- Movement facts are per work, not callbacks: `TrackWork.State` is a
  `ClipState` (`Enter`/`Stay`/`Exit`), and one `switch` in the consumer
  replaces a family of push hooks. Pinned semantics, each receipt-checked:
  - `Enter`: this step crossed the work's entry edge — forward through
    `Start` (`prevEff < Start <= tEff`, with the wrapped-span rule),
    backward through `End`. Sequentially it coincides with the first
    active frame.
  - `Exit`: POSITIONAL — the destination is the work's last active frame
    (forward `t == End-1`; backward mirror `t == clip.Start`, the last
    frame visited moving backward). Exit takes precedence over Enter when
    both apply (one-frame windows, jumps landing on the last frame): every
    visit's final frame is Exit.
  - `Stay` otherwise. A blend-resolved work reports the pair's OUTER
    window (min Start, max End). Fully-crossed clips during multi-cycle
    jumps are invisible (not in the destination rows). Stateless sampling
    carries no movement, so it never reports Enter; Exit is positional and
    still fires.
  - Effects on boundary frames: fixtures and the domain example apply work
    ONLY on `Stay` — Enter/Exit frames notify but do not accumulate. The
    engine does not enforce this; it is the consumer's switch.
  - Lifecycle: `Start(index, at)` mints a fresh `Playback` with `Started`
    set and positions silently; `default(Playback)` is uninitialized —
    Forward/Backward throw `InvalidOperationException` before any callback;
    `Stop` returns `pb | Stopped` (no callbacks, requires `Started`); a
    stopped playback rejects before any callback; a new `Start` clears the
    lifecycle bits fresh.
  - Flags: non-looping forward sets `Completed` at destination
    `>= duration-1` (backward: `== 0`); looping sets `LastLoopFrame` when
    the destination lands ON the loop's last local frame. `Cycles`
    (`ushort`): forward adds wraps exactly, backward subtracts saturating
    at 0; forward overflow past `ushort.MaxValue` throws before callbacks.
- Direction is a method, not a subtype (`Forward`/`Backward` on both the
  engine call sites and the consumer hooks). Looping is a timeline trait
  (`Loops` for generated tables, authored `Looping()` at runtime); wraps
  move `Cycles` instead of setting `Completed`.
- Non-wrapped steps use three region cut bits. A call-local cursor handles
  nearby positions inside a batch, with binary search for distant positions
  in larger tables. The cut bits feed the frozen emitter's rank tables; the
  redesigned engine no longer reads them on the hot path (the aggregate
  movement bits are gone). The reference checks and measured costs are in
  [review.md](review.md).

## Phase 1 — core library

Move the validated code out of the benchmark harness into a real project:

- `src/Tl.Core`: the three interfaces (`IBlend`, `IForward`, `IBackward`),
  `PlaybackFlags` + `ClipState` + `Playback`, the CSR rows (`RegionRow`,
  `TrackRow`, `ClipRow`, `ClipEdge`), `ITrackTables` (incl. `RegionFlags`,
  `ClipEdges`, `Loops`), `Tracks`/`TrackWork`, `GeneratedTimeline<,>`, the
  authoring surface (`TrackRef`, `TimelineBuilder`, the `TimelineBuild`
  delegates, `Timeline<,>.Build`), and the process-global `Timeline` hub
  with its function-pointer dispatch (`DataToken`, the bridge machinery)
  and never-reusing `ushort` index registry with `Destroy`. This replaces
  the current empty `src/` scaffold, whose
  layout reflects the older mock API rather than the validated architecture.
- `TClip : unmanaged` stays on the playback entry points (`stackalloc`
  resolution buffer). Managed payloads are a Phase 4 question.
- Unit tests replay the benchmark fixtures (`VitalsTrack` tables, the runtime
  `BuildTimeline()` authoring case) and assert receipt equality against the
  numbers already verified in the Dispatch `--verify` path.
- Resolved: the aggregate movement/status word and the clip-level hook shell
  are gone. Movement facts live per work as `TrackWork.State`
  (`ClipState.Enter`/`Stay`/`Exit`, pinned semantics above), consumed by one
  `switch` in the `IForward`/`IBackward` body; the frozen parity receipts
  accumulate the state codes as their checksum.

## Phase 2 — generator

- Authoring model in the spirit of the [api mock](api/README.md) generation
  example (`Timeline.Define(...).Track(...).Clip(...)`), but with `uint`
  ticks and the CSR/`ITrackTables` target, not frames.
- Waffle templates (as proven by `benchmarks/GenerateHooks` and
  `benchmarks/Generate`) emit, per timeline: the `TTrack` tables struct
  implementing `ITrackTables<TTrack,TClip>` + `IBlend<TClip>`, and the closed
  `GeneratedTimeline<TTrack,TClip,TData>` instantiation. Only implemented
  hooks are emitted.
- Acceptance: generating the `VitalsTrack` fixture from its authoring calls
  produces tables whose receipts are byte-identical to the hand-authored
  ones.
- Integration choice (CLI exe vs MSBuild vs Roslyn source generator) is a
  Phase 2 decision; the mock leaves file-writing to the integration on
  purpose.

## Phase 3 — multi-timeline dispatch

- The registry is already process-global (one `ushort` index space across
  every `<TTrack, TClip>` closure); Phase 3 wires the generated entries into
  it. Per-call dispatch is the pinned function-pointer bridge (one
  reflection bind per (entry, `TData`) pair, then a bare `delegate*` call);
  the `HubDispatch` ApiShape arm prices it against the direct closure call.
- Dense `delegate*` tables only as an opt-in when indices are dense and the
  footprint is acceptable; a two-level 256-page pointer table would shrink it
  but is unbenched — bench before adopting.
- Player-level composition (several live `ushort` indices per player) rides
  on the existing index sequencing; nothing new to design.

## Phase 4 — playback semantics (design + bench before locking)

The api mock explores a wider contract (`long` ticks, frames, budgeted
traversal, resume tokens) that the benchmarks have not validated. The former
open questions about sequential playback state and reverse playback are now
settled by the `Playback` design (see [benchmarks.md](benchmarks.md)); each
item below still enters the contract only after it has numbers:

- Explicit timeline indices for save/replay determinism.
- `[InlineArray]`-backed locals for managed `TClip` payloads (removing the
  `unmanaged` constraint from the happy path).
- Budgeted traversal and resume tokens for bounded catch-up work.

## Housekeeping (2026-09-07)

Audit of the rapid-iteration leftovers:

- Removed: `ITrack`, `ITimelineBackward`, `ITimelineStart`,
  `ITimelineStop`, `ITimelineLoop` (declared, zero references), and the
  superseded early Algorithms run `results/bdn` + `results/run.log`
  (unreferenced; `results/validated` is the copy of record).
- Kept on purpose: the empty `src/` scaffold and `docs/adapters.md` /
  `docs/semantics.md` placeholders (Phase 1 fills them), and `docs/api/`
  (explicitly labeled a discussion mock; its `long`-tick timing contract is a
  Phase 4 input, not the current contract).
