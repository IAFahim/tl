# LaneCeiling

Verdict record for the external "Performance Ceilings & Physical Limits" proposal measured against the shipped paths ([issue #222](https://github.com/IAFahim/tl/issues/222)). The kernels under test in this directory are candidates, not shipped code; the wins that survived parity and timing shipped through PR #235 into `src/Tl.Core` and `tools/Tl.Gen.Tlb`.

## Method

- Reference host: i9-14900K (Raptor Lake — AVX2, no AVX-512), Linux x64, .NET 10. Receipts recorded 2026-09-19; raw outputs retained under `results/` (AVX2 on, AVX2 off, all hardware intrinsics off, plus `probe-r1`/`probe-r2` repeats).
- Single pinned P-core (`--core`, default 2, via `ProcessorAffinity`), best-of `--rounds` x `--reps` (default 3x15).
- Parity runs first; timing refuses to start on any failure. Parity compares every arm against the reference scalar walk and requires bit-identical `ushort` positions and IEEE-754 bit-identical effect columns across arms x {looping, finite} x {forward, backward} x durations {1, 2, 4, 6, 8, 15, 16, 17, 31, 32, 33, 255, 1024, 65500} (a superset of the issue-mandated {6, 16, 1024}) x position shapes {uniform, staggered, waves, edges (0, 1, d-1, d, d+1, 65535 — at/above duration, i.e. clamped and above-duration rows), random, tail (37 rows)} on 4199-row adversarial columns. Gather indices are clamped before use.
- Receipt at record time: 1698/1698 cases bit-identical on the 6-arm harness. Re-verified 2026-09-20 on the current 7-arm harness (adds `permute8x128`): 1818/1818.
- Every warm arm allocates 0 B (`Allocated` column in the retained JSONs).

## Verdicts

ns/row from `results/lane-ceiling-avx2.json`, `loop6/fwd` staggered unless noted.

| # | Proposal claim | Verdict | Evidence |
|---|---|---|---|
| 1 | AVX-512 dual-register tiling, 32 rows/iteration | Rejected on this host | `wide512` (`Vector512`) lowers to 2x ymm without AVX-512: 0.171 vs shipped `gather256` 0.157. Gather latency, not vector width, bounds the scattered path; the proposal's ~0.5 cy/row omits `vpgatherdd` cost (~10-40 cy per vector). A native AVX-512 verdict needs AVX-512 hardware — open, owner decision. |
| 2 | Dual-256 ILP | Rejected | `dual256` 0.155 vs 0.157 at the reference shape; a wash on looping shapes, leaving a small finite/backward edge (~3-4%, e.g. 0.1855 vs 0.1944 uniform) on the table. |
| 3 | Register-resident permute lookup, d <= 8 | **Accepted** | `permute8` 0.119 vs 0.157 gather. Shipped as the d<=8 kernel behind `Timeline<TTrack,TClip>.Advance` (PR #235); recorded shipped-path receipt: 0.172 -> 0.125 ns/row (-27%) on d=6 looping. |
| 4 | Register-resident permute lookup, d <= 32 | Rejected | `permute32` 0.226 — slower than the gather at every tested shape and duration. |
| 5 | 128-bit permute variant (post-probe arm, #244) | Rejected | `permute8x128` 0.392. |
| 6 | Branchless masked gather replacing the shipped gather | Already shipped | The shipped path already runs an AVX2 gather with adaptive run detection. PR #235 additionally ungated finite (non-looping) lanes onto it: record 0.455 -> gather 0.151-0.156 kernel-level at d=1024. |
| 7 | Approximate fast float parsing | Rejected on correctness | Shipped `FastNumber` is exact (`mantissa x 5^k x 2^k`); approximation is a regression, so it was never timed. |
| 8 | Parallel SIMD structural bake partitioning | Accepted, narrower form | Whole-document SIMD re-partition rejected on determinism grounds (strict ordered grammar, deterministic slot order), as #222 predicted. Shipped form: the tracks array is partitioned across dedicated threads into isolated fragment docs and merged in document order; parse 41.8 -> 18.9 ms, bake total ~55 -> ~32 ms on the 20.28 MB corpus at degree 16; SHA-256 byte-identical to serial, including a members-after-tracks reorder and malformed mid-slice fallback. |
| 9 | Threadpool parallelism (`Parallel.For`) | Rejected | Cold-pool ramp measured ~650 ms wall for the same work the serial parse did in ~42 ms. Dedicated threads win. Affinity-masked processes gate to degree 1 (pure serial) because `Environment.ProcessorCount` ignores in-process `ProcessorAffinity`; the affinity mask is read explicitly. |
| 10 | Hardware-hash pool dedup and direct-memory TLB construction | Already shipped | Hash-keyed dedup and raw-pointer clip-pool writes predate the proposal. The proposal's 120-200 ms ingest baseline was stale: the shipped pipeline baked the 19.34-20.28 MB corpus in 55.2 ms and loaded it in 1.6 ms before this probe. |

## Timing table

From `results/lane-ceiling-avx2.json` (1M rows, pinned core 2, best-of 3x15, 0 B allocated everywhere).

Staggered positions:

| lane | record | gather256 | dual256 | wide512 | permute8 | permute8x128 | permute32 | streamadd-floor |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| loop6/fwd | 0.454 | 0.157 | 0.155 | 0.171 | 0.119 | 0.392 | 0.226 | 5.236 |
| loop16/fwd | 0.456 | 0.157 | 0.155 | 0.171 | - | - | 0.227 | 0.289 |
| loop32/fwd | 0.458 | 0.156 | 0.155 | 0.170 | - | - | 0.226 | 0.291 |
| loop1024/fwd | 0.455 | 0.158 | 0.158 | 0.168 | - | - | - | 0.330 |
| loop1024/bwd | 0.573 | 0.169 | 0.171 | 0.191 | - | - | - | 0.332 |
| finite1024/fwd | 0.455 | 0.151 | 0.151 | 0.153 | - | - | - | 0.328 |
| finite1024/bwd | 0.580 | 0.188 | 0.182 | 0.191 | - | - | - | 0.332 |

Uniform positions (from the same JSON; `waves100` and the shipped public-path rows are retained there too):

| lane | record | gather256 | dual256 | wide512 | permute8 | permute8x128 | permute32 | streamadd-floor |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| loop6/fwd | 0.454 | 0.166 | 0.164 | 0.173 | 0.119 | 0.392 | 0.226 | 0.292 |
| loop16/fwd | 0.454 | 0.167 | 0.164 | 0.171 | - | - | 0.229 | 0.293 |
| loop32/fwd | 0.454 | 0.166 | 0.164 | 0.173 | - | - | 0.226 | 0.294 |
| loop1024/fwd | 0.454 | 0.167 | 0.164 | 0.172 | - | - | - | 0.331 |
| loop1024/bwd | 0.575 | 0.174 | 0.175 | 0.194 | - | - | - | 0.335 |
| finite1024/fwd | 0.455 | 0.162 | 0.158 | 0.163 | - | - | - | 0.332 |
| finite1024/bwd | 0.574 | 0.194 | 0.185 | 0.193 | - | - | - | 0.330 |

Shipped public-path rows from the same run (`Timeline<TTrack,TClip>.Apply+Step`): loop6 0.146-0.155, loop1024 0.153-0.192, finite1024 0.156-0.188 ns/row; `Timeline.Advance(index)` 0.035-0.038, `Timeline.Advance(ids)` 0.132-0.143.

## Bake stage profile

20.28 MB corpus via `tools/Tl.Bake.Bench` (baseline -> post-PR #235, degree 16):

| stage | baseline ms | after ms |
|---|---:|---:|
| read | 2.7 | 2.7 |
| scan | 2.5 | 2.5 |
| parse | 41.8 | 18.9 |
| emit | 11.8 | 11.8 |
| total (wall) | ~55 | ~32 |

Parse was ~75% of bake, which is why track partitioning was the only bake-side atom worth taking; the stage profile ruled out spending on read/scan/emit. Load stays 1.6 ms. The canonical Numbers receipt is unchanged by the accepted atoms: checksum 2103072035, 0 B warm allocation, rewind gate pass. Single-core and affinity-pinned processes run degree 1 (pure serial, ~55 ms) by design.

## Dead ends

Recorded with numbers so they are not retried blindly:

- `Parallel.For` on a cold threadpool: ~650 ms wall vs ~42 ms serial parse for the same work (~16x worse); dedicated threads avoid the ramp.
- `Vector512` as an AVX-512 stand-in on this host: 0.168-0.175 on forward loop rows, 0.191-0.196 backward (finite forward fast-path rows 0.153-0.163) — neutral-to-worse versus the shipped gather (0.151-0.195 across shapes); 2x ymm lowering adds width without adding gather throughput.
- `dual256` ILP: 0.151-0.187 across shapes in `results/lane-ceiling-avx2.json` — a wash against `gather256` on looping shapes; the small consistent finite/backward edge (~3-4%, e.g. 0.1855 vs 0.1944 uniform) was left on the table.
- `permute32`: 0.226-0.229 — the 32-entry cross-lane permute costs more than the gather it replaces.
- `permute8x128`: 0.392 — the 128-bit split does two shuffles plus widening per 8 rows and loses to both the 256-bit permute and the gather.
- `SimdCursor.After` is strictly-after; adjacent `]}`/`[{` pairs need the inclusive `From` entry point.
- `Environment.GetEnvironmentVariable` inside a per-bit loop cost ~500 ms and masqueraded as a partition regression.
- Approximate float parsing: correctness regression by construction against exact `FastNumber`; never timed.

## Boundaries

- This directory is outside the `benchmarks/source_budget.py` scopes (`src`, `samples`, `shipped-tools`); it costs 0 budget bytes.
- Shipped code changes proven here landed via PR #235 (`src/Tl.Core/Lane.cs`, `TimelinePair`/`TimelineSet` kernel dispatch, `tools/Tl.Gen.Tlb` track partition). This directory owns only the harness, the retained receipts, and this record.
- The record-walk double-gather cell is owned by #295 and was not probed here.

## Reproduce

```sh
dotnet run --project benchmarks/LaneCeiling -c Release -- --parity
dotnet run --project benchmarks/LaneCeiling -c Release -- --core 2 --out benchmarks/LaneCeiling/results/lane-ceiling-avx2.json
```
