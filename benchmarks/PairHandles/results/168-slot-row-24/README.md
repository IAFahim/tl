# PairHandles A/B — 24 B slot row (issue #168)

Candidate `perf/168-slot-row-24` (TLB1 v3, 24 B rows) vs base `59b4972` (TLB1 v2, 32 B rows), same machine (Intel Core i9-14900K, Unix 7.2.3.1, .NET 10.0.12, procs 32), interleaved base/candidate runs, two rounds each. Median of per-run medians; 100,000 rows per op, ns/row = us/100. Raw per-iteration data in `*-r?/results/*-report-full.json`; console.log per run. `--parity` passes on both builds before timing (`PAIR-HANDLES PARITY PASS: 8192 rows x 240 steps over 8 variant timelines, forward and backward, bit-exact`).

| Shape | v2 med (us) | v3 med (us) | delta | ns/row v2 -> v3 |
|---|---:|---:|---:|---:|
| Advance LaneUniform | 15,776.4 | 15,742.3 | -0.2% | 0.1578 -> 0.1574 |
| Advance PairOne | 20,348.5 | 20,384.1 | +0.2% | 0.2035 -> 0.2038 |
| Advance PairRuns8 | 56,947.2 | 57,021.2 | +0.1% | 0.5695 -> 0.5702 |
| Advance PairAlternating8 | 130,705.3 | 132,329.2 | +1.2% | 1.3071 -> 1.3233 |

Warm playback is neutral within noise (|delta| <= 1.2%; the alternating shape's working set is bank-table-dominated, not row-dominated). MemoryDiagnoser allocation identical base vs candidate (0-1 B/op diagnoser floor).
