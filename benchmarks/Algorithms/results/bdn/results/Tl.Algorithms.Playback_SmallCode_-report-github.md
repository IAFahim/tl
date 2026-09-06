```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  steady : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=steady  IterationCount=12  IterationTime=150ms  
WarmupCount=4  

```
| Method         | Step | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------- |----- |---------:|----------:|----------:|---------:|------:|--------:|----------:|------------:|
| **Binary**         | **1**    | **3.569 ns** | **0.4302 ns** | **0.3359 ns** | **3.675 ns** |  **1.01** |    **0.13** |         **-** |          **NA** |
| Dense          | 1    | 1.154 ns | 0.1395 ns | 0.1089 ns | 1.182 ns |  0.33 |    0.04 |         - |          NA |
| Rank           | 1    | 1.641 ns | 0.0143 ns | 0.0095 ns | 1.637 ns |  0.46 |    0.04 |         - |          NA |
| GeneratedTree  | 1    | 3.457 ns | 2.0420 ns | 1.5942 ns | 4.470 ns |  0.98 |    0.44 |         - |          NA |
| Cursor         | 1    | 1.143 ns | 0.0038 ns | 0.0025 ns | 1.143 ns |  0.32 |    0.03 |         - |          NA |
| GeneratedState | 1    | 1.135 ns | 0.1913 ns | 0.1493 ns | 1.242 ns |  0.32 |    0.05 |         - |          NA |
|                |      |          |           |           |          |       |         |           |             |
| **Binary**         | **7**    | **3.650 ns** | **0.7469 ns** | **0.5832 ns** | **4.050 ns** |  **1.03** |    **0.23** |         **-** |          **NA** |
| Dense          | 7    | 1.145 ns | 0.1372 ns | 0.1071 ns | 1.199 ns |  0.32 |    0.06 |         - |          NA |
| Rank           | 7    | 1.582 ns | 0.1395 ns | 0.1089 ns | 1.663 ns |  0.44 |    0.08 |         - |          NA |
| GeneratedTree  | 7    | 4.889 ns | 0.0134 ns | 0.0104 ns | 4.886 ns |  1.37 |    0.22 |         - |          NA |
| Cursor         | 7    | 1.275 ns | 0.0027 ns | 0.0018 ns | 1.275 ns |  0.36 |    0.06 |         - |          NA |
| GeneratedState | 7    | 1.225 ns | 0.2587 ns | 0.2020 ns | 1.386 ns |  0.34 |    0.08 |         - |          NA |
