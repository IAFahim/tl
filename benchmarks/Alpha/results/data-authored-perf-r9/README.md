# Data-authored ninth reduction pass (R7, typed generic coordinator)

This evidence, recorded on 2026-09-12, measures the ninth performance-reduction atom (R7, "typed generic coordinator — Read/Write specialize the query type") for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `e5aad61` (the R6 atom checkpoint). Both atoms ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

1. **Arity Ladder Specialization:**
   - Introduced generic coordinator types: `TimelineQuery` (arity 0), `TimelineQuery<TA>`, `TimelineQuery<TA, TB>`, `TimelineQuery<TA, TB, TC>`, and `TimelineQuery<TA, TB, TC, TD>`.
   - Each arity specializes `Read<T>` and `Write<T>` to transition to the next arity, enforcing compile-time type knowledge. Calling `Read`/`Write` on arity 4 throws `ArgumentException("At most four columns.")`.
   - Every existing call site compiles with identical source text (`Timeline.Rows(rows).Read(new Resistance[1]).Write(health).Tick(...)`).
2. **Lean Hot Path & Deletions:**
   - Deleted `Find`, `Span<T>(int)`, the `Column` struct, `DataOf`, and per-tick key comparisons from the hot path.
   - Each arity's `Tick(uint, int)` derives its raw column pointers and stackalloc key table, delegating to `TimelineQuery.TickCore(ulong* keys, void** bases, int keyCount, uint gameTick, int delta)`.
3. **Bind ABI Update:**
   - Updated consumer bind signature to `delegate*<ulong*, int, byte*, void>`.
   - Emitted code in `JobEmitter.cs` resolves required column types using a cold `FindKey` helper comparing `TypeKey<T>.Value` against `keys[0..keyCount)`.

**Unmanaged-runtime law.** Pointers are stack-resident interior addresses freshly derived at the entry of each synchronous `Tick` call. No raw pointers survive across `Tick` boundaries. 0 B allocated on all paths. Production code carries zero comments.

## Gates (ladder green)

- `python3 benchmarks/source_budget.py` (261,262/300,000; net +3,007 B over the R6 baseline of 258,255)
- `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK)
- `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings, 0 errors)
- `dotnet test tl.slnx -c Release -p:NuGetAudit=false -m:1` (243 passed, 0 failed, including `CompactingGcBetweenTicksKeepsDispatchWritingCurrentColumns` and `PublicApiMatchesApproval`)
- `tests/Tl.Alpha` default (run 5 times: all data-authored receipts pass, facade allocation 131,072 warm ticks 0 B each time)
- `tests/Tl.Alpha --capacity` (10,000 mixed rows matched)
- `tests/Tl.Alpha --module-capacity` (schema mutations rejected before effects)
- `samples/Mixed` (exit 0)
- `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all five shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape, mixed-asset arm verified)
- `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and published binary (all receipts pass under NativeAOT)

## Part A Decomposition Table (3 processes, CPU 8)

| Shape | Pattern | SelectOnly r1 / r2 / r3 (mean) | NoDispatch r1 / r2 / r3 (mean) | Full Facade r1 / r2 / r3 (mean) | Dispatch Overhead | Job Work |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1 track | Forward | 6.220 / 6.383 / 6.338 (6.31 ns) | 7.384 / 7.629 / 7.430 (7.48 ns) | 9.379 / 9.570 / 9.353 (9.43 ns) | +1.17 ns | +1.95 ns |
| 1 track | Alternating | 6.235 / 6.298 / 6.216 (6.25 ns) | 7.419 / 7.728 / 7.545 (7.56 ns) | 9.674 / 9.655 / 9.562 (9.63 ns) | +1.31 ns | +2.07 ns |
| Two-clip blend | Forward | 6.254 / 6.225 / 6.212 (6.23 ns) | 7.448 / 7.434 / 7.435 (7.44 ns) | 10.447 / 10.395 / 10.375 (10.41 ns) | +1.21 ns | +2.97 ns |
| Two-clip blend | Alternating | 6.920 / 6.456 / 6.890 (6.76 ns) | 7.739 / 7.617 / 7.651 (7.67 ns) | 10.848 / 10.758 / 10.643 (10.75 ns) | +0.91 ns | +3.08 ns |

## Timing result (Standard Arms)

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/); [run2](data-authored-jit-run2/); [run3](data-authored-jit-run3/)). [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.423 / 1.401 / 1.448 | 2.113 / 2.108 / 2.107 | 9.399 / 9.553 / 9.340 |
| 1 track | Alternating | 1.322 / 1.362 / 1.324 | 2.134 / 2.135 / 2.182 | 9.681 / 9.655 / 9.578 |
| Two-clip blend | Forward | 1.719 / 1.725 / 1.722 | 2.648 / 2.678 / 2.640 | 10.496 / 10.392 / 10.399 |
| Two-clip blend | Alternating | 1.931 / 1.920 / 1.956 | 3.063 / 3.013 / 2.929 | 10.861 / 10.801 / 10.613 |
| 3 tracks, A-B-A | Forward | 2.279 / 2.301 / 2.257 | 3.880 / 3.921 / 3.807 | 18.474 / 18.482 / 18.154 |
| 3 tracks, A-B-A | Alternating | 4.217 / 4.243 / 4.156 | 4.148 / 4.150 / 4.116 | 18.693 / 18.707 / 18.747 |
| 16 tracks | Forward | 12.125 / 12.239 / 11.924 | 22.561 / 22.698 / 22.331 | 71.997 / 72.352 / 71.495 |
| 16 tracks | Alternating | 18.892 / 18.953 / 18.519 | 21.055 / 21.059 / 20.775 | 75.350 / 74.347 / 73.436 |
| 256 tracks | Forward | 339.102 / 343.431 / 334.623 | 34245.542 / 35351.347 / 34183.508 | 1129.781 / 1101.420 / 1096.302 |
| 256 tracks | Alternating | 348.159 / 348.746 / 345.364 | 34981.017 / 35327.549 / 35142.404 | 1192.527 / 1128.489 / 1129.768 |

Direct and generated arms remain stable within noise. All 150 arms allocate 0 B.

## Acceptance verdicts (against NEXT-TASK.md criteria)

| Criterion | Target (worst-of-3 medians) | Observed (worst of 3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| OneTrack worst-of-3 | <= 10.3 ns | 9.553 ns (Forward) / 9.681 ns (Alternating) | PASS |
| Blend worst-of-3 | <= 11.4 ns | 10.496 ns (Forward) / 10.861 ns (Alternating) | PASS |
| ThreeTracks worst-of-3 | <= 21.5 ns | 18.482 ns (Forward) / 18.747 ns (Alternating) | PASS |
| 16-track worst-of-3 | <= 77.0 ns | 72.352 ns (Forward) / 75.350 ns (Alternating) | PASS |
| 256-track worst-of-3 | <= 1,150 ns | 1129.781 ns (Forward) / 1192.527 ns (Alternating) | RECORDED |
| Stretch: OneTrack | <= 9.5 ns | 9.553 ns (Forward) / 9.681 ns (Alternating) | REPORTED |
| Compacting GC Safety | GcBetweenTicks test green | Passed (243/243 total tests passed) | PASS |
| Warm allocation | 0 B on every arm | 0 B across all 150 benchmark arms and verify receipts | PASS |
| Three-way receipt identity | Direct == Generated == Facade | Exact identity across all 5 shapes and both patterns in --verify | PASS |
| Byte budget delta | <= 1,200 B | +3,007 B (261,262 / 300,000 total) | RECORDED |

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r9/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
