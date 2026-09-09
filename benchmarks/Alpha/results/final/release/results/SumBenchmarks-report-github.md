```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=Jit  Toolchain=InProcessNoEmitToolchain  IterationCount=12  
IterationTime=250ms  WarmupCount=16  

```
| Method        | Pattern    | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------- |----------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **DirectScalar**  | **Sequential** | **0.6084 ns** | **0.0066 ns** | **0.0051 ns** | **0.6093 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| PublicScalar  | Sequential | 1.3815 ns | 0.0081 ns | 0.0059 ns | 1.3794 ns |  2.27 |    0.02 |         - |          NA |
| IndexedScalar | Sequential | 1.3764 ns | 0.0115 ns | 0.0083 ns | 1.3760 ns |  2.26 |    0.02 |         - |          NA |
| PublicBatch8  | Sequential | 1.3818 ns | 0.0097 ns | 0.0070 ns | 1.3801 ns |  2.27 |    0.02 |         - |          NA |
| IndexedBatch8 | Sequential | 1.3862 ns | 0.0140 ns | 0.0109 ns | 1.3851 ns |  2.28 |    0.03 |         - |          NA |
|               |            |           |           |           |           |       |         |           |             |
| **DirectScalar**  | **Random**     | **3.5324 ns** | **0.0485 ns** | **0.0379 ns** | **3.5302 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| PublicScalar  | Random     | 4.8891 ns | 0.0356 ns | 0.0278 ns | 4.8798 ns |  1.38 |    0.02 |         - |          NA |
| IndexedScalar | Random     | 4.9165 ns | 0.0856 ns | 0.0668 ns | 4.9208 ns |  1.39 |    0.02 |         - |          NA |
| PublicBatch8  | Random     | 4.8524 ns | 0.0329 ns | 0.0238 ns | 4.8538 ns |  1.37 |    0.02 |         - |          NA |
| IndexedBatch8 | Random     | 4.8891 ns | 0.0811 ns | 0.0633 ns | 4.8657 ns |  1.38 |    0.02 |         - |          NA |
