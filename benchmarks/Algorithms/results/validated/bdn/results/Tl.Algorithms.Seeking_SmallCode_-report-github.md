```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  steady : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=steady  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method        | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| Binary        | 13.800 ns | 0.0401 ns | 0.0290 ns | 13.791 ns |  1.00 |    0.00 |         - |          NA |
| Dense         |  2.563 ns | 0.0144 ns | 0.0104 ns |  2.568 ns |  0.19 |    0.00 |         - |          NA |
| Rank          |  4.181 ns | 0.0255 ns | 0.0184 ns |  4.186 ns |  0.30 |    0.00 |         - |          NA |
| GeneratedTree | 14.393 ns | 0.7728 ns | 0.5111 ns | 14.286 ns |  1.04 |    0.04 |         - |          NA |
