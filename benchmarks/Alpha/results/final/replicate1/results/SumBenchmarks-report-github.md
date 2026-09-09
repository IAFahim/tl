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
| **PublicScalar** | **Sequential** | **1.386 ns** | **0.0177 ns** | **0.0138 ns** | **1.382 ns** |         **-** |
| PublicBatch8 | Sequential | 1.390 ns | 0.0197 ns | 0.0154 ns | 1.384 ns |         - |
| **PublicScalar** | **Random**     | **4.953 ns** | **0.0763 ns** | **0.0595 ns** | **4.964 ns** |         **-** |
| PublicBatch8 | Random     | 4.963 ns | 0.0651 ns | 0.0508 ns | 4.975 ns |         - |
