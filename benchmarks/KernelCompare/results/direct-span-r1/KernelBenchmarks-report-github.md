```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JitCore4 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=JitCore4  Affinity=10000  IterationCount=12  
IterationTime=250ms  WarmupCount=12  

```
| Method           | Mean     | Error    | StdDev   | Median   | Allocated |
|----------------- |---------:|---------:|---------:|---------:|----------:|
| SequentialSingle | 16.86 ns | 0.063 ns | 0.045 ns | 16.85 ns |         - |
| RandomSingle     | 24.80 ns | 0.152 ns | 0.118 ns | 24.80 ns |         - |
| SameTickSingle   | 18.02 ns | 0.021 ns | 0.016 ns | 18.03 ns |         - |
| SequentialBatch8 | 10.79 ns | 0.021 ns | 0.015 ns | 10.79 ns |         - |
| RandomBatch8     | 15.73 ns | 0.082 ns | 0.064 ns | 15.73 ns |         - |
| SameTickBatch8   | 13.74 ns | 0.054 ns | 0.042 ns | 13.73 ns |         - |
