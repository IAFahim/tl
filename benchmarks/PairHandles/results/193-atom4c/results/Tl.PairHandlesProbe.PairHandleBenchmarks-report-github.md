```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |---------:|---------:|---------:|---------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          | **18.40 μs** | **0.078 μs** | **0.073 μs** | **18.37 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          | 18.66 μs | 0.227 μs | 0.189 μs | 18.55 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              | **18.29 μs** | **0.070 μs** | **0.062 μs** | **18.27 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              | 18.33 μs | 0.229 μs | 0.191 μs | 18.31 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            | **20.24 μs** | **0.128 μs** | **0.120 μs** | **20.19 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            | 20.58 μs | 0.021 μs | 0.016 μs | 20.58 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     | **87.12 μs** | **1.848 μs** | **1.638 μs** | **87.76 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     | 82.85 μs | 1.651 μs | 2.088 μs | 83.67 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       | **17.94 μs** | **0.060 μs** | **0.057 μs** | **17.91 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 18.12 μs | 0.255 μs | 0.226 μs | 18.08 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     | **20.61 μs** | **0.070 μs** | **0.059 μs** | **20.61 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 20.99 μs | 0.248 μs | 0.232 μs | 20.85 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** | **62.37 μs** | **1.033 μs** | **0.966 μs** | **62.23 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] | 64.25 μs | 0.560 μs | 0.496 μs | 64.26 μs |         - |
