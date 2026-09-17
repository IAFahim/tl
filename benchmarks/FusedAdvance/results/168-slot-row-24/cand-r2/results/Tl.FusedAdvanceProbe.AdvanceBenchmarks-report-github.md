```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape               | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |-------------------- |----------:|---------:|---------:|----------:|------:|--------:|----------:|------------:|
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**         |  **11.08 μs** | **0.012 μs** | **0.011 μs** |  **11.08 μs** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniform         |  10.89 μs | 0.294 μs | 0.288 μs |  11.03 μs |  0.98 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  10.41 μs | 0.206 μs | 0.220 μs |  10.49 μs |  1.00 |    0.03 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  10.84 μs | 0.198 μs | 0.186 μs |  10.91 μs |  1.04 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneWaves**           |  **11.91 μs** | **0.239 μs** | **0.235 μs** |  **11.94 μs** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneWaves           |  12.24 μs | 0.315 μs | 0.309 μs |  12.35 μs |  1.03 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  12.07 μs | 0.230 μs | 0.283 μs |  11.98 μs |  1.00 |    0.03 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  11.78 μs | 0.176 μs | 0.165 μs |  11.72 μs |  0.98 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneStaggered**       |  **17.10 μs** | **1.554 μs** | **1.526 μs** |  **16.38 μs** |  **1.01** |    **0.12** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneStaggered       |  15.79 μs | 0.015 μs | 0.015 μs |  15.79 μs |  0.93 |    0.08 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  15.71 μs | 0.191 μs | 0.149 μs |  15.64 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  15.87 μs | 0.025 μs | 0.022 μs |  15.86 μs |  1.01 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniformBackward** |  **10.57 μs** | **0.315 μs** | **0.309 μs** |  **10.36 μs** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniformBackward |  10.53 μs | 0.333 μs | 0.327 μs |  10.33 μs |  1.00 |    0.04 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |  10.50 μs | 0.013 μs | 0.012 μs |  10.50 μs |  1.00 |    0.00 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |  10.91 μs | 0.213 μs | 0.237 μs |  11.03 μs |  1.04 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetWavesOne**         |  **14.95 μs** | **0.046 μs** | **0.045 μs** |  **14.95 μs** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetWavesOne         |  15.06 μs | 0.130 μs | 0.127 μs |  15.02 μs |  1.01 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  14.54 μs | 0.027 μs | 0.023 μs |  14.54 μs |  1.00 |    0.00 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  15.09 μs | 0.218 μs | 0.203 μs |  14.97 μs |  1.04 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetStaggeredMixed**   | **123.09 μs** | **3.161 μs** | **3.104 μs** | **123.51 μs** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetStaggeredMixed   | 116.44 μs | 0.435 μs | 0.428 μs | 116.53 μs |  0.95 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 119.46 μs | 1.712 μs | 1.601 μs | 120.27 μs |  1.00 |    0.02 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 131.43 μs | 1.453 μs | 1.359 μs | 131.67 μs |  1.10 |    0.02 |       1 B |          NA |
