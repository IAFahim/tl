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
| SequentialSingle | 16.04 ns | 0.076 ns | 0.050 ns | 16.03 ns |         - |
| RandomSingle     | 23.90 ns | 0.112 ns | 0.081 ns | 23.90 ns |         - |
| SameTickSingle   | 18.00 ns | 0.020 ns | 0.015 ns | 17.99 ns |         - |
| SequentialBatch8 | 10.33 ns | 0.076 ns | 0.059 ns | 10.30 ns |         - |
| RandomBatch8     | 15.62 ns | 0.069 ns | 0.054 ns | 15.62 ns |         - |
| SameTickBatch8   | 12.45 ns | 0.010 ns | 0.007 ns | 12.45 ns |         - |
