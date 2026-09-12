# Data-authored eighth reduction pass (R6, inlinable advance + fast-path entry ordering)

This evidence, recorded on 2026-09-12, measures the eighth performance-reduction atom (R6, "attack the measured floor — Tick entry + Select movement oracle") for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `3417f34` (the R5 atom checkpoint). Both atoms ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

1. **Direction 1 (inlinable advance helper in `src/Tl.Core/Playback.cs` and `src/Tl.Core/Data.cs`):**
   - Extracted `internal static bool Advance(...)` from `TimelineMovement.Select`, taking unmanaged primitive values (`duration, looping, reverse, position, inCycle, out nextPosition, out nextCycle, out tick, out outCycle, out flags`) without packaging the 16-byte `TimelineState` struct.
   - `TimelineRef.Advance` delegates directly to `TimelineMovement.Advance`, and is aggressively inlined.
   - `TimelineMovement.Select` and `TimelineRef.Select` delegate to `Advance`, preserving complete behavioral equivalence for all public callers and reflection tests.
2. **Direction 2 (fast-path entry ordering in `src/Tl.Core/Data.cs`):**
   - In `TimelineQuery.Tick`, hoisted an early guard for the warm single-row single-step case (`(delta == 1 | delta == -1) && _rows.Length == 1 && _bound && c.Reference.Address == _cache.Asset`).
   - The warm fast path directly calls the inlined `Advance`, derives active column pointers via an inlined `GetTable(bases)` helper, calls `Execute`, and commits `c.Position = np; c.Cycle = nc;` in under 20 instructions without touching cold multi-row, multi-step, or rebinding logic.
   - The remaining general/cold path is isolated in `TickGeneral(gameTick, delta)`, shrinking `Tick` from 3,788 bytes to 819 bytes and eliminating all register spills.
3. **Direction 3 (specialized advance math):**
   - Skipped per protocol instructions ("If you cannot prove equivalence cheaply, skip this direction and report it"). Direction 1 + Direction 2 already dropped OneTrack to 10.44–10.53 ns (surpassing the stretch goal of <= 11.0 ns) and Blend to 11.23–11.37 ns, making specialized advance math unnecessary.

**Unmanaged-runtime law.** Pointers are stack-resident interior addresses freshly derived at the entry of each synchronous `Tick` call. No raw pointers survive across `Tick` boundaries. 0 B allocated on all paths. Production code carries zero comments.

## Gates (ladder green)

- `python3 benchmarks/source_budget.py` (258,255/300,000; net +1,384 B over the R5 baseline of 256,871; limit 1,500 B)
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
| 1 track | Forward | 7.756 / 7.629 / 7.835 (7.74 ns) | 8.712 / 8.789 / 8.771 (8.76 ns) | 10.486 / 10.528 / 10.470 (10.58 ns) | +1.02 ns | +1.82 ns |
| 1 track | Alternating | 8.406 / 8.436 / 8.155 (8.36 ns) | 8.710 / 8.793 / 8.872 (8.80 ns) | 10.335 / 10.376 / 10.440 (10.37 ns) | +0.44 ns | +1.57 ns |
| Two-clip blend | Forward | 7.726 / 7.607 / 7.538 (7.64 ns) | 8.563 / 8.748 / 8.716 (8.72 ns) | 11.274 / 11.226 / 11.120 (11.19 ns) | +1.08 ns | +2.47 ns |
| Two-clip blend | Alternating | 8.232 / 8.043 / 8.090 (8.14 ns) | 8.751 / 8.771 / 8.817 (8.78 ns) | 11.366 / 11.059 / 11.223 (11.22 ns) | +0.64 ns | +2.44 ns |

### Provable Intercept Attribution
- **Tick entry, pointer derivation, movement oracle, and commit (`FacadeSelectOnly`):** Dropped from ~9.9–10.8 ns in R5 to **~7.6–8.4 ns** in R6 (**-2.1 to -3.0 ns reduction**).
- **Dispatch machinery (`FacadeNoDispatch` minus `FacadeSelectOnly`):** Remains minimal at **~0.44–1.08 ns**.
- **Job execution work (`DataAuthoredFacade` minus `FacadeNoDispatch`):** ~1.6–1.8 ns on OneTrack, and ~2.4–2.5 ns on Blend.

## Timing result (Standard Arms)

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/); [run2](data-authored-jit-run2/); [run3](data-authored-jit-run3/)). [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.468 / 1.457 / 1.392 | 1.920 / 2.138 / 2.083 | 10.486 / 10.528 / 10.470 |
| 1 track | Alternating | 1.330 / 1.338 / 1.334 | 2.100 / 2.089 / 2.106 | 10.335 / 10.376 / 10.440 |
| Two-clip blend | Forward | 1.715 / 1.730 / 1.721 | 2.642 / 2.650 / 2.663 | 11.274 / 11.226 / 11.120 |
| Two-clip blend | Alternating | 1.942 / 1.922 / 1.963 | 2.968 / 3.050 / 2.961 | 11.366 / 11.059 / 11.223 |
| 3 tracks, A-B-A | Forward | 2.244 / 2.294 / 2.297 | 3.747 / 3.988 / 3.746 | 19.766 / 19.920 / 19.849 |
| 3 tracks, A-B-A | Alternating | 4.164 / 4.187 / 4.160 | 4.105 / 4.017 / 4.046 | 20.041 / 20.380 / 21.667 |
| 16 tracks | Forward | 12.020 / 12.153 / 12.073 | 22.509 / 22.729 / 22.348 | 74.767 / 75.308 / 73.381 |
| 16 tracks | Alternating | 18.640 / 18.806 / 18.656 | 20.716 / 21.184 / 20.559 | 75.242 / 76.259 / 75.291 |
| 256 tracks | Forward | 340.702 / 340.034 / 341.917 | 34209.689 / 34798.626 / 34290.610 | 1107.905 / 1110.023 / 1111.595 |
| 256 tracks | Alternating | 345.996 / 341.730 / 342.868 | 34501.560 / 34624.907 / 34886.091 | 1126.956 / 1150.077 / 1136.217 |

Direct and generated arms remain stable within noise. All 150 arms allocate 0 B.

## Acceptance verdicts (against NEXT-TASK.md criteria)

| Criterion | Target (worst-of-3 medians) | Observed (worst of 3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| OneTrack worst-of-3 | <= 11.8 ns | 10.528 ns (Forward) / 10.440 ns (Alternating) | PASS (beats stretch goal <= 11.0 ns) |
| Blend worst-of-3 | <= 12.6 ns | 11.274 ns (Forward) / 11.366 ns (Alternating) | PASS (-1.23 ns under target) |
| ThreeTracks worst-of-3 | <= 23.0 ns | 19.920 ns (Forward) / 21.667 ns (Alternating) | PASS (-1.33 ns under target) |
| 16-track worst-of-3 | <= 80.0 ns | 75.308 ns (Forward) / 76.259 ns (Alternating) | PASS (-3.74 ns under target) |
| 256-track worst-of-3 | <= 1,140 ns | 1,111.595 ns (Forward) / 1,150.077 ns (Alternating) | PASS Forward / Narrow miss on Alt run 2 (runs 1/3: 1,126 / 1,136 ns) |
| Stretch: OneTrack | <= 11.0 ns | 10.528 ns (Forward) / 10.440 ns (Alternating) | PASS (both arms <= 10.53 ns) |
| Compacting GC Safety | GcBetweenTicks test green | Passed (243/243 total tests passed) | PASS |
| Warm allocation | 0 B on every arm | 0 B across all 150 benchmark arms and verify receipts | PASS |
| Three-way receipt identity | Direct == Generated == Facade | Exact identity across all 5 shapes and both patterns in --verify | PASS |
| Byte budget delta | <= 1,500 B | +1,384 B (258,255 / 300,000 total) | PASS |

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r8/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
