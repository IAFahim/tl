```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method         | Job       | EnvironmentVariables       | Mean           | Error        | StdDev       | Median         | Gen0    | Gen1    | Allocated |
|--------------- |---------- |--------------------------- |---------------:|-------------:|-------------:|---------------:|--------:|--------:|----------:|
| PlainSingle    | Jit       | Empty                      |       160.7 ns |      2.41 ns |      1.88 ns |       160.9 ns |       - |       - |         - |
| DedupSingle    | Jit       | Empty                      |       158.6 ns |      0.68 ns |      0.53 ns |       158.6 ns |       - |       - |         - |
| PlainBatchFour | Jit       | Empty                      |       144.1 ns |      2.65 ns |      2.07 ns |       144.4 ns |       - |       - |         - |
| DedupBatchFour | Jit       | Empty                      |       143.7 ns |      1.39 ns |      1.08 ns |       143.7 ns |       - |       - |         - |
| BuildPlain     | Jit       | Empty                      | 5,896,856.6 ns | 47,175.06 ns | 31,203.38 ns | 5,889,567.0 ns |       - |       - | 3369656 B |
| BuildDedup     | Jit       | Empty                      | 7,988,716.0 ns | 77,611.88 ns | 60,594.25 ns | 7,996,104.5 ns | 62.5000 |       - | 5352168 B |
| PlainSingle    | NoTiering | DOTNET_TieredCompilation=0 |       158.9 ns |      1.23 ns |      0.89 ns |       158.8 ns |       - |       - |         - |
| DedupSingle    | NoTiering | DOTNET_TieredCompilation=0 |       155.4 ns |      0.72 ns |      0.56 ns |       155.6 ns |       - |       - |         - |
| PlainBatchFour | NoTiering | DOTNET_TieredCompilation=0 |       153.4 ns |      0.64 ns |      0.50 ns |       153.4 ns |       - |       - |         - |
| DedupBatchFour | NoTiering | DOTNET_TieredCompilation=0 |       150.3 ns |      0.71 ns |      0.56 ns |       150.3 ns |       - |       - |         - |
| BuildPlain     | NoTiering | DOTNET_TieredCompilation=0 | 6,073,326.2 ns | 36,804.67 ns | 28,734.66 ns | 6,072,095.6 ns | 25.0000 |       - | 3369656 B |
| BuildDedup     | NoTiering | DOTNET_TieredCompilation=0 | 7,083,045.2 ns | 27,045.53 ns | 21,115.37 ns | 7,079,944.8 ns | 93.7500 | 31.2500 | 5352168 B |
