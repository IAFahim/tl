```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=Jit  Toolchain=InProcessNoEmitToolchain  IterationCount=12  
IterationTime=250ms  WarmupCount=16  

```
| Method       | Pattern    | Mean     | Error     | StdDev    | Median   | Allocated |
|------------- |----------- |---------:|----------:|----------:|---------:|----------:|
| **PublicScalar** | **Sequential** | **2.218 ns** | **0.0361 ns** | **0.0261 ns** | **2.215 ns** |         **-** |
| PublicBatch8 | Sequential | 2.014 ns | 0.0139 ns | 0.0101 ns | 2.015 ns |         - |
| **PublicScalar** | **Random**     | **6.939 ns** | **0.1161 ns** | **0.0906 ns** | **6.908 ns** |         **-** |
| PublicBatch8 | Random     | 6.665 ns | 0.1210 ns | 0.0944 ns | 6.633 ns |         - |
