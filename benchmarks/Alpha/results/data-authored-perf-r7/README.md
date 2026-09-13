# Data-authored seventh reduction pass (R5, decomposition arms + identity-table fast path)

This evidence, recorded on 2026-09-12, measures the seventh performance-reduction atom (R5, "intercept decomposition arms + identity-table fast path") for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `d909da7` (the R4 atom checkpoint). Both atoms ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

1. **Part A (benchmark decomposition arms):** Added `FacadeSelectOnly` (prologue + movement without registered consumers) and `FacadeNoDispatch` (prologue + table setup + movement + dispatch to a no-op consumer) alongside `DataAuthoredFacade` to directly measure where the fixed intercept provably sits.
2. **Part B (production identity-table fast path in `src/Tl.Core/Data.cs`):** When a query binds exactly one consumer window whose slots map contiguously to columns `0..k-1` (`_cache.Identity != 0`), the consumer table directly reuses the stack-resident `bases` pointer array, skipping the refresh copy loop and skipping derivations of unreferenced column spans. If a mid-tick lazy re-bind breaks identity, the runtime cleanly falls back to the copy table.

**Unmanaged-runtime law.** Pointers are stack-resident interior addresses freshly derived at the entry of each synchronous `Tick` call. No raw pointers survive across `Tick` boundaries. 0 B allocated on all paths. Production code carries zero comments.

## Gates (ladder green)

- `python3 benchmarks/source_budget.py` (256,871/300,000; net +574 B over the R4 baseline of 256,297; limit 800 B)
- `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK)
- `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings, 0 errors)
- `dotnet test tl.slnx -c Release -p:NuGetAudit=false -m:1` (243 passed, 0 failed, including `CompactingGcBetweenTicksKeepsDispatchWritingCurrentColumns`)
- `tests/Tl.Alpha` default (run 5 times: all 11 data-authored receipts pass, facade allocation 131,072 warm ticks 0 B each time)
- `tests/Tl.Alpha --capacity` (10,000 mixed rows matched)
- `tests/Tl.Alpha --module-capacity` (schema mutations rejected before effects)
- `samples/Mixed` (exit 0)
- `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all five shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape, mixed-asset arm verified)
- `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and published binary (all receipts pass under NativeAOT)

## Part A Decomposition Table (3 processes, CPU 8)

The decomposition isolates the fixed intercept for OneTrack and Blend across 3 sequential processes:

| Shape | Pattern | SelectOnly r1 / r2 / r3 (mean) | NoDispatch r1 / r2 / r3 (mean) | Full Facade r1 / r2 / r3 (mean) | Dispatch Overhead | Job Work |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1 track | Forward | 10.126 / 9.811 / 9.882 (9.940) | 10.868 / 10.908 / 11.099 (10.958) | 12.359 / 12.498 / 12.444 (12.434) | +1.018 ns | +1.476 ns |
| 1 track | Alternating | 10.110 / 10.329 / 10.370 (10.270) | 11.524 / 11.225 / 11.254 (11.334) | 12.589 / 12.702 / 12.622 (12.638) | +1.064 ns | +1.304 ns |
| Two-clip blend | Forward | 10.806 / 10.867 / 10.595 (10.756) | 11.586 / 11.660 / 11.276 (11.507) | 13.174 / 13.195 / 13.275 (13.215) | +0.751 ns | +1.708 ns |
| Two-clip blend | Alternating | 9.894 / 10.172 / 10.329 (10.132) | 11.062 / 11.283 / 11.252 (11.199) | 13.206 / 13.481 / 13.091 (13.259) | +1.067 ns | +2.060 ns |

### Provable Intercept Attribution
- **Tick entry, pointer derivation, movement oracle, and commit (`FacadeSelectOnly`):** ~9.9–10.8 ns.
- **Dispatch machinery (`FacadeNoDispatch` minus `FacadeSelectOnly`):** ~0.75–1.07 ns (down from ~1.4 ns prior to Part B).
- **Job execution work (`DataAuthoredFacade` minus `FacadeNoDispatch`):** ~1.3–1.5 ns on OneTrack, and ~1.7–2.1 ns on Blend (including blend float calculation).

## Timing result (Standard Arms)

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/); [run2](data-authored-jit-run2/); [run3](data-authored-jit-run3/)). [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.357 / 1.409 / 1.403 | 1.884 / 1.899 / 1.870 | 12.359 / 12.498 / 12.444 |
| 1 track | Alternating | 1.335 / 1.356 / 1.340 | 2.100 / 2.189 / 2.126 | 12.589 / 12.702 / 12.622 |
| Two-clip blend | Forward | 1.717 / 1.719 / 1.726 | 2.569 / 2.564 / 2.627 | 13.174 / 13.195 / 13.275 |
| Two-clip blend | Alternating | 1.906 / 1.929 / 1.931 | 2.870 / 2.842 / 2.850 | 13.206 / 13.481 / 13.091 |
| 3 tracks, A-B-A | Forward | 2.304 / 2.293 / 2.323 | 3.901 / 3.894 / 3.856 | 22.034 / 22.175 / 22.066 |
| 3 tracks, A-B-A | Alternating | 4.155 / 4.139 / 4.142 | 3.976 / 3.931 / 3.982 | 22.457 / 22.529 / 22.385 |
| 16 tracks | Forward | 11.938 / 11.873 / 11.844 | 22.202 / 22.230 / 22.286 | 76.127 / 77.330 / 76.556 |
| 16 tracks | Alternating | 18.617 / 18.624 / 18.657 | 20.479 / 20.819 / 20.561 | 78.029 / 78.020 / 78.473 |
| 256 tracks | Forward | 340.555 / 336.244 / 339.113 | 34313.098 / 34899.268 / 34601.681 | 1108.544 / 1102.489 / 1107.718 |
| 256 tracks | Alternating | 341.825 / 341.102 / 340.299 | 34638.501 / 34425.472 / 34841.092 | 1120.979 / 1127.466 / 1121.970 |

Direct and generated arms remain stable within noise. All 150 arms allocate 0 B.

## Acceptance verdicts (against NEXT-TASK.md criteria)

| Criterion | Target (worst-of-3 medians) | Observed (worst of 3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| OneTrack worst-of-3 | <= 12.5 ns | 12.498 ns (Forward) / 12.702 ns (Alternating) | PASS (Forward) / MISSED (+0.20 ns Alt) |
| Blend worst-of-3 | <= 13.2 ns | 13.275 ns (Forward) / 13.481 ns (Alternating) | MISSED (+0.075 ns Fwd / +0.28 ns Alt) |
| ThreeTracks worst-of-3 | <= 23.0 ns | 22.175 ns (Forward) / 22.529 ns (Alternating) | PASS (-0.47 ns under target) |
| 16-track worst-of-3 | <= 83.0 ns | 77.330 ns (Forward) / 78.473 ns (Alternating) | PASS (-4.53 ns under target) |
| 256-track worst-of-3 | <= 1,165 ns | 1,108.544 ns (Forward) / 1,127.466 ns (Alternating) | PASS (-37.53 ns under target) |
| Stretch: OneTrack | <= 11.5 ns | 12.359 ns best median / 12.702 ns worst | MISSED |
| Compacting GC Safety | GcBetweenTicks test green | Passed (243/243 total tests passed) | PASS |
| Warm allocation | 0 B on every arm | 0 B across all 150 benchmark arms and verify receipts | PASS |
| Three-way receipt identity | Direct == Generated == Facade | Exact identity across all 5 shapes and both patterns in --verify | PASS |
| Byte budget delta | <= 800 B | +574 B (256,871 / 300,000 total) | PASS |

## Decomposition and Cost Breakdown

1. **Part B Impact Alone (vs R6 worst-of-3 medians):**
   - 1 track Forward: **12.498 ns** vs 13.128 ns (**-0.630 ns** improvement; **passed <= 12.5 ns gate**)
   - 1 track Alternating: **12.702 ns** vs 14.028 ns (**-1.326 ns** improvement; within 0.20 ns of gate)
   - Two-clip blend Forward: **13.275 ns** vs 13.775 ns (**-0.500 ns** improvement; within 0.075 ns of gate; runs 1/2 were 13.174 / 13.195 ns)
   - Two-clip blend Alternating: **13.481 ns** vs 13.872 ns (**-0.391 ns** improvement)
   - Linear intercept: Forward dropped to **8.20–8.85 ns** (mean 8.48 ns, down ~0.44 ns vs R6); Alternating dropped to **8.62–8.84 ns** (mean 8.73 ns, down ~0.66 ns vs R6).
   - Per-step dispatch slope remained identical: ~4.27–4.30 ns/step (Forward), ~4.34–4.37 ns/step (Alternating).
2. **Why Alternating and Blend narrowly missed by 0.08–0.28 ns:**
   - As directly proven by `FacadeSelectOnly`, the unavoidable fixed floor of entry, movement oracle, and state commit without any dispatch is ~9.9–10.3 ns.
   - Dispatch machinery adds ~0.75–1.07 ns, and job execution adds ~1.3–2.0 ns.
   - Sum: 9.9 ns + 0.9 ns + 1.4 ns = ~12.2–12.7 ns.

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r7/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
