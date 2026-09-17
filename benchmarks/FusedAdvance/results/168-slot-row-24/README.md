# FusedAdvance A/B — 24 B slot row (issue #168)

Candidate `perf/168-slot-row-24` (TLB1 v3, 24 B rows) vs base `59b4972` (TLB1 v2, 32 B rows), same machine (Intel Core i9-14900K, Unix 7.2.3.1, .NET 10.0.12, procs 32), interleaved base/candidate runs, two rounds each (`base-r1 cand-r1 base-r2 cand-r2` ordering inside each round). Median of per-run medians over the two rounds; 100,000 rows per op, so ns/row = us/100. Raw per-iteration data in `*-r?/results/*-report-full.json`; medians.json carries the extracted table. `--parity` passes on both builds before timing.

| Method/Shape | v2 med (us) | v3 med (us) | delta | ns/row v2 -> v3 |
|---|---:|---:|---:|---:|
| TwoCall LaneUniform | 10,509.1 | 10,470.2 | -0.4% | 0.1051 -> 0.1047 |
| TwoCall LaneUniformBackward | 10,499.2 | 10,496.4 | -0.0% | 0.1050 -> 0.1050 |
| TwoCall LaneWaves | 12,453.2 | 12,313.3 | -1.1% | 0.1245 -> 0.1231 |
| TwoCall LaneStaggered | 15,629.6 | 15,676.0 | +0.3% | 0.1563 -> 0.1568 |
| TwoCall SetWavesOne | 14,563.8 | 14,595.5 | +0.2% | 0.1456 -> 0.1460 |
| TwoCall SetStaggeredMixed | 127,176.9 | 122,693.3 | -3.5% | 1.2718 -> 1.2269 |
| Fused LaneUniform | 10,971.6 | 10,935.7 | -0.3% | 0.1097 -> 0.1094 |
| Fused LaneUniformBackward | 11,062.1 | 11,011.9 | -0.5% | 0.1106 -> 0.1101 |
| Fused LaneWaves | 12,359.4 | 12,043.0 | -2.6% | 0.1236 -> 0.1204 |
| Fused LaneStaggered | 15,857.4 | 15,876.5 | +0.1% | 0.1586 -> 0.1588 |
| Fused SetWavesOne | 15,006.5 | 14,962.3 | -0.3% | 0.1501 -> 0.1496 |
| Fused SetStaggeredMixed | 137,186.0 | 133,476.3 | -2.7% | 1.3719 -> 1.3348 |

Warm playback reads pre-folded lane tables that are byte-identical between layouts (parity-proven), so the lane shapes are neutral as expected (within +/-0.5% except waves at -1..-3%). The two timeline-set mixed shapes — the largest working sets in the suite — improve 2.7-3.5%. No shape regressed. MemoryDiagnoser allocation is identical base vs candidate (0-1 B/op, the diagnoser floor).
