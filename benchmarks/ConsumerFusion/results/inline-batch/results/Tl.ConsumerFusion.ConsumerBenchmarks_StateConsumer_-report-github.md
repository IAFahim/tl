```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JitCore4 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=JitCore4  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method      | Pattern    | Mean      | Error     | StdDev    | Median    | Allocated |
|------------ |----------- |----------:|----------:|----------:|----------:|----------:|
| **FusedSingle** | **Sequential** |  **2.769 ns** | **0.0127 ns** | **0.0084 ns** |  **2.768 ns** |         **-** |
| FusedBatch8 | Sequential |  1.844 ns | 0.0044 ns | 0.0029 ns |  1.843 ns |         - |
| **FusedSingle** | **Random**     | **10.037 ns** | **0.0272 ns** | **0.0180 ns** | **10.039 ns** |         **-** |
| FusedBatch8 | Random     |  9.333 ns | 0.0372 ns | 0.0269 ns |  9.335 ns |         - |
| **FusedSingle** | **Repeated**   |  **2.746 ns** | **0.0107 ns** | **0.0071 ns** |  **2.743 ns** |         **-** |
| FusedBatch8 | Repeated   |  1.894 ns | 0.0111 ns | 0.0074 ns |  1.891 ns |         - |
