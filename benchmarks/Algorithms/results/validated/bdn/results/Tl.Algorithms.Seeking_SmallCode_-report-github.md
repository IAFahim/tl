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
| Binary        | 13.293 ns | 0.0161 ns | 0.0116 ns | 13.293 ns |  1.00 |         - |          NA |
| Dense         |  2.559 ns | 0.0131 ns | 0.0087 ns |  2.561 ns |  0.19 |         - |          NA |
| Rank          |  4.172 ns | 0.0205 ns | 0.0136 ns |  4.172 ns |  0.31 |         - |          NA |
| GeneratedTree |  7.201 ns | 0.0170 ns | 0.0133 ns |  7.202 ns |  0.54 |         - |          NA |
