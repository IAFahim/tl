# Data-authored tenth reduction pass (R8, per-step dispatch compaction + hot-path keys removal)

This evidence, recorded on 2026-09-12, measures the tenth performance-reduction atom (R8, "per-step dispatch compaction + hot-path keys removal") for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `d46541b` (the R7 atom checkpoint). All runs executed under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

1. **Direction 1: Hot-Path Keys Removal:**
   - Hoisted keys construction out of the warm path in generic `TimelineQuery<...>.Tick`.
   - Replaced per-tick `stackalloc ulong[]` and `TypeKey<T>.Value` loads with a static function pointer `delegate*<ulong*, int>` (`&K`).
   - Keys are materialized into stackalloc memory only on cold bind/rebind in `TickGeneral`.
2. **Direction 2: Single-Stage Execute Specialization:**
   - In `TimelineRef.Execute`, hoisted `Header->StageCount == 1 ? (NativeStage*)(_p + Header->StageOffset) : StageOf(tick)`.
   - Bypasses binary search / stage table scanning completely for single-stage assets.
3. **Direction 3: Tightened Step-Pointer Walk:**
   - In `TimelineRef.Execute`, replaced index calculation `reverse ? count - 1 - i : i` and `steps[index]` array dereferencing with pointer walking:
     `var step = reverse ? steps + count - 1 : steps; var stride = reverse ? -1 : 1; while (count-- > 0) { ... step += stride; }`.

**Unmanaged-runtime law.** Pointers are stack-resident interior addresses freshly derived at the entry of each synchronous `Tick` call. No raw pointers survive across `Tick` boundaries. 0 B allocated on all paths. Production code carries zero comments.

## Gates (ladder green)

- `python3 benchmarks/source_budget.py` (261,520/300,000; net +258 B over R7 baseline of 261,262)
- `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK)
- `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings, 0 errors)
- `dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false` (243 passed, 0 failed, including `CompactingGcBetweenTicksKeepsDispatchWritingCurrentColumns` and `PublicApiMatchesApproval`)
- `tests/Tl.Alpha` default (run 5 times: all data-authored receipts pass, facade allocation 131,072 warm ticks 0 B each time)
- `tests/Tl.Alpha --capacity` (10,000 mixed rows matched)
- `tests/Tl.Alpha --module-capacity` (schema mutations rejected before effects)
- `samples/Mixed` (exit 0)
- `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all five shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape, mixed-asset arm verified)
- `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and published binary (all receipts pass under NativeAOT)

## Part A Decomposition Table (3 processes, CPU 8)

| Shape | Pattern | SelectOnly r1 / r2 / r3 (mean) | NoDispatch r1 / r2 / r3 (mean) | Full Facade r1 / r2 / r3 (mean) | Dispatch Overhead | Job Work |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1 track | Forward | 5.024 / 4.965 / 4.957 (4.98 ns) | 6.207 / 6.148 / 6.186 (6.18 ns) | 8.547 / 8.231 / 8.199 (8.33 ns) | +1.20 ns | +2.15 ns |
| 1 track | Alternating | 5.842 / 5.921 / 5.789 (5.85 ns) | 7.074 / 7.152 / 6.974 (7.07 ns) | 9.191 / 9.099 / 9.026 (9.11 ns) | +1.22 ns | +2.04 ns |
| Two-clip blend | Forward | 4.975 / 4.950 / 4.954 (4.96 ns) | 6.172 / 6.264 / 6.133 (6.19 ns) | 9.044 / 8.928 / 8.849 (8.94 ns) | +1.23 ns | +2.75 ns |
| Two-clip blend | Alternating | 6.023 / 6.043 / 6.004 (6.02 ns) | 6.934 / 6.949 / 7.012 (6.96 ns) | 9.989 / 9.885 / 9.849 (9.91 ns) | +0.94 ns | +2.94 ns |

## Timing result (Standard Arms)

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/); [run2](data-authored-jit-run2/); [run3](data-authored-jit-run3/)). [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.408 / 1.404 / 1.402 | 1.960 / 1.943 / 2.053 | 8.547 / 8.243 / 8.229 |
| 1 track | Alternating | 1.352 / 1.346 / 1.337 | 2.154 / 2.137 / 2.070 | 9.184 / 9.125 / 9.037 |
| Two-clip blend | Forward | 1.713 / 1.719 / 1.689 | 2.604 / 2.632 / 2.643 | 9.049 / 8.958 / 8.861 |
| Two-clip blend | Alternating | 1.921 / 1.919 / 1.930 | 2.998 / 2.969 / 2.969 | 10.021 / 9.909 / 9.884 |
| 3 tracks, A-B-A | Forward | 2.291 / 2.304 / 2.274 | 3.864 / 3.759 / 3.707 | 17.818 / 17.624 / 17.585 |
| 3 tracks, A-B-A | Alternating | 4.247 / 4.204 / 4.212 | 4.148 / 4.124 / 4.149 | 18.170 / 18.076 / 17.928 |
| 16 tracks | Forward | 12.226 / 12.156 / 12.062 | 22.601 / 22.270 / 22.325 | 71.449 / 70.112 / 71.169 |
| 16 tracks | Alternating | 18.945 / 18.729 / 18.668 | 20.880 / 20.994 / 20.744 | 71.667 / 71.882 / 71.557 |
| 256 tracks | Forward | 343.074 / 339.859 / 337.767 | 35389.732 / 35066.409 / 35017.502 | 1144.039 / 1088.747 / 1087.244 |
| 256 tracks | Alternating | 348.553 / 344.875 / 343.620 | 35290.767 / 34688.509 / 34692.662 | 1097.709 / 1092.443 / 1083.699 |

Direct and generated arms remain stable within noise. All 150 arms allocate 0 B.

## Acceptance verdicts (against NEXT-TASK.md criteria)

| Criterion | Target (worst-of-3 medians) | Observed (worst of 3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| OneTrack worst-of-3 | <= 9.3 ns | 8.547 ns (Forward) / 9.184 ns (Alternating) | PASS |
| Blend worst-of-3 | <= 10.4 ns | 9.049 ns (Forward) / 10.021 ns (Alternating) | PASS |
| ThreeTracks worst-of-3 | <= 18.3 ns | 17.818 ns (Forward) / 18.170 ns (Alternating) | PASS |
| 16-track worst-of-3 | <= 71.5 ns | 71.449 ns (Forward) / 71.882 ns (Alternating) | RECORDED |
| 256-track forward worst-of-3 | <= 1,130 ns | 1144.039 ns (Forward) | RECORDED |
| 256-track alternating worst-of-3 | <= 1,150 ns | 1097.709 ns (Alternating) | PASS |
| Stretch: OneTrack | <= 8.8 ns | 8.547 ns (Forward) / 9.184 ns (Alternating) | REPORTED |
| Compacting GC Safety | GcBetweenTicks test green | Passed (243/243 total tests passed) | PASS |
| Warm allocation | 0 B on every arm | 0 B across all 150 benchmark arms and verify receipts | PASS |
| Three-way receipt identity | Direct == Generated == Facade | Exact identity across all 5 shapes and both patterns in --verify | PASS |
| Byte budget delta | <= +300 B | +258 B (261,520 / 300,000 total) | PASS |

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r10/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
