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
| **FusedSingle** | **Sequential** |  **5.151 ns** | **0.0027 ns** | **0.0018 ns** |  **5.150 ns** |         **-** |
| FusedBatch8 | Sequential |  4.658 ns | 0.0381 ns | 0.0276 ns |  4.659 ns |         - |
| **FusedSingle** | **Random**     | **12.440 ns** | **0.0379 ns** | **0.0274 ns** | **12.432 ns** |         **-** |
| FusedBatch8 | Random     | 12.968 ns | 0.0678 ns | 0.0490 ns | 12.942 ns |         - |
| **FusedSingle** | **Repeated**   |  **6.461 ns** | **0.0535 ns** | **0.0354 ns** |  **6.451 ns** |         **-** |
| FusedBatch8 | Repeated   |  5.853 ns | 0.0289 ns | 0.0191 ns |  5.845 ns |         - |
