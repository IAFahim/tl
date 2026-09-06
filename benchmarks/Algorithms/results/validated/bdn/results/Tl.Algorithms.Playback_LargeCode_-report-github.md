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
| **Binary**         | **1**    |  **6.0625 ns** | **0.0261 ns** | **0.0189 ns** |  **6.0573 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 1    |  1.1772 ns | 0.0028 ns | 0.0019 ns |  1.1768 ns |  0.19 |         - |          NA |
| Rank           | 1    |  1.5372 ns | 0.0021 ns | 0.0015 ns |  1.5374 ns |  0.25 |         - |          NA |
| GeneratedTree  | 1    |  3.0432 ns | 0.0046 ns | 0.0036 ns |  3.0422 ns |  0.50 |         - |          NA |
| Cursor         | 1    |  0.9832 ns | 0.0028 ns | 0.0020 ns |  0.9835 ns |  0.16 |         - |          NA |
| GeneratedState | 1    |  9.0507 ns | 0.0778 ns | 0.0562 ns |  9.0422 ns |  1.49 |         - |          NA |
|                |      |            |           |           |            |       |           |             |
| **Binary**         | **7**    | **10.7333 ns** | **0.0128 ns** | **0.0093 ns** | **10.7349 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 7    |  1.3552 ns | 0.0017 ns | 0.0011 ns |  1.3551 ns |  0.13 |         - |          NA |
| Rank           | 7    |  1.7643 ns | 0.0054 ns | 0.0039 ns |  1.7645 ns |  0.16 |         - |          NA |
| GeneratedTree  | 7    |  3.8546 ns | 0.0014 ns | 0.0009 ns |  3.8548 ns |  0.36 |         - |          NA |
| Cursor         | 7    |  1.2340 ns | 0.0026 ns | 0.0017 ns |  1.2336 ns |  0.11 |         - |          NA |
| GeneratedState | 7    | 15.3872 ns | 0.1827 ns | 0.1321 ns | 15.3123 ns |  1.43 |         - |          NA |
