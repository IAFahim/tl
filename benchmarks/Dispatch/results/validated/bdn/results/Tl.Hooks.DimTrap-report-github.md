```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method        | Job       | EnvironmentVariables       | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------- |---------- |--------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Overridden    | Jit       | Empty                      | 0.1758 ns | 0.0001 ns | 0.0000 ns | 0.1758 ns |  1.00 |    0.00 |      - |         - |          NA |
| NotOverridden | Jit       | Empty                      | 1.8104 ns | 0.0213 ns | 0.0154 ns | 1.8062 ns | 10.30 |    0.08 | 0.0013 |      24 B |          NA |
|               |           |                            |           |           |           |           |       |         |        |           |             |
| Overridden    | NoTiering | DOTNET_TieredCompilation=0 | 0.1757 ns | 0.0000 ns | 0.0000 ns | 0.1757 ns |  1.00 |    0.00 |      - |         - |          NA |
| NotOverridden | NoTiering | DOTNET_TieredCompilation=0 | 1.8055 ns | 0.0180 ns | 0.0141 ns | 1.8074 ns | 10.27 |    0.08 | 0.0013 |      24 B |          NA |
