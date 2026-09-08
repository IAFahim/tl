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
| SequentialSingle | 16.29 ns | 0.104 ns | 0.075 ns | 16.26 ns |         - |
| RandomSingle     | 25.66 ns | 0.074 ns | 0.053 ns | 25.65 ns |         - |
| SameTickSingle   | 17.65 ns | 0.013 ns | 0.010 ns | 17.65 ns |         - |
| SequentialBatch8 | 11.09 ns | 0.014 ns | 0.009 ns | 11.09 ns |         - |
| RandomBatch8     | 16.70 ns | 0.099 ns | 0.077 ns | 16.71 ns |         - |
| SameTickBatch8   | 14.03 ns | 0.031 ns | 0.024 ns | 14.04 ns |         - |
