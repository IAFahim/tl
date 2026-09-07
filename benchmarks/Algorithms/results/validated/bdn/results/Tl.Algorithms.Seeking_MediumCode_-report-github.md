```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  steady : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=steady  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method        | Mean      | Error     | StdDev    | Median    | Ratio | Allocated | Alloc Ratio |
|-------------- |----------:|----------:|----------:|----------:|------:|----------:|------------:|
| Binary        | 24.584 ns | 0.0527 ns | 0.0349 ns | 24.594 ns |  1.00 |         - |          NA |
| Dense         |  1.833 ns | 0.0118 ns | 0.0085 ns |  1.830 ns |  0.07 |         - |          NA |
| Rank          |  2.692 ns | 0.0162 ns | 0.0117 ns |  2.689 ns |  0.11 |         - |          NA |
| GeneratedTree | 12.616 ns | 0.0324 ns | 0.0234 ns | 12.618 ns |  0.51 |         - |          NA |
