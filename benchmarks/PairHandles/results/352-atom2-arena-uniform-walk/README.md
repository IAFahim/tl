# PairHandles A/B — ArenaUniformWalk direction-twin collapse (issue #352 atom 2)

Candidate `refactor/352-dedup-sweep` (ArenaUniformWalk instantiated over ForwardRows/BackwardRows, forward and backward row loops merged into one direction-generic body) vs base `5407c99` (atom-1 head, hand-copied forward/backward loops), same machine, interleaved base/candidate runs (B,A,B,A,B,A,B,A), four rounds each, 2026-09-22. Median of per-run medians; 100,000 rows per op. Raw per-run data in `*-r?/results/*-report-full.json`, console capture per run. `--parity` passes on both builds before timing.

This session's ambient noise is wider than atom 1's window: same-binary run spread reaches 8% (cand-1 vs cand-2 on `PairAlternating8Waves`). Every median delta below sits inside that same-binary spread, and the oracle shapes are pinned.

| Shape | base med (us) | cand med (us) | delta |
|---|---:|---:|---:|
| LaneUniform | 32,353.8 | 31,553.0 | -2.47% |
| PairOne | 29,685.9 | 29,166.2 | -1.75% |
| PairRuns8 | 37,384.8 | 36,258.0 | -3.01% |
| PairAlternating8 | 66,179.5 | 65,883.7 | -0.45% |
| PairRuns8Waves | 33,192.8 | 32,567.0 | -1.89% |
| PairBlocks8Waves | 47,060.7 | 46,885.8 | -0.37% |
| PairAlternating8Waves | 58,469.1 | 60,019.5 | +2.65% |

Direct scalar oracle, `benchmarks/Numbers --steady --rows 100000 --rounds 3 --reps 5 --core 2`, four interleaved runs per side (ns/entity, median of 4): `per-entity-apply-step` 1.95 -> 1.95, `per-entity-fused` 1.23/1.24 -> 1.23/1.24, `per-entity-lane` 1.23 -> 1.23, `sparse-separate-apply-step` 2.30-2.32 -> 2.30-2.31, `sparse-batch-apply-step` 2.05-2.09 -> 2.06-2.07 (+0.01, inside the +/-0.05 gate), `sparse-batch-dense-list` 1.32-1.33 -> 1.33, all remaining shapes unchanged within 0.01. Full one-run capture: `numbers-oracle-{base,cand}-r1.log`.

Warm playback allocates 0 B on both sides.
