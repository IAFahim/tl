```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JitCore4 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=JitCore4  IterationCount=10  IterationTime=250ms  
WarmupCount=8  

```
| Method            | Pattern    | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |----------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **InterpreterSingle** | **Sequential** | **13.487 ns** | **3.9754 ns** | **2.6295 ns** | **11.664 ns** |  **1.03** |    **0.26** |         **-** |          **NA** |
| CompiledSingle    | Sequential | 18.355 ns | 0.0146 ns | 0.0087 ns | 18.351 ns |  1.40 |    0.24 |         - |          NA |
| FusedSingle       | Sequential |  2.534 ns | 0.0230 ns | 0.0152 ns |  2.532 ns |  0.19 |    0.03 |         - |          NA |
| InterpreterBatch8 | Sequential |  8.690 ns | 2.6206 ns | 1.7334 ns |  7.508 ns |  0.67 |    0.17 |         - |          NA |
| CompiledBatch8    | Sequential |  9.998 ns | 0.0276 ns | 0.0144 ns |  9.997 ns |  0.77 |    0.13 |         - |          NA |
| FusedBatch8       | Sequential |  2.420 ns | 0.0006 ns | 0.0004 ns |  2.420 ns |  0.19 |    0.03 |         - |          NA |
|                   |            |           |           |           |           |       |         |           |             |
| **InterpreterSingle** | **Random**     | **16.928 ns** | **0.1354 ns** | **0.0896 ns** | **16.916 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| CompiledSingle    | Random     | 25.365 ns | 0.3192 ns | 0.1900 ns | 25.348 ns |  1.50 |    0.01 |         - |          NA |
| FusedSingle       | Random     |  2.784 ns | 0.0021 ns | 0.0013 ns |  2.783 ns |  0.16 |    0.00 |         - |          NA |
| InterpreterBatch8 | Random     | 10.651 ns | 0.0681 ns | 0.0405 ns | 10.665 ns |  0.63 |    0.00 |         - |          NA |
| CompiledBatch8    | Random     | 16.555 ns | 0.0675 ns | 0.0353 ns | 16.571 ns |  0.98 |    0.01 |         - |          NA |
| FusedBatch8       | Random     |  2.768 ns | 0.0023 ns | 0.0015 ns |  2.768 ns |  0.16 |    0.00 |         - |          NA |
|                   |            |           |           |           |           |       |         |           |             |
| **InterpreterSingle** | **Repeated**   | **12.803 ns** | **0.0464 ns** | **0.0307 ns** | **12.817 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| CompiledSingle    | Repeated   | 20.494 ns | 0.0213 ns | 0.0126 ns | 20.492 ns |  1.60 |    0.00 |         - |          NA |
| FusedSingle       | Repeated   |  2.502 ns | 0.0008 ns | 0.0004 ns |  2.502 ns |  0.20 |    0.00 |         - |          NA |
| InterpreterBatch8 | Repeated   |  8.312 ns | 0.0424 ns | 0.0281 ns |  8.308 ns |  0.65 |    0.00 |         - |          NA |
| CompiledBatch8    | Repeated   | 12.036 ns | 0.0311 ns | 0.0163 ns | 12.034 ns |  0.94 |    0.00 |         - |          NA |
| FusedBatch8       | Repeated   |  4.505 ns | 0.0223 ns | 0.0147 ns |  4.504 ns |  0.35 |    0.00 |         - |          NA |
