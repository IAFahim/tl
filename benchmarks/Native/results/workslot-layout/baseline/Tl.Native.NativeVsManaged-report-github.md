```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=Jit  IterationCount=12  IterationTime=1s  
WarmupCount=16  

```
| Method           | Mean     | Error    | StdDev   | Median   | Allocated |
|----------------- |---------:|---------:|---------:|---------:|----------:|
| NativeSingle     | 17.16 ns | 0.039 ns | 0.026 ns | 17.16 ns |         - |
| NativeParamsFour | 14.09 ns | 0.024 ns | 0.016 ns | 14.09 ns |         - |
