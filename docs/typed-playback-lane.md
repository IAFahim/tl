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
at 60 fps); the baker rejects longer timelines at bake time and `Bind` keeps the same
check as a backstop for pre-existing bytes.

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

- forward and backward are always exactly ONE frame — no step parameter exists on this lane,
  now or in the future (owner decision 2026-09-15); lag catch-up is repeated calls; rewind is
  `Seek(positions, false)`
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
  designed timeline; the baker rejects duration above 65,535 at bake time (diagnostic with
  line and column) and `Bind` keeps the same check for pre-existing bytes
- rebinding the same closed generic swaps the tables and frees the previous pair; the host must
  quiesce applies across a rebind (single-owner discipline, same shape as asset disposal);
  several same-pair assets that must coexist belong in a `TimelineSet` (next section)

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

## Many timelines under one pair: `TimelineSet<TTrack, TClip>`

The static lane binds one table per closed generic: two assets sharing `<TTrack, TClip>`
would overwrite each other through `BakedLane.Bind`. `TimelineSet` removes that ceiling:
every asset loaded for one pair gets a dense `ushort` id **at load time** — ids are never
authored, never baked, and carry no content identity — and all measured tables live in one
contiguous native block owned by the set.

```cs
using var minionAsset = TimelineAsset.Load(File.ReadAllBytes("minion-jump.tlb"));
using var bossAsset   = TimelineAsset.Load(File.ReadAllBytes("boss-jump.tlb"));
var jumps    = new TimelineSet<JumpTrack, JumpClip>();
var minionId = jumps.Add(minionAsset);   // 0
var bossId   = jumps.Add(bossAsset);     // 1

var timelineIds = new ushort[] { minionId, minionId, bossId };
var lastTick    = new ushort[] { 0, 0, 2 };
var height      = new float[3];
var cycle       = new long[3];

jumps.Gather(timelineIds).Seek(lastTick, true).Apply(height, cycle);
```

Contract:

- ids come from `Add` at load time (first asset 0, dense from there); a set holds at most
  65,536 timelines; an unbound id throws `ArgumentException` naming the row and id
- each `Add` measures through the same cold core as `BakedLane.Bind` (position purity,
  duration ≤ 65,535, consumer order) and appends the forward/backward tables into the one
  contiguous block; `Dispose` frees it; the asset may be disposed once `Add` returns
- two timelines in one set are respected as different: rows on different ids advance and
  fold through their own tables, durations, and loop flags in one call, bit-exact with
  running each asset through the static lane separately (asserted per frame in tests)
- `Gather(ids).Seek(positions, forward).Apply(effects, cycles)` follows the one-frame law
  exactly; an empty cycle column is legal only when every timeline in the set is finite
- the warm path allocates 0 B; the id column is read once per 4,096-row chunk by a probe
  that validates ids and detects single-timeline chunks — those run at the static lane's
  shape with the table hoisted per chunk; mixed chunks scan (id, tick) pairs with one
  combined vector mask per 16 rows

Receipts (same host and protocol as above; 1M rows, 20-frame reps, best of 5 over 3
interleaved rounds, real `Tl.Core`; parity bit-exact vs per-asset static lanes, forward
and backward):

| shape (one call over a mixed crowd) | ns/row | vs single-table lane |
| --- | ---: | ---: |
| static lane, waves of 100 (baseline) | 0.13 | — |
| set, uniform ids, waves of 100 | 0.17 | +32% |
| set, id blocks of 100, waves of 100 | 0.18 | +42% |
| set, id blocks of 64 | 0.25 | +95% |
| set, staggered ticks, uniform ids | 4.48 | +1% |
| set, id blocks of 16 (adversarial interleave) | 0.45 | +245% |

The same-window control is a replica of the two-span experiment loop (specialized
forward-only law, constant duration, prebuilt `float**` tables, no chunk probe, no id
validation) run under this exact protocol:

| shape | production set | replica |
| --- | ---: | ---: |
| uniform ids, waves of 100 | 0.17 | 0.16 |
| id blocks of 100 | 0.18 | 0.16 |
| id blocks of 64 | 0.25 | 0.25 |
| id blocks of 16 | 0.45 | 0.39 |
| staggered ticks | 4.48 | 5.19 |

Production sits within 8-15% of the specialized replica on crowd shapes and beats it on
staggered ticks, while carrying direction, finiteness, and id-safety semantics the replica
lacks entirely. The earlier experiment window (which printed 0.15-0.16 on crowd shapes
against a 0.12 baseline) does not reproduce on this host: the replica itself measures
0.39-0.41 on 16-row interleaves today against a 0.13 baseline — host drift of exactly this
kind was documented on #104 during the fused-scan dead end, which is why every number
above is a same-window, same-protocol, interleaved-rounds A/B. Per-run cost in mixed
chunks is branch-miss bound (PMU `perf stat`: ~0.8 extra misses per 16-row run at ~17
cycles); component-sorted crowds — the ECS norm — sit on the first rows of both tables.

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
  checkable per (position, frames) at bind exactly like position purity. **Rejected by owner decision
  (2026-09-15):** a game runs thousands of systems that must all observe every tick — a
  K-tick skip hides K-1 intermediate states from them — and a precomputed K-frame fold can
  differ from K sequential folds in the last float bit. The number stays on record as the
  measured ceiling only; the one-frame contract is permanent.

Relations: #88 (coordinator-owns-rows variant, closed as doc+prototype), #56 (data-authored
contract), #102 (host-owned execution shape and facade cost ladder), #104 (this work).
