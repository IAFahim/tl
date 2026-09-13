# Data-authored fifth reduction pass (R3, fixed-intercept reduction)

This evidence, recorded on 2026-09-12, measures the fifth performance-reduction atom (R3, "fixed-intercept reduction") for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `41f0ba0` (the R2.1 atom checkpoint). Both atoms ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

One file in `src/`: `src/Tl.Core/Data.cs` implements the four pre-approved fixed-overhead reduction directions:
1. **Compacted refresh map:** `PairCache` gains `fixed byte RefreshSlot[256], RefreshCol[256]`, `int RefreshCount`, and `ulong BoundMask`. `PairTable.Bind` appends `(windowSlot, column)` once per bound consumer slot (deduplicated by `BoundMask`), completely replacing the flat `BoundSlots` loop with a branchless `for i < RefreshCount: table[rSlots[i]] = bases[rCols[i]]` loop.
2. **Scalar single-step `Tick` specialization:** `|delta| == 1` executes through a dedicated path eliminating `while (moved && remaining-- != 0)` scaffolding, `moved` flag management, and tick arithmetic.
3. **Few-stage `StageOf` linear scan:** `StageOf` checks `count <= 4` with linear early-exit and hoisted stage count; binary search is used only for `count > 4`.
4. **Micro-hoists:** `_rows.Length` and `_cache.Asset` are hoisted before row loops; single-element uniform asset check when `rowCount == 1`.

**Unmanaged-runtime law.** Interior pointers derived in `Tick` remain strictly transient and GC-safe (passing `CompactingGcBetweenTicksKeepsDispatchWritingCurrentColumns`). 0 B allocated on all paths.

## Gates (all green)

- `python3 benchmarks/source_budget.py` (255,614/300,000; net +1,813 B over the R2.1 baseline of 253,801; limit 2,000 B)
- `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK)
- `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings, 0 errors)
- `dotnet test tl.slnx -c Release -p:NuGetAudit=false -m:1` (243 passed, 0 failed, including `CompactingGcBetweenTicksKeepsDispatchWritingCurrentColumns`)
- `tests/Tl.Alpha` default, `--capacity`, `--module-capacity` (all 11 data-authored receipts pass, facade allocation 131,072 warm ticks 0 B)
- `samples/Mixed` (exit 0)
- `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all five shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape, mixed-asset arm verified)
- `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and the published binary (all receipts pass under NativeAOT)

## Timing result

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/); [run2](data-authored-jit-run2/); [run3](data-authored-jit-run3/)). High-priority setup was denied by the host; every raw log records the failure. [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.390 / 1.408 / 1.397 | 1.867 / 1.880 / 1.891 | 13.448 / 13.416 / 13.532 |
| 1 track | Alternating | 1.335 / 1.334 / 1.323 | 2.055 / 2.093 / 2.077 | 13.826 / 13.931 / 14.032 |
| Two-clip blend | Forward | 1.702 / 1.700 / 1.699 | 2.547 / 2.552 / 2.605 | 15.126 / 14.038 / 14.328 |
| Two-clip blend | Alternating | 1.892 / 1.897 / 1.896 | 2.858 / 2.805 / 2.905 | 14.628 / 14.412 / 14.384 |
| 3 tracks, A-B-A | Forward | 2.269 / 2.282 / 2.268 | 3.871 / 3.854 / 3.897 | 22.357 / 22.529 / 22.486 |
| 3 tracks, A-B-A | Alternating | 4.132 / 4.149 / 4.161 | 3.976 / 3.932 / 3.949 | 22.910 / 22.938 / 22.977 |
| 16 tracks | Forward | 11.965 / 11.961 / 12.006 | 22.179 / 22.141 / 22.213 | 76.700 / 76.811 / 77.496 |
| 16 tracks | Alternating | 18.529 / 18.623 / 18.471 | 20.981 / 20.830 / 20.520 | 78.900 / 79.225 / 79.937 |
| 256 tracks | Forward | 336.343 / 336.458 / 335.875 | 33377.358 / 33264.804 / 33474.806 | 1096.705 / 1095.868 / 1115.035 |
| 256 tracks | Alternating | 341.831 / 340.817 / 339.813 | 34395.113 / 34499.697 / 34003.885 | 1122.542 / 1125.016 / 1124.533 |

Direct and generated arms remain stable within noise. All 90 arms allocate 0 B.

## Acceptance verdicts (against NEXT-TASK.md criteria)

| Criterion | Target (worst-of-3 medians) | Observed (worst of 3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| OneTrack worst-of-3 | <= 13.3 ns | 14.032 ns (Run 3 Alternating; Forward worst: 13.532 ns) | MISSED (+0.73 ns over target) |
| Blend worst-of-3 | <= 13.9 ns | 15.126 ns (Run 1 Forward; Alternating worst: 14.628 ns) | MISSED (+1.23 ns over target) |
| ThreeTracks worst-of-3 | <= 23.0 ns | 22.977 ns (Run 3 Alternating; Forward worst: 22.529 ns) | PASS (-0.02 ns under target) |
| 16-track worst-of-3 | <= 84.0 ns | 79.937 ns (Run 3 Alternating; Forward worst: 77.496 ns) | PASS (-4.06 ns under target) |
| 256-track worst-of-3 | <= 1,180 ns | 1,125.016 ns (Run 2 Alternating; Forward worst: 1,115.035 ns) | PASS (-54.98 ns under target) |
| Compacting GC Safety | GcBetweenTicks test green | Passed (243/243 total tests passed) | PASS |
| Warm allocation | 0 B on every arm | 0 B across all 90 benchmark arms and verify receipts | PASS |
| Three-way receipt identity | Direct == Generated == Facade | Exact identity across all 5 shapes and both patterns in --verify | PASS |
| Byte budget delta | <= 2,000 B | +1,813 B (255,614 / 300,000 total) | PASS |

## Decomposition and Cost Breakdown

1. **Fixed intercept dropped by ~2.8–3.6 ns vs R4:**
   - Linear regression on track counts (1, 3, 16, 256 steps):
     - R4 (R2.1) intercept: ~11.9–12.4 ns (Forward), ~13.1 ns (Alternating).
     - R5 (R3) intercept: ~9.05–9.30 ns (Forward), ~9.57–9.95 ns (Alternating).
   - The compacted refresh map and scalar specialization successfully reduced the fixed intercept by ~2.8–3.6 ns.
2. **Per-step dispatch slope held rock steady:**
   - R4 (R2.1) slope: ~4.29 ns/step (Forward), ~4.35–4.39 ns/step (Alternating).
   - R5 (R3) slope: ~4.24–4.32 ns/step (Forward), ~4.35–4.36 ns/step (Alternating).
3. **Small shape recovery:**
   - ThreeTracks fully recovered to the R1.1 legal baseline: 22.53 ns / 22.98 ns (down -5.6 ns vs R4, meeting the <= 23.0 ns gate).
   - OneTrack recovered -2.08 to -3.36 ns vs R4 (now 13.53 ns Forward, 14.03 ns Alternating), within 0.23–0.73 ns of the 13.3 ns target.
   - Blend recovered -1.25 to -1.82 ns vs R4 (now 14.04–15.13 ns Forward, 14.38–14.63 ns Alternating), within 0.7–1.2 ns of the 13.9 ns target.
   - Large shapes (16-track and 256-track) improved even further: 16-track worst is 79.94 ns (vs 81.30 ns in R4 and 84.67 ns in R2), 256-track worst is 1,125.02 ns (vs 1,137.78 ns in R4 and 1,242.16 ns in R2).

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r5/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
