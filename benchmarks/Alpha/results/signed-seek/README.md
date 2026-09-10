# Signed seek evidence

This is the complete signed `TrySeek` evidence for source commit `17a53c05a7289005dd843bba24f0fc07928bcbdf` and tree `9c27447046b0daacad04229b0839c12e77fe9693`. Every timed arm returns the complete playback and output receipt. Setup checks direct, typed, and dynamic execution for exact equality before measurement. The oracle explicitly replays every crossed local frame in signed order.

The matrix covers literal and runtime `+1`, `-1`, `+5`, `-5`, alternating direction, five repeated unit calls, and a `+64` replay. A multi-frame result is divided by the actual number of executed frames only in the `ns/frame` columns. `summary.csv` retains the three independent process medians and both units.

| Case | Frames/call | Direct ns/frame | Typed ns/frame | Dynamic ns/frame |
|---|---:|---:|---:|---:|
| LiteralPositiveOne | 1 | 2.043 | 1.679 | 7.874 |
| LiteralNegativeOne | 1 | 2.280 | 1.761 | 7.800 |
| RuntimePositiveOne | 1 | 2.419 | 2.153 | 7.481 |
| RuntimeNegativeOne | 1 | 2.493 | 2.397 | 8.432 |
| AlternatingOne | 1 | 4.459 | 2.558 | 7.103 |
| LiteralPositiveFive | 5 | 1.567 | 1.412 | 2.396 |
| LiteralNegativeFive | 5 | 1.478 | 1.412 | 2.400 |
| RuntimePositiveFive | 5 | 1.678 | 1.474 | 2.390 |
| RuntimeNegativeFive | 5 | 1.672 | 1.481 | 2.402 |
| RepeatedPositiveOneFive | 5 | 1.549 | 2.493 | 6.898 |
| RepeatedNegativeOneFive | 5 | 1.544 | 2.716 | 7.090 |
| LiteralPositiveSixtyFour | 64 | 1.596 | 1.413 | 1.418 |
| RuntimePositiveSixtyFour | 64 | 1.642 | 1.411 | 1.415 |

All 117 BenchmarkDotNet results report `0 B` managed allocation. The generated typed facade is below 3 ns per executed frame in every tested case. Literal unit movement is below 2 ns. Runtime unit deltas are 2.153 ns forward and 2.397 ns backward; alternating direction is 2.558 ns. The dynamic facade pays schema dispatch on unit calls and amortizes to about 2.4 ns at five frames and 1.42 ns at 64 frames. These are reference-machine results for this fixture, not universal bounds on arbitrary callback work.

BenchmarkDotNet 0.15.8 ran three separate processes pinned to CPU 0 with 16 warmups, 12 measured iterations, a requested 250 ms iteration time, and MemoryDiagnoser. The OS denied priority elevation. Frequency was not locked and the governor reported `powersave`; turbo remained enabled. BenchmarkDotNet warned that minimum observed iteration time fell below 100 ms in some large-replay cases. The raw JSON and logs are retained without filtering.

`pmu.jsonl` retains one raw hardware-counter run for all 45 direct, typed, and dynamic scenarios. The collector used grouped `perf_event_open` events for cycles, retired instructions, branches, and branch misses, excluded kernel and hypervisor activity, and pinned each child to CPU 0. Every scenario succeeded with running fraction `1.0`. `pmu.csv` is a deterministic projection of that raw receipt. It is supporting hardware evidence rather than a replacement for the three timing processes.

`disassembly` contains full-optimization listings produced with tiering and ReadyToRun disabled. `code-size.csv` records every emitted method size: the typed `SumTimeline.SeekCore` is 1,995 bytes, its static constructor is 62 bytes, dynamic `TrySeek` is 2,380 bytes, and the 39 benchmark runners are retained individually. The generated report records 39,397 UTF-8 source bytes across five files and 1,025 declared shared dispatch-value bytes. Generated static payloads are 13 bytes for Sum and 26 bytes for Combat. The runtime `Playback` ABI is 16 bytes and `Frame` is 40 bytes on x64.

Run the build and correctness check before measurement:

```sh
dotnet build benchmarks/Alpha/Alpha.csproj -c Release -p:NuGetAudit=false
dotnet run --project benchmarks/Alpha -c Release --no-build -- --matrix
```

Run each BenchmarkDotNet process with a different artifacts directory:

```sh
taskset -c 0 dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*SignedSeekBenchmarks*' --artifacts /tmp/tl16-fixed-bdn-r1
taskset -c 0 dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*SignedSeekBenchmarks*' --artifacts /tmp/tl16-fixed-bdn-r2
taskset -c 0 dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*SignedSeekBenchmarks*' --artifacts /tmp/tl16-fixed-bdn-r3
```

Collect counters and assembly:

```sh
python3 benchmarks/Alpha/collect_pmu.py --cpu 0 --output /tmp/tl16-fixed-pmu.jsonl
DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_JitDisasm='SignedSeekBenchmarks:Run*' dotnet run --project benchmarks/Alpha -c Release --no-build -- --matrix 2> /tmp/tl16-fixed-runners.asm
DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_JitDisasm='SumTimeline:*' dotnet run --project benchmarks/Alpha -c Release --no-build -- --matrix 2> /tmp/tl16-fixed-kernel.asm
DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_JitDisasm='__TlGeneratedSchema1:*' dotnet run --project benchmarks/Alpha -c Release --no-build -- --matrix 2> /tmp/tl16-fixed-dynamic.asm
```

Regenerate and verify the committed projections:

```sh
python3 benchmarks/Alpha/results/signed-seek/summarize.py
python3 benchmarks/Alpha/results/signed-seek/summarize_pmu.py
python3 benchmarks/Alpha/results/signed-seek/summarize_code.py
sha256sum -c benchmarks/Alpha/results/signed-seek/SHA256SUMS
```

NativeAOT is covered here only as a correctness and package-consumer gate. C ABI and Unity/Burst qualification remain separate workstreams under issues #8 and #7 and are not evidence required to close the C# integration workstream.
