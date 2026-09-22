```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |---------:|---------:|---------:|---------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          | **32.90 μs** | **0.610 μs** | **0.509 μs** | **32.71 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          | 32.60 μs | 0.057 μs | 0.045 μs | 32.59 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              | **32.72 μs** | **0.149 μs** | **0.139 μs** | **32.71 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              | 29.25 μs | 0.328 μs | 0.291 μs | 29.12 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            | **37.75 μs** | **0.391 μs** | **0.327 μs** | **37.83 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            | 37.36 μs | 0.064 μs | 0.056 μs | 37.35 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     | **64.94 μs** | **1.106 μs** | **1.086 μs** | **64.31 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     | 64.78 μs | 0.835 μs | 0.781 μs | 64.45 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       | **34.03 μs** | **0.513 μs** | **0.480 μs** | **33.89 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 32.20 μs | 0.030 μs | 0.023 μs | 32.20 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     | **45.96 μs** | **0.183 μs** | **0.153 μs** | **45.91 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 46.19 μs | 0.138 μs | 0.129 μs | 46.20 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** | **58.52 μs** | **1.024 μs** | **1.006 μs** | **58.79 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] | 62.06 μs | 0.045 μs | 0.042 μs | 62.05 μs |         - |
