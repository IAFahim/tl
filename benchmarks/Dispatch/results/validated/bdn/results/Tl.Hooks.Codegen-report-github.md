```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-VBSMZP : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=6  IterationTime=250ms  WarmupCount=16  

```
| Method        | Mean      | Error     | StdDev    | Ratio | Code Size | Allocated | Alloc Ratio |
|-------------- |----------:|----------:|----------:|------:|----------:|----------:|------------:|
| Direct        | 0.1755 ns | 0.0016 ns | 0.0004 ns |  1.00 |      71 B |         - |          NA |
| GeneratedLink | 0.1753 ns | 0.0004 ns | 0.0001 ns |  1.00 |      71 B |         - |          NA |
| Constrained   | 0.1746 ns | 0.0005 ns | 0.0002 ns |  1.00 |      71 B |         - |          NA |
