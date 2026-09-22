```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |---------:|---------:|---------:|---------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          | 32.69 μs | 0.117 μs | 0.098 μs | 32.67 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              | 29.57 μs | 0.301 μs | 0.267 μs | 29.58 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            | 37.47 μs | 0.069 μs | 0.065 μs | 37.48 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     | 66.70 μs | 0.216 μs | 0.192 μs | 66.71 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 33.22 μs | 0.070 μs | 0.066 μs | 33.23 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 47.80 μs | 0.057 μs | 0.054 μs | 47.79 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] | 62.23 μs | 0.071 μs | 0.066 μs | 62.24 μs |         - |

Benchmarks with issues:
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=LaneUniform]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairOne]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairRuns8]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairAlternating8]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairRuns8Waves]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairBlocks8Waves]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairA(...)Waves [21]]
