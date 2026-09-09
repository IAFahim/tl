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
| **InterpreterSingle** | **Sequential** | **12.617 ns** | **0.0116 ns** | **0.0077 ns** | **12.614 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Sequential | 14.266 ns | 0.0110 ns | 0.0073 ns | 14.269 ns |  1.13 |         - |          NA |
| FusedSingle       | Sequential |  2.466 ns | 0.0024 ns | 0.0015 ns |  2.466 ns |  0.20 |         - |          NA |
| InterpreterBatch8 | Sequential |  8.551 ns | 0.0591 ns | 0.0391 ns |  8.538 ns |  0.68 |         - |          NA |
| CompiledBatch8    | Sequential |  7.001 ns | 0.0791 ns | 0.0523 ns |  6.992 ns |  0.55 |         - |          NA |
| FusedBatch8       | Sequential |  2.837 ns | 0.0035 ns | 0.0021 ns |  2.836 ns |  0.22 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Random**     | **20.202 ns** | **0.0405 ns** | **0.0293 ns** | **20.197 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Random     | 23.533 ns | 0.0442 ns | 0.0320 ns | 23.527 ns |  1.16 |         - |          NA |
| FusedSingle       | Random     |  8.378 ns | 0.0241 ns | 0.0160 ns |  8.381 ns |  0.41 |         - |          NA |
| InterpreterBatch8 | Random     | 17.418 ns | 0.0278 ns | 0.0201 ns | 17.414 ns |  0.86 |         - |          NA |
| CompiledBatch8    | Random     | 16.133 ns | 0.0457 ns | 0.0330 ns | 16.122 ns |  0.80 |         - |          NA |
| FusedBatch8       | Random     |  7.854 ns | 0.0185 ns | 0.0122 ns |  7.851 ns |  0.39 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Repeated**   | **14.198 ns** | **0.0126 ns** | **0.0083 ns** | **14.200 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Repeated   | 15.551 ns | 0.0153 ns | 0.0110 ns | 15.549 ns |  1.10 |         - |          NA |
| FusedSingle       | Repeated   |  2.366 ns | 0.0053 ns | 0.0035 ns |  2.365 ns |  0.17 |         - |          NA |
| InterpreterBatch8 | Repeated   |  9.498 ns | 0.0260 ns | 0.0188 ns |  9.502 ns |  0.67 |         - |          NA |
| CompiledBatch8    | Repeated   |  8.165 ns | 0.0436 ns | 0.0288 ns |  8.173 ns |  0.58 |         - |          NA |
| FusedBatch8       | Repeated   |  4.293 ns | 0.0099 ns | 0.0065 ns |  4.293 ns |  0.30 |         - |          NA |
