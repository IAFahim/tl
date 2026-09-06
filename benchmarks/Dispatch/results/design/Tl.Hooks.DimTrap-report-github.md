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
| Overridden    | Jit       | Empty                      | 0.1763 ns | 0.0015 ns | 0.0010 ns | 0.1758 ns |  1.00 |    0.01 |      - |         - |          NA |
| NotOverridden | Jit       | Empty                      | 1.9172 ns | 0.0554 ns | 0.0401 ns | 1.9124 ns | 10.88 |    0.22 | 0.0013 |      24 B |          NA |
|               |           |                            |           |           |           |           |       |         |        |           |             |
| Overridden    | NoTiering | DOTNET_TieredCompilation=0 | 0.1761 ns | 0.0004 ns | 0.0003 ns | 0.1760 ns |  1.00 |    0.00 |      - |         - |          NA |
| NotOverridden | NoTiering | DOTNET_TieredCompilation=0 | 2.0095 ns | 0.0699 ns | 0.0462 ns | 2.0076 ns | 11.41 |    0.25 | 0.0013 |      24 B |          NA |
