```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method              | Job       | EnvironmentVariables       | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |---------- |--------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Direct              | Jit       | Empty                      | 0.3523 ns | 0.0008 ns | 0.0005 ns | 0.3522 ns |  1.00 |    0.00 |      - |         - |          NA |
| GeneratedLink       | Jit       | Empty                      | 0.3518 ns | 0.0002 ns | 0.0002 ns | 0.3518 ns |  1.00 |    0.00 |      - |         - |          NA |
| Constrained         | Jit       | Empty                      | 0.3523 ns | 0.0009 ns | 0.0006 ns | 0.3521 ns |  1.00 |    0.00 |      - |         - |          NA |
| ExplicitConstrained | Jit       | Empty                      | 0.3522 ns | 0.0005 ns | 0.0004 ns | 0.3522 ns |  1.00 |    0.00 |      - |         - |          NA |
| CachedDelegate      | Jit       | Empty                      | 1.4143 ns | 0.0012 ns | 0.0008 ns | 1.4140 ns |  4.01 |    0.01 |      - |         - |          NA |
| BoxedOnce           | Jit       | Empty                      | 1.4116 ns | 0.0020 ns | 0.0016 ns | 1.4115 ns |  4.01 |    0.01 |      - |         - |          NA |
| MixedBoxes          | Jit       | Empty                      | 1.5886 ns | 0.0047 ns | 0.0037 ns | 1.5889 ns |  4.51 |    0.01 |      - |         - |          NA |
| EscapingBoxPerCall  | Jit       | Empty                      | 4.0084 ns | 0.0299 ns | 0.0198 ns | 4.0069 ns | 11.38 |    0.06 | 0.0021 |      40 B |          NA |
|                     |           |                            |           |           |           |           |       |         |        |           |             |
| Direct              | NoTiering | DOTNET_TieredCompilation=0 | 0.3521 ns | 0.0002 ns | 0.0001 ns | 0.3521 ns |  1.00 |    0.00 |      - |         - |          NA |
| GeneratedLink       | NoTiering | DOTNET_TieredCompilation=0 | 0.3519 ns | 0.0004 ns | 0.0003 ns | 0.3519 ns |  1.00 |    0.00 |      - |         - |          NA |
| Constrained         | NoTiering | DOTNET_TieredCompilation=0 | 0.3519 ns | 0.0002 ns | 0.0001 ns | 0.3519 ns |  1.00 |    0.00 |      - |         - |          NA |
| ExplicitConstrained | NoTiering | DOTNET_TieredCompilation=0 | 0.3518 ns | 0.0007 ns | 0.0005 ns | 0.3517 ns |  1.00 |    0.00 |      - |         - |          NA |
| CachedDelegate      | NoTiering | DOTNET_TieredCompilation=0 | 1.4068 ns | 0.0008 ns | 0.0006 ns | 1.4066 ns |  4.00 |    0.00 |      - |         - |          NA |
| BoxedOnce           | NoTiering | DOTNET_TieredCompilation=0 | 2.2840 ns | 0.0009 ns | 0.0007 ns | 2.2837 ns |  6.49 |    0.00 |      - |         - |          NA |
| MixedBoxes          | NoTiering | DOTNET_TieredCompilation=0 | 2.6445 ns | 0.0055 ns | 0.0043 ns | 2.6425 ns |  7.51 |    0.01 |      - |         - |          NA |
| EscapingBoxPerCall  | NoTiering | DOTNET_TieredCompilation=0 | 5.9413 ns | 0.0544 ns | 0.0424 ns | 5.9330 ns | 16.88 |    0.12 | 0.0021 |      40 B |          NA |
