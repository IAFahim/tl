```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape               | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |-------------------- |----------:|---------:|---------:|----------:|------:|--------:|----------:|------------:|
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**         |  **11.03 μs** | **0.015 μs** | **0.014 μs** |  **11.03 μs** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniform         |  10.93 μs | 0.281 μs | 0.249 μs |  11.02 μs |  0.99 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  10.46 μs | 0.143 μs | 0.134 μs |  10.51 μs |  1.00 |    0.02 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  10.86 μs | 0.215 μs | 0.230 μs |  10.97 μs |  1.04 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneWaves**           |  **12.38 μs** | **0.236 μs** | **0.232 μs** |  **12.47 μs** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneWaves           |  12.61 μs | 0.100 μs | 0.099 μs |  12.60 μs |  1.02 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  12.73 μs | 0.187 μs | 0.175 μs |  12.75 μs |  1.00 |    0.02 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  12.35 μs | 0.219 μs | 0.205 μs |  12.26 μs |  0.97 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneStaggered**       |  **15.86 μs** | **0.066 μs** | **0.058 μs** |  **15.84 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneStaggered       |  15.92 μs | 0.161 μs | 0.126 μs |  15.87 μs |  1.00 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  15.68 μs | 0.111 μs | 0.087 μs |  15.64 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  15.85 μs | 0.169 μs | 0.173 μs |  15.80 μs |  1.01 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniformBackward** |  **10.92 μs** | **0.266 μs** | **0.249 μs** |  **11.02 μs** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniformBackward |  11.11 μs | 0.004 μs | 0.004 μs |  11.11 μs |  1.02 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |  10.36 μs | 0.206 μs | 0.275 μs |  10.51 μs |  1.00 |    0.04 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |  10.95 μs | 0.195 μs | 0.183 μs |  11.01 μs |  1.06 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetWavesOne**         |  **15.04 μs** | **0.169 μs** | **0.166 μs** |  **14.97 μs** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetWavesOne         |  15.01 μs | 0.091 μs | 0.090 μs |  14.97 μs |  1.00 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  14.58 μs | 0.030 μs | 0.026 μs |  14.58 μs |  1.00 |    0.00 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  15.04 μs | 0.129 μs | 0.121 μs |  15.01 μs |  1.03 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetStaggeredMixed**   | **124.17 μs** | **0.805 μs** | **0.791 μs** | **124.06 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetStaggeredMixed   | 123.29 μs | 0.636 μs | 0.624 μs | 123.11 μs |  0.99 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 126.58 μs | 1.675 μs | 1.567 μs | 127.28 μs |  1.00 |    0.02 |       1 B |        1.00 |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 137.40 μs | 0.907 μs | 0.848 μs | 137.69 μs |  1.09 |    0.01 |       1 B |        1.00 |
