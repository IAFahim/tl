```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method                   | Job       | EnvironmentVariables       | Mean      | Error     | StdDev    | Median    | Ratio | Allocated | Alloc Ratio |
|------------------------- |---------- |--------------------------- |----------:|----------:|----------:|----------:|------:|----------:|------------:|
| InterpreterSingleTick    | Jit       | Empty                      | 15.344 ns | 0.0493 ns | 0.0356 ns | 15.340 ns |  1.00 |         - |          NA |
| CompiledSingleTick       | Jit       | Empty                      |  4.488 ns | 0.0182 ns | 0.0132 ns |  4.489 ns |  0.29 |         - |          NA |
| InterpreterBatch8        | Jit       | Empty                      | 11.229 ns | 0.0397 ns | 0.0263 ns | 11.231 ns |  0.73 |         - |          NA |
| CompiledBatch8           | Jit       | Empty                      |  4.104 ns | 0.0147 ns | 0.0097 ns |  4.104 ns |  0.27 |         - |          NA |
| SumInterpreterSingleTick | Jit       | Empty                      | 11.162 ns | 0.0825 ns | 0.0545 ns | 11.157 ns |  0.73 |         - |          NA |
| SumCompiledSingleTick    | Jit       | Empty                      |  2.475 ns | 0.0093 ns | 0.0073 ns |  2.473 ns |  0.16 |         - |          NA |
|                          |           |                            |           |           |           |           |       |           |             |
| InterpreterSingleTick    | NoTiering | DOTNET_TieredCompilation=0 | 28.321 ns | 0.3015 ns | 0.2180 ns | 28.283 ns |  1.00 |         - |          NA |
| CompiledSingleTick       | NoTiering | DOTNET_TieredCompilation=0 |  6.323 ns | 0.1181 ns | 0.0922 ns |  6.322 ns |  0.22 |         - |          NA |
| InterpreterBatch8        | NoTiering | DOTNET_TieredCompilation=0 | 16.823 ns | 0.0548 ns | 0.0428 ns | 16.830 ns |  0.59 |         - |          NA |
| CompiledBatch8           | NoTiering | DOTNET_TieredCompilation=0 |  5.964 ns | 0.1847 ns | 0.1442 ns |  5.891 ns |  0.21 |         - |          NA |
| SumInterpreterSingleTick | NoTiering | DOTNET_TieredCompilation=0 | 25.370 ns | 0.1417 ns | 0.1024 ns | 25.332 ns |  0.90 |         - |          NA |
| SumCompiledSingleTick    | NoTiering | DOTNET_TieredCompilation=0 |  2.480 ns | 0.0072 ns | 0.0052 ns |  2.480 ns |  0.09 |         - |          NA |
