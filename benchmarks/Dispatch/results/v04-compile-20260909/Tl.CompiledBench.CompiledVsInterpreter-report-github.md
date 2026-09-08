```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method                   | Job       | EnvironmentVariables       | Mean     | Error    | StdDev   | Median   | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------------- |---------- |--------------------------- |---------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| InterpreterSingleTick    | Jit       | Empty                      | 21.61 ns | 0.260 ns | 0.172 ns | 21.58 ns |  1.00 |     860 B |         - |          NA |
| CompiledSingleTick       | Jit       | Empty                      | 17.09 ns | 0.062 ns | 0.049 ns | 17.08 ns |  0.79 |        NA |         - |          NA |
| InterpreterBatch8        | Jit       | Empty                      | 14.45 ns | 0.079 ns | 0.062 ns | 14.42 ns |  0.67 |     886 B |         - |          NA |
| CompiledBatch8           | Jit       | Empty                      | 11.77 ns | 0.086 ns | 0.067 ns | 11.76 ns |  0.54 |        NA |         - |          NA |
| SumInterpreterSingleTick | Jit       | Empty                      | 18.19 ns | 0.130 ns | 0.094 ns | 18.19 ns |  0.84 |     860 B |         - |          NA |
| SumCompiledSingleTick    | Jit       | Empty                      | 19.29 ns | 0.129 ns | 0.100 ns | 19.24 ns |  0.89 |        NA |         - |          NA |
|                          |           |                            |          |          |          |          |       |           |           |             |
| InterpreterSingleTick    | NoTiering | DOTNET_TieredCompilation=0 | 29.32 ns | 0.175 ns | 0.137 ns | 29.32 ns |  1.00 |     861 B |         - |          NA |
| CompiledSingleTick       | NoTiering | DOTNET_TieredCompilation=0 | 28.76 ns | 0.177 ns | 0.138 ns | 28.74 ns |  0.98 |        NA |         - |          NA |
| InterpreterBatch8        | NoTiering | DOTNET_TieredCompilation=0 | 21.10 ns | 0.134 ns | 0.104 ns | 21.13 ns |  0.72 |     896 B |         - |          NA |
| CompiledBatch8           | NoTiering | DOTNET_TieredCompilation=0 | 18.93 ns | 0.074 ns | 0.054 ns | 18.93 ns |  0.65 |        NA |         - |          NA |
| SumInterpreterSingleTick | NoTiering | DOTNET_TieredCompilation=0 | 25.00 ns | 0.127 ns | 0.092 ns | 25.01 ns |  0.85 |     861 B |         - |          NA |
| SumCompiledSingleTick    | NoTiering | DOTNET_TieredCompilation=0 | 17.80 ns | 0.053 ns | 0.035 ns | 17.80 ns |  0.61 |        NA |         - |          NA |
