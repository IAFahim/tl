# Data-authored first-pass shape comparison (gate 7)

This evidence, recorded on 2026-09-12, measures commit `548c802022a9e12eb4ab65da4fad6d94d8876b4a` on `feat/56-data-authored-api`. The changes are benchmark fixtures, verification, and documentation only; production source under `src` is unchanged at 249,258/250,000 B.

`DataAuthoredQueryBenchmarks` runs three arms per shape in one process: a hand-written direct oracle, the alpha.3 generated catalog query, and the data-authored facade. The facade bakes each shape to TLB1 bytes, loads them through `TimelineAsset.Load` into one native block, and plays `TimelineComponent` rows through `Timeline.Rows(rows).Read(...).Read(...).Read(...).Write(accumulators).Tick(gameTick)` with the same generated `AlphaJob`/`BetaJob` consumers as the alpha.3 arms. The shape geometry mirrors `Domain.cs` exactly — same track codes, clip amounts, `[0,64)` windows, and looping duration 64 — so the comparison is one-variable. The internal gap shape is not part of this first-pass matrix.

Every arm performs 4,096 ticks per invocation and returns the complete `BenchmarkReceipt` of consumed playback state, output, order, game-tick, and call-count hashes. `GlobalSetup` requires exact three-way receipt agreement before any timing is trusted. The facade additionally carries three borrowed read columns (`FirstInput`, `SecondInput`, `ThirdInput`) because the process-global consumer table also holds `ComponentJob`, whose bind requires them; their values never reach the Alpha/Beta receipts, and their bind check is an honest part of the current facade cost.

## Receipts

`dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify` now verifies, per shape and for both forward and alternating patterns, that direct, generated, and facade receipts are identical, throwing on any mismatch. Representative 256-track forward line:

```
data-authored/TwoHundredFiftySixTracks/Forward: direct=BenchmarkReceipt { StateHash = 64, ValueHash = -837655896031494144, OrderHash = 910950324627632150, GameTickHash = 2146959360, Calls = 1048576 } generated=BenchmarkReceipt { ... identical ... } facade=BenchmarkReceipt { ... identical ... }
allocation: 128 x 4096 TwoHundredFiftySixTracks data-authored facade ticks retained 0 B
```

All five shapes retain `0 B` over `128 x 4096` warm facade ticks; the `Tl.Alpha` suite separately proves `131,072` warm facade ticks at `0 B`.

## Timing result

BenchmarkDotNet 0.15.8, one process per job, 16 warmups, 12 iterations of 250 ms, 4,096 operations per invocation, `MemoryDiagnoser`, pinned to CPU 8. Medians from the primary filtered run; [summary.csv](summary.csv) retains every run's mean, median, standard deviation, and allocation, including the no-tiering and full-baseline processes.

| Shape | Direct ns/tick | Alpha.3 generated ns/tick | Data-authored facade ns/tick | Facade/generated | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 track | 1.418 | 1.898 | 15.118 | 7.97x | 0 B |
| Two-clip blend | 1.690 | 2.531 | 16.318 | 6.45x | 0 B |
| 3 tracks, A-B-A | 2.276 | 3.829 | 27.302 | 7.13x | 0 B |
| 16 tracks | 12.101 | 22.343 | 110.868 | 4.96x | 0 B |
| 256 tracks | 339.299 | 34,646.702 | 1,655.134 | 0.048x | 0 B |

## Same-machine alpha.3 baselines

The full existing suite ran once on this host for same-machine controls ([full-baseline](full-baseline/BenchmarkDotNet.Artifacts/results/)); the historical release medians from the release machine remain reference only.

| Arm | This host median | Release median |
| --- | ---: | ---: |
| Generated 1 track | 1.865 ns | 1.804 ns |
| Generated A-B-A | 3.795 ns | 3.754 ns |
| Generated 16 tracks | 22.375 ns | 21.230 ns |
| Generated 256 tracks | 34,592.495 ns | 34,250.868 ns |
| Generated internal gap | 2.043 ns | 1.997 ns |
| Generated two-clip blend | 2.524 ns | 2.555 ns |
| Generated three input columns | 2.409 ns | 2.256 ns |
| Generated 256 rows, 5 assets | 38.644 ns | 34.650 ns |

The generated controls sit within 5% of the release medians except the three-input component arm at +6.8%, so the same-machine comparison is sound. The mixed 256-row stream drifted +11.5% above its release median in this single process; the release record itself spans 34.562-38.040 ns across processes, so this is flagged as single-process dispersion, not selected away.

## Default tiering and no-tiering

A separate paired process ran the fifteen data-authored arms under the default tiered runtime and with `DOTNET_TieredCompilation=0` ([data-authored-notiering](data-authored-notiering/BenchmarkDotNet.Artifacts/results/)). These are diagnostic single-process medians.

| Shape | Facade, default tiering | Facade, no tiering | Generated, default tiering | Generated, no tiering |
| --- | ---: | ---: | ---: | ---: |
| 1 track | 15.118 ns | 21.814 ns | 1.898 ns | 3.356 ns |
| Two-clip blend | 16.318 ns | 22.312 ns | 2.531 ns | 4.650 ns |
| 3 tracks, A-B-A | 27.302 ns | 36.739 ns | 3.829 ns | 5.935 ns |
| 16 tracks | 110.868 ns | 157.696 ns | 22.343 ns | 21.892 ns |
| 256 tracks | 1,655.134 ns | 2,145.534 ns | 34,646.702 ns | 34,581.660 ns |

All 60 paired results allocate `0 B`. The facade is tiering-sensitive like the generated path; the 256-track ordering is unchanged in both jobs.

## Readings, honestly

- The 256-track cliff is decisively removed: 1,655.134 ns against 34,646.702 ns for the generated kernel, a 20.9x reduction and 4.9x above the direct oracle. The facade has no per-asset generated code; it walks a compact native program of 256 steps, while the alpha.3 kernel exceeds the 32 KiB fusion budget and pays the staged-scheduler instruction-cache cost recorded in issue #10.
- Small shapes lose to alpha.3 in this first pass: 15.118 ns one-track against 1.898 ns generated. The issue's compiled-kernel tier target of roughly 2.5-4.5 ns is not met.
- The measured structure is a fixed per-tick facade overhead near 8.7 ns plus about 6.43 ns per dispatched step (the 1-to-256-track ladder is linear: `(1655.134 - 15.118) / 255`). Per tick the facade binds every consumer in the process-global table, selects movement, and scans stages; per step it binary-searches the pair table, walks the consumer chain through a `delegate*` thunk, re-finds the accumulator column inside the thunk, and converts the native slot to a frame.
- No tuning was applied in this atom. Reducing the fixed overhead (stage-index caching, bind hoisting) or the per-step cost (per-stage dispatch hoisting, column lookup hoisting) is optimization work that requires its own atom and fresh receipts under verdict law B19; [optimization-verdicts.md](../../../../docs/optimization-verdicts.md) records the bindings.
- The full-baseline process re-measured the data-authored arms independently and agrees with the filtered run within 1% on every shape (for example 256-track facade 1,653.340 ns there against 1,655.134 ns above).

## Environment and integrity

[environment.json](environment.json) records the host, toolchain, CPU affinity, governor, and source budget: Intel i9-14900K, .NET SDK 10.0.401, runtime 10.0.12, `powersave` governor, benchmark CPU 8, high-priority setup denied by the host and recorded in every raw log. [SHA256SUMS](SHA256SUMS) covers every retained file.

## Reproduction

```sh
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
cd benchmarks/Alpha/results/data-authored-first-pass/data-authored-jit
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
cd ../data-authored-notiering
TL_ALPHA_NO_TIERING=1 taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*DataAuthored*'
cd ../full-baseline
taskset -c 8 dotnet run --project ../../../Alpha.csproj -c Release --no-build -- --filter '*'
```
