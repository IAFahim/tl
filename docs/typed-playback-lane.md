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

`positions : Span<uint>`, `effects : Span<float>`, `cycles : Span<long>`; all three are the
caller's arrays, borrowed only for the call. There is no per-row object, no run-record buffer,
and no allocation on the warm path.

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
- `Apply` validates lengths and pairwise non-overlap and throws otherwise; effects and cycles
  are written only for rows that moved
- the fold is a function of position only: consumers that read the game tick, another entity's
  column, or any external state do not belong on the lane (game-tick-dependent behavior is a host
  system's job); `Bind` measures at game tick 0 and freezes that value
- rebinding the same closed generic swaps the tables and frees the previous pair; the host must
  quiesce applies across a rebind (single-owner discipline, same shape as asset disposal)

## Why it is fast (each step measured; prototype lanes in FrentFun, production receipts below)

1. Thinking is O(unique positions), not O(rows). Contiguous rows sharing a position form one
   run; the effect and next position resolve once per run. The scan finds run boundaries with
   one vector compare per 16 rows; singleton runs apply inline without leaving the scan loop.
2. The clock is a dense `uint` column. Commit is a vector fill of the next position; the cycle
   column is touched only in wrapping runs. Splitting the clock out of a packed row struct was
   the single largest win (strided stores became vector fills).
3. Application is uniform within a run: one vector add over the run's column slice. Backward
   inverts the effect and reverses the wrap arithmetic with zero shared branches.

## Receipts (Ryzen 5 8500G, .NET 10, Release, best of 5, 60 ticks; parity bit-exact vs the hand lane)

`dotnet run --project benchmarks/TypedPlaybackProto -c Release` (prints 100k then 1M; exits
nonzero on any parity FAIL); `samples/ManyEntities` adds pulse/churn shapes at 200k rows:

| lane | 100k ns/row | 1M ns/row |
| --- | ---: | ---: |
| plain rows `HP += 1` | 0.29 | 0.29 |
| hand lane (per-row table walk) | 0.69 | 0.70 |
| Seek, uniform | 0.18 | 0.19 |
| Seek, waves of 100 | 0.23 | 0.25 |
| Seek, staggered singles | 3.10 | 2.92 |
| Seek+sort, staggered | 3.09 | 3.38 |
| Seek+sort, uniform | 2.29 | 2.30 |
| Backward, uniform | 0.19 | 0.20 |
| Seek x2 (catch-up) | 5.50 | 5.90 |

ManyEntities (200k rows x 60 ticks, lane vs hand SoA sweep): pulse 0.42 vs 0.51 ns/row,
churn 0.70 vs 0.78 (lane wins; no per-pass copies), sweep 4.06 vs 0.76 (staggered clocks
fragment runs; grouped storage stays the ceiling). Warm playback allocates 0 B
(`tests/Tl.Alpha --capacity`, `benchmarks/Alpha --verify`); 256 same-pair tracks fold into
one 34,688-effect per tick (`tests/Tl.Alpha --module-capacity`).

## Verdicts (all backed by lanes above)

- **Memory floor.** Per row per frame the lane must touch ~16 bytes (read+write position,
  read+write one column). At 1M rows that is 16 MB/frame — uniform sits on the DRAM
  bandwidth limit. Further crowd gains require fewer bytes per row (u16 positions; cycle
  column only for looping assets), not faster code.
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

Relations: #88 (coordinator-owns-rows variant, closed as doc+prototype), #56 (data-authored
contract), #102 (host-owned execution shape and facade cost ladder), #104 (this work).
