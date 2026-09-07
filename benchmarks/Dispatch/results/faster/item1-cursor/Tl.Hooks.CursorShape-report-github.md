```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method              | Job       | EnvironmentVariables       | Clips | Sequential | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------- |---------- |--------------------------- |------ |----------- |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| **Single**              | **Jit**       | **Empty**                      | **16**    | **False**      | **31.53 ns** | **0.385 ns** | **0.279 ns** | **31.60 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| SingleCursor        | Jit       | Empty                      | 16    | False      | 25.40 ns | 0.132 ns | 0.103 ns | 25.43 ns |  0.81 |    0.01 |         - |          NA |
| SingleCursorInvalid | Jit       | Empty                      | 16    | False      | 26.34 ns | 0.163 ns | 0.128 ns | 26.28 ns |  0.84 |    0.01 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| Single              | NoTiering | DOTNET_TieredCompilation=0 | 16    | False      | 41.22 ns | 0.282 ns | 0.204 ns | 41.27 ns |  1.00 |    0.01 |         - |          NA |
| SingleCursor        | NoTiering | DOTNET_TieredCompilation=0 | 16    | False      | 41.31 ns | 0.223 ns | 0.174 ns | 41.34 ns |  1.00 |    0.01 |         - |          NA |
| SingleCursorInvalid | NoTiering | DOTNET_TieredCompilation=0 | 16    | False      | 42.52 ns | 0.310 ns | 0.242 ns | 42.55 ns |  1.03 |    0.01 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| **Single**              | **Jit**       | **Empty**                      | **16**    | **True**       | **18.62 ns** | **0.083 ns** | **0.060 ns** | **18.60 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| SingleCursor        | Jit       | Empty                      | 16    | True       | 10.96 ns | 0.049 ns | 0.038 ns | 10.95 ns |  0.59 |    0.00 |         - |          NA |
| SingleCursorInvalid | Jit       | Empty                      | 16    | True       | 12.40 ns | 0.044 ns | 0.029 ns | 12.40 ns |  0.67 |    0.00 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| Single              | NoTiering | DOTNET_TieredCompilation=0 | 16    | True       | 26.64 ns | 0.349 ns | 0.272 ns | 26.52 ns |  1.00 |    0.01 |         - |          NA |
| SingleCursor        | NoTiering | DOTNET_TieredCompilation=0 | 16    | True       | 26.01 ns | 0.207 ns | 0.161 ns | 26.04 ns |  0.98 |    0.01 |         - |          NA |
| SingleCursorInvalid | NoTiering | DOTNET_TieredCompilation=0 | 16    | True       | 27.54 ns | 0.157 ns | 0.122 ns | 27.57 ns |  1.03 |    0.01 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| **Single**              | **Jit**       | **Empty**                      | **64**    | **False**      | **37.79 ns** | **0.598 ns** | **0.467 ns** | **37.80 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| SingleCursor        | Jit       | Empty                      | 64    | False      | 34.66 ns | 0.391 ns | 0.305 ns | 34.56 ns |  0.92 |    0.01 |         - |          NA |
| SingleCursorInvalid | Jit       | Empty                      | 64    | False      | 32.94 ns | 0.310 ns | 0.242 ns | 32.91 ns |  0.87 |    0.01 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| Single              | NoTiering | DOTNET_TieredCompilation=0 | 64    | False      | 47.04 ns | 0.202 ns | 0.158 ns | 47.08 ns |  1.00 |    0.00 |         - |          NA |
| SingleCursor        | NoTiering | DOTNET_TieredCompilation=0 | 64    | False      | 49.46 ns | 0.849 ns | 0.663 ns | 49.40 ns |  1.05 |    0.01 |         - |          NA |
| SingleCursorInvalid | NoTiering | DOTNET_TieredCompilation=0 | 64    | False      | 47.82 ns | 0.334 ns | 0.242 ns | 47.77 ns |  1.02 |    0.01 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| **Single**              | **Jit**       | **Empty**                      | **64**    | **True**       | **19.27 ns** | **0.103 ns** | **0.081 ns** | **19.28 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| SingleCursor        | Jit       | Empty                      | 64    | True       | 11.45 ns | 0.038 ns | 0.030 ns | 11.45 ns |  0.59 |    0.00 |         - |          NA |
| SingleCursorInvalid | Jit       | Empty                      | 64    | True       | 13.15 ns | 0.059 ns | 0.046 ns | 13.15 ns |  0.68 |    0.00 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| Single              | NoTiering | DOTNET_TieredCompilation=0 | 64    | True       | 27.62 ns | 0.177 ns | 0.138 ns | 27.58 ns |  1.00 |    0.01 |         - |          NA |
| SingleCursor        | NoTiering | DOTNET_TieredCompilation=0 | 64    | True       | 26.38 ns | 0.192 ns | 0.150 ns | 26.40 ns |  0.96 |    0.01 |         - |          NA |
| SingleCursorInvalid | NoTiering | DOTNET_TieredCompilation=0 | 64    | True       | 29.25 ns | 0.160 ns | 0.116 ns | 29.25 ns |  1.06 |    0.01 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| **Single**              | **Jit**       | **Empty**                      | **512**   | **False**      | **47.82 ns** | **0.269 ns** | **0.195 ns** | **47.78 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| SingleCursor        | Jit       | Empty                      | 512   | False      | 45.77 ns | 0.156 ns | 0.113 ns | 45.80 ns |  0.96 |    0.00 |         - |          NA |
| SingleCursorInvalid | Jit       | Empty                      | 512   | False      | 44.22 ns | 0.101 ns | 0.073 ns | 44.23 ns |  0.92 |    0.00 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| Single              | NoTiering | DOTNET_TieredCompilation=0 | 512   | False      | 57.47 ns | 0.282 ns | 0.220 ns | 57.38 ns |  1.00 |    0.01 |         - |          NA |
| SingleCursor        | NoTiering | DOTNET_TieredCompilation=0 | 512   | False      | 60.19 ns | 0.304 ns | 0.237 ns | 60.24 ns |  1.05 |    0.01 |         - |          NA |
| SingleCursorInvalid | NoTiering | DOTNET_TieredCompilation=0 | 512   | False      | 58.96 ns | 0.155 ns | 0.112 ns | 58.96 ns |  1.03 |    0.00 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| **Single**              | **Jit**       | **Empty**                      | **512**   | **True**       | **30.91 ns** | **0.192 ns** | **0.150 ns** | **30.93 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| SingleCursor        | Jit       | Empty                      | 512   | True       | 11.53 ns | 0.033 ns | 0.026 ns | 11.52 ns |  0.37 |    0.00 |         - |          NA |
| SingleCursorInvalid | Jit       | Empty                      | 512   | True       | 28.22 ns | 0.529 ns | 0.413 ns | 28.16 ns |  0.91 |    0.01 |         - |          NA |
|                     |           |                            |       |            |          |          |          |          |       |         |           |             |
| Single              | NoTiering | DOTNET_TieredCompilation=0 | 512   | True       | 37.64 ns | 0.390 ns | 0.282 ns | 37.65 ns |  1.00 |    0.01 |         - |          NA |
| SingleCursor        | NoTiering | DOTNET_TieredCompilation=0 | 512   | True       | 26.07 ns | 0.209 ns | 0.163 ns | 26.11 ns |  0.69 |    0.01 |         - |          NA |
| SingleCursorInvalid | NoTiering | DOTNET_TieredCompilation=0 | 512   | True       | 37.14 ns | 0.177 ns | 0.138 ns | 37.16 ns |  0.99 |    0.01 |         - |          NA |
