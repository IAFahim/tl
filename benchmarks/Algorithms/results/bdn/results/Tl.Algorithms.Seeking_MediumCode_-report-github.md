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
| Binary        | 25.639 ns | 1.0921 ns | 0.7897 ns | 25.774 ns |  1.00 |    0.04 |         - |          NA |
| Dense         |  2.094 ns | 0.2655 ns | 0.2073 ns |  2.257 ns |  0.08 |    0.01 |         - |          NA |
| Rank          |  2.788 ns | 0.1701 ns | 0.1328 ns |  2.705 ns |  0.11 |    0.01 |         - |          NA |
| GeneratedTree | 16.251 ns | 3.4714 ns | 2.7102 ns | 18.306 ns |  0.63 |    0.10 |         - |          NA |
