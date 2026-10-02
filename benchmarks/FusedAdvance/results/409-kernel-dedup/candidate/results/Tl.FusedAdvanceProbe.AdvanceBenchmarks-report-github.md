```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape               | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |-------------------- |----------:|---------:|---------:|----------:|------:|--------:|----------:|------------:|
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**         |  **28.00 μs** | **0.414 μs** | **0.407 μs** |  **27.89 μs** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniform         |  27.86 μs | 0.163 μs | 0.152 μs |  27.86 μs |  1.00 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  27.74 μs | 0.171 μs | 0.152 μs |  27.69 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  27.64 μs | 0.123 μs | 0.109 μs |  27.64 μs |  1.00 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneWaves**           |  **29.59 μs** | **0.445 μs** | **0.395 μs** |  **29.66 μs** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneWaves           |  30.28 μs | 0.393 μs | 0.386 μs |  30.34 μs |  1.02 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  29.34 μs | 0.485 μs | 0.454 μs |  29.19 μs |  1.00 |    0.02 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  29.61 μs | 0.424 μs | 0.397 μs |  29.40 μs |  1.01 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneStaggered**       |  **33.14 μs** | **0.178 μs** | **0.158 μs** |  **33.16 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneStaggered       |  32.83 μs | 0.221 μs | 0.185 μs |  32.88 μs |  0.99 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  32.27 μs | 0.106 μs | 0.089 μs |  32.27 μs |  1.00 |    0.00 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  31.63 μs | 0.597 μs | 0.586 μs |  31.42 μs |  0.98 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniformBackward** |  **29.40 μs** | **0.198 μs** | **0.194 μs** |  **29.37 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniformBackward |  29.26 μs | 0.188 μs | 0.176 μs |  29.25 μs |  1.00 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |  29.74 μs | 0.117 μs | 0.110 μs |  29.70 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |  28.61 μs | 0.087 μs | 0.082 μs |  28.62 μs |  0.96 |    0.00 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetWavesOne**         |  **27.36 μs** | **0.127 μs** | **0.119 μs** |  **27.32 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetWavesOne         |  27.27 μs | 0.053 μs | 0.047 μs |  27.27 μs |  1.00 |    0.00 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  27.73 μs | 0.098 μs | 0.087 μs |  27.73 μs |  1.00 |    0.00 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  27.02 μs | 0.159 μs | 0.149 μs |  26.98 μs |  0.97 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetStaggeredMixed**   | **147.74 μs** | **3.098 μs** | **3.043 μs** | **147.22 μs** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetStaggeredMixed   | 151.33 μs | 2.469 μs | 2.309 μs | 152.10 μs |  1.02 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |           |          |          |           |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 153.73 μs | 1.408 μs | 1.099 μs | 153.83 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 157.31 μs | 1.045 μs | 0.978 μs | 157.23 μs |  1.02 |    0.01 |       1 B |          NA |
