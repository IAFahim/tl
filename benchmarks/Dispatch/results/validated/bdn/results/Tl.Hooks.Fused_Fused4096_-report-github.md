```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method      | Mean     | Error    | StdDev   | Ratio |
|------------ |---------:|---------:|---------:|------:|
| TwoLevelFp  | 13.66 ns | 0.041 ns | 0.032 ns |  1.00 |
| FusedSparse | 51.49 ns | 0.196 ns | 0.174 ns |  3.77 |
| FusedDense  | 15.91 ns | 0.037 ns | 0.033 ns |  1.17 |
