```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method          | Job       | EnvironmentVariables       | Clips | Sequential | Mean     | Error    | StdDev   | Median   | Ratio | Allocated | Alloc Ratio |
|---------------- |---------- |--------------------------- |------ |----------- |---------:|---------:|---------:|---------:|------:|----------:|------------:|
| **ScanSingle**      | **Jit**       | **Empty**                      | **16**    | **False**      | **31.36 ns** | **0.464 ns** | **0.335 ns** | **31.28 ns** |  **1.00** |         **-** |          **NA** |
| CountsSingle    | Jit       | Empty                      | 16    | False      | 30.83 ns | 0.206 ns | 0.149 ns | 30.81 ns |  0.98 |         - |          NA |
| ScanBatchFour   | Jit       | Empty                      | 16    | False      | 23.28 ns | 0.158 ns | 0.114 ns | 23.26 ns |  0.74 |         - |          NA |
| CountsBatchFour | Jit       | Empty                      | 16    | False      | 24.87 ns | 0.160 ns | 0.116 ns | 24.91 ns |  0.79 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| ScanSingle      | NoTiering | DOTNET_TieredCompilation=0 | 16    | False      | 40.61 ns | 0.553 ns | 0.432 ns | 40.70 ns |  1.00 |         - |          NA |
| CountsSingle    | NoTiering | DOTNET_TieredCompilation=0 | 16    | False      | 40.90 ns | 0.128 ns | 0.085 ns | 40.92 ns |  1.01 |         - |          NA |
| ScanBatchFour   | NoTiering | DOTNET_TieredCompilation=0 | 16    | False      | 31.06 ns | 0.293 ns | 0.229 ns | 31.12 ns |  0.77 |         - |          NA |
| CountsBatchFour | NoTiering | DOTNET_TieredCompilation=0 | 16    | False      | 31.20 ns | 0.219 ns | 0.171 ns | 31.11 ns |  0.77 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| **ScanSingle**      | **Jit**       | **Empty**                      | **16**    | **True**       | **25.43 ns** | **0.178 ns** | **0.129 ns** | **25.39 ns** |  **1.00** |         **-** |          **NA** |
| CountsSingle    | Jit       | Empty                      | 16    | True       | 25.16 ns | 0.200 ns | 0.156 ns | 25.21 ns |  0.99 |         - |          NA |
| ScanBatchFour   | Jit       | Empty                      | 16    | True       | 15.25 ns | 0.094 ns | 0.073 ns | 15.26 ns |  0.60 |         - |          NA |
| CountsBatchFour | Jit       | Empty                      | 16    | True       | 15.95 ns | 0.087 ns | 0.068 ns | 15.95 ns |  0.63 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| ScanSingle      | NoTiering | DOTNET_TieredCompilation=0 | 16    | True       | 27.26 ns | 0.291 ns | 0.228 ns | 27.30 ns |  1.00 |         - |          NA |
| CountsSingle    | NoTiering | DOTNET_TieredCompilation=0 | 16    | True       | 27.67 ns | 0.101 ns | 0.079 ns | 27.67 ns |  1.02 |         - |          NA |
| ScanBatchFour   | NoTiering | DOTNET_TieredCompilation=0 | 16    | True       | 16.95 ns | 0.238 ns | 0.186 ns | 16.99 ns |  0.62 |         - |          NA |
| CountsBatchFour | NoTiering | DOTNET_TieredCompilation=0 | 16    | True       | 17.46 ns | 0.223 ns | 0.174 ns | 17.47 ns |  0.64 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| **ScanSingle**      | **Jit**       | **Empty**                      | **64**    | **False**      | **40.85 ns** | **0.295 ns** | **0.230 ns** | **40.82 ns** |  **1.00** |         **-** |          **NA** |
| CountsSingle    | Jit       | Empty                      | 64    | False      | 41.25 ns | 0.152 ns | 0.110 ns | 41.27 ns |  1.01 |         - |          NA |
| ScanBatchFour   | Jit       | Empty                      | 64    | False      | 34.12 ns | 0.429 ns | 0.310 ns | 34.17 ns |  0.84 |         - |          NA |
| CountsBatchFour | Jit       | Empty                      | 64    | False      | 35.62 ns | 0.185 ns | 0.144 ns | 35.60 ns |  0.87 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| ScanSingle      | NoTiering | DOTNET_TieredCompilation=0 | 64    | False      | 51.45 ns | 0.361 ns | 0.281 ns | 51.36 ns |  1.00 |         - |          NA |
| CountsSingle    | NoTiering | DOTNET_TieredCompilation=0 | 64    | False      | 51.65 ns | 0.390 ns | 0.304 ns | 51.70 ns |  1.00 |         - |          NA |
| ScanBatchFour   | NoTiering | DOTNET_TieredCompilation=0 | 64    | False      | 40.73 ns | 0.169 ns | 0.122 ns | 40.72 ns |  0.79 |         - |          NA |
| CountsBatchFour | NoTiering | DOTNET_TieredCompilation=0 | 64    | False      | 41.09 ns | 0.240 ns | 0.188 ns | 41.08 ns |  0.80 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| **ScanSingle**      | **Jit**       | **Empty**                      | **64**    | **True**       | **25.66 ns** | **0.126 ns** | **0.091 ns** | **25.69 ns** |  **1.00** |         **-** |          **NA** |
| CountsSingle    | Jit       | Empty                      | 64    | True       | 25.46 ns | 0.154 ns | 0.111 ns | 25.50 ns |  0.99 |         - |          NA |
| ScanBatchFour   | Jit       | Empty                      | 64    | True       | 15.58 ns | 0.074 ns | 0.058 ns | 15.59 ns |  0.61 |         - |          NA |
| CountsBatchFour | Jit       | Empty                      | 64    | True       | 15.92 ns | 0.244 ns | 0.191 ns | 15.93 ns |  0.62 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| ScanSingle      | NoTiering | DOTNET_TieredCompilation=0 | 64    | True       | 27.91 ns | 0.332 ns | 0.259 ns | 27.83 ns |  1.00 |         - |          NA |
| CountsSingle    | NoTiering | DOTNET_TieredCompilation=0 | 64    | True       | 27.54 ns | 0.196 ns | 0.141 ns | 27.52 ns |  0.99 |         - |          NA |
| ScanBatchFour   | NoTiering | DOTNET_TieredCompilation=0 | 64    | True       | 17.15 ns | 0.112 ns | 0.081 ns | 17.17 ns |  0.61 |         - |          NA |
| CountsBatchFour | NoTiering | DOTNET_TieredCompilation=0 | 64    | True       | 16.94 ns | 0.058 ns | 0.042 ns | 16.93 ns |  0.61 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| **ScanSingle**      | **Jit**       | **Empty**                      | **512**   | **False**      | **51.06 ns** | **0.183 ns** | **0.143 ns** | **51.03 ns** |  **1.00** |         **-** |          **NA** |
| CountsSingle    | Jit       | Empty                      | 512   | False      | 51.85 ns | 0.487 ns | 0.352 ns | 51.95 ns |  1.02 |         - |          NA |
| ScanBatchFour   | Jit       | Empty                      | 512   | False      | 47.45 ns | 0.309 ns | 0.241 ns | 47.50 ns |  0.93 |         - |          NA |
| CountsBatchFour | Jit       | Empty                      | 512   | False      | 46.25 ns | 0.681 ns | 0.532 ns | 46.34 ns |  0.91 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| ScanSingle      | NoTiering | DOTNET_TieredCompilation=0 | 512   | False      | 62.27 ns | 0.362 ns | 0.262 ns | 62.29 ns |  1.00 |         - |          NA |
| CountsSingle    | NoTiering | DOTNET_TieredCompilation=0 | 512   | False      | 61.45 ns | 0.400 ns | 0.312 ns | 61.49 ns |  0.99 |         - |          NA |
| ScanBatchFour   | NoTiering | DOTNET_TieredCompilation=0 | 512   | False      | 52.76 ns | 0.359 ns | 0.280 ns | 52.76 ns |  0.85 |         - |          NA |
| CountsBatchFour | NoTiering | DOTNET_TieredCompilation=0 | 512   | False      | 54.18 ns | 0.462 ns | 0.361 ns | 54.09 ns |  0.87 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| **ScanSingle**      | **Jit**       | **Empty**                      | **512**   | **True**       | **30.74 ns** | **0.129 ns** | **0.101 ns** | **30.74 ns** |  **1.00** |         **-** |          **NA** |
| CountsSingle    | Jit       | Empty                      | 512   | True       | 33.84 ns | 0.193 ns | 0.150 ns | 33.81 ns |  1.10 |         - |          NA |
| ScanBatchFour   | Jit       | Empty                      | 512   | True       | 17.03 ns | 0.111 ns | 0.080 ns | 17.01 ns |  0.55 |         - |          NA |
| CountsBatchFour | Jit       | Empty                      | 512   | True       | 16.46 ns | 0.051 ns | 0.034 ns | 16.45 ns |  0.54 |         - |          NA |
|                 |           |                            |       |            |          |          |          |          |       |           |             |
| ScanSingle      | NoTiering | DOTNET_TieredCompilation=0 | 512   | True       | 40.69 ns | 0.273 ns | 0.198 ns | 40.76 ns |  1.00 |         - |          NA |
| CountsSingle    | NoTiering | DOTNET_TieredCompilation=0 | 512   | True       | 42.08 ns | 0.219 ns | 0.159 ns | 42.09 ns |  1.03 |         - |          NA |
| ScanBatchFour   | NoTiering | DOTNET_TieredCompilation=0 | 512   | True       | 17.50 ns | 0.184 ns | 0.143 ns | 17.53 ns |  0.43 |         - |          NA |
| CountsBatchFour | NoTiering | DOTNET_TieredCompilation=0 | 512   | True       | 18.27 ns | 0.109 ns | 0.079 ns | 18.26 ns |  0.45 |         - |          NA |
