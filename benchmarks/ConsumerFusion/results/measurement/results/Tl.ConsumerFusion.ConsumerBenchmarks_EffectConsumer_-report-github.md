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
| **InterpreterSingle** | **Sequential** | **17.477 ns** | **0.0190 ns** | **0.0137 ns** | **17.477 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Sequential | 17.897 ns | 0.0219 ns | 0.0158 ns | 17.898 ns |  1.02 |         - |          NA |
| FusedSingle       | Sequential |  5.162 ns | 0.0089 ns | 0.0064 ns |  5.160 ns |  0.30 |         - |          NA |
| InterpreterBatch8 | Sequential | 13.289 ns | 0.0383 ns | 0.0277 ns | 13.292 ns |  0.76 |         - |          NA |
| CompiledBatch8    | Sequential | 12.038 ns | 0.0385 ns | 0.0254 ns | 12.031 ns |  0.69 |         - |          NA |
| FusedBatch8       | Sequential |  4.664 ns | 0.0442 ns | 0.0320 ns |  4.653 ns |  0.27 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Random**     | **24.893 ns** | **0.0835 ns** | **0.0552 ns** | **24.903 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Random     | 28.213 ns | 0.0288 ns | 0.0209 ns | 28.213 ns |  1.13 |         - |          NA |
| FusedSingle       | Random     | 12.794 ns | 0.0689 ns | 0.0498 ns | 12.784 ns |  0.51 |         - |          NA |
| InterpreterBatch8 | Random     | 21.436 ns | 0.0764 ns | 0.0552 ns | 21.417 ns |  0.86 |         - |          NA |
| CompiledBatch8    | Random     | 22.185 ns | 0.0777 ns | 0.0562 ns | 22.188 ns |  0.89 |         - |          NA |
| FusedBatch8       | Random     | 12.543 ns | 0.0548 ns | 0.0396 ns | 12.529 ns |  0.50 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Repeated**   | **20.574 ns** | **0.0624 ns** | **0.0451 ns** | **20.571 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Repeated   | 19.498 ns | 0.0129 ns | 0.0085 ns | 19.497 ns |  0.95 |         - |          NA |
| FusedSingle       | Repeated   |  6.562 ns | 0.0980 ns | 0.0765 ns |  6.540 ns |  0.32 |         - |          NA |
| InterpreterBatch8 | Repeated   | 15.928 ns | 0.0617 ns | 0.0408 ns | 15.923 ns |  0.77 |         - |          NA |
| CompiledBatch8    | Repeated   | 14.287 ns | 0.0985 ns | 0.0712 ns | 14.242 ns |  0.69 |         - |          NA |
| FusedBatch8       | Repeated   |  6.021 ns | 0.0249 ns | 0.0180 ns |  6.026 ns |  0.29 |         - |          NA |
