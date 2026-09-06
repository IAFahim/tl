```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  steady : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=steady  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method         | Step | Mean      | Error     | StdDev    | Median    | Ratio | Allocated | Alloc Ratio |
|--------------- |----- |----------:|----------:|----------:|----------:|------:|----------:|------------:|
| **Binary**         | **1**    | **4.1273 ns** | **0.0149 ns** | **0.0099 ns** | **4.1247 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 1    | 1.2035 ns | 0.0038 ns | 0.0025 ns | 1.2045 ns |  0.29 |         - |          NA |
| Rank           | 1    | 1.5086 ns | 0.0098 ns | 0.0065 ns | 1.5093 ns |  0.37 |         - |          NA |
| GeneratedTree  | 1    | 1.9563 ns | 0.0352 ns | 0.0275 ns | 1.9578 ns |  0.47 |         - |          NA |
| Cursor         | 1    | 1.0453 ns | 0.0064 ns | 0.0047 ns | 1.0449 ns |  0.25 |         - |          NA |
| GeneratedState | 1    | 1.7924 ns | 0.0028 ns | 0.0022 ns | 1.7932 ns |  0.43 |         - |          NA |
|                |      |           |           |           |           |       |           |             |
| **Binary**         | **7**    | **5.7989 ns** | **0.0138 ns** | **0.0100 ns** | **5.7970 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 7    | 1.0829 ns | 0.0008 ns | 0.0006 ns | 1.0828 ns |  0.19 |         - |          NA |
| Rank           | 7    | 1.4816 ns | 0.0018 ns | 0.0013 ns | 1.4810 ns |  0.26 |         - |          NA |
| GeneratedTree  | 7    | 2.2078 ns | 0.0119 ns | 0.0093 ns | 2.2059 ns |  0.38 |         - |          NA |
| Cursor         | 7    | 0.9592 ns | 0.0018 ns | 0.0013 ns | 0.9594 ns |  0.17 |         - |          NA |
| GeneratedState | 7    | 1.8183 ns | 0.0392 ns | 0.0306 ns | 1.8225 ns |  0.31 |         - |          NA |
