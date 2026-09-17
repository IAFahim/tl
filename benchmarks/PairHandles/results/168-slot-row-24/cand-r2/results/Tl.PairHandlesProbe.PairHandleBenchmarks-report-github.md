```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape            | Mean      | Error    | StdDev   | Median    | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |----------------- |----------:|---------:|---------:|----------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**      |  **15.74 μs** | **0.028 μs** | **0.028 μs** |  **15.74 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform      |  15.69 μs | 0.026 μs | 0.020 μs |  15.68 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**          |  **22.08 μs** | **1.814 μs** | **1.781 μs** |  **21.36 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne          |  20.30 μs | 0.024 μs | 0.023 μs |  20.30 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**        |  **55.77 μs** | **1.206 μs** | **1.184 μs** |  **55.79 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8        |  56.75 μs | 0.363 μs | 0.340 μs |  56.88 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8** | **122.73 μs** | **2.869 μs** | **2.818 μs** | **123.31 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8 | 129.70 μs | 1.653 μs | 1.546 μs | 129.94 μs |       1 B |
