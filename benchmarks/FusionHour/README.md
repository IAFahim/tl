# One-hour consumer-fusion experiment

This worktree tests whether compiling the consumer operation together with a timeline removes the cost retained by the general generated view/callback path. It uses the current Pulse authoring: four tracks, nineteen clips, three blend regions, duration 600, gaps, and looping playback.

The interpreter, existing compiled kernel, and experimental fused kernel all perform the same ordered `float` additions in Forward and subtractions in Backward. Every active work contributes its resolved `Clip.Amount`; clip windows are start-inclusive and end-exclusive, and blends include both factor endpoints. This consumer never reads `ClipState`; its state calculations can therefore be removed. Playback tick, cycles, flags, lifecycle rejection, and cycle overflow remain observable and must match.

This is an experimental backend for an explicitly restricted operation and blend law. It accepts only `Pulse.Decls.Pulse`, `readonly record struct PulseClip(float Amount)`, float-literal payloads, looping playback, and the exact normalized `PulseTrack.Blend` expression `first.Amount + (second.Amount - first.Amount) * t`. Dense emission additionally requires duration at most 4096 and at most three active works, and is tested only with carry batching. It does not analyze arbitrary consumer C#, change the production API, or provide transparent `.Compile()` integration. Existing frozen benchmarks already explored consumer fusion; the contribution here is a generated Pulse backend with stronger contract checks and matched measurements against the current engines.

## Correctness

The verifier compares all Playback fields and accumulator bits, error types and argument names, and accumulator effects before a batch fails. It covers both directions, authored boundaries, gaps, repeated ticks, raw ticks through `uint.MaxValue`, cycle overflow and saturation, empty batches, stopped and unstarted playback, special floating-point initial values, batch composition, and a separate clip-list sampling oracle.

The default tree/carry kernel has no retained payload or work-slot arrays. The dense alternative retains 7,800 bytes of primitive count and ordered-operand data without managed arrays. Playback minting uses the existing experimental compiled path's little-endian eight-byte layout. Verification checks the size and field offsets. A supported public construction boundary is still required before this becomes a production API.

## Measurement contract

The main benchmark runs on logical CPU 4, a P-core of the i9-14900K, with its SMT sibling unused by our workers. Generator/build work uses CPU 17 or 19 and is paused during measurement. Affinity does not isolate shared package power or unrelated applications.

Each invocation starts from fresh playback and accumulator state and processes 65,536 ticks. Sequential ticks are raw values 0 through 65,535. Random ticks use xorshift32 seed `0xA312AFD5`, modulo 2400; this exact sequence accrues 61,434 cycles and fits the supported capacity. Repeated ticks are all 321. Batch calls receive eight ticks from the same array. All arms return the complete Playback plus the accumulator's exact float bits. Setup verifies equal receipts across all arms.

Five seconds of preparation exercises every arm before BenchmarkDotNet's sixteen warmup and twelve measurement iterations. Requested iteration time is 250 ms. Reports retain actual iterations and outlier handling. MemoryDiagnoser and the full JSON exporter are enabled. Results are amortized nanoseconds per tick, including consumer work; the scalar entry-point benchmark is still a loop throughput measurement, not an isolated-call latency claim.

The earlier `initial` run used a shorter 4,096-tick pattern and insufficient warmup for some interpreter cases. Keep it as exploratory evidence, not the headline comparison. Its very low random-seek estimate does not represent the larger workload.

Hardware-counter runs use Linux `perf_event_open` for cycles, instructions, branches, and branch misses on the warmed main thread. They include the loop and receipt overhead and exclude kernel/hypervisor execution. Raw counts, enabled/running time, scaling, and checksums are saved. These are separate runs from BenchmarkDotNet.

## Reproduction

From the worktree root, generate the selected backend, force a fresh build, verify its embedded source hash, and run:

```sh
python3 benchmarks/FusionHour/prepare.py --backend tree --batch carry
taskset -c 4 env NuGetAudit=false dotnet benchmarks/FusionHour/bin/Release/net10.0/FusionHour.dll --filter '*FusionBenchmarks*' --artifacts benchmarks/FusionHour/results/reproduction
```

Run each command only after the previous command succeeds. `NuGetAudit=false` is a command-local accommodation for the offline environment, not a shipped package setting.

Copying an earlier generated file with its old modification time can make a normal incremental build reuse a previous binary. The preparation script forces a rebuild and verifies the generated source hash printed by the executing assembly. Later benchmark logs print that hash too. BenchmarkDotNet's fresh generated-project builds produced the earlier timing reports, but initial stand-alone disassembly and verification were repeated after this timestamp hazard was found.

The `FusionChecks` project links the identical generated kernel, consumer, and verifier without BenchmarkDotNet dependencies so that the correctness battery can also run under NativeAOT.

The production `src` directory is unchanged by this experiment. Experimental generator size, emitted C# size, and native method sizes are separate costs; keeping experiments outside `src` does not make them free to ship under the 200,000-byte library budget.

## Final result

The default remains tree/carry. The independent repeat used the same generated SHA-256 `452d4ad76e15bbbd45c0491aea136433fb01c45aec4d10d074cc988e42917499` and confirmed the predictable batch result. Medians, nanoseconds per tick:

| Workload | Interpreter | General compiled | Fused tree/carry |
|---|---:|---:|---:|
| Sequential scalar | 11.047 | 18.479 | 2.524 |
| Sequential batch 8 | 6.789 | 9.693 | 1.334 |
| Random scalar | 18.538 | 26.017 | 7.208 |
| Random batch 8 | 14.566 | 19.006 | 7.291 |
| Repeated scalar | 12.683 | 20.671 | 2.504 |
| Repeated batch 8 | 8.332 | 12.103 | 1.246 |

All eighteen repeated arms measured zero managed allocations. These are complete ordered-sum and playback receipts, including per-call checks. The 1–2 ns result applies to predictable batches of this operation, not arbitrary consumers or isolated-call latency. There is no claim that NativeAOT has the same throughput.

[Original carry report](results/wide-v2/results/Tl.FusionExperiment.FusionBenchmarks-report-github.md), [repeat](results/wide-v2-repeat/results/Tl.FusionExperiment.FusionBenchmarks-report-github.md), [dense](results/wide-dense/results/Tl.FusionExperiment.FusionBenchmarks-report-github.md), and [rejected region-run variant](results/wide-v3/results/Tl.FusionExperiment.FusionBenchmarks-report-github.md) retain their complete matching arms. Full JSON is beside each report.

The dense backend's batch medians were 1.401 ns sequential, 6.544 ns random, and 1.549 ns repeated. Its 7,800-byte primitive operand/count data reduced random-region overhead, with a cost in predictable streams. It passed fresh JIT parity; NativeAOT parity for dense was not run. The region-run variant was slower in every stream and expands generated C# from 18,651 to 44,175 bytes. Neither replaces the default.

The earlier hardware-counter run in `results/wide-v2-source/counters.json` measured about 7.60 cycles/tick sequential batch, 40.09 random, and 7.08 repeated. Random traffic incurred about 1.339 branch misses/tick versus 0.0029 sequential. All counter groups ran without multiplexing. These separate counters explain why random throughput remains above the target without claiming an exact universal physical floor.

Final fresh-build JIT proof and chosen-backend disassembly are in `results/final-verification`. Earlier standalone assembly files whose build identity was uncertain are explicitly labeled unverified; do not use them for code-size comparisons. Release NativeAOT parity logs are in `docs/verification/v0.5` from the repository root. Source snapshots are excluded from compilation. The benchmark source hash identifies the emitted kernel, while snapshots and reports record the surrounding harness and environment.
