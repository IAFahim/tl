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
| Binary        | 36.064 ns | 0.0279 ns | 0.0185 ns | 36.057 ns |  1.00 |         - |          NA |
| Dense         |  1.986 ns | 0.0079 ns | 0.0057 ns |  1.988 ns |  0.06 |         - |          NA |
| Rank          |  2.675 ns | 0.0067 ns | 0.0044 ns |  2.674 ns |  0.07 |         - |          NA |
| GeneratedTree | 23.739 ns | 0.0330 ns | 0.0239 ns | 23.743 ns |  0.66 |         - |          NA |
