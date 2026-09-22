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

Reading: `PairOne` sits exactly on the `TimelineSet` uniform path and pays only the handle-column read over gold. The grouped and alternating shapes pay the mixed-chunk boundary and singleton costs; the guard-free mixed singleton walk (`FastMixedChunk` probe + duration-gated kernels, #167) cuts the alternating singleton row cost to a record load, a slot-record chain, and the two column writes. Run-heavy mixed chunks (`PairBlocks8Waves`) were already within 0.03 ns/row of their uniform twin, which bounds the recoverable cost of packed run detection at well under its scratch-pass price. Every default-job row allocates 0 B; the single 1 B on the InProcess alternating shapes is the same emitted-toolchain harness artifact recorded for #148. Raw BDN output: `results/validated/`, `results/rung0-baseline-anchor/`, `results/rung1-uniform-chunk*/`, `results/rung2-shapes-baseline/`, `results/rung2-mixed-fast*/`; `results/rung1-alternating8waves-baseline/` holds the pre-split waves baseline (its internal `PairRuns8Waves` label predates the shape split and measured alternating handles, now `PairAlternating8Waves`), and `results/rung2-mixed-unchecked/` records the discarded unchecked-loop variant. `results/168-slot-row-24/` is the #168 24 B slot row A/B on this lane (README inside). `results/174-one-api/` re-runs every shape on the #174 one-API surface (gold arm through `Slot` + the public bank; `Bind` removed): all medians within the #167 envelopes — gold 18.17 us (0.182 ns/row), PairOne InProcess 18.04 us, PairRuns8 31.50 us, PairAlternating8 79.17-79.92 us, waves 16.09/18.64/63.99 us; 0 B on every default-job row.

## Why handles are ushort

Handles are dense per-pair indices assigned by `Timeline<TTrack, TClip>.Slot` at load time (owner directive: never pointers, never authored, never baked). The per-pair bank holds at most 65,536 timelines, matching the format's own slot-index width; the warm loop resolves one native slot per row and the consumer stays statically known per closed generic.

## Issue #351 PMU-floor receipts (this machine, 2026-09-23, P-core pinned)

`--counters <shape>` runs the exact default-job op per shape in a fixed window under `perf stat -D` (raw outputs under `results/351-atom1-counters/` and `results/351-atomA-runheavy-route/`); `results/351-baseline/` plus the interleaved pinned BDN runs under `results/351-atomA-runheavy-route/bdn-*/` hold the BenchmarkDotNet receipts. The #174-era wall numbers above do not reproduce on the current platform state (identical sources from 2026-09-21 measure 1.8-2.5x slower on bulk vector shapes while the scalar per-entity bands reproduce exactly), so the counters below are the baseline of record. Per-shape counters (median of 3, 2e9 rows per run): branch misses 0.0006-0.0014/row and LLC references <=0.0004/row on every shape — the alternating residual is gather throughput and dependent-load latency (IPC 3.3), reconfirming the #222 LaneCeiling verdict; slot-record resolution is already one dense indexed load into 8 B arena records, and the dense per-position effect tables the issue suspected are the shipped fold-time layout (`slot->Forward/Backward`). Fused single-walk probes (`--counters fused-<shape>`, the `Apply(indices, positions, next, forward, effects)` overload) bound the two-call pattern: fused-uniform 0.205 ns/row vs two-call 0.328 (+60%), fused-alternating 0.447 vs 0.675 (+51%) — the second pass's re-streamed columns are inherent to the `Apply` + `Advance` contract.

Atom A (#351) routes run-heavy mixed chunks back to the run-aware cascade: `FastMixedChunk` returns false when the chunk head holds 12+ consecutive equal (handle, position) pairs, so constant-clock crowds fall to the segment cascade instead of the gather kernel while alternating crowds keep the guard-free walk. Interleaved pinned A/B (order-balanced BDN rounds plus 3+4 counter reps): `PairBlocks8Waves` 47.9 -> 32.9 us InProcess (-31%; counters 2.77 -> 1.81 cyc/row, IPC 3.69 -> 5.24), `PairRuns8Waves` 33.2 -> 27.5 us (-17%), alternating shapes unchanged, uniform shapes -3.5 to -3.8% from the shifted `TimelineSet` code layout. `MinSegment` 128 -> 64 was measured and rejected on this shape (366 vs 341 ns per 1000 rows: segment probes lose to `ArenaMixed` on 100-row runs). Parity PASS, suite 1,998/0, checked 1,107/0, `--capacity` checksum unchanged, budget 299,290/300,000.

## Retained issue receipts

- `results/180-atom1/` — issue #180 atom 1.
- `results/193-*/` — issue #193 atoms and rechecks.
- `results/r2-anchor/`, `results/r2-packed-recheck/`, `results/r2-packed-runendtwo/`, `results/r2-prefetch-deadend/`, `results/r2-pristine-recheck/`, `results/r2-runsplit/`, `results/r2-runsplit-rebase/` — issue #167 packed-runs ladder and dead ends.
- `results/validated-console.log` — console capture of the cited `results/validated/` run.
