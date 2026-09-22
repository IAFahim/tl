# PairHandles A/B — ApplyRows/AdvanceRows direction-twin collapse (issue #352 atom 1)

Candidate `refactor/352-dedup-sweep` (ApplyRowsCore/AdvanceRowsCore instantiated over nested ForwardRows/BackwardRows direction structs) vs base `7775bef` (ApplyRowsForward/Backward + AdvanceRowsForward/Backward), same machine, interleaved base/candidate runs (B,A,B,A,B,A,B,A), four rounds each, 2026-09-22. Median of per-run medians; 100,000 rows per op. Raw per-run data in `*-r?/results/*-report-full.json`, console capture per run in `console.log`. `--parity` passes on both builds before timing.

The collapsed cores are the `rows` overloads (`Timeline<,>.Apply/Advance(rows, ids, ...)`, TimelinePair.cs 492-516). The PairHandles shapes drive the non-rows bank walks, so they receipt the neighboring machinery: every shape's median moved within run-to-run noise (|delta| <= 0.63%, <= 0.002 ns/row).

| Shape | base med (us) | cand med (us) | delta |
|---|---:|---:|---:|
| LaneUniform | 31,424.0 | 31,433.7 | +0.03% |
| PairOne | 29,581.5 | 29,011.9 | -1.93% |
| PairRuns8 | 36,236.6 | 36,240.9 | +0.01% |
| PairAlternating8 | 65,755.4 | 65,832.9 | +0.12% |
| PairRuns8Waves | 32,323.2 | 32,525.6 | +0.63% |
| PairBlocks8Waves | 46,893.4 | 46,786.4 | -0.23% |
| PairAlternating8Waves | 61,512.9 | 61,625.8 | +0.18% |

The direct scalar A/B for the collapsed family is `benchmarks/Numbers --steady --rows 100000 --rounds 3 --reps 5 --core 2` (four interleaved runs per side; per-shape medians, ns/entity): `sparse-batch-apply-step` 2.05 -> 2.06 and `sparse-batch-dense-list` 1.31 -> 1.33 (rows-overload Apply + Advance), `per-entity-apply-step` 1.95 -> 1.95, `per-entity-fused` 1.24 -> 1.24, `per-entity-lane` 1.23 -> 1.23, `sparse-separate-apply-step` 2.31 -> 2.31, `sparse-list-crowd-apply-step` 0.33 -> 0.34, `shared-clock-crowd` 0.05 -> 0.05, `per-entity-record-floor` 0.68 -> 0.68. Every shape moved <= 0.02 ns/entity, inside the +/-0.05 noise gate. Full one-run capture: `numbers-oracle-{base,cand}-r1.log`; the remaining seven runs differ only in `/tmp` scratch and are summarized here.

Warm playback allocates 0 B on both sides (MemoryDiagnoser flat, diagnoser-floor bytes only).
