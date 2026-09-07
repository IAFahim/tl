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
| Direct              | Jit       | Empty                      | 0.3522 ns | 0.0007 ns | 0.0005 ns | 0.3521 ns |  1.00 |    0.00 |      - |         - |          NA |
| GeneratedLink       | Jit       | Empty                      | 0.3522 ns | 0.0007 ns | 0.0005 ns | 0.3522 ns |  1.00 |    0.00 |      - |         - |          NA |
| Constrained         | Jit       | Empty                      | 0.3519 ns | 0.0005 ns | 0.0004 ns | 0.3517 ns |  1.00 |    0.00 |      - |         - |          NA |
| ExplicitConstrained | Jit       | Empty                      | 0.3521 ns | 0.0004 ns | 0.0003 ns | 0.3520 ns |  1.00 |    0.00 |      - |         - |          NA |
| CachedDelegate      | Jit       | Empty                      | 1.4123 ns | 0.0003 ns | 0.0002 ns | 1.4122 ns |  4.01 |    0.01 |      - |         - |          NA |
| BoxedOnce           | Jit       | Empty                      | 1.4083 ns | 0.0005 ns | 0.0004 ns | 1.4082 ns |  4.00 |    0.01 |      - |         - |          NA |
| MixedBoxes          | Jit       | Empty                      | 1.7546 ns | 0.0042 ns | 0.0028 ns | 1.7546 ns |  4.98 |    0.01 |      - |         - |          NA |
| EscapingBoxPerCall  | Jit       | Empty                      | 4.1486 ns | 0.0527 ns | 0.0411 ns | 4.1426 ns | 11.78 |    0.11 | 0.0021 |      40 B |          NA |
|                     |           |                            |           |           |           |           |       |         |        |           |             |
| Direct              | NoTiering | DOTNET_TieredCompilation=0 | 0.3522 ns | 0.0007 ns | 0.0005 ns | 0.3520 ns |  1.00 |    0.00 |      - |         - |          NA |
| GeneratedLink       | NoTiering | DOTNET_TieredCompilation=0 | 0.3519 ns | 0.0002 ns | 0.0002 ns | 0.3520 ns |  1.00 |    0.00 |      - |         - |          NA |
| Constrained         | NoTiering | DOTNET_TieredCompilation=0 | 0.3519 ns | 0.0002 ns | 0.0002 ns | 0.3519 ns |  1.00 |    0.00 |      - |         - |          NA |
| ExplicitConstrained | NoTiering | DOTNET_TieredCompilation=0 | 0.3519 ns | 0.0002 ns | 0.0001 ns | 0.3519 ns |  1.00 |    0.00 |      - |         - |          NA |
| CachedDelegate      | NoTiering | DOTNET_TieredCompilation=0 | 1.4093 ns | 0.0006 ns | 0.0005 ns | 1.4092 ns |  4.00 |    0.01 |      - |         - |          NA |
| BoxedOnce           | NoTiering | DOTNET_TieredCompilation=0 | 2.4605 ns | 0.0010 ns | 0.0007 ns | 2.4602 ns |  6.99 |    0.01 |      - |         - |          NA |
| MixedBoxes          | NoTiering | DOTNET_TieredCompilation=0 | 2.9110 ns | 0.0013 ns | 0.0010 ns | 2.9110 ns |  8.27 |    0.01 |      - |         - |          NA |
| EscapingBoxPerCall  | NoTiering | DOTNET_TieredCompilation=0 | 6.5084 ns | 0.0542 ns | 0.0423 ns | 6.4968 ns | 18.48 |    0.12 | 0.0021 |      40 B |          NA |
