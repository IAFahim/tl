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
| **Binary**         | **1**    | **4.537 ns** | **0.0792 ns** | **0.0471 ns** | **4.565 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Dense          | 1    | 1.303 ns | 0.0763 ns | 0.0596 ns | 1.343 ns |  0.29 |    0.01 |         - |          NA |
| Rank           | 1    | 1.679 ns | 0.1783 ns | 0.1392 ns | 1.785 ns |  0.37 |    0.03 |         - |          NA |
| GeneratedTree  | 1    | 3.696 ns | 1.9838 ns | 1.5488 ns | 4.437 ns |  0.81 |    0.33 |         - |          NA |
| Cursor         | 1    | 1.207 ns | 0.1609 ns | 0.1256 ns | 1.302 ns |  0.27 |    0.03 |         - |          NA |
| GeneratedState | 1    | 3.821 ns | 2.0585 ns | 1.6072 ns | 4.938 ns |  0.84 |    0.34 |         - |          NA |
|                |      |          |           |           |          |       |         |           |             |
| **Binary**         | **7**    | **5.395 ns** | **0.0102 ns** | **0.0074 ns** | **5.396 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Dense          | 7    | 1.241 ns | 0.1669 ns | 0.1303 ns | 1.340 ns |  0.23 |    0.02 |         - |          NA |
| Rank           | 7    | 1.683 ns | 0.2143 ns | 0.1673 ns | 1.801 ns |  0.31 |    0.03 |         - |          NA |
| GeneratedTree  | 7    | 4.126 ns | 2.2063 ns | 1.7225 ns | 4.903 ns |  0.76 |    0.31 |         - |          NA |
| Cursor         | 7    | 1.409 ns | 0.0047 ns | 0.0037 ns | 1.407 ns |  0.26 |    0.00 |         - |          NA |
| GeneratedState | 7    | 4.170 ns | 2.1243 ns | 1.6585 ns | 5.318 ns |  0.77 |    0.30 |         - |          NA |
