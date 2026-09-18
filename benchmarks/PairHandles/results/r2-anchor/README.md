# r2-anchor: second-workstream anchor on 1.0.0-alpha.9 main

Same-machine baseline for the `perf/167-packed-runs` workstream of issue #167
(uniform-prefix run split, packed run detection, mixed-walk prefetch). Base =
origin/main @ 50d5c39 (alpha.9), fresh Release build on i9-14900K, .NET 10,
BDN 0.15.x, 100k rows, medians over 16 iterations of 200 ms after 8 warmups.
`--parity` ran before the suite: PAIR-HANDLES PARITY PASS (8192 rows x 240
steps x 8 variants, forward and backward, bit-exact).

Medians in us per 100k-row pass (ns/row = us value / 100):

| Shape | Default | InProcess | Allocated |
|---|---:|---:|---:|
| LaneUniform (gold) | 18.24 | 18.33 | 0 B / 0 B |
| PairOne | 18.77 | 18.14 | 0 B / 0 B |
| PairRuns8 | 31.64 | 31.63 | 0 B / 0 B |
| PairAlternating8 | 85.96 | 80.51 | 0 B / 1 B |
| PairRuns8Waves | 17.14 | 17.20 | 0 B / 0 B |
| PairBlocks8Waves | 20.58 | 19.76 | 0 B / 0 B |
| PairAlternating8Waves | 68.14 | 63.40 | 0 B / 0 B |

All shapes sit inside their committed envelopes from rung1/rung2 sessions. In
this session gold (18.24/18.33) and PairOne (18.77/18.14) overlap on the
InProcess job; the recorded "structural" PairOne gap is not reproduced here.

Harness note: `Program.cs` in this workstream gains a `--filter <arg>`
passthrough for filtered A/B runs; default remains the full suite. No
production change.
