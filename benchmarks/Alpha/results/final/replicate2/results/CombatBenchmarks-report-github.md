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
| **PublicScalar** | **Sequential** | **2.237 ns** | **0.0227 ns** | **0.0178 ns** | **2.239 ns** |         **-** |
| PublicBatch8 | Sequential | 2.033 ns | 0.0093 ns | 0.0073 ns | 2.033 ns |         - |
| **PublicScalar** | **Random**     | **7.042 ns** | **0.0598 ns** | **0.0467 ns** | **7.025 ns** |         **-** |
| PublicBatch8 | Random     | 6.607 ns | 0.0459 ns | 0.0358 ns | 6.601 ns |         - |
