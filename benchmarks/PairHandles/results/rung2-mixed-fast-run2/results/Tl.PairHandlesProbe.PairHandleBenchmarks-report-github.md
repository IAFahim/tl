```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |---------:|---------:|---------:|---------:|----------:|
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**          | **17.67 μs** | **1.519 μs** | **1.492 μs** | **18.23 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform          | 16.52 μs | 0.324 μs | 0.601 μs | 16.19 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairOne**              | **18.22 μs** | **0.025 μs** | **0.021 μs** | **18.22 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairOne              | 17.99 μs | 0.011 μs | 0.009 μs | 17.99 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8**            | **31.71 μs** | **0.079 μs** | **0.074 μs** | **31.70 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8            | 31.57 μs | 0.201 μs | 0.188 μs | 31.62 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairAlternating8**     | **82.99 μs** | **0.305 μs** | **0.254 μs** | **82.96 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairAlternating8     | 85.96 μs | 0.831 μs | 0.736 μs | 85.80 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairRuns8Waves**       | **17.42 μs** | **0.205 μs** | **0.201 μs** | **17.42 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairRuns8Waves       | 17.96 μs | 0.196 μs | 0.183 μs | 17.97 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairBlocks8Waves**     | **20.27 μs** | **0.306 μs** | **0.301 μs** | **20.33 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairBlocks8Waves     | 19.71 μs | 0.188 μs | 0.176 μs | 19.72 μs |         - |
| **Advance** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **PairA(...)Waves [21]** | **67.88 μs** | **0.692 μs** | **0.679 μs** | **68.13 μs** |         **-** |
| Advance | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | PairA(...)Waves [21] | 66.94 μs | 0.450 μs | 0.420 μs | 67.01 μs |         - |
