# v0.5 release validation

The complete local release gate passed on 2026-09-09 (Asia/Dhaka), using SDK 10.0.400 and runtime 10.0.11 on linux-x64.

| Gate | Result |
|---|---|
| Release solution build | Passed, warnings treated as errors |
| Core tests | 46 passed |
| Generator tests | 52 passed |
| Benchmark collector | 7 passed |
| Compiled sample | Interpreter/generated parity passed |
| Dispatch and edge verification | Passed |
| Algorithms, Review, AotBench checks | Passed |
| Runtime NativeAOT executable | Published and executed successfully |
| Experimental generator | Rejection self-tests passed; regenerated output identical |
| Experimental JIT and NativeAOT | 40,868 exact comparisons each, same embedded source hash |
| NuGet pack | Tl.Runtime 0.5.0 and Tl.Gen 0.5.0 created |
| Source budget | 186,154 / 200,000 bytes including paths |

Logs alongside this file record each command's result. NuGetAudit=false was command-scoped for this local validation; vulnerability auditing was not completed locally. CI and publishing retain their normal audit behavior.

The receipts below were originally committed at the `docs/verification` root by 104536d and moved here by #94; content is unchanged.

- `core-tests.log`: core test run, 46 passed.
- `generator-tests.log`: generator test run, 52 passed.
- `cache-final-tests.log`: warm-cache generator test rerun, 14 passed at that checkpoint.
- `dispatch-build.log`: release build of the compiled sample and dispatch suite with generated fixtures, 0 warnings, 0 errors.
- `dispatch-verify.log`: dispatch and edge verification program output.
- `compiled-aot-build.log`: NativeAOT publish of the compiled sample.
- `compiled-aot-parity.log`: interpreter versus generated parity pins.
- `package-noop.log`: package consumer rebuild with a generator cache hit.
- `package-parity.log`: installed package execution receipt.
- `core.sarif`, `generator.sarif`, `generator-inspection-initial.sarif`: Inspect Code reports retained from the review iterations.

The source hash printed by the experimental JIT and NativeAOT checks is `452d4ad76e15bbbd45c0491aea136433fb01c45aec4d10d074cc988e42917499`. NativeAOT correctness does not establish NativeAOT performance. Benchmark throughput and source-size tradeoffs are documented in `benchmarks/FusionHour/README.md`.
