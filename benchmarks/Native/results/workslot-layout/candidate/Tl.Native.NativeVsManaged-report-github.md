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
| NativeSingle     | 16.86 ns | 0.060 ns | 0.040 ns | 16.86 ns |         - |
| NativeParamsFour | 13.89 ns | 0.064 ns | 0.046 ns | 13.87 ns |         - |
