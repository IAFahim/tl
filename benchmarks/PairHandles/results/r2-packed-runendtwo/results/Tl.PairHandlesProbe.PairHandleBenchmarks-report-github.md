```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean      | Error    | StdDev   | Median    | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |----------:|---------:|---------:|----------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          |  **18.41 μs** | **0.049 μs** | **0.041 μs** |  **18.42 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          |  18.50 μs | 0.368 μs | 0.452 μs |  18.29 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              |  **19.57 μs** | **1.652 μs** | **1.623 μs** |  **18.44 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              |  18.38 μs | 0.363 μs | 0.445 μs |  18.16 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            |  **19.68 μs** | **0.057 μs** | **0.051 μs** |  **19.67 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            |  31.00 μs | 0.085 μs | 0.066 μs |  30.98 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     |  **79.98 μs** | **2.365 μs** | **2.323 μs** |  **80.22 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     |  80.01 μs | 1.547 μs | 1.956 μs |  79.48 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       |  **16.94 μs** | **0.351 μs** | **0.344 μs** |  **16.97 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 191.38 μs | 1.898 μs | 1.775 μs | 190.93 μs |       1 B |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     | **201.67 μs** | **1.809 μs** | **1.692 μs** | **201.14 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 625.26 μs | 2.937 μs | 2.604 μs | 625.18 μs |       5 B |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** |  **65.46 μs** | **1.452 μs** | **1.426 μs** |  **65.36 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] |  61.39 μs | 0.633 μs | 0.561 μs |  61.39 μs |       1 B |
