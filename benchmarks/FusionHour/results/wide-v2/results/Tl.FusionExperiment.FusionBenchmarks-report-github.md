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
| **InterpreterSingle** | **Sequential** | **12.926 ns** | **0.1291 ns** | **0.0933 ns** | **12.885 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Sequential | 19.112 ns | 0.1745 ns | 0.1262 ns | 19.136 ns |  1.48 |         - |          NA |
| FusedSingle       | Sequential |  2.617 ns | 0.0348 ns | 0.0251 ns |  2.616 ns |  0.20 |         - |          NA |
| InterpreterBatch8 | Sequential |  7.841 ns | 0.0543 ns | 0.0393 ns |  7.846 ns |  0.61 |         - |          NA |
| CompiledBatch8    | Sequential | 10.460 ns | 0.1290 ns | 0.0933 ns | 10.471 ns |  0.81 |         - |          NA |
| FusedBatch8       | Sequential |  1.347 ns | 0.0159 ns | 0.0115 ns |  1.344 ns |  0.10 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Random**     | **18.626 ns** | **0.1387 ns** | **0.1003 ns** | **18.629 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Random     | 27.528 ns | 0.2227 ns | 0.1473 ns | 27.570 ns |  1.48 |         - |          NA |
| FusedSingle       | Random     |  7.608 ns | 0.0626 ns | 0.0453 ns |  7.599 ns |  0.41 |         - |          NA |
| InterpreterBatch8 | Random     | 15.565 ns | 0.2541 ns | 0.1837 ns | 15.595 ns |  0.84 |         - |          NA |
| CompiledBatch8    | Random     | 19.599 ns | 0.3598 ns | 0.2601 ns | 19.545 ns |  1.05 |         - |          NA |
| FusedBatch8       | Random     |  7.456 ns | 0.1183 ns | 0.0855 ns |  7.483 ns |  0.40 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Repeated**   | **13.551 ns** | **0.0522 ns** | **0.0377 ns** | **13.548 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Repeated   | 20.753 ns | 0.1110 ns | 0.0803 ns | 20.753 ns |  1.53 |         - |          NA |
| FusedSingle       | Repeated   |  2.584 ns | 0.0274 ns | 0.0181 ns |  2.583 ns |  0.19 |         - |          NA |
| InterpreterBatch8 | Repeated   |  8.771 ns | 0.0737 ns | 0.0533 ns |  8.761 ns |  0.65 |         - |          NA |
| CompiledBatch8    | Repeated   | 12.618 ns | 0.0616 ns | 0.0366 ns | 12.627 ns |  0.93 |         - |          NA |
| FusedBatch8       | Repeated   |  1.282 ns | 0.0161 ns | 0.0116 ns |  1.283 ns |  0.09 |         - |          NA |
