```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22631.6936/23H2/2023Update/SunValley3)
AMD Ryzen 5 8500G w/ Radeon 740M Graphics 3.55GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method  | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape               | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------- |----------- |----------------------- |--------------- |-------------- |------------ |-------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniform**         |   **8.407 μs** | **0.2680 μs** | **0.2632 μs** |   **8.398 μs** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniform         |   8.222 μs | 0.2620 μs | 0.2574 μs |   8.155 μs |  0.98 |    0.04 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |   9.998 μs | 0.0539 μs | 0.0421 μs |   9.999 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniform         |  10.684 μs | 0.0498 μs | 0.0416 μs |  10.700 μs |  1.07 |    0.01 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneWaves**           |  **13.423 μs** | **0.4828 μs** | **0.4280 μs** |  **13.398 μs** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneWaves           |  13.156 μs | 0.1965 μs | 0.1742 μs |  13.110 μs |  0.98 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  18.857 μs | 0.1847 μs | 0.1728 μs |  18.865 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneWaves           |  19.314 μs | 0.3243 μs | 0.2875 μs |  19.207 μs |  1.02 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneStaggered**       |  **31.499 μs** | **1.2805 μs** | **1.1978 μs** |  **31.440 μs** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneStaggered       |  32.732 μs | 0.9577 μs | 0.8958 μs |  32.637 μs |  1.04 |    0.05 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  41.100 μs | 0.3234 μs | 0.2701 μs |  41.058 μs |  1.00 |    0.01 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneStaggered       |  31.280 μs | 0.5149 μs | 0.8881 μs |  31.166 μs |  0.76 |    0.02 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **LaneUniformBackward** |   **8.215 μs** | **0.2252 μs** | **0.2211 μs** |   **8.168 μs** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | LaneUniformBackward |   8.182 μs | 0.2302 μs | 0.2153 μs |   8.094 μs |  1.00 |    0.04 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |   6.296 μs | 0.0980 μs | 0.0869 μs |   6.269 μs |  1.00 |    0.02 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | LaneUniformBackward |   7.967 μs | 0.1545 μs | 0.1839 μs |   7.940 μs |  1.27 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetWavesOne**         |  **10.512 μs** | **0.2166 μs** | **0.2127 μs** |  **10.456 μs** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetWavesOne         |  10.831 μs | 0.2960 μs | 0.2907 μs |  10.789 μs |  1.03 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |   8.947 μs | 0.1483 μs | 0.1315 μs |   8.909 μs |  1.00 |    0.02 |         - |          NA |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetWavesOne         |  10.522 μs | 0.1995 μs | 0.1960 μs |  10.548 μs |  1.18 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| **TwoCall** | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **SetStaggeredMixed**   | **130.951 μs** | **3.6745 μs** | **3.4371 μs** | **129.480 μs** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Fused   | Job-TZECNT | Default                | 16             | 200ms         | 8           | SetStaggeredMixed   | 131.082 μs | 2.2749 μs | 2.2343 μs | 130.952 μs |  1.00 |    0.03 |         - |          NA |
|         |            |                        |                |               |             |                     |            |           |           |            |       |         |           |             |
| TwoCall | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 132.151 μs | 2.5655 μs | 2.6346 μs | 132.461 μs |  1.00 |    0.03 |       1 B |        1.00 |
| Fused   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | SetStaggeredMixed   | 130.191 μs | 2.1039 μs | 1.7568 μs | 130.322 μs |  0.99 |    0.02 |       1 B |        1.00 |
