# Signed seek evidence

This evidence measures source commit `12119ca583884390c523049a3309ce5a3d244730` and tree `733c5454af90e9cdd5fe3eb554a5136005cddcaf` after rebasing the signed-seek integration onto `main` at `8f6545a`. Every timed operation consumes success, playback, and output state. Setup proves direct, generated typed, and generated dynamic execution against an independent oracle before measurement. Signed deltas replay every crossed local frame in order.

## SumTimeline matrix

The `SumTimeline` matrix covers literal and runtime `+1`, `-1`, `+5`, `-5`, alternating direction, five repeated unit calls, and `+64`. A multi-frame result is divided by the executed frame count only in the `ns/frame` column. [summary.csv](summary.csv) retains each fresh-process median, time per call, time per frame, and allocation.

| Case | Frames/call | Direct ns/frame | Typed ns/frame | Dynamic ns/frame |
| --- | ---: | ---: | ---: | ---: |
| LiteralPositiveOne | 1 | 2.247 | 1.674 | 6.950 |
| LiteralNegativeOne | 1 | 2.269 | 1.780 | 8.363 |
| RuntimePositiveOne | 1 | 2.468 | 2.170 | 6.987 |
| RuntimeNegativeOne | 1 | 2.481 | 2.495 | 7.332 |
| AlternatingOne | 1 | 4.431 | 2.562 | 7.141 |
| LiteralPositiveFive | 5 | 1.534 | 1.448 | 2.397 |
| LiteralNegativeFive | 5 | 1.520 | 1.412 | 2.443 |
| RuntimePositiveFive | 5 | 1.732 | 1.457 | 2.398 |
| RuntimeNegativeFive | 5 | 1.731 | 1.486 | 2.396 |
| RepeatedPositiveOneFive | 5 | 1.515 | 2.494 | 6.910 |
| RepeatedNegativeOneFive | 5 | 1.578 | 2.780 | 7.282 |
| LiteralPositiveSixtyFour | 64 | 1.564 | 1.411 | 1.421 |
| RuntimePositiveSixtyFour | 64 | 1.933 | 1.411 | 1.416 |

All 117 Sum results report `0 B` managed allocation. The generated typed facade is below 3 ns per executed frame in every measured Sum case. Literal unit movement is below 2 ns. The dynamic facade is above 3 ns for unit calls and amortizes below 2.5 ns at five frames.

## Combat callback boundary

`CombatTimeline` exercises heterogeneous animation and damage tracks, generated borrowed data, blending, state branches, and ordered output mutations. [combat-summary.csv](combat-summary.csv) retains all fresh-process medians.

| Stream | Direct ns/frame | Typed ns/frame | Dynamic ns/frame |
| --- | ---: | ---: | ---: |
| Forward +1 | 3.778 | 4.165 | 6.039 |
| Alternating +1/-1 | 8.005 | 7.696 | 20.258 |

All 18 Combat results report `0 B` managed allocation. The Sum target does not imply that arbitrary callbacks execute below 3 ns. Callback instructions, active tracks, blend work, branches, memory dependencies, and effect ordering establish the physical floor for each consumer.

## Environment and method

BenchmarkDotNet 0.15.8 ran six separate processes on .NET 10.0.12: three Sum processes with 39 cases each and three Combat processes with six cases each. Each process was pinned to CPU 0 and used 16 warmups, 12 measured iterations, a requested 250 ms iteration time, and MemoryDiagnoser. The OS denied priority elevation. Frequency was not locked, the governor reported `powersave`, and turbo remained enabled. BenchmarkDotNet warned that minimum observed iteration time fell below 100 ms in some large-replay cases. Raw JSON and unfiltered logs are retained in `run1` through `run3` and `combat-run1` through `combat-run3`.

[pmu.jsonl](pmu.jsonl) retains 45 raw hardware-counter scenarios. The collector used a grouped `perf_event_open` file descriptor for cycles, retired instructions, branches, and branch misses; excluded kernel and hypervisor activity; and pinned every child to CPU 0. All scenarios succeeded with running fraction `1.0`. [pmu.csv](pmu.csv) is its deterministic projection. PMU data supports the timing evidence and does not replace the three-process distributions.

[disassembly](disassembly) contains full-optimization listings with tiering and ReadyToRun disabled. [code-size.csv](code-size.csv) records every emitted method size. The typed `SumTimeline.SeekCore` is 1,995 bytes, its static constructor is 62 bytes, and dynamic `TrySeek` is 2,380 bytes. [generated-report.txt](generated-report.txt) records 39,397 UTF-8 source bytes across five generated files and 1,025 declared shared dispatch-value bytes. Generated static payloads are 13 bytes for Sum and 26 bytes for Combat. The runtime `Playback` ABI is 16 bytes and `Frame` is 40 bytes on x64.

NativeAOT is a correctness and package-consumer gate in this workstream. It is not a timing claim. C signed-seek ABI qualification and Unity/Burst integration remain separate issue #16 workstreams.

## Reproduce

Build and verify correctness:

```sh
dotnet build benchmarks/Alpha/Alpha.csproj -c Release -p:NuGetAudit=false
dotnet run --project benchmarks/Alpha -c Release --no-build -- --matrix
```

Run three Sum and three Combat processes, substituting `N` with `1`, `2`, and `3`:

```sh
taskset -c 0 dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*SignedSeekBenchmarks*' --artifacts /tmp/tl16-sum-rN
taskset -c 0 dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*CombatBenchmarks*' --artifacts /tmp/tl16-combat-rN
```

Collect PMU counters and assembly:

```sh
python3 benchmarks/Alpha/collect_pmu.py --cpu 0 --output /tmp/tl16-pmu.jsonl
DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_JitDisasm='SignedSeekBenchmarks:Run*' DOTNET_JitStdOutFile=/tmp/tl16-runners.asm dotnet run --project benchmarks/Alpha -c Release --no-build -- --matrix
DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_JitDisasm='SumTimeline:*' DOTNET_JitStdOutFile=/tmp/tl16-kernel.asm dotnet run --project benchmarks/Alpha -c Release --no-build -- --matrix
DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_JitDisasm='__TlGeneratedSchema1:*' DOTNET_JitStdOutFile=/tmp/tl16-dynamic.asm dotnet run --project benchmarks/Alpha -c Release --no-build -- --matrix
```

Regenerate projections and verify every retained byte:

```sh
python3 benchmarks/Alpha/results/signed-seek/summarize.py
python3 benchmarks/Alpha/results/signed-seek/summarize_combat.py
python3 benchmarks/Alpha/results/signed-seek/summarize_pmu.py
python3 benchmarks/Alpha/results/signed-seek/summarize_code.py
sha256sum -c benchmarks/Alpha/results/signed-seek/SHA256SUMS
```
