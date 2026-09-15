# Typed playback lane: `Timeline<T>.Seek`

Status: shipped production playback surface (issue #104). It replaces the removed alpha.6
`Timeline.Rows(...).Tick` facade, the `TimelineKernels` kernel catalog, and `TickUnmanaged`
(removed in the same rewrite; see #104 and #65 for the removal protocol). Runnable receipts:
`benchmarks/TypedPlaybackProto`, `benchmarks/Alpha --verify`, `tests/Tl.Alpha`,
`samples/ManyEntities`. Prototype history with the host ECS (Frent):
[IAFahim/FrentFun](https://github.com/IAFahim/FrentFun) — `Proto/` (525530e), `Proto2/`
(1ba53d4), `Proto3/ (8d38105, sort rejection 5fe37d8)`.

## The lane

One timeline type per call, one frame per call, host-owned storage, run-length groups:

```cs
Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, forward: true).Apply(effects, cycles);
Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, forward: false).Apply(effects, cycles);   // measured cost of forward
```

(`BakedLane<TTrack, TClip>` is one `ITimelineLane<T>`; any hand-authored `T : ITimelineLane<T>` drives the same lane.)

`positions : Span<ushort>`, `effects : Span<float>`, `cycles : Span<long>`; all three are the
caller's arrays, borrowed only for the call. There is no per-row object, no run-record buffer,
and no allocation on the warm path. The position column is `ushort` (65,535 ticks = 18 minutes
at 60 fps); `Bind` rejects assets whose duration exceeds it.

`T` is the timeline: duration, looping, and per-position effects are `static abstract` members
of `ITimelineLane<T>`, so the JIT compiles them per closed generic — effect lookup is literal
code, no indirection. Two implementations ship:

- **Authored lanes** implement `ITimelineLane<T>` in code: `Duration`, `Looping`, `Effect`,
  `InverseEffect` are yours. Cost: authoring them by hand is only worth it for closed forms.
- **`BakedLane<TTrack, TClip>`** binds a loaded `TimelineAsset` once per (type, asset) at cold
  time: it measures the per-position forward and backward float effect of every consumer in
  the asset through the cold executor, validates position purity, and keeps the two tables on
  native blocks. Multi-pair assets fold all pairs' float-writing consumers into one column
  automatically; `Bind` rejects impure consumers (column-value or cycle dependence) with a
  diagnostic naming the pair.

## Contract

- forward and backward are always exactly ONE frame — no step parameter exists on this lane;
  lag catch-up is repeated calls; rewind is `Seek(positions, false)`
- movement is `TimelineMovement.Advance` exactly: forward skips only when
  `position >= duration`; backward skips only when `(position == 0 && !looping)`, or
  `position > duration`, or `(looping && position == duration)`; looping timelines touch the
  cycle column only on wrap (±1); finite timelines zero-fill cycles on every successful move
- consumers must self-invert through `Frame.Direction` / `FrameFlags.Reverse`; the backward
  table is the measured inverse, so forward-then-backward returns the column bit-exactly
- the host owns the columns; the lane never moves, copies, or retains row data
- finite lanes may pass `Span<long>.Empty` instead of a cycle column: finite timelines never
  wrap, so there is no cycle state to maintain and the lane touches no cycle storage (the
  zero-fill law applies to a full-length column); looping lanes require the full column
- `Apply` validates lengths and pairwise non-overlap and throws otherwise; effects and cycles
  are written only for rows that moved
- the fold is a function of position only: consumers that read the game tick, another entity's
  column, or any external state do not belong on the lane (game-tick-dependent behavior is a host
  system's job); `Bind` measures at game tick 0 and freezes that value
- the position column is `ushort`: one frame moves one tick, and 65,535 ticks bounds any
  designed timeline; `Bind` rejects an asset whose duration exceeds 65,535 with a diagnostic
- rebinding the same closed generic swaps the tables and frees the previous pair; the host must
  quiesce applies across a rebind (single-owner discipline, same shape as asset disposal)

## Why it is fast (each step measured; prototype lanes in FrentFun, production receipts below)

1. Thinking is O(unique positions), not O(rows). Contiguous rows sharing a position form one
   run; the effect and next position resolve once per run. The scan finds run boundaries with
   one vector compare per 32 rows (`ushort` lanes double the per-vector row count); singleton
   runs apply inline without leaving the scan loop.
2. The clock is a dense `ushort` column — 2 bytes per row, half the `uint` column it replaced.
   Commit is a vector fill of the next position; the cycle column is touched only in wrapping
   runs. Splitting the clock out of a packed row struct was the single largest win (strided
   stores became vector fills).
3. Application is uniform within a run: one vector add over the run's column slice. Backward
   inverts the effect and reverses the wrap arithmetic with zero shared branches.

## Receipts (Ryzen 5 8500G, .NET 10, Release, best of 5, 60 ticks; parity bit-exact vs the hand lane)

`dotnet run --project benchmarks/TypedPlaybackProto -c Release` (prints 100k then 1M; exits
nonzero on any parity FAIL); `samples/ManyEntities` adds pulse/churn shapes at 200k rows:

| lane | 100k ns/row | 1M ns/row |
| --- | ---: | ---: |
| plain rows `HP += 1` | 0.32 | 0.33 |
| hand lane (per-row table walk) | 0.73 | 0.72 |
| Seek, uniform | 0.14 | 0.16 |
| Seek, waves of 100 | 0.20 | 0.21 |
| Seek, staggered singles | 3.00 | 3.14 |
| Seek+sort, staggered | 3.36 | 3.50 |
| Seek+sort, uniform | 2.16 | 2.21 |
| Backward, uniform | 0.15 | 0.16 |
| Seek x2 (catch-up) | 6.02 | 6.23 |

Finite crowds with an empty cycle column measure 0.28 -> 0.12 ns/row at 1M rows
(60,000-tick finite asset, uniform positions): the 8-byte zero-fill store per row disappears.
`samples/ManyEntities` churn exercises the API on its finite window asset (parity OK).

ManyEntities (200k rows x 60 ticks, lane vs hand SoA sweep): pulse 0.35 vs 0.51 ns/row,
churn 0.71 vs 0.71 (lane wins/ties; no per-pass copies), sweep 4.46 vs 0.76 (staggered clocks
fragment runs; grouped storage stays the ceiling). Warm playback allocates 0 B
(`tests/Tl.Alpha --capacity`, `benchmarks/Alpha --verify`); 256 same-pair tracks fold into
one 34,688-effect per tick (`tests/Tl.Alpha --module-capacity`).

## Verdicts (all backed by lanes above)

- **Memory floor.** Per row per frame the lane must touch ~12 bytes (read+write a 2-byte
  position, read+write one 4-byte column). At 1M rows that is 12 MB/frame — uniform sits on
  the DRAM bandwidth limit. Further crowd gains require fewer bytes per row (a cycle column
  only for looping assets), not faster code.
- **`ushort` positions shipped (uint → u16 A/B, interleaved rounds, controls flat).** The
  column halves (4 → 2 B/row) and the run scan compares 32 rows per vector instead of 16:
  uniform 0.19 → 0.15-0.16, backward 0.19 → 0.16, waves 0.21-0.22 → 0.19-0.20 ns/row at 1M.
  The singleton-run shapes pay for 16-bit scalar ops in the per-row path: staggered
  2.79-2.92 → 3.13, catch-up 5.66-5.80 → 6.23 (+8%). Kept: crowd shapes are the lane's
  purpose, staggered is already the documented sort/grouping case, and the halved column
  halves every host cache line budget for positions. Duration is capped at 65,535 ticks at
  bind. Rejected in the same sweep: packing `bool`s into flags (the warm path has no
  per-row bool; `Looping` is a JIT constant) and 32-bit pair keys (pairs are per closed
  generic, not per row).
- **Sort-then-sweep is rejected.** Counting sort by position is cheap, but making runs
  physically contiguous requires permuting every column through the index (~6 indirect
  memory ops/row), which costs more than the run bookkeeping it removes — on staggered rows
  it ties-to-loses at 100k and loses clearly at 1M, and on already-grouped data it is pure
  overhead (2.3 vs 0.19). Never move the data to match the algorithm.
- **Hash-map dedupe is rejected** (FrentFun `Proto/` map lane: 4.4-7.6 ns/row): cache-hostile
  indirection; run-length scanning over contiguous storage wins.
- **Scattered singles use a singleton fast path**, not sorting: the scan applies run length 1
  inline, skipping run bookkeeping entirely (staggered 2.9-3.1 vs prototype 4.1-4.4 without
  it).
- **Direction is free.** Backward shares no branches with forward and measures identical.
- **Catch-up is repeated calls**, linear at 100k, slightly superlinear at 1M where the second
  sweep re-faults the working set.
- **Bind-time measurement answers the table question.** Per-position effect tables come from
  the cold executor over the loaded asset (no authored table, no content-hash kernel): the
  fold is exact for dyadic float domains and ULP-stable otherwise, and the position-purity
  validation turns "consumers must be pure" from a convention into a bind-time error.

- **Catch-up time-skip is a validated prototype, not shipped.** Cumulative effect tables
  (one extra `duration+1` float table per bind) collapse K-frame catch-up from K passes to
  one: measured on staggered 1M rows, x2 catch-up 9.3 -> 4.9 ns/row and x5 23.8 -> 5.0
  (the pass cost stops scaling with K). Position and cycle math are integer-exact; the float
  fold is bit-exact only when the cumulative table equals the sequential fold, which is
  checkable per (position, frames) at bind exactly like position purity. Shipping it needs a
  `frames` contract decision (the lane today is strictly one frame per call) — owner call.

Relations: #88 (coordinator-owns-rows variant, closed as doc+prototype), #56 (data-authored
contract), #102 (host-owned execution shape and facade cost ladder), #104 (this work).
