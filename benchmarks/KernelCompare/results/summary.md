# Generated Pulse kernel experiment

All values are BenchmarkDotNet medians in ns/tick. Lower is better. Each invocation returns the final `Playback` fields and every `PulseResult` field. Managed allocations were zero in every measured method.

| Method | Baseline | Direct span | Change | Direction generic | Change |
|---|---:|---:|---:|---:|---:|
| Sequential single | 16.261 | 16.847 | +3.6% | 16.030 | -1.4% |
| Random single | 25.646 | 24.799 | -3.3% | 23.897 | -6.8% |
| Same tick single | 17.653 | 18.026 | +2.1% | 17.992 | +1.9% |
| Sequential batch 8 | 11.091 | 10.790 | -2.7% | 10.301 | -7.1% |
| Random batch 8 | 16.711 | 15.731 | -5.9% | 15.622 | -6.5% |
| Same tick batch 8 | 14.038 | 13.734 | -2.2% | 12.453 | -11.3% |

The direct-span candidate is rejected because it regresses both sequential and same-tick single calls. Its source patch is retained as `direct-span.patch`.

The direction-generic candidate is retained provisionally because it improves five of six shapes, including both sequential and random single calls and every batch shape. The 1.9% same-tick single regression remains a qualification for the serial control rerun.

The focused `--verify` mode compared the exact three streams through the interpreter and generated kernel, for both single and batch calls, and found bit-identical receipts.


## Serial control on logical CPU 4

Same six methods and observable receipts; 12 warmups, 12 target iterations, requested iteration time 1000 ms. Actual iteration durations, rejected outliers and all samples are retained in each run log and full JSON. The requested time is not a claim that every measured iteration lasted one second. `NuGetAudit=false` was scoped to these offline benchmark processes, with no project-wide audit suppression.

| Method | Baseline ns/tick | Direction generic ns/tick | Change |
|---|---:|---:|---:|
| Sequential single | 17.7955 | 15.9867 | -10.16% |
| Random single | 26.0599 | 23.9964 | -7.92% |
| Same tick single | 17.6664 | 17.9860 | +1.81% |
| Sequential batch 8 | 10.9658 | 10.2481 | -6.55% |
| Random batch 8 | 16.2844 | 15.3466 | -5.76% |
| Same tick batch 8 | 14.1344 | 12.4153 | -12.16% |

The repeat supports accepting direction specialization for these mixed/sequential workloads. The repeat-position single-call regression remains explicit. Sequential single improved much more in this session than the first one; do not treat its best percentage as a portable promise. All six measured allocation values remain zero.

The no-tiering disassembly is a separate code-size observation, not the tiered timing configuration: baseline shared Advance is 2,410 bytes; specialized Forward Advance is 2,223 bytes and Backward Advance is 1,292 bytes. The forward body shrinks by 7.8%, but a program using both directions materializes 3,515 bytes rather than 2,410, a 45.9% increase for these bodies. This tradeoff matters for many timelines/consumer closures.

The full Pulse consumer remains approximately 16 ns for sequential single calls and 10.25 ns for sequential batches in this run. This experiment does not establish 1–2 ns for the general callback contract.
