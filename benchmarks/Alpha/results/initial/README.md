# Alpha initial baseline

This baseline measures the generated heterogeneous public API at the dynamic `ushort` timeline boundary. Every arm executes 65,536 ticks and setup requires exact agreement for the complete playback state, output bits, callback state counts, and success count.

The run used BenchmarkDotNet 0.15.8, .NET 10.0.11, Tier-1 RyuJIT x86-64-v3, 16 warmups, 12 measured iterations, 250 ms iteration time, and `InProcessNoEmitToolchain`. The command was:

```sh
TL_ALPHA_IN_PROCESS=1 dotnet run --project benchmarks/Alpha/Alpha.csproj -c Release --no-build -- --filter '*Benchmarks*'
```

All measured arms allocated zero managed bytes. Sequential `PublicScalar` measured 1.5751 ns for Sum and 3.135 ns for heterogeneous Combat. Sequential `PublicBatch8` measured 1.4210 ns and 2.223 ns respectively. Random input raised the valid handwritten scalar floors to 3.6631 ns and 6.745 ns.

This is a historical development baseline. Its synthetic `DirectBatch8` arm repeated the scalar loop and was removed because it did not measure a distinct batch implementation. The final release evidence retains only the valid handwritten scalar oracle and the complete public/indexed scalar and batch paths.
