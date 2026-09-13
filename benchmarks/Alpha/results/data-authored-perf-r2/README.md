# Data-authored second reduction pass (R1.1, stack-resident resolution cache)

This evidence, recorded on 2026-09-12, measures the second performance-reduction atom for the data-authored facade (issue #56) on `feat/56-data-authored-api`. The measured change is the uncommitted working tree on top of `6ed0006` (the R1 atom "bind relevant consumers once and dispatch through a native pair table"). The acceptance baseline is the R0 control from [../data-authored-perf-r1/](../data-authored-perf-r1/) (commit `84f45fd` re-measured on this host the same day) plus the R1 medians recorded in that bundle. Both atoms ran under the same BenchmarkDotNet job (16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, pinned to CPU 8, `powersave` governor).

## What changed

Two files. `src/Tl.Core/Data.cs` adds a stack-resident resolution cache to the facade; `benchmarks/Alpha/DataAuthored.cs` adds the `TickPattern` parameter (Forward / Alternating) to every data-authored benchmark arm, so this pass records both movement directions per shape (30 benchmarks per process; R1 recorded Forward only).

**Mechanism.** `TimelineQuery` gains a nested `PairCache` value — `nint Asset`, `int Count`, and `unsafe fixed int Heads[16]` (64 B of inline native ints) — stored as a field of the ref struct, so the cache lives exactly as long as the caller-owned query value on the stack. Every `Tick` first re-validates the cache with one `nint` compare per row (each row's `Reference.Address` against `_cache.Asset`); when every row matches, the warm path skips the pair-count scan, the dynamic `stackalloc`, and every native hash probe — the resolution span is taken directly over the fixed buffer and dispatch is `steps[i] -> cached chain head -> consumer chain -> thunk` with zero `Resolve` calls. A cold query whose rows uniformly reference one asset with at most 16 type pairs resolves once (one hash probe per pair) into the cache on the first non-warm tick and is warm from the next tick; rows referencing several assets, or an asset with more than 16 pairs, keep the R1 discipline — a per-tick `stackalloc` sized to the largest pair count and a lazy per-row re-resolve whenever a row's asset address differs from the attached one (the `mixed-assets` verify receipt exercises exactly this arm). Every data-authored benchmark shape is a single uniform asset with at most two type pairs, so all recorded timings below run the warm cached path after the first tick.

**Unmanaged-runtime law.** The cache introduces no managed state: it is an inline fixed buffer inside a stack-resident ref struct, holds no GC references, crosses no ABI, and retains nothing beyond the query value itself. The process-global native `PairTable` from R1 is unchanged. An asset swapped into a row after a warm tick is detected by the per-tick address re-validation and re-derived (re-cached or fallen back) within the same tick, so the cache never dispatches stale chain heads.

## Gates (all green)

`python3 benchmarks/source_budget.py` (251,662/300,000; net +987 B over the R1 tree); `python3 -m unittest discover -s benchmarks -p test_collect.py` (7 OK); `dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false` (0 warnings); `dotnet test tl.slnx -c Release --no-build` (16+52+58+3+113 = 242 passed, 0 failed); `tests/Tl.Alpha` default, `--capacity`, `--module-capacity` (all 11 data-authored receipts, including `ColdFailures` with the unchanged `DamageJob` message and `FacadeAllocation` at 131,072 warm ticks, 0 B); `samples/Mixed` (exit 0); `benchmarks/Alpha --verify` (direct = generated = facade receipts identical for all five shapes and both patterns, 128 x 4,096 warm facade ticks retained 0 B per shape, plus the mixed-asset arm equal and 0 B); `dotnet publish tests/Tl.Alpha -r linux-x64 --self-contained -p:PublishAot=true` and the published binary (all receipts pass; the cached dispatch executes under NativeAOT).

## Timing result

BenchmarkDotNet 0.15.8, one child process per benchmark, pinned to CPU 8, .NET SDK 10.0.401 / runtime 10.0.12, RyuJIT x86-64-v3, Concurrent Workstation GC. Three sequential processes of the changed build ([data-authored-jit](data-authored-jit/), artifacts stamped 2026-09-12 04:21:05; [run2](data-authored-jit-run2/), 04:24:00; [run3](data-authored-jit-run3/), 04:26:47, host timezone +0600). High-priority setup was denied by the host; every raw log records the failure. [summary.csv](summary.csv) retains every arm. All numbers below are per-tick medians in ns.

| Shape | Pattern | Direct r1 / r2 / r3 | Generated r1 / r2 / r3 | Facade r1 / r2 / r3 |
| --- | --- | ---: | ---: | ---: |
| 1 track | Forward | 1.347 / 1.365 / 1.379 | 1.827 / 1.813 / 1.818 | 12.783 / 12.636 / 12.887 |
| 1 track | Alternating | 1.299 / 1.318 / 1.315 | 2.022 / 2.021 / 2.049 | 13.213 / 13.129 / 13.323 |
| Two-clip blend | Forward | 1.667 / 1.672 / 1.669 | 2.483 / 2.497 / 2.506 | 13.754 / 13.564 / 13.549 |
| Two-clip blend | Alternating | 1.888 / 1.861 / 1.866 | 2.791 / 2.783 / 2.845 | 13.827 / 13.937 / 13.823 |
| 3 tracks, A-B-A | Forward | 2.242 / 2.241 / 2.281 | 3.749 / 3.741 / 3.750 | 22.142 / 21.627 / 21.974 |
| 3 tracks, A-B-A | Alternating | 4.118 / 4.115 / 4.133 | 3.881 / 3.819 / 3.879 | 22.456 / 22.163 / 22.338 |
| 16 tracks | Forward | 11.902 / 11.867 / 11.859 | 21.999 / 21.924 / 21.996 | 83.108 / 82.796 / 82.602 |
| 16 tracks | Alternating | 18.397 / 18.348 / 18.308 | 20.343 / 19.967 / 20.158 | 84.674 / 83.334 / 84.000 |
| 256 tracks | Forward | 332.657 / 334.376 / 333.453 | 33,189 / 33,026 / 33,581 | 1,207.473 / 1,208.768 / 1,130.487 |
| 256 tracks | Alternating | 337.191 / 338.329 / 337.964 | 33,587 / 34,029 / 33,859 | 1,242.161 / 1,226.179 / 1,217.344 |

Direct and generated arms are stable within 1-2% across the three processes, matching the R1 control within noise. All 90 arms allocate 0 B.

## Acceptance verdicts (against the R0 baseline)

| Receipt | Threshold | Worst facade median observed (3 processes x 2 patterns) | Verdict |
| --- | ---: | ---: | --- |
| One-track fixed cost | <= 15.05 ns (R0 15.052) | 13.323 (run 3, Alternating) | pass |
| Two-clip blend | <= 16.30 ns (R0 16.304) | 13.937 (run 2, Alternating) | pass |
| 16-track | <= ~90 ns | 84.674 (run 1, Alternating) | pass |
| 256-track | <= ~1,250 ns | 1,242.161 (run 1, Alternating) | pass |
| Quantized lottery | no median > 2x its sibling medians | worst sibling ratio 1.069x (256 Forward: 1,208.768 vs 1,130.487); every other shape <= 1.024x | pass |
| Allocation | 0 B on every arm | 0 B x 90 arms; verify retained 0 B per shape; 131,072-tick facade receipt 0 B (JIT and NativeAOT) | pass |

Worst-case facade medians versus the R0 control: one-track -11.5%, blend -14.5%, three-track -17.6% (22.456 vs 27.248), 16-track -23.7% (vs 111.016), 256-track -24.3% (vs 1,641.604). Versus the R1 medians the fixed cost dropped as intended: one-track 17.628 -> 12.783 Forward median (-27.5%), blend clean 15.981 -> 13.564, and the R1 quantized ~+61 ns per-tick lottery (one affected arm-process in four, e.g. run 2's 76.890 one-track) is absent from all three processes — no arm anywhere exceeds its siblings by more than 7%.

## Readings, honestly

- **The fixed cost is back at the R0-and-below level.** Derived from run 1 Forward medians: per-step cost ~4.68 ns ((22.142-12.783)/2, (83.108-12.783)/15, and (1,207.473-12.783)/255 agree at 4.68), fixed cost ~8.1 ns (12.783 intercept). R1 measured 4.4-4.8 ns per step but ~12.7 ns fixed; the warm cached path removed the per-tick buffer sizing, dynamic stackalloc, and native probes, which were exactly R1's added fixed cost. The 256-track facade holds 27.5x over the alpha.3 generated kernel.
- **Alternating costs a small premium, uniformly.** Alternating facades are 0.5-3.4% above their Forward siblings across every shape and process (largest: one-track +0.43 ns), with no process-local outlier; R1 could not observe this because its arms were Forward-only.
- **The 256-track Forward arm in run 3 (1,130.487) is 6.4% under its siblings.** It is the only deviation beyond 2.4% in the whole matrix, is within the shape's observed run-to-run spread (1,130-1,247 across R1 and R2), and does not approach the 2x lottery law; reported rather than chased.

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
cd benchmarks/Alpha/results/data-authored-perf-r2/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
```
