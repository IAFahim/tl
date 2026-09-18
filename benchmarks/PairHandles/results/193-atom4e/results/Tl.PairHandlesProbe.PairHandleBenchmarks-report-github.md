```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |---------:|---------:|---------:|---------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          | **19.57 μs** | **1.601 μs** | **1.573 μs** | **18.38 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          | 18.59 μs | 0.371 μs | 0.381 μs | 18.48 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              | **18.29 μs** | **0.035 μs** | **0.033 μs** | **18.28 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              | 18.39 μs | 0.362 μs | 0.470 μs | 18.10 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            | **21.86 μs** | **1.701 μs** | **1.671 μs** | **21.54 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            | 20.40 μs | 0.092 μs | 0.072 μs | 20.38 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     | **80.10 μs** | **2.494 μs** | **2.449 μs** | **81.15 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     | 79.58 μs | 0.608 μs | 0.539 μs | 79.55 μs |       1 B |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       | **16.98 μs** | **0.260 μs** | **0.255 μs** | **16.85 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 17.17 μs | 0.116 μs | 0.103 μs | 17.19 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     | **19.86 μs** | **0.296 μs** | **0.262 μs** | **19.95 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 20.23 μs | 0.378 μs | 0.491 μs | 20.27 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** | **66.05 μs** | **1.057 μs** | **1.039 μs** | **66.09 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] | 62.16 μs | 1.239 μs | 1.159 μs | 61.69 μs |       1 B |
