```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape            | Mean      | Error    | StdDev   | Median    | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |----------------- |----------:|---------:|---------:|----------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**      |  **16.87 μs** | **1.377 μs** | **1.353 μs** |  **15.96 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform      |  15.72 μs | 0.023 μs | 0.019 μs |  15.73 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**          |  **20.18 μs** | **0.037 μs** | **0.033 μs** |  **20.17 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne          |  20.29 μs | 0.134 μs | 0.132 μs |  20.25 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**        |  **56.91 μs** | **0.280 μs** | **0.234 μs** |  **56.97 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8        |  56.96 μs | 0.017 μs | 0.013 μs |  56.96 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8** | **123.95 μs** | **2.407 μs** | **2.364 μs** | **124.86 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8 | 130.57 μs | 0.218 μs | 0.204 μs | 130.62 μs |         - |
