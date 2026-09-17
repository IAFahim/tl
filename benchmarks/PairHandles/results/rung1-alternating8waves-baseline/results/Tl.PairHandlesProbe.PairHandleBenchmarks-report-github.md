```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape            | Mean      | Error    | StdDev   | Median    | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |----------------- |----------:|---------:|---------:|----------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**      |  **18.42 μs** | **1.222 μs** | **1.200 μs** |  **18.83 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform      |  16.81 μs | 0.336 μs | 0.935 μs |  16.46 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**          |  **19.52 μs** | **1.618 μs** | **1.589 μs** |  **18.22 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne          |  18.11 μs | 0.357 μs | 0.721 μs |  17.67 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**        |  **55.68 μs** | **0.587 μs** | **0.577 μs** |  **56.02 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8        |  55.51 μs | 0.518 μs | 0.485 μs |  55.64 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8** | **119.72 μs** | **0.694 μs** | **0.649 μs** | **119.64 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8 | 118.52 μs | 0.878 μs | 0.821 μs | 118.67 μs |       1 B |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**   | **117.35 μs** | **0.689 μs** | **0.644 μs** | **117.37 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves   | 117.26 μs | 0.153 μs | 0.136 μs | 117.28 μs |       1 B |
