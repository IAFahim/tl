# tl roadmap

The benchmark phase answered the design questions; nothing in `src/` is real
yet. This plan turns the validated shapes in [benchmarks.md](benchmarks.md)
into an engine, one phase at a time, keeping the discipline that produced
those verdicts: every performance claim gets a BenchmarkDotNet arm, every
semantic claim gets a receipt check under `--verify`, and runs are pinned by
`benchmarks/run.py` (CPU affinity, environment, source hashes in
`environment.json`).

## What is already decided

From [benchmarks.md](benchmarks.md), the contract is fixed and must not drift:

- Ticks are `uint`; multi-tick calls are `params ReadOnlySpan<uint>`.
- One callback per tick through `IForwardTracks`; the frame is never
  materialized. `Tracks`/`TrackItem` are `ref struct` views over the
  CSR tables.
- Blending is resolved before the consumer sees it: `IBlend<TClip>.Blend`
  collapses an overlapping pair into one clip, fused into enumerator
  `MoveNext`, written into a single per-call `stackalloc` sized
  `MaxActiveTracks`.
- Static-abstract table fetches (`ITrackTables`) are hoisted once per call.
- Hooks are abstract-only: no default interface methods (10.9–11.4× and
  silent mutation loss), no delegates (4×), no boxing (4–4.5×).
- Region navigation stays data-driven (cursor/CSR), not generated code.
- Runtime-authored timelines cost the same as the generated shell and share
  the receipts; authoring is a `Timeline<,,>.Build` callback over a
  stack-scoped builder, per-closed-generic-type `ushort` index sequencing
  works, and the index registry never reuses slots (Destroy tombstones).
- Sparse `ushort` timeline dispatch: Radix8 nested switches are the safe
  default; dense `delegate*` tables win on speed (6–11 ns) but cost 512 KB
  for the full space; the fused megaswitch loses on sparse keys. With the
  expected ~256 timelines per closed type the index space is dense, so the
  hub is one flat switch the JIT lowers to a jump table; the dense registry
  is capped at 256 and Radix8 takes over beyond that.
- Playback is an 8-byte blittable value: `uint Tick` plus a packed word of
  26-bit `Cycles` and six status flags (`Enter`, `First`, `Active`, `Last`,
  `Complete`, `Exit`). It flows `in` and comes back by value — no hidden
  state, snapshot-friendly for save games and rewind. Movement facts are
  polled bits, not push callbacks: the entire enter/exit/looped hook family
  is replaced by the status word and `Tracks.Status`.
- Direction is a method, not a subtype: `IForward`/`IBackward` (clip-level)
  and `IForwardTracks`/`IBackwardTracks` (view-level). Looping is a timeline
  trait (`Loops` for generated tables, authored `Looping()` at runtime);
  wraps move `Cycles` (forward adds, backward
  saturates at zero) instead of setting `Complete`.
- Non-wrapped steps use three region cut bits. A call-local cursor handles
  nearby positions inside a batch, with binary search for distant positions
  in larger tables. Jump scans stop once both movement bits are known; wraps
  scan `ClipEdge`. The reference checks and measured costs are in
  [review.md](review.md).

## Phase 1 — core library

Move the validated code out of the benchmark harness into a real project:

- `src/Tl.Core`: `IForward`, `IBackward`, `IBlend`, `IForwardTracks`,
  `IBackwardTracks`, the CSR rows (`RegionRow`, `TrackRow`, `ClipRow`,
  `ClipEdge`), `ITrackTables` (incl. `RegionFlags`, `ClipEdges`, `Loops`),
  `Playback` + `PlaybackFlags`, `Tracks`, `TrackItem`,
  `GeneratedTimeline<,,>`, `ClipTimeline<,,>`, and the runtime-authored
  `Timeline<,,>` (static `Build(Definition)` over a stack-scoped
  `TimelineBuilder`, a never-reusing `ushort` index registry with `Destroy`,
  index-keyed `Start`/`Forward`/`Backward`; looping is authored). This
  replaces the current empty `src/` scaffold, whose
  layout reflects the older mock API rather than the validated architecture.
- `TClip : unmanaged` stays on the playback entry points (`stackalloc`
  resolution buffer). Managed payloads are a Phase 4 question.
- Unit tests replay the benchmark fixtures (`VitalsTrack` tables, the runtime
  `BuildTimeline()` authoring case) and assert receipt equality against the
  numbers already verified in the Dispatch `--verify` path.
- Resolved: `IForward<TClip,TData>` stays in the contract (with its
  `IBackward` mirror and the `ClipTimeline<,,>` shell); it has an ApiShape
  benchmark arm and rewind receipts.

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

- One registry per `<TTrack, TClip>` closed type: timeline `ushort Index` →
  generated entry, dispatched Radix8-style (nested switches on 8-bit slices).
- Dense `delegate*` table only as an opt-in when indices are dense and the
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
