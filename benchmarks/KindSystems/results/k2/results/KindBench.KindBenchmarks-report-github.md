```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessNoEmitToolchain  IterationCount=15  IterationTime=250ms  
WarmupCount=6  

```
| Method         | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------- |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| PlanA          |  33.966 ms | 0.3466 ms | 0.3072 ms |  33.878 ms |  1.00 |    0.01 |         - |          NA |
| KindsA         |  31.088 ms | 0.2759 ms | 0.2446 ms |  31.089 ms |  0.92 |    0.01 |     624 B |          NA |
| KindsB         |   3.469 ms | 0.2250 ms | 0.1878 ms |   3.398 ms |  0.10 |    0.01 |      73 B |          NA |
| KindsC         |  35.166 ms | 0.5667 ms | 0.5301 ms |  35.043 ms |  1.04 |    0.02 |     569 B |          NA |
| KindsDTickOnly |   7.509 ms | 0.0823 ms | 0.0770 ms |   7.523 ms |  0.22 |    0.00 |       7 B |          NA |
| KindsE2        |  76.556 ms | 1.1323 ms | 1.0592 ms |  76.224 ms |  2.25 |    0.04 |    1092 B |          NA |
| KindsE64       |  97.178 ms | 1.1683 ms | 1.0928 ms |  97.461 ms |  2.86 |    0.04 |     672 B |          NA |
| KindsF1        |  33.746 ms | 3.2083 ms | 3.0010 ms |  34.513 ms |  0.99 |    0.09 |      84 B |          NA |
| KindsF8        | 188.446 ms | 3.8841 ms | 3.6332 ms | 188.939 ms |  5.55 |    0.11 |    4656 B |          NA |
