# v0.6 verification

Date: 2026-09-09

| Gate | Result |
| --- | --- |
| Release solution build | 0 warnings, 0 errors |
| `Tl.Core.Tests` | 43 passed |
| `Tl.Gen.Tests` | 60 passed |
| Compiled/interpreter JIT parity | Passed 1,201-step forward, 1,201-step backward, mirror, 4,096 jumps, batch, gaps, and lifecycle battery |
| Runtime NativeAOT smoke | Passed |
| Named compiled timeline NativeAOT | ILC emitted a 2.0 MB native ELF; full parity battery passed |
| Dispatch verification | Passed |
| Algorithms verification | 812,544 exact trace comparisons plus aggregate receipt checks passed |
| Review movement verification | Passed |
| AOT harness JIT checks | Passed with stable checksums |
| Source budget | 29 files; 177,443 content bytes; 856 path bytes; 178,299/200,000 total |
| Package creation | `Tl.Runtime.0.6.0.nupkg` and `Tl.Gen.0.6.0.nupkg` created; compressed and uncompressed sizes recorded in the [archived report](plan.md) |
| Clean package consumer | Generated `PackageTimeline.g.cs`, built with no project references, returned `7`; application output contained no generator, Roslyn, or Waffle assembly |
| Repeat package build | Generator cache hit; only `PackageTimeline.g.cs` was compiled; no legacy shim emitted |
| Application package output | `Tl.Core.dll` is 40,448 bytes; generator, Roslyn, and Waffle assemblies are absent |

BenchmarkDotNet used .NET 10.0.11, x64 RyuJIT x86-64-v3, an i9-14900K, 16 warmups, 12 measured iterations, and 250 ms requested iteration time. Medians were 15.340 ns interpreter versus 4.489 ns compiled for the full scalar consumer, 11.231 ns versus 4.104 ns for full batch-8 throughput, and 11.157 ns versus 2.473 ns for the scalar sum consumer. Every invocation used fresh state and a fixed tick sequence, returned the complete result receipt, checked parity in setup, and reported zero managed allocation.

The retained benchmark artifacts are [full JSON](../../../benchmarks/Dispatch/results/v06-production-fusion-20260909/Tl.CompiledBench.CompiledVsInterpreter-report-full.json), [Markdown](../../../benchmarks/Dispatch/results/v06-production-fusion-20260909/Tl.CompiledBench.CompiledVsInterpreter-report-github.md), and [CSV](../../../benchmarks/Dispatch/results/v06-production-fusion-20260909/Tl.CompiledBench.CompiledVsInterpreter-report.csv). Their SHA-256 values are `4280dce6625fc1979caca791f88f2bc856696b63b48e09fc5472422e29c4186c`, `b06df6fd643ab5d79b852e7baf7208ea73effdc4f7183916ed4b5e65c6d0b78e`, and `fd860dabecd13f065b5b440b67bb58f910ff39f3a2640f34df0485e0c1b96b37` in that order.

The NativeAOT identification check used `file` on the published sample and confirmed an x86-64 ELF executable. The successful publish included the `Generating native code` ILC phase. A runtime-identifier-only publish was explicitly rejected as AOT evidence because it contained CoreCLR and no native application executable.
