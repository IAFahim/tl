```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape            | Mean     | Error   | StdDev  | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |----------------- |---------:|--------:|--------:|---------:|----------:|
| Advance | Job-TZECNT | Default                | 16             | 200ms         | 8           | PairBlocks8Waves | 202.1 μs | 0.58 μs | 0.57 μs | 202.0 μs |         - |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves | 196.1 μs | 1.07 μs | 1.00 μs | 196.3 μs |         - |
