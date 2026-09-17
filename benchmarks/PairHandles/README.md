# PairHandles

Receipts for issue #160: the pair-typed many-timeline lane `Timeline<TTrack, TClip>.Seek(handles, positions, forward).Apply(effects)` over per-row `ushort` handles, against the single-bound `BakedLane` gold path.

## Parity

`dotnet run -c Release -- --parity` advances 8,192 rows x 240 steps over 8 designer-variant timelines (different track scales, clip splits, all looping, duration 1024), forward and backward, and compares positions and effect folds bit-exactly against per-variant uniform `TimelineSet` lanes: `PAIR-HANDLES PARITY PASS`.

The same guarantee is unit-receipted in `tests/Tl.Core.Tests/PairHandleLaneTests.cs` (varied handles vs per-asset lanes, per-row looping durations, both `Bind` overloads, guards, 0 B warm).

## Throughput (this machine, 100,000 rows, InProcess + default job, 0 B)

| Shape | Median | vs gold |
| --- | --- | --- |
| `LaneUniform` — one `BakedLane`, staggered positions | 15.9 us (0.159 ns/row) | 1.00x |
| `PairOne` — pair-typed, one handle for the batch | 20.3 us (0.203 ns/row) | 1.28x |
| `PairRuns8` — eight variants, rows grouped by handle | 57.0 us (0.570 ns/row) | 3.58x |
| `PairAlternating8` — eight variants alternating per row | 132.6 us (1.326 ns/row) | 8.34x |

Reading: `PairOne` sits exactly on the `TimelineSet` uniform path — the typed façade adds nothing over the set (it is the same slot bank and kernels). The grouped and alternating shapes are the set's known uniform-run and mixed-id costs; alternating per-row distinct assets carries the historical alternating-ids receipt (~1.2-1.7 ns/row in the #104/#113 records). Raw BDN output: `results/validated/`.

## Why handles are ushort

Handles are dense per-pair indices assigned by `Timeline<TTrack, TClip>.Bind` at load time (owner directive: never pointers, never authored, never baked). The per-pair bank holds at most 65,536 timelines, matching the format's own slot-index width; the warm loop resolves one native slot per row and the consumer stays statically known per closed generic.
