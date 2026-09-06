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
| Binary        | 13.759 ns | 0.4774 ns | 0.3158 ns | 13.969 ns |  1.00 |    0.03 |         - |          NA |
| Dense         |  2.803 ns | 0.2385 ns | 0.1862 ns |  2.931 ns |  0.20 |    0.01 |         - |          NA |
| Rank          |  3.397 ns | 0.0067 ns | 0.0049 ns |  3.395 ns |  0.25 |    0.01 |         - |          NA |
| GeneratedTree |  9.813 ns | 2.4034 ns | 1.8764 ns | 11.223 ns |  0.71 |    0.13 |         - |          NA |
