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
| SequentialSingle | 17.80 ns | 0.010 ns | 0.008 ns | 17.80 ns |         - |
| RandomSingle     | 26.06 ns | 0.028 ns | 0.019 ns | 26.06 ns |         - |
| SameTickSingle   | 17.67 ns | 0.008 ns | 0.006 ns | 17.67 ns |         - |
| SequentialBatch8 | 10.97 ns | 0.023 ns | 0.016 ns | 10.97 ns |         - |
| RandomBatch8     | 16.28 ns | 0.072 ns | 0.043 ns | 16.28 ns |         - |
| SameTickBatch8   | 14.15 ns | 0.057 ns | 0.044 ns | 14.13 ns |         - |
