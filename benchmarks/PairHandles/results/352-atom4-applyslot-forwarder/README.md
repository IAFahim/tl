# PairHandles A/B — ApplySlot 1-span overload forwarding (issue #352 atom 4)

Candidate `refactor/352-dedup-sweep` (ApplySlot(ushort, Span<float>) forwards to ApplySlot(ushort, Span<float>, Span<ushort>) with default next, same default-argument forwarding as the shipped Apply(Span<float>) overload) vs base `1cb790f` (atom-2 head, duplicated 19-line loop body), same machine, interleaved runs (B,A,B,A,B,A,B,A), four rounds each, 2026-09-22. Median of per-run medians; 100,000 rows per op. Raw data in `*-r?/results/*-report-full.json`. `--parity` passes on both builds before timing.

| Shape | base med (us) | cand med (us) | delta |
|---|---:|---:|---:|
| LaneUniform | 32,420.5 | 32,572.5 | +0.47% |
| PairOne | 29,663.1 | 29,711.7 | +0.16% |
| PairRuns8 | 37,400.6 | 37,317.1 | -0.22% |
| PairAlternating8 | 66,270.0 | 65,345.2 | -1.40% |
| PairRuns8Waves | 33,167.4 | 32,758.8 | -1.23% |
| PairBlocks8Waves | 47,166.1 | 47,076.7 | -0.19% |
| PairAlternating8Waves | 58,162.9 | 58,184.4 | +0.04% |

The protected 1-row oracle path flows through the new forwarder (Numbers `per-entity-apply-step`: TimelinePair.Apply(index, positions, forward, effects) -> TimelineSet.ApplySlot -> TimelineSetLane.ApplySlot): 1.95 -> 1.95 ns/entity on all four interleaved pairs; every other steady shape within 0.01 (`numbers-oracle-{base,cand}-r1.log`).
