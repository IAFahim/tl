```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessNoEmitToolchain  IterationCount=15  IterationTime=250ms  
WarmupCount=6  

```
| Method  | Mean     | Error    | StdDev   | Median   | Allocated |
|-------- |---------:|---------:|---------:|---------:|----------:|
| KindsF1 | 27.71 ms | 0.484 ms | 0.404 ms | 27.78 ms |         - |
| KindsF8 | 74.74 ms | 8.122 ms | 7.598 ms | 71.21 ms |     996 B |
