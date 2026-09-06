```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  steady : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=steady  IterationCount=12  IterationTime=150ms  
WarmupCount=4  

```
| Method        | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| Binary        | 36.015 ns | 0.3613 ns | 0.2612 ns | 35.946 ns |  1.00 |    0.01 |         - |          NA |
| Dense         |  2.214 ns | 0.2403 ns | 0.1876 ns |  2.360 ns |  0.06 |    0.01 |         - |          NA |
| Rank          |  2.556 ns | 0.0252 ns | 0.0197 ns |  2.561 ns |  0.07 |    0.00 |         - |          NA |
| GeneratedTree | 32.121 ns | 7.7746 ns | 6.0699 ns | 35.726 ns |  0.89 |    0.16 |         - |          NA |
