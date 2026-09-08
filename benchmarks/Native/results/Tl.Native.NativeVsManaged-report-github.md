```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=Jit  IterationCount=12  IterationTime=250ms  
WarmupCount=16  

```
| Method            | Mean     | Error    | StdDev   | Median   | Ratio | Allocated | Alloc Ratio |
|------------------ |---------:|---------:|---------:|---------:|------:|----------:|------------:|
| ManagedSingle     | 23.32 ns | 0.146 ns | 0.105 ns | 23.30 ns |  1.00 |         - |          NA |
| ManagedParamsFour | 18.12 ns | 0.094 ns | 0.074 ns | 18.14 ns |  0.78 |         - |          NA |
| NativeSingle      | 17.93 ns | 0.134 ns | 0.105 ns | 17.93 ns |  0.77 |         - |          NA |
| NativeParamsFour  | 14.61 ns | 0.091 ns | 0.071 ns | 14.62 ns |  0.63 |         - |          NA |
