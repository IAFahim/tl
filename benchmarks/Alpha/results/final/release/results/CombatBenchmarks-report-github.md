```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=Jit  Toolchain=InProcessNoEmitToolchain  IterationCount=12  
IterationTime=250ms  WarmupCount=16  

```
| Method        | Pattern    | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|----------:|------------:|
| **DirectScalar**  | **Sequential** | **1.516 ns** | **0.0256 ns** | **0.0200 ns** | **1.512 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| PublicScalar  | Sequential | 2.319 ns | 0.0139 ns | 0.0108 ns | 2.318 ns |  1.53 |    0.02 |         - |          NA |
| IndexedScalar | Sequential | 2.221 ns | 0.0212 ns | 0.0153 ns | 2.224 ns |  1.47 |    0.02 |         - |          NA |
| PublicBatch8  | Sequential | 2.050 ns | 0.0107 ns | 0.0083 ns | 2.051 ns |  1.35 |    0.02 |         - |          NA |
| IndexedBatch8 | Sequential | 2.039 ns | 0.0201 ns | 0.0157 ns | 2.041 ns |  1.35 |    0.02 |         - |          NA |
|               |            |          |           |           |          |       |         |           |             |
| **DirectScalar**  | **Random**     | **6.826 ns** | **0.0848 ns** | **0.0662 ns** | **6.826 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| PublicScalar  | Random     | 6.985 ns | 0.0531 ns | 0.0351 ns | 6.974 ns |  1.02 |    0.01 |         - |          NA |
| IndexedScalar | Random     | 7.057 ns | 0.0956 ns | 0.0747 ns | 7.046 ns |  1.03 |    0.01 |         - |          NA |
| PublicBatch8  | Random     | 6.859 ns | 0.1049 ns | 0.0819 ns | 6.848 ns |  1.00 |    0.01 |         - |          NA |
| IndexedBatch8 | Random     | 6.841 ns | 0.1295 ns | 0.1011 ns | 6.806 ns |  1.00 |    0.02 |         - |          NA |
