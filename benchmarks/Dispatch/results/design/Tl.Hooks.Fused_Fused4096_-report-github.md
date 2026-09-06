```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method      | Mean     | Error    | StdDev   | Ratio | RatioSD |
|------------ |---------:|---------:|---------:|------:|--------:|
| TwoLevelFp  | 13.86 ns | 0.145 ns | 0.128 ns |  1.00 |    0.01 |
| FusedSparse | 52.23 ns | 0.511 ns | 0.478 ns |  3.77 |    0.05 |
| FusedDense  | 16.09 ns | 0.166 ns | 0.156 ns |  1.16 |    0.02 |
