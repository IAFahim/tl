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
| Binary        | 47.388 ns | 1.5871 ns | 1.1476 ns | 46.820 ns |  1.00 |    0.03 |         - |          NA |
| Dense         |  3.677 ns | 0.0403 ns | 0.0291 ns |  3.670 ns |  0.08 |    0.00 |         - |          NA |
| Rank          |  5.787 ns | 0.0529 ns | 0.0382 ns |  5.785 ns |  0.12 |    0.00 |         - |          NA |
| GeneratedTree | 24.747 ns | 0.5774 ns | 0.4175 ns | 24.587 ns |  0.52 |    0.01 |         - |          NA |
