# PairHandles

Receipts for issues #160/#167: the pair-typed many-timeline lane `Timeline<TTrack, TClip>.Seek(handles, positions, forward).Apply(effects)` over per-row `ushort` handles, against the single-bound `BakedLane` gold path.

## Parity

`dotnet run -c Release -- --parity` advances 8,192 rows x 240 steps over 8 designer-variant timelines (different track scales, clip splits, all looping, duration 1024), forward and backward, and compares positions and effect folds bit-exactly against per-variant uniform `TimelineSet` lanes: `PAIR-HANDLES PARITY PASS`. The mixed singleton kernel runs this whole session through the guard-free walk (equal durations keep every position below the set's minimum duration).

The same guarantee is unit-receipted in `tests/Tl.Core.Tests/PairHandleLaneTests.cs` (varied handles vs per-asset lanes, positions below every bound duration, block runs of equal (handle, position), per-row looping durations, both `Bind` overloads, guards, 0 B warm).

## Throughput (this machine, 100,000 rows, InProcess + default job; table shows default-job medians with InProcess alongside)

| Shape | #160 validated | #167 rung 1 | #167 rung 2/3 | vs gold |
| --- | --- | --- | --- | --- |
| `LaneUniform` — one `BakedLane`, staggered positions | 15.9 us (0.159) | 15.8-16.0 | 15.8-18.2 / 16.1-16.4 | 1.00x |
| `PairOne` — pair-typed, one handle for the batch | 20.3 us (0.203) | 18.2-18.9 | 18.2-19.4 / 18.0-18.7 | ~1.13x |
| `PairRuns8` — eight variants, rows grouped by handle | 57.0 us (0.570) | 55.7-56.1 | 31.7-33.8 / 31.6-32.6 | ~1.98x |
| `PairAlternating8` — eight variants alternating per row | 132.6 us (1.326) | 118.5-119.7 | 83.0-87.9 / 85.3-86.5 | ~5.2x |
| `PairRuns8Waves` — grouped handles, wave positions (uniform run walk) | — | — | 17.4-17.9 / 16.5-18.0 | ~1.07x |
| `PairBlocks8Waves` — handle and position runs of 100 in mixed chunks | — | — | 19.5-20.3 / 19.6-19.8 | ~1.20x |
| `PairAlternating8Waves` — alternating handles, wave positions | — | — | 66.5-69.4 / 66.9-68.1 | ~4.1x |

Reading: `PairOne` sits exactly on the `TimelineSet` uniform path and pays only the handle-column read over gold. The grouped and alternating shapes pay the mixed-chunk boundary and singleton costs; the guard-free mixed singleton walk (`FastMixedChunk` probe + duration-gated kernels, #167) cuts the alternating singleton row cost to a record load, a slot-record chain, and the two column writes. Run-heavy mixed chunks (`PairBlocks8Waves`) were already within 0.03 ns/row of their uniform twin, which bounds the recoverable cost of packed run detection at well under its scratch-pass price. Every default-job row allocates 0 B; the single 1 B on the InProcess alternating shapes is the same emitted-toolchain harness artifact recorded for #148. Raw BDN output: `results/validated/`, `results/rung0-baseline-anchor/`, `results/rung1-uniform-chunk*/`, `results/rung2-shapes-baseline/`, `results/rung2-mixed-fast*/`; `results/rung1-alternating8waves-baseline/` holds the pre-split waves baseline (its internal `PairRuns8Waves` label predates the shape split and measured alternating handles, now `PairAlternating8Waves`), and `results/rung2-mixed-unchecked/` records the discarded unchecked-loop variant.

## Why handles are ushort

Handles are dense per-pair indices assigned by `Timeline<TTrack, TClip>.Bind` at load time (owner directive: never pointers, never authored, never baked). The per-pair bank holds at most 65,536 timelines, matching the format's own slot-index width; the warm loop resolves one native slot per row and the consumer stays statically known per closed generic.
