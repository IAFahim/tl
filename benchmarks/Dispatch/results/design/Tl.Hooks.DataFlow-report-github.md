```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method         | Job       | EnvironmentVariables       | Mean      | Error     | StdDev    | Median    | Ratio | Allocated | Alloc Ratio |
|--------------- |---------- |--------------------------- |----------:|----------:|----------:|----------:|------:|----------:|------------:|
| DirectData     | Jit       | Empty                      | 0.3525 ns | 0.0003 ns | 0.0002 ns | 0.3524 ns |  1.00 |         - |          NA |
| RefGenericData | Jit       | Empty                      | 0.3523 ns | 0.0004 ns | 0.0003 ns | 0.3521 ns |  1.00 |         - |          NA |
|                |           |                            |           |           |           |           |       |           |             |
| DirectData     | NoTiering | DOTNET_TieredCompilation=0 | 0.3524 ns | 0.0002 ns | 0.0001 ns | 0.3524 ns |  1.00 |         - |          NA |
| RefGenericData | NoTiering | DOTNET_TieredCompilation=0 | 0.3535 ns | 0.0022 ns | 0.0016 ns | 0.3529 ns |  1.00 |         - |          NA |
