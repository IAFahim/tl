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
| Binary        | 71.361 ns | 0.7488 ns | 0.4953 ns | 71.252 ns |  1.00 |         - |          NA |
| Dense         |  4.081 ns | 0.0479 ns | 0.0317 ns |  4.089 ns |  0.06 |         - |          NA |
| Rank          |  5.387 ns | 0.0703 ns | 0.0508 ns |  5.371 ns |  0.08 |         - |          NA |
| GeneratedTree | 46.551 ns | 0.5363 ns | 0.3547 ns | 46.595 ns |  0.65 |         - |          NA |
