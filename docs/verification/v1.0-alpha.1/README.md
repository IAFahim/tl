# v1.0.0-alpha.1 verification

Validated on 2026-09-10 from the tree tagged `v1.0.0-alpha.1`. The implementation base was v0.6.0 at `f3f91fe`. The release tag identifies the exact source because documentation and packages are part of the validated tree.

## Toolchain and host

| Property | Value |
| --- | --- |
| SDK | 10.0.401, commit `e34a38d2ae` |
| Runtime | .NET 10.0.12, x64 RyuJIT x86-64-v3 |
| MSBuild | 18.9.11 |
| Language | C# 14 |
| BenchmarkDotNet | 0.15.8 |
| OS | Omarchy 4.0.2, Linux 7.1.9-arch1-2 x86_64 |
| CPU | Intel Core i9-14900K, 24 physical cores, 32 logical CPUs |
| Cache | 896 KiB aggregate L1d, 1.3 MiB aggregate L1i, 32 MiB L2, 36 MiB L3 |
| Microcode | `0x137` |

`global.json` pins SDK 10.0.401 with latest-patch roll-forward. The benchmark host reported AVX2, BMI1/2, FMA, AVX-VNNI, and 256-bit vectors. BenchmarkDotNet could not elevate process priority; CPU affinity and fixed-frequency controls were not available. `perf` was not installed. The assembly listings are therefore the low-level evidence for this alpha, and the timing is a machine-specific result rather than a universal cycle bound.

## Build and correctness

The final gate sequence is:

```sh
python3 benchmarks/source_budget.py
python3 -m unittest discover -s benchmarks -p test_collect.py
dotnet build tl.slnx -c Release -m:1 -p:NuGetAudit=false
dotnet test tl.slnx -c Release --no-build -p:NuGetAudit=false
dotnet run --project tests/Tl.Alpha -c Release --no-build
dotnet run --project tests/Tl.Alpha -c Release --no-build -- --capacity
dotnet run --project tests/Tl.Alpha -c Release --no-build -- --module-capacity
dotnet run --project samples/Mixed -c Release --no-build
dotnet run --project benchmarks/Alpha -c Release --no-build -- --verify
```

The solution builds with zero warnings and zero errors. Core public API approval passes 1/1. Generator tests pass 22/22. The standalone receipts are:

```text
registry: 512 parallel unique IDs; 64 parallel unique modules; lazy=517 route=A5/5A
behavior: pose=Pose { X = 11, Y = 20.5 } health=Health { Value = 100 } trace=Trace { Calls = 6321, Enters = 69, Stays = 6054, Exits = 198 }
lifecycle: owner=0 tick=63 flags=Started, Completed size=12
allocation: 4096 scalar calls retained 0 B
capacity: 65536 live IDs, max=65535, overflow rejected
module capacity: first available=0, max=255, overflow rejected
```

The mixed sample returns `Pose { X = -2, Y = -1 }`, `Health { Value = 90 }`, and an owner-authenticated playback at tick 5. The benchmark verifier checks sequential and random patterns across 65,536 operations and requires exact receipts for direct, scalar, indexed, batch-8, and indexed batch-8 paths.

The semantic suite covers heterogeneous kinds, arbitrary context arity, ref/out normalization, managed object-field and array-element aliases through compacting GC, hooks, nested include order, alternate compatible IDs, missing-schema rejection, forged owner rejection, deterministic generation, cache invalidation, and diagnostics.

## Performance

Each run uses 65,536 operations per invocation, 16 warmups, 12 measured iterations, 250 ms iteration time, and `MemoryDiagnoser`. The table reports the median of three independent run medians for public methods. Each hot stream uses one runtime-loaded ID; a stream switching among 16 compatible definitions remains a separate unmeasured stress case.

| Consumer | Pattern | Public scalar | Public batch-8 | Allocation |
| --- | --- | ---: | ---: | ---: |
| Sum | Sequential | 1.382 ns/tick | 1.384 ns/tick | 0 B |
| Combat | Sequential | 2.239 ns/tick | 2.033 ns/tick | 0 B |
| Sum | Random | 4.964 ns/tick | 4.854 ns/tick | 0 B |
| Combat | Random | 6.974 ns/tick | 6.633 ns/tick | 0 B |

The complete final run measured handwritten scalar-oracle floors of 0.609 ns/tick for Sum and 1.512 ns/tick for Combat. Full results live under [benchmarks/Alpha/results/final](../../../benchmarks/Alpha/results/final/README.md). The focused Tier-1 OSR listings are 465 bytes for Sum scalar and 1,888 bytes for Combat scalar. Neither exact-schema hot body calls the registry, route resolver, compatibility router, or track callback. NativeAOT correctness is verified below; no NativeAOT timing claim is made.

The sub-3 ns target applies to hot sequential public playback. Random selection remains 4.854–6.974 ns/tick because data-dependent region selection and prediction are part of the requested work.

## Size

| Artifact | Bytes |
| --- | ---: |
| Production source content plus relative paths | 116,255 / 200,000 |
| Runtime assembly | 15,360 |
| Alpha test generated C# | 88,898 |
| Benchmark generated C# | 47,815 |
| Mixed sample generated C# | 26,549 |
| Alpha NativeAOT executable | 1,672,584 |
| Package-consumer NativeAOT executable | 1,279,072 |

The source metric is UTF-8 content bytes plus relative-path bytes and one separator byte for every non-ignored file under `src`. Generated application code and native runtime payloads are reported separately because they cannot honestly share the library-source denominator.

## NativeAOT and packages

The final alpha test NativeAOT executable passes normal, ID-capacity, and module-capacity modes with the same receipts as JIT.

A fresh consumer restores solely from the two packed artifacts, prints `7`, retains no Tl generator or Roslyn assembly in application output, and reports `TlGenCompile: cache hit` on its second build. Its package-only NativeAOT executable prints `7`.

`SHA256SUMS` is generated from the exact tagged packages and executables and attached to the GitHub prerelease with those assets. Package hashes cannot be embedded in this source commit because the packages themselves record this commit as `RepositoryCommit`.

## Alpha boundaries

- Include and compatible runtime routing require definitions visible in the same C# compilation. Referenced assemblies do not yet publish a schema manifest.
- Timeline declarations emitted by another source generator are unavailable to this pre-compilation build pass.
- The internal model still carries C# type and expression spellings. `Tl.Gen.CSharp` is an honest C# backend package; no language-neutral IR or other-language backend is claimed.
- Runtime topology mutation, arbitrary NativeAOT managed plugins, Unity Burst, cross-definition kernel interning, and a measured NativeAOT throughput result remain post-alpha work.
- The repository does not declare a license. Package metadata preserves that state rather than choosing one for the author.
