# v1.0.0-alpha.2 performance evidence

The measured source is commit `b02687e7257a8a190e1494ad5b85f3889d1770b1`, tree `8a6f0981537d585e1ac402dc5980423af6918385`. Every timed method uses the public runtime-ID API, consumes success and playback state, mutates observable output, and is checked against the independent scalar oracle before timing.

## BenchmarkDotNet

Three independent processes ran on the i9-14900K with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8, 65,536 operations per invocation, 16 warmups, 12 measured iterations, requested 250 ms iterations, and MemoryDiagnoser.

| Public path | Pattern | Run 1 median | Run 2 median | Run 3 median | Median of medians | Allocated |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Sum scalar | Sequential | 1.396 ns | 1.395 ns | 1.400 ns | 1.396 ns | 0 B |
| Sum batch-8 | Sequential | 1.425 ns | 1.436 ns | 1.438 ns | 1.436 ns | 0 B |
| Combat scalar | Sequential | 2.210 ns | 2.247 ns | 2.290 ns | 2.247 ns | 0 B |
| Combat batch-8 | Sequential | 2.840 ns | 2.883 ns | 2.870 ns | 2.870 ns | 0 B |
| Sum scalar | Random | 4.972 ns | 4.826 ns | 4.988 ns | 4.972 ns | 0 B |
| Sum batch-8 | Random | 5.194 ns | 5.206 ns | 5.247 ns | 5.206 ns | 0 B |
| Combat scalar | Random | 7.186 ns | 7.085 ns | 7.077 ns | 7.085 ns | 0 B |
| Combat batch-8 | Random | 7.910 ns | 8.047 ns | 7.999 ns | 7.999 ns | 0 B |

All six sequential measurements for the two public scalar and batch paths are below 3 ns/tick. Batch values are throughput per tick across eight-tick calls. Random seeking remains a separate workload because unpredictable region selection is part of its cost.

Each `runN` directory retains the complete BenchmarkDotNet console log and full JSON for both fixtures. The JSON contains every workload sample, statistics, runtime and host information, GC result, allocation result, and benchmark identity.

## PMU

The checked-in collector pins each scenario to CPU 0 and reads a four-event Intel raw perf group with kernel and hypervisor activity excluded. Each scenario executes 2,147,483,648 ticks after warmup. The group ran at a 1.0 enabled/running fraction.

| Scenario | Cycles/tick | Instructions/tick | Branches/tick | Branch misses/tick |
| --- | ---: | ---: | ---: | ---: |
| Sum direct | 6.092 | 24.416 | 6.059 | 0.014692 |
| Sum public | 9.552 | 46.416 | 11.031 | 0.026084 |
| Combat direct | 10.912 | 60.911 | 11.367 | 0.000568 |
| Combat public | 14.659 | 85.535 | 16.186 | 0.013314 |

The raw counter output is [pmu.jsonl](pmu.jsonl). Reproduce it after a Release build with:

```sh
python3 benchmarks/Alpha/collect_pmu.py
```

## Generated code and assembly

The compiler emitted five files totaling 47,858 UTF-8 bytes. [generated.sha256](generated.sha256) identifies every generated file from the measured build. The two fixtures retain 13 and 26 bytes of declared static timeline data, and the runtime registry retains 8,192 bytes at its configured capacity.

[PublicPaths.asm](disassembly/PublicPaths.asm) contains diffable x64 full-optimization listings with tiering and ReadyToRun disabled for the complete public scalar benchmark methods, public batch wrappers, and generated batch kernels. The observed code sizes are recorded in [environment.json](environment.json).

The measurements could not elevate process priority, and the operating system controlled frequency. These results establish behavior on this exact machine, source, SDK, and runtime. They do not claim universal scalar latency, NativeAOT timing, or random-seek performance below 3 ns.

## Commands

```sh
dotnet build benchmarks/Alpha/Alpha.csproj -c Release -p:NuGetAudit=false
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
dotnet run --project benchmarks/Alpha -c Release --no-build -- --filter '*Public*' --artifacts /tmp/tl-alpha2-exact-b02687e-r1
dotnet run --project benchmarks/Alpha -c Release --no-build -- --filter '*Public*' --artifacts /tmp/tl-alpha2-exact-b02687e-r2
dotnet run --project benchmarks/Alpha -c Release --no-build -- --filter '*Public*' --artifacts /tmp/tl-alpha2-exact-b02687e-r3
python3 benchmarks/Alpha/collect_pmu.py
```
