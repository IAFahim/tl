# Alpha.3 semantic shape matrix

This evidence measures commit `5a98eda47c82ac9911414da4211792675e509285`, merged over `docs/27-alpha3-plan` commit `5776ee50f34ab58dc22825e4c903dfa4611419d9`. The changes are benchmark fixtures and evidence only; production source under `src` is unchanged.

Every generated arm performs the complete public `Catalog.Query.Tick` operation and returns consumed playback-state, output, order, game-tick, and call-count hashes. Setup requires exact agreement with an independent direct implementation. Forward generated arms call the default `Tick(uint)` or forward-many overload, and their direct controls use direction-specialized movement and effects. Alternating verification calls the signed overload and signed direct implementation. Fixed-shape direct route selection occurs outside each 4,096-tick loop. The mixed workload intentionally selects one of five assets for each of 256 rows and reports throughput per entity-tick.

The shape series covers looping 1-, 3-, 16-, and 256-track single assets, including the three-track A-B-A stage sequence. It also covers an inactive internal gap, a two-clip overlap through `IBlend<T>`, three borrowed input columns plus one output column, and a fixed 256-row stream cycling over five asset shapes. The 256-track case is separate from the 256-row mixed stream.

## Timing result

BenchmarkDotNet 0.15.8 ran three independent processes on .NET 10.0.12. Each process used 16 warmups, 12 measured iterations, a requested 250 ms iteration time, 4,096 operations per fixed/component invocation, `MemoryDiagnoser`, and CPU 8 affinity. Values are median-of-three process medians. The process-median range records between-process dispersion; [summary.csv](summary.csv) also retains each process mean, median, within-process standard deviation, and allocation result.

| Semantic shape | Direct ns/tick | Generated ns/tick | Generated process-median range | Allocation |
| --- | ---: | ---: | ---: | ---: |
| 1 track | 1.325 | 1.804 | 1.759–1.937 ns | 0 B |
| 3 tracks, A-B-A | 2.221 | 3.754 | 3.669–3.758 ns | 0 B |
| 16 tracks | 11.467 | 21.230 | 21.215–29.042 ns | 0 B |
| 256 tracks | 323.500 | 34,250.868 | 32,948.029–35,872.974 ns | 0 B |
| Internal gap | 1.191 | 1.997 | 1.896–3.958 ns | 0 B |
| Two-clip blend | 1.690 | 2.555 | 2.367–5.084 ns | 0 B |
| Three input columns | 1.493 | 2.256 | 2.099–2.259 ns | 0 B |
| 256 rows, 5 mixed assets | 5.233 | 34.650 | 34.562–38.040 ns | 0 B |

The strict `<3 ns` generated full-operation target holds for one track, gap, blend, and the three-input component fixture on the median-of-process-medians result. It is not met by 3 tracks, 16 tracks, 256 tracks, or the mixed-ID stream. Issue #10 therefore remains open. Run 3 showed material host drift in the gap, blend, and 16-track cases; the report keeps that distribution and uses the prespecified median-of-process-medians aggregation rather than selecting a faster sample.

## Default tiering and no-tiering

A separate paired process ran the same 16 arms under the default tiered runtime and with `DOTNET_TieredCompilation=0`. These are diagnostic single-process medians; they are separate from the three-process result above. [tiering.csv](tiering.csv) retains direct and generated means, medians, standard deviations, and allocations for both jobs.

| Semantic shape | Default generated | No-tiering generated |
| --- | ---: | ---: |
| 1 track | 1.967 ns | 3.414 ns |
| 3 tracks, A-B-A | 3.827 ns | 11.395 ns |
| 16 tracks | 29.386 ns | 46.915 ns |
| 256 tracks | 56,732.629 ns | 32,563.127 ns |
| Internal gap | 1.816 ns | 3.015 ns |
| Two-clip blend | 2.389 ns | 4.468 ns |
| Three input columns | 2.129 ns | 3.854 ns |
| 256 rows, 5 mixed assets | 35.366 ns | 58.351 ns |

All 32 paired-process results allocate `0 B`. The 256-track generated method is the exception to the smaller default-tiering result: its very large staged execution body is faster with tiering disabled in this process.

## Hardware counters

The collector pinned each child to CPU 8, excluded kernel and hypervisor events, and read cycles, retired instructions, branches, and branch misses as one `perf_event_open` group. Every group has running fraction `1.0`. Each direct/query pair used the same tick count and produced the same receipt. The 256-track pair used 8,192 warmup and 262,144 measured ticks, a count chosen and recorded before collection; every other pair used 4,194,304 warmup and 268,435,456 measured ticks. [pmu.jsonl](pmu.jsonl) is the raw record.

| Semantic shape | Path | Cycles/tick | Instructions/tick | Branches/tick | Misses/tick |
| --- | --- | ---: | ---: | ---: | ---: |
| 1 track | Direct | 13.614 | 67.466 | 8.921 | 0.017176 |
| 1 track | Generated | 42.188 | 181.048 | 29.830 | 0.018356 |
| 3 tracks | Direct | 20.968 | 106.938 | 9.410 | 0.017264 |
| 3 tracks | Generated | 51.804 | 216.290 | 35.393 | 0.017889 |
| 16 tracks | Direct | 76.664 | 300.893 | 9.589 | 0.003603 |
| 16 tracks | Generated | 146.157 | 537.661 | 74.603 | 0.003112 |
| 256 tracks | Direct | 2,449.575 | 11,689.669 | 542.721 | 0.382431 |
| 256 tracks | Generated | 193,104.124 | 611,686.261 | 120,509.354 | 398.440456 |
| Internal gap | Direct | 12.861 | 64.801 | 10.003 | 0.014176 |
| Internal gap | Generated | 42.068 | 166.100 | 29.007 | 0.057358 |
| Two-clip blend | Direct | 16.150 | 81.687 | 8.922 | 0.008436 |
| Two-clip blend | Generated | 44.076 | 199.579 | 30.900 | 0.018090 |
| Three input columns | Direct | 18.510 | 98.600 | 13.382 | 0.012986 |
| Three input columns | Generated | 44.048 | 196.827 | 32.328 | 0.018053 |
| 256 rows, 5 assets | Direct | 20.223 | 110.478 | 9.964 | 0.005267 |
| 256 rows, 5 assets | Generated | 215.361 | 1,286.273 | 301.902 | 0.155046 |

The generated path pays public state validation, asset routing, movement, lifecycle flags, ordered stage execution, component access where present, and commit. The gap and blend shapes show that inactive-region selection and blend construction add different work. The mixed stream retains per-row routing in both controls, while the generated query also preserves schema validation and stage-major ordering. At 256 tracks, the 32 KiB fusion budget deliberately selects the general staged scheduler; the counter and timing jump is measured behavior, not a scalar fast-path claim.

## Generated, JIT, and NativeAOT size

`benchmarks/Alpha/prepare_code_size.py` generated single-asset 1/16/256-track consumers from the measured production binaries. Source bytes are the UTF-8 sum of generated catalog and timeline files. Declared static data counts the generated track and clip values. FullOpts JIT and NativeAOT columns sum generated catalog/timeline text symbols while excluding the user `Job` and compiler-created state helper. Executable size is separate.

| Tracks | Generated source | Static data | FullOpts JIT generated code | NativeAOT generated code | AOT executable |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 10,187 B | 8 B | 2,848 B | 2,078 B | 3,734,856 B |
| 16 | 38,686 B | 128 B | 18,328 B | 9,900 B | 3,744,672 B |
| 256 | 298,075 B | 2,048 B | 87,959 B | 75,547 B | 3,837,488 B |

[code-size.csv](code-size.csv) retains those values. Each NativeAOT executable ran successfully and printed the expected deterministic receipt: `3576`, `2014190333107895056`, and `-4185154976049692416` for 1, 16, and 256 tracks respectively.

[ShapePaths.asm](disassembly/ShapePaths.asm) is the benchmark's full-optimization x64 listing with ReadyToRun and tiering disabled. The fixed 256-track forward direct method is 13,090 bytes. The generated staged forward path contains a 654-byte query scheduler and a 46,567-byte `ExecuteForward0` body; its 35,586-byte reverse body is retained because signed correctness is part of the fixture. These are distinct method sizes and are not added to present a single hot-path size.

The product source-and-path budget is `188676/250000` bytes. Generated consumer output is reported separately from that repository budget.

## Environment and reproduction

The host is Linux 7.2.3 on an Intel Core i9-14900K with SDK 10.0.401 and runtime 10.0.12. The governor reported `powersave`, turbo remained enabled, and BenchmarkDotNet could not raise process priority. [environment.json](environment.json) retains the complete environment record. The results establish behavior on this host and do not supply the ARM64 comparison still required by issue #10.

Build and verify exact receipts and warmed allocation:

```sh
dotnet restore benchmarks/Alpha/Alpha.csproj -p:NuGetAudit=false
dotnet build benchmarks/Alpha/Alpha.csproj -c Release --no-restore -m:1 -nr:false -p:NuGetAudit=false
dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --verify
```

Run three full timing processes, substituting `N` with `1`, `2`, and `3`:

```sh
NuGetAudit=false taskset --cpu-list 8 dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*ShapeCatalogQueryBenchmarks*' '*ComponentCatalogQueryBenchmarks*' '*MixedAssetCatalogQueryBenchmarks*' --artifacts /tmp/tl-alpha3-shape-matrix-runN
```

Run the paired tiering process, PMU matrix, and full-optimization disassembly:

```sh
NuGetAudit=false TL_ALPHA_NO_TIERING=1 taskset --cpu-list 8 dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*ShapeCatalogQueryBenchmarks*' '*ComponentCatalogQueryBenchmarks*' '*MixedAssetCatalogQueryBenchmarks*' --artifacts /tmp/tl-alpha3-shape-matrix-tiering
python3 benchmarks/Alpha/collect_pmu.py --cpu 8 --output /tmp/tl-alpha3-shape-matrix-pmu.jsonl
DOTNET_ReadyToRun=0 DOTNET_TieredCompilation=0 DOTNET_JitDisasm='*' DOTNET_JitDisasmAssemblies=Alpha DOTNET_JitStdOutFile=/tmp/tl-alpha3-shape-paths.asm taskset --cpu-list 8 dotnet benchmarks/Alpha/bin/Release/net10.0/Alpha.dll --verify
```

Create the code-size fixtures, then build each track count with generated-file output enabled, collect FullOpts disassembly, and publish NativeAOT. Substitute `N` with `1`, `16`, and `256`:

```sh
python3 benchmarks/Alpha/prepare_code_size.py --repository . --output /tmp/tl-alpha3-shape-size
dotnet build /tmp/tl-alpha3-shape-size/tracks-N/Scale.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=generated
DOTNET_ReadyToRun=0 DOTNET_TieredCompilation=0 DOTNET_JitDisasm='*' DOTNET_JitDisasmAssemblies=Scale DOTNET_JitStdOutFile=/tmp/tl-alpha3-shape-size/tracks-N/Scale.asm dotnet /tmp/tl-alpha3-shape-size/tracks-N/bin/Release/net10.0/Scale.dll
dotnet publish /tmp/tl-alpha3-shape-size/tracks-N/Scale.csproj -c Release -r linux-x64 --self-contained true -m:1 -nr:false -p:PublishAot=true -p:NuGetAudit=false -o /tmp/tl-alpha3-shape-size/tracks-N/publish
/tmp/tl-alpha3-shape-size/tracks-N/publish/Scale
nm -S --size-sort /tmp/tl-alpha3-shape-size/tracks-N/publish/Scale
sha256sum -c benchmarks/Alpha/results/v1.0.0-alpha.3-shape-matrix/SHA256SUMS
```
