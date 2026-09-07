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
| **Binary**         | **1**    | **4.4189 ns** | **0.0162 ns** | **0.0117 ns** | **4.4138 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 1    | 1.2186 ns | 0.0014 ns | 0.0010 ns | 1.2184 ns |  0.28 |         - |          NA |
| Rank           | 1    | 1.5087 ns | 0.0018 ns | 0.0013 ns | 1.5091 ns |  0.34 |         - |          NA |
| GeneratedTree  | 1    | 1.9195 ns | 0.0301 ns | 0.0235 ns | 1.9202 ns |  0.43 |         - |          NA |
| Cursor         | 1    | 1.0584 ns | 0.0032 ns | 0.0023 ns | 1.0584 ns |  0.24 |         - |          NA |
| GeneratedState | 1    | 1.7973 ns | 0.0050 ns | 0.0039 ns | 1.7977 ns |  0.41 |         - |          NA |
|                |      |           |           |           |           |       |           |             |
| **Binary**         | **7**    | **4.5542 ns** | **0.0072 ns** | **0.0052 ns** | **4.5541 ns** |  **1.00** |         **-** |          **NA** |
| Dense          | 7    | 1.0829 ns | 0.0003 ns | 0.0002 ns | 1.0829 ns |  0.24 |         - |          NA |
| Rank           | 7    | 1.4913 ns | 0.0014 ns | 0.0010 ns | 1.4914 ns |  0.33 |         - |          NA |
| GeneratedTree  | 7    | 2.2124 ns | 0.0250 ns | 0.0195 ns | 2.2077 ns |  0.49 |         - |          NA |
| Cursor         | 7    | 0.9550 ns | 0.0016 ns | 0.0011 ns | 0.9545 ns |  0.21 |         - |          NA |
| GeneratedState | 7    | 1.8137 ns | 0.0090 ns | 0.0070 ns | 1.8143 ns |  0.40 |         - |          NA |
