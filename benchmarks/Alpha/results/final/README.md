# v1 alpha benchmark evidence

Measured with SDK 10.0.401 and .NET 10.0.12 using BenchmarkDotNet 0.15.8 on an Intel Core i9-14900K. Each benchmark uses 65,536 operations per invocation, 16 warmups, 12 measured iterations, 250 ms iteration time, and `MemoryDiagnoser`.

The public figures below are medians across the medians of three independent final runs. Every setup checks the public scalar, indexed, batch-8, indexed batch-8, and handwritten-oracle receipts bit for bit before measurement. Each case uses one runtime-loaded ID in its hot stream; this is not a many-ID dispatch result.

| Consumer | Pattern | Public scalar | Public batch-8 | Allocation |
| --- | --- | ---: | ---: | ---: |
| Sum | Sequential | 1.382 ns/tick | 1.384 ns/tick | 0 B |
| Combat | Sequential | 2.239 ns/tick | 2.033 ns/tick | 0 B |
| Sum | Random | 4.964 ns/tick | 4.854 ns/tick | 0 B |
| Combat | Random | 6.974 ns/tick | 6.633 ns/tick | 0 B |

The complete final run also measured the handwritten scalar oracle and indexed public facade:

| Consumer | Pattern | Handwritten oracle | Public scalar | Indexed | Public batch-8 | Indexed batch-8 |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Sum | Sequential | 0.609 ns | 1.379 ns | 1.376 ns | 1.380 ns | 1.385 ns |
| Combat | Sequential | 1.512 ns | 2.318 ns | 2.224 ns | 2.051 ns | 2.041 ns |
| Sum | Random | 3.530 ns | 4.880 ns | 4.921 ns | 4.854 ns | 4.866 ns |
| Combat | Random | 6.826 ns | 6.974 ns | 7.046 ns | 6.848 ns | 6.806 ns |

`release/`, `replicate1/`, and `replicate2/` retain the reports. `disassembly/` records the focused JIT listings. The Tier-1 OSR body is 465 bytes for Sum scalar and 1,888 bytes for Combat scalar. The exact path contains no registry, route, compatibility-router, or callback call; its only call is the cold range-check failure helper.

BenchmarkDotNet could not raise process priority on this machine. CPU affinity and fixed-frequency controls were not available, so these results establish the alpha result on this machine rather than a universal hardware bound. NativeAOT correctness was verified separately; this record makes no NativeAOT timing claim.
