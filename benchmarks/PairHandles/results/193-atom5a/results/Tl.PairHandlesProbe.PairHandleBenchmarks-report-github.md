```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |---------:|---------:|---------:|---------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          | **18.36 μs** | **0.034 μs** | **0.031 μs** | **18.35 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          | 18.54 μs | 0.339 μs | 0.429 μs | 18.36 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              | **18.43 μs** | **0.094 μs** | **0.093 μs** | **18.42 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              | 18.06 μs | 0.047 μs | 0.037 μs | 18.05 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            | **20.40 μs** | **0.147 μs** | **0.130 μs** | **20.45 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            | 20.33 μs | 0.147 μs | 0.115 μs | 20.30 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     | **82.35 μs** | **0.529 μs** | **0.442 μs** | **82.33 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     | 79.22 μs | 0.480 μs | 0.426 μs | 79.33 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       | **17.48 μs** | **0.742 μs** | **0.729 μs** | **17.59 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 17.19 μs | 0.296 μs | 0.263 μs | 17.24 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     | **19.38 μs** | **0.139 μs** | **0.130 μs** | **19.35 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 19.50 μs | 0.252 μs | 0.235 μs | 19.47 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** | **64.33 μs** | **0.651 μs** | **0.578 μs** | **64.50 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] | 60.26 μs | 0.179 μs | 0.168 μs | 60.25 μs |       1 B |
