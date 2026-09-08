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

The source hash printed by the experimental JIT and NativeAOT checks is `452d4ad76e15bbbd45c0491aea136433fb01c45aec4d10d074cc988e42917499`. NativeAOT correctness does not establish NativeAOT performance. Benchmark throughput and source-size tradeoffs are documented in `benchmarks/FusionHour/README.md`.
