```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JitCore4 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=JitCore4  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method            | Pattern    | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |----------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **InterpreterSingle** | **Sequential** | **11.125 ns** | **0.1138 ns** | **0.0823 ns** | **11.155 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| CompiledSingle    | Sequential | 18.498 ns | 0.0246 ns | 0.0163 ns | 18.494 ns |  1.66 |    0.01 |         - |          NA |
| FusedSingle       | Sequential |  2.374 ns | 0.0027 ns | 0.0019 ns |  2.373 ns |  0.21 |    0.00 |         - |          NA |
| InterpreterBatch8 | Sequential |  7.287 ns | 0.0252 ns | 0.0182 ns |  7.289 ns |  0.66 |    0.00 |         - |          NA |
| CompiledBatch8    | Sequential | 10.150 ns | 0.0585 ns | 0.0423 ns | 10.144 ns |  0.91 |    0.01 |         - |          NA |
| FusedBatch8       | Sequential |  1.400 ns | 0.0024 ns | 0.0015 ns |  1.401 ns |  0.13 |    0.00 |         - |          NA |
|                   |            |           |           |           |           |       |         |           |             |
| **InterpreterSingle** | **Random**     | **18.623 ns** | **0.2667 ns** | **0.2082 ns** | **18.499 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| CompiledSingle    | Random     | 25.873 ns | 0.6460 ns | 0.5043 ns | 25.701 ns |  1.39 |    0.03 |         - |          NA |
| FusedSingle       | Random     |  7.097 ns | 0.1960 ns | 0.1530 ns |  7.057 ns |  0.38 |    0.01 |         - |          NA |
| InterpreterBatch8 | Random     | 15.173 ns | 0.0194 ns | 0.0140 ns | 15.172 ns |  0.81 |    0.01 |         - |          NA |
| CompiledBatch8    | Random     | 18.664 ns | 0.0514 ns | 0.0371 ns | 18.677 ns |  1.00 |    0.01 |         - |          NA |
| FusedBatch8       | Random     |  6.544 ns | 0.0143 ns | 0.0094 ns |  6.544 ns |  0.35 |    0.00 |         - |          NA |
|                   |            |           |           |           |           |       |         |           |             |
| **InterpreterSingle** | **Repeated**   | **12.761 ns** | **0.0196 ns** | **0.0142 ns** | **12.757 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| CompiledSingle    | Repeated   | 20.430 ns | 0.0277 ns | 0.0200 ns | 20.423 ns |  1.60 |    0.00 |         - |          NA |
| FusedSingle       | Repeated   |  2.376 ns | 0.0030 ns | 0.0018 ns |  2.375 ns |  0.19 |    0.00 |         - |          NA |
| InterpreterBatch8 | Repeated   |  8.659 ns | 0.0459 ns | 0.0332 ns |  8.662 ns |  0.68 |    0.00 |         - |          NA |
| CompiledBatch8    | Repeated   | 12.520 ns | 0.0376 ns | 0.0272 ns | 12.515 ns |  0.98 |    0.00 |         - |          NA |
| FusedBatch8       | Repeated   |  1.551 ns | 0.0063 ns | 0.0042 ns |  1.549 ns |  0.12 |    0.00 |         - |          NA |
