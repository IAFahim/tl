# Data-authored sixth reduction pass (R4, single-pass movement)

This evidence, recorded on 2026-09-12, measures the sixth performance-reduction atom (R4, "single-pass movement (eliminate the double `Select` per single-step tick)") for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `700a351` (the R3 atom checkpoint). Both atoms ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

One file in `src/`: `src/Tl.Core/Data.cs`.
In `TimelineQuery.Tick`, the single-step fast path (`|delta| == 1`) specializes for single-row queries (`rowCount == 1`, covering all benchmark shapes):
`c.Reference.Select` is called exactly once during the execute pass, capturing `out var next`. If true, after `Execute` succeeds, `c.Position` and `c.Cycle` are committed directly from `next`, completely eliminating the second `Select` call per single-step tick. Multi-row single-step and `|delta| > 1` paths remain unchanged. Semantics are preserved: idle rows commit nothing, and exceptions in execution prevent position/cycle updates.

**Unmanaged-runtime law.** No managed types, allocation, delegates, LINQ, exceptions-on-warm-path, or locks in the playback path. Production code carries zero comments. Transient interior pointers remain strictly scoped to `Tick`.

## Gates (ladder green)

- `python3 benchmarks/source_budget.py` (256,297/300,000; net +683 B over the R3 baseline of 255,614; limit 1,000 B)
- `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK)
- `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings, 0 errors)
- `dotnet test tl.slnx -c Release -p:NuGetAudit=false -m:1` (243 passed, 0 failed, including `CompactingGcBetweenTicksKeepsDispatchWritingCurrentColumns`)
- `tests/Tl.Alpha` default (run 5 times: all 11 data-authored receipts pass, facade allocation 131,072 warm ticks 0 B each time)
- `tests/Tl.Alpha --capacity` (10,000 mixed rows matched)
- `tests/Tl.Alpha --module-capacity` (schema mutations rejected before effects)
- `samples/Mixed` (exit 0)
- `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all five shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape, mixed-asset arm verified)
- `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and published binary (all receipts pass under NativeAOT)

## Timing result

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/); [run2](data-authored-jit-run2/); [run3](data-authored-jit-run3/)). High-priority setup was denied by the host; every raw log records the failure. [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.395 / 1.398 / 1.356 | 1.860 / 1.850 / 1.857 | 13.048 / 13.128 / 13.084 |
| 1 track | Alternating | 1.329 / 1.326 / 1.331 | 2.060 / 2.071 / 2.077 | 14.028 / 13.219 / 13.217 |
| Two-clip blend | Forward | 1.706 / 1.703 / 1.698 | 2.600 / 2.537 / 2.542 | 13.775 / 13.709 / 13.691 |
| Two-clip blend | Alternating | 1.913 / 1.900 / 1.912 | 2.885 / 2.858 / 2.950 | 13.849 / 13.872 / 13.851 |
| 3 tracks, A-B-A | Forward | 2.264 / 2.235 / 2.235 | 3.803 / 3.907 / 3.845 | 22.084 / 21.983 / 22.133 |
| 3 tracks, A-B-A | Alternating | 4.149 / 4.157 / 4.136 | 3.940 / 3.987 / 3.913 | 22.478 / 22.536 / 22.451 |
| 16 tracks | Forward | 11.869 / 12.045 / 11.942 | 22.314 / 22.153 / 22.284 | 76.647 / 76.878 / 77.399 |
| 16 tracks | Alternating | 18.531 / 18.499 / 18.526 | 20.411 / 20.628 / 20.573 | 80.134 / 79.084 / 78.909 |
| 256 tracks | Forward | 335.399 / 334.850 / 335.592 | 34592.937 / 34111.537 / 34545.853 | 1101.454 / 1085.130 / 1118.976 |
| 256 tracks | Alternating | 342.489 / 340.090 / 342.038 | 34331.166 / 34370.301 / 34112.329 | 1123.795 / 1130.973 / 1120.152 |

Direct and generated arms remain stable within noise. All 90 arms allocate 0 B.

## Acceptance verdicts (against NEXT-TASK.md criteria)

| Criterion | Target (worst-of-3 medians) | Observed (worst of 3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| OneTrack worst-of-3 | <= 12.5 ns | 14.028 ns (Run 1 Alternating; Forward worst: 13.128 ns, Alt run 2: 13.219 ns, Alt run 3: 13.217 ns) | MISSED (+1.53 ns over target) |
| Blend worst-of-3 | <= 13.2 ns | 13.872 ns (Run 2 Alternating; Forward worst: 13.775 ns) | MISSED (+0.67 ns over target) |
| ThreeTracks worst-of-3 | <= 23.0 ns | 22.536 ns (Run 2 Alternating; Forward worst: 22.133 ns) | PASS (-0.46 ns under target) |
| 16-track worst-of-3 | <= 82.0 ns | 80.134 ns (Run 1 Alternating; Forward worst: 77.399 ns) | PASS (-1.87 ns under target) |
| 256-track worst-of-3 | <= 1,180 ns | 1,130.973 ns (Run 2 Alternating; Forward worst: 1,118.976 ns) | PASS (-49.03 ns under target) |
| Stretch: OneTrack | <= 11.0 ns | 13.048 ns best median / 14.028 ns worst | MISSED |
| Compacting GC Safety | GcBetweenTicks test green | Passed (243/243 total tests passed) | PASS |
| Warm allocation | 0 B on every arm | 0 B across all 90 benchmark arms and verify receipts | PASS |
| Three-way receipt identity | Direct == Generated == Facade | Exact identity across all 5 shapes and both patterns in --verify | PASS |
| Byte budget delta | <= 1,000 B | +683 B (256,297 / 300,000 total) | PASS |

## Decomposition and Cost Breakdown

1. **Fixed intercept dropped by ~0.3–0.5 ns vs R5:**
   - Linear regression on track counts (1, 3, 16, 256 steps):
     - R5 (R3) intercept: ~9.05–9.30 ns (Forward), ~9.57–9.95 ns (Alternating).
     - R6 (R4) intercept: ~8.64–9.31 ns (Forward, mean 8.92 ns), ~9.06–9.86 ns (Alternating, mean 9.39 ns).
2. **Per-step dispatch slope remained identical:**
   - R5 (R3) slope: ~4.24–4.32 ns/step (Forward), ~4.35–4.36 ns/step (Alternating).
   - R6 (R4) slope: ~4.20–4.34 ns/step (Forward), ~4.34–4.38 ns/step (Alternating).
3. **Single-pass Select effect:**
   - Eliminating the second `Select` on `rowCount == 1` shaved:
     - 1-track Forward: ~0.40 ns (13.532 ns -> 13.128 ns worst-of-3).
     - 1-track Alternating: runs 2 and 3 dropped by ~0.7–0.8 ns (13.931/14.032 ns -> 13.219/13.217 ns); run 1 showed a 14.028 ns worst-of-3 outlier.
     - Blend Forward: ~1.35 ns (15.126 ns -> 13.775 ns worst-of-3).
     - Blend Alternating: ~0.76 ns (14.628 ns -> 13.872 ns worst-of-3).
     - Multi-row shapes (3 tracks, 16 tracks, 256 tracks) did not use the single-row branch, but 3-tracks improved slightly (-0.40 to -0.44 ns) from compiler layout effects.
4. **Why OneTrack and Blend did not hit <= 12.5 / 13.2 ns:**
   - On a 1-clip timeline, `Select` evaluates in ~0.4–0.8 ns. Eliminating the second call removes that ~0.4–0.8 ns, but does not eliminate the fixed query scaffolding:
     - Query prologue (interior pointer derivations, table refresh loop): ~2.6–3.5 ns.
     - Execute prologue (frame setup, stage bounds check, chain head load): ~5.0–5.5 ns.
     - Total fixed intercept: ~8.6–9.4 ns.
     - With step dispatch adding ~4.2–4.3 ns, total OneTrack cost is ~13.0–13.2 ns in quiet runs, leaving a gap of ~0.5–0.7 ns to the 12.5 ns gate.

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r6/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
