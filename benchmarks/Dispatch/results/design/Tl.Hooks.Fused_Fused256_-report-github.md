```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method      | Mean      | Error     | StdDev    | Ratio | RatioSD |
|------------ |----------:|----------:|----------:|------:|--------:|
| TwoLevelFp  |  8.134 ns | 0.1000 ns | 0.0935 ns |  1.00 |    0.02 |
| FusedSparse | 26.896 ns | 0.2626 ns | 0.2456 ns |  3.31 |    0.05 |
| FusedDense  |  7.394 ns | 0.0930 ns | 0.0825 ns |  0.91 |    0.01 |
