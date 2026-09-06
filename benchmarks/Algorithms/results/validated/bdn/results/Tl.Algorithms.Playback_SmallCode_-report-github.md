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
| **Binary**         | **1**    | **3.1716 ns** | **0.0024 ns** | **0.0016 ns** | **3.1721 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 1    | 1.0150 ns | 0.0004 ns | 0.0002 ns | 1.0150 ns |  0.32 |         - |          NA |
| Rank           | 1    | 1.4449 ns | 0.0361 ns | 0.0282 ns | 1.4368 ns |  0.46 |         - |          NA |
| GeneratedTree  | 1    | 1.5012 ns | 0.0265 ns | 0.0207 ns | 1.5070 ns |  0.47 |         - |          NA |
| Cursor         | 1    | 0.8703 ns | 0.0033 ns | 0.0022 ns | 0.8707 ns |  0.27 |         - |          NA |
| GeneratedState | 1    | 0.8532 ns | 0.0031 ns | 0.0022 ns | 0.8529 ns |  0.27 |         - |          NA |
|                |      |           |           |           |           |       |           |             |
| **Binary**         | **7**    | **2.9355 ns** | **0.0209 ns** | **0.0151 ns** | **2.9393 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 7    | 1.0252 ns | 0.0002 ns | 0.0002 ns | 1.0252 ns |  0.35 |         - |          NA |
| Rank           | 7    | 1.4498 ns | 0.0018 ns | 0.0012 ns | 1.4495 ns |  0.49 |         - |          NA |
| GeneratedTree  | 7    | 1.7048 ns | 0.0056 ns | 0.0041 ns | 1.7052 ns |  0.58 |         - |          NA |
| Cursor         | 7    | 0.9628 ns | 0.0036 ns | 0.0024 ns | 0.9626 ns |  0.33 |         - |          NA |
| GeneratedState | 7    | 0.9951 ns | 0.0016 ns | 0.0012 ns | 0.9953 ns |  0.34 |         - |          NA |
