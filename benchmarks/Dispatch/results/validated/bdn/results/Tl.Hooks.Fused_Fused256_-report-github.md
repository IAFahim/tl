```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method      | Mean      | Error     | StdDev    | Ratio |
|------------ |----------:|----------:|----------:|------:|
| TwoLevelFp  |  8.003 ns | 0.0285 ns | 0.0253 ns |  1.00 |
| FusedSparse | 26.462 ns | 0.0517 ns | 0.0458 ns |  3.31 |
| FusedDense  |  7.233 ns | 0.0355 ns | 0.0315 ns |  0.90 |
