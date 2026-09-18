# 180-atom1: index-model rerun of the pair-handle envelopes

One-variable receipt for issue #180 atom 1 (`feat/180-bake-seam`): `TimelineAsset.Load`
returns interned `ushort` timeline indices, the pair bank folds `(index, pair)` lazily on
first typed use, and the crowd kernels read indices directly. Kernels are unchanged.

Baseline: `origin/main` @ 8731eb7 rebuilt and rerun on the same machine on the same day
(i9-14900K, .NET 10.0.12), alongside the stored #167/#174 receipts. `--parity` runs before
the benchmarks: PAIR-HANDLES PARITY PASS, 8192 rows x 240 steps x 8 variants, forward and
backward, bit-exact.

| Shape | #174 receipt | main rerun | 180-atom1 (Default) | 180-atom1 (InProcess) | main rerun (InProcess) |
|---|---:|---:|---:|---:|---:|
| LaneUniform | 18.18 us | 18.46 us | 18.39 us | 18.59 us | 18.31 us |
| PairOne | 20.61 us | 18.29 us | 18.47 us | 18.17 us | 18.18 us |
| PairRuns8 | 31.50 us | 31.72 us | 31.54 us | 31.58 us | 31.00 us |
| PairAlternating8 | 78.80 us | 81.35 us | 86.99 us | 84.07 us | 83.10 us |
| PairRuns8Waves | 16.32 us | 17.13 us | 17.88 us | 17.75 us | 16.51 us |
| PairBlocks8Waves | 18.84 us | 20.24 us | 20.98 us | 20.44 us | 20.28 us |
| PairAlternating8Waves | 63.61 us | 68.29 us | 68.31 us | 64.50 us | 64.90 us |

Verdict: envelopes hold; the id indirection costs nothing measurable (worst shape
PairAlternating8 is within run-to-run noise on the InProcess leg). Allocated: 0 B on every
warm shape.

Note: an intermediate state of this branch (checkpoint B, 70bf881) regressed the mixed
shapes +40..190% because a capacity jump on an empty bank left the skipped slot region
uninitialized; the garbage read as already-folded slots, one hole stayed pending forever,
and every mixed chunk paid the pending scan (and could crash). The final code clears the
new slot region on every growth; that fix is part of this receipt's build.
