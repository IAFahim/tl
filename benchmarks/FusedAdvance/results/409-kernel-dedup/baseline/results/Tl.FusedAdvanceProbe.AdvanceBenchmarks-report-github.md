```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape               | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |-------------------- |----------:|---------:|---------:|----------:|------:|--------:|----------:|------------:|
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**         |  **27.85 μs** | **0.125 μs** | **0.116 μs** |  **27.87 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniform         |  27.99 μs | 0.393 μs | 0.386 μs |  27.82 μs |  1.01 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  27.62 μs | 0.107 μs | 0.100 μs |  27.60 μs |  1.00 |    0.00 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  27.73 μs | 0.218 μs | 0.204 μs |  27.65 μs |  1.00 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneWaves**           |  **30.40 μs** | **0.318 μs** | **0.313 μs** |  **30.35 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneWaves           |  30.58 μs | 0.456 μs | 0.448 μs |  30.62 μs |  1.01 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  29.19 μs | 0.398 μs | 0.352 μs |  29.11 μs |  1.00 |    0.02 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  30.02 μs | 0.398 μs | 0.372 μs |  30.19 μs |  1.03 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneStaggered**       |  **32.80 μs** | **0.452 μs** | **0.378 μs** |  **32.71 μs** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneStaggered       |  32.93 μs | 0.142 μs | 0.126 μs |  32.95 μs |  1.00 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  32.23 μs | 0.120 μs | 0.094 μs |  32.21 μs |  1.00 |    0.00 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  32.27 μs | 0.222 μs | 0.186 μs |  32.33 μs |  1.00 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniformBackward** |  **29.42 μs** | **0.126 μs** | **0.124 μs** |  **29.41 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniformBackward |  29.59 μs | 0.528 μs | 0.494 μs |  29.35 μs |  1.01 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |  29.05 μs | 0.276 μs | 0.258 μs |  29.00 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |  28.48 μs | 0.167 μs | 0.156 μs |  28.44 μs |  0.98 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetWavesOne**         |  **27.26 μs** | **0.038 μs** | **0.032 μs** |  **27.25 μs** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetWavesOne         |  27.18 μs | 0.040 μs | 0.033 μs |  27.17 μs |  1.00 |    0.00 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  27.77 μs | 0.101 μs | 0.089 μs |  27.75 μs |  1.00 |    0.00 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  27.02 μs | 0.106 μs | 0.099 μs |  26.99 μs |  0.97 |    0.00 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetStaggeredMixed**   | **150.14 μs** | **3.558 μs** | **3.494 μs** | **149.84 μs** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetStaggeredMixed   | 147.24 μs | 3.004 μs | 2.951 μs | 146.52 μs |  0.98 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 157.70 μs | 0.654 μs | 0.580 μs | 157.72 μs |  1.00 |    0.01 |       1 B |        1.00 |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 156.33 μs | 1.796 μs | 1.680 μs | 157.01 μs |  0.99 |    0.01 |       1 B |        1.00 |
