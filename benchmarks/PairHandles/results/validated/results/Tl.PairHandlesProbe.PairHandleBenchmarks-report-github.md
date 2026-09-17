```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape            | Mean      | Error    | StdDev   | Median    | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |----------------- |----------:|---------:|---------:|----------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**      |  **15.92 μs** | **0.025 μs** | **0.025 μs** |  **15.91 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform      |  15.82 μs | 0.172 μs | 0.144 μs |  15.76 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**          |  **20.27 μs** | **0.055 μs** | **0.051 μs** |  **20.26 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne          |  20.32 μs | 0.190 μs | 0.211 μs |  20.26 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**        |  **57.04 μs** | **0.029 μs** | **0.024 μs** |  **57.04 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8        |  56.71 μs | 0.314 μs | 0.278 μs |  56.80 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8** | **132.59 μs** | **0.558 μs** | **0.522 μs** | **132.67 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8 | 134.19 μs | 1.780 μs | 1.665 μs | 134.57 μs |       1 B |
