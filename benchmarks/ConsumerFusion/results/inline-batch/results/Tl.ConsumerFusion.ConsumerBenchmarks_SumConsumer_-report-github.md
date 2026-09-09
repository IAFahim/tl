```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JitCore4 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=JitCore4  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method      | Pattern    | Mean     | Error     | StdDev    | Median   | Allocated |
|------------ |----------- |---------:|----------:|----------:|---------:|----------:|
| **FusedSingle** | **Sequential** | **2.464 ns** | **0.0050 ns** | **0.0033 ns** | **2.463 ns** |         **-** |
| FusedBatch8 | Sequential | 1.196 ns | 0.0068 ns | 0.0049 ns | 1.195 ns |         - |
| **FusedSingle** | **Random**     | **8.261 ns** | **0.0440 ns** | **0.0291 ns** | **8.264 ns** |         **-** |
| FusedBatch8 | Random     | 7.105 ns | 0.1064 ns | 0.0769 ns | 7.071 ns |         - |
| **FusedSingle** | **Repeated**   | **2.365 ns** | **0.0023 ns** | **0.0013 ns** | **2.365 ns** |         **-** |
| FusedBatch8 | Repeated   | 1.133 ns | 0.0067 ns | 0.0044 ns | 1.133 ns |         - |
