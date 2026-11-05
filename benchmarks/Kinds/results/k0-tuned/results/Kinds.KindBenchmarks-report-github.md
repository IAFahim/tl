```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessNoEmitToolchain  IterationCount=15  IterationTime=250ms  
WarmupCount=6  

```
| Method          | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| PlanOneA        | 33.621 ms | 0.7095 ms | 0.6637 ms | 33.643 ms |  1.00 |    0.03 |         - |          NA |
| PlanOneC        | 37.678 ms | 0.7552 ms | 0.7065 ms | 37.573 ms |  1.12 |    0.03 |         - |          NA |
| KindsA          | 20.087 ms | 0.1892 ms | 0.1677 ms | 20.060 ms |  0.60 |    0.01 |     365 B |          NA |
| KindsB          |  2.430 ms | 0.0931 ms | 0.0778 ms |  2.405 ms |  0.07 |    0.00 |      42 B |          NA |
| KindsC          | 21.342 ms | 0.3566 ms | 0.3336 ms | 21.367 ms |  0.64 |    0.02 |         - |          NA |
| KindsDTickOnly  |  7.408 ms | 0.0196 ms | 0.0183 ms |  7.409 ms |  0.22 |    0.00 |      35 B |          NA |
| KindsE2         | 31.878 ms | 0.4445 ms | 0.3712 ms | 31.752 ms |  0.95 |    0.02 |     546 B |          NA |
| KindsE64        | 38.632 ms | 1.9809 ms | 1.7561 ms | 37.847 ms |  1.15 |    0.05 |     168 B |          NA |
| KindsE64Blocked | 34.262 ms | 0.6073 ms | 0.5681 ms | 34.343 ms |  1.02 |    0.03 |         - |          NA |
| KindsF1         | 20.111 ms | 0.6499 ms | 0.6079 ms | 20.085 ms |  0.60 |    0.02 |      72 B |          NA |
| KindsF8         | 64.658 ms | 8.6284 ms | 8.0710 ms | 63.845 ms |  1.92 |    0.24 |     996 B |          NA |
