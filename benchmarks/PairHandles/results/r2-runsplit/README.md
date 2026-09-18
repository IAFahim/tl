# r2-runsplit: uniform-prefix run split in mixed chunks

Atom 2 of the `perf/167-packed-runs` workstream (issue #167), on top of
`r2-anchor` (origin/main @ 50d5c39 + harness `--filter` passthrough). Change,
`src/Tl.Core/TimelineSet.cs` only:

- `TimelineSetLane.Apply`'s mixed branch, after the existing whole-chunk
  validation (`ResolveChunk` loop + `ValidateChunk`, unchanged), now cascades
  id-run segments: `RunEnd(ids, i, chunkEnd)` finds the first id change; a
  segment of at least `MinSegment` (128) rows is applied through the extracted
  `ApplyUniformSegment` (gather probe + gather blocks + uniform scalar walk,
  the exact machinery uniform chunks use); the first shorter segment hands the
  chunk remainder to the unchanged mixed walk. Segments below the threshold
  keep the previous single mixed pass, so alternating crowds are unaffected.
- `ApplyUniformSegment` is extracted from the uniform branch of `Apply` and
  reused by `ApplySlot` (behavior-identical, no per-chunk probe change).
- `RunEnd` widened to `ReadOnlySpan<ushort>` so it scans the ids column.

No semantic change: validation order, throw rows, resolve order, row order,
and FP evaluation are unchanged; gather/uniform-vs-mixed parity is bit-proven
by the receipts. Exceptions still fire before any row of the chunk is touched.
New unit receipts: `ChunkStraddlingStaggeredRunsMatchPerAssetUniformLanes`
(5,000-row runs straddling chunk boundaries vs per-asset oracle, forward and
backward) and `ChunkStraddlingBlocksMatchPerAssetUniformLanes` (100-row
(handle, position) blocks under one chunk vs oracle). 565/565 tests green;
`--parity` PASS; `FusedAdvance` parity unchanged by construction (no shared
code); `benchmarks/Alpha -- --verify` PASS; Tl.Alpha receipts 0 B; NativeAOT
publish PASS; budget 188,819/300,000.

Receipts (100k rows, medians, us per pass = ns/row x 100; same session and
machine as `r2-anchor`, i9-14900K, .NET 10):

| Shape | anchor Default | split Default | anchor InProcess | split InProcess | Allocated |
|---|---:|---:|---:|---:|---:|
| LaneUniform (gold) | 18.24 | 18.22 | 18.33 | 18.29 | 0 B |
| PairOne | 18.77 | 18.20 | 18.14 | 18.11 | 0 B |
| PairRuns8 | 31.64 | **20.12** | 31.63 | **20.25** | 0 B |
| PairAlternating8 | 85.96 | 79.20 | 80.51 | 79.44 | 0 B |
| PairRuns8Waves | 17.14 | 17.22 | 17.20 | 16.87 | 0 B |
| PairBlocks8Waves | 20.58 | 20.12 | 19.76 | 19.04 | 0 B |
| PairAlternating8Waves | 68.14 | 64.91 | 63.40 | 61.29 | 0 B |

Reading: `PairRuns8` drops 36% because its ~7 chunks per pass that straddle a
12,500-row run boundary used to walk all ~4,096 rows through the mixed
walk; each now splits into one 200-1,500 row boundary segment plus a uniform
remainder, both on the gather path. PairRuns8 sits at 1.11x gold InProcess
(was 1.73x). Untouched shapes are flat to slightly favorable (session drift);
`PairBlocks8Waves` keeps its 100-row blocks on the mixed scanner because a
~100-row segment pays more in per-segment probe cost than the walk saves
(`MinSegment = 128` encodes that crossover).

## Recorded dead end in this atom: per-chunk rescan cascade

The first cut (not committed) split only the first segment and let the outer
loop re-probe `UniformChunk` + `ValidateChunk` per segment. Both probes are
full-chunk scans without early exit, so `PairBlocks8Waves` (41 segments per
chunk) paid ~2 chunk scans per segment: 20.6 -> 201.1 us Default, 19.8 ->
203.1 InProcess. The committed cascade scans once per chunk (`RunEnd` per
segment early-exits at the boundary) and hands short-segment remainders to
the mixed walk in one pass.

## Recorded dead end in this atom: packed `(handle << 16) | position` RunEndTwo

Hypothesis 2 of the issue, interleave variant (two loads, `Avx2.UnpackLow` /
`UnpackHigh` to one `Vector256<uint>` stream, two compares, lane-reassembled
mask). Functionally correct (parity bit-exact, 16/16 pair-lane unit receipts
green) but catastrophically counterproductive on this host: `PairBlocks8Waves`
20.6 -> 201-605 us Default, 19.8 -> 611-625 InProcess, reproducible across
fresh builds (pristine rebuild in the same minutes-window measured 20.0/19.9).
Rejected; the recorded ceiling bound (entire mixed-run machinery
`PairBlocks8Waves` - `PairRuns8Waves` = ~0.03 ns/row) already capped the
hypothesis below default-job noise; the interleave adds two unpacks and a
lane-scrambled mask reassembly per 16 rows to a loop whose whole budget is
that ceiling. Not retried again.
