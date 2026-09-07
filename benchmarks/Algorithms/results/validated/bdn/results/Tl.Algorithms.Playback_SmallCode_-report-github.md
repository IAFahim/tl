```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  steady : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=steady  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method         | Step | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------- |----- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **Binary**         | **1**    | **3.1748 ns** | **0.0175 ns** | **0.0116 ns** | **3.1746 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Dense          | 1    | 1.0198 ns | 0.0021 ns | 0.0015 ns | 1.0190 ns |  0.32 |    0.00 |         - |          NA |
| Rank           | 1    | 1.4042 ns | 0.0021 ns | 0.0015 ns | 1.4043 ns |  0.44 |    0.00 |         - |          NA |
| GeneratedTree  | 1    | 1.4602 ns | 0.0212 ns | 0.0166 ns | 1.4549 ns |  0.46 |    0.01 |         - |          NA |
| Cursor         | 1    | 0.8679 ns | 0.0055 ns | 0.0037 ns | 0.8688 ns |  0.27 |    0.00 |         - |          NA |
| GeneratedState | 1    | 0.8484 ns | 0.0006 ns | 0.0005 ns | 0.8484 ns |  0.27 |    0.00 |         - |          NA |
|                |      |           |           |           |           |       |         |           |             |
| **Binary**         | **7**    | **2.9633 ns** | **0.0674 ns** | **0.0487 ns** | **2.9458 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Dense          | 7    | 1.0315 ns | 0.0002 ns | 0.0001 ns | 1.0315 ns |  0.35 |    0.01 |         - |          NA |
| Rank           | 7    | 1.4501 ns | 0.0004 ns | 0.0002 ns | 1.4502 ns |  0.49 |    0.01 |         - |          NA |
| GeneratedTree  | 7    | 1.5900 ns | 0.0096 ns | 0.0075 ns | 1.5871 ns |  0.54 |    0.01 |         - |          NA |
| Cursor         | 7    | 0.9712 ns | 0.0036 ns | 0.0024 ns | 0.9711 ns |  0.33 |    0.01 |         - |          NA |
| GeneratedState | 7    | 0.9944 ns | 0.0007 ns | 0.0005 ns | 0.9946 ns |  0.34 |    0.01 |         - |          NA |
