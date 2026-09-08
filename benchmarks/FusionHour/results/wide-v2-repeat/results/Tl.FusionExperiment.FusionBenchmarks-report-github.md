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
| **InterpreterSingle** | **Sequential** | **11.048 ns** | **0.0185 ns** | **0.0122 ns** | **11.047 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Sequential | 18.514 ns | 0.0937 ns | 0.0678 ns | 18.479 ns |  1.68 |         - |          NA |
| FusedSingle       | Sequential |  2.523 ns | 0.0119 ns | 0.0079 ns |  2.524 ns |  0.23 |         - |          NA |
| InterpreterBatch8 | Sequential |  6.791 ns | 0.0186 ns | 0.0123 ns |  6.789 ns |  0.61 |         - |          NA |
| CompiledBatch8    | Sequential |  9.694 ns | 0.0202 ns | 0.0146 ns |  9.693 ns |  0.88 |         - |          NA |
| FusedBatch8       | Sequential |  1.334 ns | 0.0068 ns | 0.0045 ns |  1.334 ns |  0.12 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Random**     | **18.574 ns** | **0.1336 ns** | **0.0966 ns** | **18.538 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Random     | 26.025 ns | 0.0704 ns | 0.0509 ns | 26.017 ns |  1.40 |         - |          NA |
| FusedSingle       | Random     |  7.211 ns | 0.0285 ns | 0.0206 ns |  7.208 ns |  0.39 |         - |          NA |
| InterpreterBatch8 | Random     | 14.567 ns | 0.0345 ns | 0.0249 ns | 14.566 ns |  0.78 |         - |          NA |
| CompiledBatch8    | Random     | 18.999 ns | 0.0336 ns | 0.0243 ns | 19.006 ns |  1.02 |         - |          NA |
| FusedBatch8       | Random     |  7.292 ns | 0.0463 ns | 0.0307 ns |  7.291 ns |  0.39 |         - |          NA |
|                   |            |           |           |           |           |       |           |             |
| **InterpreterSingle** | **Repeated**   | **12.684 ns** | **0.0549 ns** | **0.0363 ns** | **12.683 ns** |  **1.00** |         **-** |          **NA** |
| CompiledSingle    | Repeated   | 20.674 ns | 0.0141 ns | 0.0102 ns | 20.671 ns |  1.63 |         - |          NA |
| FusedSingle       | Repeated   |  2.505 ns | 0.0029 ns | 0.0019 ns |  2.504 ns |  0.20 |         - |          NA |
| InterpreterBatch8 | Repeated   |  8.332 ns | 0.0121 ns | 0.0088 ns |  8.332 ns |  0.66 |         - |          NA |
| CompiledBatch8    | Repeated   | 12.142 ns | 0.1013 ns | 0.0732 ns | 12.103 ns |  0.96 |         - |          NA |
| FusedBatch8       | Repeated   |  1.249 ns | 0.0107 ns | 0.0071 ns |  1.246 ns |  0.10 |         - |          NA |
