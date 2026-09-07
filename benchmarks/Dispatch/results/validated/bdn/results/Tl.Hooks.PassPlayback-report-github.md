```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method      | Job       | EnvironmentVariables       | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------ |---------- |--------------------------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| PassByValue | Jit       | Empty                      | 0.0000 ns | 0.0000 ns | 0.0000 ns | 0.0000 ns |     ? |       ? |         - |           ? |
| PassIn      | Jit       | Empty                      | 0.0005 ns | 0.0010 ns | 0.0008 ns | 0.0001 ns |     ? |       ? |         - |           ? |
| PassRef     | Jit       | Empty                      | 0.0004 ns | 0.0006 ns | 0.0005 ns | 0.0003 ns |     ? |       ? |         - |           ? |
|             |           |                            |           |           |           |           |       |         |           |             |
| PassByValue | NoTiering | DOTNET_TieredCompilation=0 | 0.5268 ns | 0.0002 ns | 0.0002 ns | 0.5268 ns |  1.00 |    0.00 |         - |          NA |
| PassIn      | NoTiering | DOTNET_TieredCompilation=0 | 0.5273 ns | 0.0004 ns | 0.0003 ns | 0.5273 ns |  1.00 |    0.00 |         - |          NA |
| PassRef     | NoTiering | DOTNET_TieredCompilation=0 | 0.5271 ns | 0.0008 ns | 0.0006 ns | 0.5268 ns |  1.00 |    0.00 |         - |          NA |
