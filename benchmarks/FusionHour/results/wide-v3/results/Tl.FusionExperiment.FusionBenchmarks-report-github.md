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
| **FusedBatch8** | **Sequential** | **2.848 ns** | **0.0190 ns** | **0.0126 ns** | **2.844 ns** |         **-** |
| **FusedBatch8** | **Random**     | **9.664 ns** | **0.2943 ns** | **0.2297 ns** | **9.515 ns** |         **-** |
| **FusedBatch8** | **Repeated**   | **4.303 ns** | **0.0550 ns** | **0.0398 ns** | **4.302 ns** |         **-** |
