```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JitCore4 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=JitCore4  Affinity=10000  IterationCount=12  
IterationTime=1s  WarmupCount=12  

```
| Method           | Mean     | Error    | StdDev   | Median   | Allocated |
|----------------- |---------:|---------:|---------:|---------:|----------:|
| SequentialSingle | 15.99 ns | 0.029 ns | 0.019 ns | 15.99 ns |         - |
| RandomSingle     | 24.02 ns | 0.139 ns | 0.092 ns | 24.00 ns |         - |
| SameTickSingle   | 17.99 ns | 0.012 ns | 0.009 ns | 17.99 ns |         - |
| SequentialBatch8 | 10.25 ns | 0.016 ns | 0.011 ns | 10.25 ns |         - |
| RandomBatch8     | 15.34 ns | 0.023 ns | 0.015 ns | 15.35 ns |         - |
| SameTickBatch8   | 12.42 ns | 0.006 ns | 0.004 ns | 12.42 ns |         - |
