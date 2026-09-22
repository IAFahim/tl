```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |---------:|---------:|---------:|---------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          | 32.80 μs | 0.174 μs | 0.154 μs | 32.77 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              | 29.95 μs | 0.296 μs | 0.277 μs | 30.04 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            | 37.41 μs | 0.059 μs | 0.055 μs | 37.42 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     | 64.80 μs | 0.187 μs | 0.166 μs | 64.81 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 32.78 μs | 0.475 μs | 0.444 μs | 32.92 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 46.39 μs | 0.063 μs | 0.049 μs | 46.38 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** |       **NA** |       **NA** |       **NA** |       **NA** |        **NA** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] | 61.67 μs | 0.157 μs | 0.147 μs | 61.67 μs |         - |

Benchmarks with issues:
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=LaneUniform]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairOne]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairRuns8]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairAlternating8]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairRuns8Waves]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairBlocks8Waves]
  PairHandleBenchmarks.Advance: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=PairA(...)Waves [21]]
