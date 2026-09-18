# r2-prefetch-deadend: distance-8 record prefetch in the mixed walk

Atom 3 candidate of the `perf/167-packed-runs` workstream (issue #167), tried
on top of efad94d. `FastMixedForward` / `FastMixedBackward` gained a
software prefetch of the record for row i+8:
`Sse.Prefetch0(slots[ids[i+8]].ForwardRecords + positions[i+8])` issued from a
non-inlined helper (ARM-safe per-method JIT gating), plus the three address
loads and one predictable branch per row.

Functionally correct (`--parity` PASS, 18/18 pair-lane unit receipts) and
catastrophically counterproductive. Medians, us per 100k-row pass, raw JSON
in `results/`, console in `console.log`:

| Shape | efad94d Default | prefetch Default | efad94d InProcess | prefetch InProcess |
|---|---:|---:|---:|---:|
| PairAlternating8 | 79.20 | 170.11 | 79.44 | 171.31 |
| PairAlternating8Waves | 64.91 | 167.78 | 61.29 | 169.90 |

Both alternating shapes ~2.1-2.8x slower on both jobs; the address-computation
loads and the call compete with a walk that is limited by its dependent
slot-to-record load chain, consistent with the recorded unchecked-loop dead
end. Reverted; distance variants not retried. The alternating shapes stand at
their structural floor.
