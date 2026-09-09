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
| **PublicScalar** | **Sequential** | **1.389 ns** | **0.0175 ns** | **0.0137 ns** | **1.385 ns** |         **-** |
| PublicBatch8 | Sequential | 1.392 ns | 0.0184 ns | 0.0143 ns | 1.395 ns |         - |
| **PublicScalar** | **Random**     | **5.016 ns** | **0.0722 ns** | **0.0563 ns** | **5.001 ns** |         **-** |
| PublicBatch8 | Random     | 4.615 ns | 0.0273 ns | 0.0180 ns | 4.610 ns |         - |
