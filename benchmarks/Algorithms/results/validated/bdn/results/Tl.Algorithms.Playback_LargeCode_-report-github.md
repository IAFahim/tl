```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  steady : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=steady  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method         | Step | Mean       | Error     | StdDev    | Median     | Ratio | Allocated | Alloc Ratio |
|--------------- |----- |-----------:|----------:|----------:|-----------:|------:|----------:|------------:|
| **Binary**         | **1**    |  **5.8658 ns** | **0.0085 ns** | **0.0062 ns** |  **5.8671 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 1    |  1.1799 ns | 0.0007 ns | 0.0005 ns |  1.1800 ns |  0.20 |         - |          NA |
| Rank           | 1    |  1.5301 ns | 0.0013 ns | 0.0009 ns |  1.5302 ns |  0.26 |         - |          NA |
| GeneratedTree  | 1    |  2.8991 ns | 0.0072 ns | 0.0056 ns |  2.8979 ns |  0.49 |         - |          NA |
| Cursor         | 1    |  0.9591 ns | 0.0077 ns | 0.0056 ns |  0.9584 ns |  0.16 |         - |          NA |
| GeneratedState | 1    |  9.1547 ns | 0.0864 ns | 0.0625 ns |  9.1686 ns |  1.56 |         - |          NA |
|                |      |            |           |           |            |       |           |             |
| **Binary**         | **7**    |  **9.9400 ns** | **0.0103 ns** | **0.0075 ns** |  **9.9379 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 7    |  1.3538 ns | 0.0012 ns | 0.0008 ns |  1.3538 ns |  0.14 |         - |          NA |
| Rank           | 7    |  1.7228 ns | 0.0167 ns | 0.0100 ns |  1.7252 ns |  0.17 |         - |          NA |
| GeneratedTree  | 7    |  3.7422 ns | 0.0036 ns | 0.0028 ns |  3.7427 ns |  0.38 |         - |          NA |
| Cursor         | 7    |  1.2505 ns | 0.0063 ns | 0.0046 ns |  1.2519 ns |  0.13 |         - |          NA |
| GeneratedState | 7    | 12.5101 ns | 0.1692 ns | 0.1223 ns | 12.4607 ns |  1.26 |         - |          NA |
