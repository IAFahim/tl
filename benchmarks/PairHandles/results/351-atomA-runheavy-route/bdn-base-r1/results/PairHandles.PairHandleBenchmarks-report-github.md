```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |---------:|---------:|---------:|---------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          | 32.75 μs | 0.112 μs | 0.094 μs | 32.70 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              | 30.36 μs | 0.108 μs | 0.096 μs | 30.35 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            | 37.51 μs | 0.059 μs | 0.055 μs | 37.50 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     | 66.43 μs | 0.200 μs | 0.187 μs | 66.48 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 33.25 μs | 0.129 μs | 0.114 μs | 33.26 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 47.51 μs | 0.168 μs | 0.149 μs | 47.55 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] | 62.07 μs | 0.110 μs | 0.103 μs | 62.11 μs |         - |

Benchmarks with issues:
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=LaneUniform]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairOne]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairRuns8]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairAlternating8]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairRuns8Waves]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairBlocks8Waves]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairA(...)Waves [21]]
