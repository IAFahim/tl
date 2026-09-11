# Issue 19 schema routing evidence

Commit `9257ea2` is the exact `1b4c1a7` base plus the benchmark harness. Commit `540d5db` is the compact-router candidate. Both were built and measured on the same machine with .NET SDK 10.0.401 and .NET 10.0.12.

The benchmark generates its 273 timeline declarations under `obj` before compilation. BenchmarkDotNet times only the already-compiled dynamic seek workload. Every iteration consumes the success count and playback owner sum for 65,536 scalar zero-delta seeks. The three schemas have 1, 16, and 256 compatible targets; their declaration order places the 256-target schema across two modules.

| compatible targets | base mean | candidate mean | change | base 99.9% CI | candidate 99.9% CI | allocated |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 2.976 ns | 1.287 ns | -56.8% | 2.921–3.032 ns | 1.270–1.303 ns | 0 B/op |
| 16 | 3.438 ns | 3.548 ns | +3.2% | 3.411–3.466 ns | 3.397–3.699 ns | 0 B/op |
| 256 | 4.094 ns | 4.210 ns | +2.8% | 4.045–4.143 ns | 4.169–4.250 ns | 0 B/op |

The sole-target path materially improves because its schema router disappears. The 16 and 256 measurements do not show a latency improvement; the 16-target intervals overlap widely, while the 256-target run shows a small regression on this unpinned machine. The change is retained for its 83.2% shared-state reduction and the sole-target improvement.

An independent reviewer reran the exact commits consecutively on logical CPU 4. That pair measured 3.032 to 1.295 ns at one target, 3.539 to 3.471 ns at 16 targets, and 4.125 to 4.161 ns at 256 targets, all at 0 B/op. The repeat confirms the sole-target gain and places both multi-target changes within normal run variance.

| quantity | base | candidate | change |
|---|---:|---:|---:|
| generated UTF-8 bytes | 1,662,281 | 1,660,305 | -1,976 |
| declared shared routing bytes | 1,538 | 258 | -1,280 |
| generated files | 277 | 277 | 0 |
| timelines | 273 | 273 | 0 |

The base declares three 512-byte ushort maps and two module bytes. The candidate declares one 256-byte byte map for the only cross-module schema plus the same two module bytes. It emits no map or schema router for the sole target and no map for the 16-target one-module router.

FullOpts JIT assembly removes the 311-byte sole-target schema router. The 16-target router shrinks from 1,025 to 958 bytes. The 256-target router remains 6,482 bytes, while its map access changes from a zero-extended word load at `base + 2 * module` to a zero-extended byte load at `base + module`. The benchmark's `RunOne` body shrinks from 182 to 176 bytes; the other two bodies remain 182 bytes.

The NativeAOT Alpha fixture produces the same 1,680,792-byte ELF before and after this change. Its retained one-module schema router bodies shrink from 656 to 400 bytes and from 288 to 272 bytes. NativeAOT correctness passes the normal, 65,536-ID, and 256-module modes.

The 65,536-timeline acceptance check operates on the real emitter routing plan and emits only the selected schema. It verifies indices 0, 65,024, 65,280, and 65,535, byte value 255 for logical module 254, omission of module 255 from the map, and the module 255 direct comparison before the map. A one-schema full-capacity plan declares 512 shared routing bytes: 256 module identity bytes and one 256-byte map. It does not compile or register all 65,536 kernels. At the observed roughly 5.94 KB per empty generated timeline, that would create about 390 MB of source and permanently consume the process-lifetime ID and module registries, contaminating the remaining test process.

Compatible-router source and native code still grow linearly with compatible targets. The maximum router contains 65,536 ordinal case arms grouped under 256 module arms. This work compacts the fixed routing map; it does not remove per-target switch code. NativeAOT disassembly covers ordinary one-module routing. The module 255 boundary is proven by deterministic generated source and planner receipts rather than a 65,536-kernel NativeAOT build.

Reproduction:

```sh
dotnet build benchmarks/Routing/Routing.csproj -c Release -m:1
dotnet run --project benchmarks/Routing/Routing.csproj -c Release --no-build -- --verify
dotnet run --project benchmarks/Routing/Routing.csproj -c Release --no-build -- --filter '*RoutingBenchmarks.Dynamic*'
env COMPlus_TieredCompilation=0 COMPlus_JitDisasm='*TrySeek*' COMPlus_JitStdOutFile=jit.txt dotnet run --project benchmarks/Routing/Routing.csproj -c Release --no-build -- --verify
dotnet publish tests/Tl.Alpha/Tl.Alpha.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

Raw BenchmarkDotNet JSON and complete JIT router listings are retained beside this document. `summary.csv` and `code-size.csv` provide compact comparisons.
