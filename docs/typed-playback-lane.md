# Typed playback lane: `Timeline<T>.Forward` / `Timeline<T>.Backward`

Status: prototype (issue #104). Runnable receipt: `benchmarks/TypedPlaybackProto`.
Original prototypes with the host ECS (Frent) wired end to end: [IAFahim/FrentFun](https://github.com/IAFahim/FrentFun) — `Proto/` (525530e), `Proto2/` (1ba53d4), `Proto3/` (8d38105, sort rejection 5fe37d8).

## The lane

One timeline type per frame, one frame per call, host-owned storage, run-length groups:

```cs
Timeline<Combat>.Forward(posSpan).Apply(hpSpan, cycSpan);
Timeline<Combat>.Backward(posSpan).Apply(hpSpan, cycSpan);   // measured cost of Forward
```

The promises the lane is built on:

- forward and backward are always exactly ONE frame — no step parameter exists on this lane
- lag catch-up is repeated calls; rewind is `Backward`
- the host owns the columns; the lane never moves or copies row data
- `T` is the timeline: duration and per-position effects are `static abstract` members, so the
  JIT compiles them per closed generic — effect lookup is literal code, no table indirection

## Why it is fast (each step measured; see the FrentFun commits)

1. Thinking is O(unique positions), not O(rows). Contiguous rows sharing a position form one
   run; the effect and next position resolve once per run. The scan finds run boundaries with
   one vector compare per 16 rows.
2. The clock is a dense `uint` column. Commit is a vector fill of the next position; the cycle
   column is touched only in wrapping runs. Splitting the clock out of a packed row struct was
   the single largest win (strided stores became vector fills).
3. Application is uniform within a run: one vector add over the run's column slice. Backward
   inverts the effect and reverses the wrap arithmetic with zero shared branches.

## Receipts (Ryzen 5 8500G, .NET 10, Release, best of 5, 60 ticks; parity bit-exact vs the hand lane)

In-repo (`dotnet run --project benchmarks/TypedPlaybackProto -c Release`; prints 100k then 1M;
exits nonzero on any parity FAIL):

| lane | 100k ns/row | 1M ns/row |
| --- | ---: | ---: |
| plain rows `HP += 1` | 0.41 | 0.45 |
| hand lane (per-row table walk) | 3.09 | 3.32 |
| Forward, uniform | 0.19 | 0.21 |
| Forward, waves of 100 | 0.24 | 0.30 |
| Forward, staggered singles | 4.10 | 4.43 |
| Forward+sort, staggered | 4.39 | 4.75 |
| Forward+sort, uniform | 2.76 | 2.86 |
| Backward, uniform | 0.20 | 0.21 |
| Forward x2 (catch-up) | 8.24 | 11.01 |

With the Frent host in the loop (FrentFun `Proto3`), uniform reached 0.13-0.16 ns/row and the
in-process tl facade reference measured 19.5 ns/row kernel-bound — the lane is ~100-150x the
facade on crowd shapes and ~4.5x on staggered singles.

## Verdicts (all backed by lanes above)

- **Memory floor.** Per row per frame the lane must touch ~16 bytes (read+write position,
  read+write one column). At 1M rows that is 16 MB/frame — uniform sits on the DRAM
  bandwidth limit. Further crowd gains require fewer bytes per row (u16 positions; cycle
  column only for looping assets), not faster code.
- **Sort-then-sweep is rejected.** Counting sort by position is cheap, but making runs
  physically contiguous requires permuting every column through the index (~6 indirect
  memory ops/row), which costs more than the run bookkeeping it removes — on staggered rows
  it ties-to-loses at 100k and loses clearly at 1M, and on already-grouped data it is pure
  overhead (2.8+ vs 0.21). Never move the data to match the algorithm.
- **Hash-map dedupe is rejected** (FrentFun `Proto/` map lane: 4.4-7.6 ns/row): cache-hostile
  indirection; run-length scanning over contiguous storage wins.
- **Scattered singles fix is a singleton fast path**, not sorting: apply inline when the scan
  sees run length 1, skipping record buffering (FrentFun `Proto/` inline shape measured ~3.0).
  Not yet implemented in this prototype; the lane to beat is 4.1-4.4.
- **Direction is free.** `Backward` shares no branches with `Forward` and measures identical.
- **Catch-up is repeated calls**, linear at 100k, slightly superlinear at 1M where the second
  sweep re-faults the working set.
- **Entry cost** is ~300 ns per timeline type per frame regardless of row count.

## What a production lane must still answer (tracked in #104)

- Mapping to the irreducible laws: authored order across multiple consumers per position
  (per-position resolved consumer program applied per run, in order), blending factors
  (constant per position, precomputable), exact reverse (`Backward` = reversed consumer order
  with inverted effects), two-pass execute/commit at run granularity.
- Where per-position tables come from: bind-time precompute per (type, asset) through the
  public stage view, content-hash keyed like kernels; sparse/lazy fallback for very large
  durations (#88 open question 3).
- Purity lanes: analyzer-proven pure consumers broadcast as effects; column-reading
  consumers walk members with the cached frame.
- The prototype hardcodes pure per-position effects; the general `ITimelineJob<,>` surface is
  not wired into it.

Relations: #88 (coordinator-owns-rows variant, closed as doc+prototype), #56 (data-authored
contract this must not break), #102 (host-owned execution shape and facade cost ladder),
#104 (this work).
