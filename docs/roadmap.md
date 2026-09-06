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
  materialized. `ForwardTracks`/`ForwardItem` are `ref struct` views over the
  CSR tables.
- Blending is resolved before the consumer sees it: `IBlend<TClip>.Blend`
  collapses an overlapping pair into one clip, fused into enumerator
  `MoveNext`, written into a single per-call `stackalloc` sized
  `MaxActiveTracks`.
- Static-abstract table fetches (`ITrackTables`) are hoisted once per call.
- Hooks are abstract-only: no default interface methods (10.9–11.4× and
  silent mutation loss), no delegates (4×), no boxing (4–4.5×).
- Region navigation stays data-driven (cursor/CSR), not generated code.
- Runtime-authored instances cost the same as the generated shell and share
  the receipts; per-closed-generic-type `ushort Index` sequencing works.
- Sparse `ushort` timeline dispatch: Radix8 nested switches are the safe
  default; dense `delegate*` tables win on speed (6–11 ns) but cost 512 KB
  for the full space; the fused megaswitch loses on sparse keys.

## Phase 1 — core library

Move the validated code out of the benchmark harness into a real project:

- `src/Tl.Core`: `IForward`, `IBlend`, `IForwardTracks`, the CSR rows
  (`RegionRow`, `TrackRow`, `ClipRow`), `ITrackTables`, `ForwardTracks`,
  `ForwardItem`, `GeneratedTimeline<,,>`, and the runtime-authored
  `Timeline<,,>` (`AddTrack`/`AddClip`/`Build`/`Forward`, `ushort Index`).
  This replaces the current empty `src/` scaffold, whose layout reflects the
  older mock API rather than the validated architecture.
- `TClip : unmanaged` stays on the playback entry points (`stackalloc`
  resolution buffer). Managed payloads are a Phase 4 question.
- Unit tests replay the benchmark fixtures (`VitalsTrack` tables, the runtime
  `BuildTimeline()` authoring case) and assert receipt equality against the
  numbers already verified in the Dispatch `--verify` path.
- Decision needed: `IForward<TClip,TData>` (the simple one-clip hook) is
  currently implemented by `VitalsClip` but exercised by nothing — not a
  benchmark arm, not a line in benchmarks.md. Either give it an ApiShape arm
  and document it, or drop it from the contract before Phase 1 freezes.

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
- Player-level composition (several `Timeline<,,>` instances per player)
  rides on the existing `Index` sequencing; nothing new to design.

## Phase 4 — playback semantics (design + bench before locking)

The api mock explores a wider contract (`long` ticks, frames, transitions,
budgeted traversal, resume tokens) that the benchmarks have not validated.
Each item below enters the contract only after it has numbers:

- `ref State` sequential playback: transition events across successive
  `Forward` calls (the mock's `Update`/`Seek`/`Advance` split).
- Reverse playback: the old `ITimelineBackward` sketch was dead weight and
  was removed; reintroduce only with a bench that shows what it costs.
- Explicit timeline indices for save/replay determinism.
- `[InlineArray]`-backed locals for managed `TClip` payloads (removing the
  `unmanaged` constraint from the happy path).

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
