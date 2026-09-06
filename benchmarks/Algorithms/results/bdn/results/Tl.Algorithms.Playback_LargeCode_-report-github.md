```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  steady : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=steady  IterationCount=12  IterationTime=150ms  
WarmupCount=4  

```
| Method         | Step | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------- |----- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **Binary**         | **1**    |  **7.230 ns** | **1.4237 ns** | **1.1116 ns** |  **6.584 ns** |  **1.02** |    **0.20** |         **-** |          **NA** |
| Dense          | 1    |  1.258 ns | 0.0807 ns | 0.0630 ns |  1.304 ns |  0.18 |    0.03 |         - |          NA |
| Rank           | 1    |  1.675 ns | 0.1407 ns | 0.1098 ns |  1.759 ns |  0.24 |    0.04 |         - |          NA |
| GeneratedTree  | 1    |  7.522 ns | 4.4181 ns | 3.4494 ns |  9.535 ns |  1.06 |    0.49 |         - |          NA |
| Cursor         | 1    |  1.158 ns | 0.2091 ns | 0.1633 ns |  1.287 ns |  0.16 |    0.03 |         - |          NA |
| GeneratedState | 1    | 10.350 ns | 1.4120 ns | 1.1024 ns |  9.578 ns |  1.46 |    0.25 |         - |          NA |
|                |      |           |           |           |           |       |         |           |             |
| **Binary**         | **7**    | **10.115 ns** | **0.0163 ns** | **0.0118 ns** | **10.117 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Dense          | 7    |  1.483 ns | 0.1171 ns | 0.0914 ns |  1.553 ns |  0.15 |    0.01 |         - |          NA |
| Rank           | 7    |  1.959 ns | 0.0198 ns | 0.0155 ns |  1.963 ns |  0.19 |    0.00 |         - |          NA |
| GeneratedTree  | 7    |  8.791 ns | 4.7532 ns | 3.7110 ns | 11.048 ns |  0.87 |    0.35 |         - |          NA |
| Cursor         | 7    |  1.510 ns | 0.2759 ns | 0.2154 ns |  1.679 ns |  0.15 |    0.02 |         - |          NA |
| GeneratedState | 7    | 13.099 ns | 2.7918 ns | 2.1796 ns | 11.404 ns |  1.30 |    0.21 |         - |          NA |
