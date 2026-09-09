```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JitCore4 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=JitCore4  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method            | Pattern    | Mean      | Error     | StdDev    | Median    | Ratio | Allocated | Alloc Ratio |
|------------------ |----------- |----------:|----------:|----------:|----------:|------:|----------:|------------:|
| **InterpreterSingle** | **Sequential** | **14.327 ns** | **0.0646 ns** | **0.0467 ns** | **14.322 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Sequential | 16.323 ns | 0.0115 ns | 0.0076 ns | 16.322 ns |  1.14 |         - |          NA |
| FusedSingle       | Sequential |  2.768 ns | 0.0137 ns | 0.0099 ns |  2.765 ns |  0.19 |         - |          NA |
| InterpreterBatch8 | Sequential |  9.785 ns | 0.0228 ns | 0.0165 ns |  9.785 ns |  0.68 |         - |          NA |
| CompiledBatch8    | Sequential |  8.861 ns | 0.0099 ns | 0.0065 ns |  8.862 ns |  0.62 |         - |          NA |
| FusedBatch8       | Sequential |  2.956 ns | 0.0056 ns | 0.0034 ns |  2.957 ns |  0.21 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Random**     | **21.546 ns** | **0.1093 ns** | **0.0790 ns** | **21.516 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Random     | 30.378 ns | 0.0396 ns | 0.0286 ns | 30.366 ns |  1.41 |         - |          NA |
| FusedSingle       | Random     | 10.033 ns | 0.0556 ns | 0.0402 ns | 10.024 ns |  0.47 |         - |          NA |
| InterpreterBatch8 | Random     | 19.355 ns | 0.1033 ns | 0.0747 ns | 19.327 ns |  0.90 |         - |          NA |
| CompiledBatch8    | Random     | 19.619 ns | 0.0455 ns | 0.0329 ns | 19.623 ns |  0.91 |         - |          NA |
| FusedBatch8       | Random     |  9.554 ns | 0.0455 ns | 0.0301 ns |  9.553 ns |  0.44 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Repeated**   | **16.810 ns** | **0.0240 ns** | **0.0174 ns** | **16.806 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Repeated   | 16.126 ns | 0.0158 ns | 0.0105 ns | 16.123 ns |  0.96 |         - |          NA |
| FusedSingle       | Repeated   |  2.746 ns | 0.0124 ns | 0.0082 ns |  2.742 ns |  0.16 |         - |          NA |
| InterpreterBatch8 | Repeated   | 12.338 ns | 0.0797 ns | 0.0576 ns | 12.356 ns |  0.73 |         - |          NA |
| CompiledBatch8    | Repeated   | 11.040 ns | 0.0217 ns | 0.0157 ns | 11.036 ns |  0.66 |         - |          NA |
| FusedBatch8       | Repeated   |  4.257 ns | 0.0118 ns | 0.0078 ns |  4.255 ns |  0.25 |         - |          NA |
