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
| PlanOneA       |  31.889 ms | 0.7053 ms | 0.6598 ms |  31.802 ms |  1.00 |    0.03 |         - |          NA |
| PlanOneC       |  37.725 ms | 0.6498 ms | 0.6078 ms |  37.821 ms |  1.18 |    0.03 |         - |          NA |
| KindsA         |  22.059 ms | 0.3913 ms | 0.3267 ms |  22.134 ms |  0.69 |    0.02 |         - |          NA |
| KindsB         |   2.700 ms | 0.0333 ms | 0.0295 ms |   2.707 ms |  0.08 |    0.00 |         - |          NA |
| KindsC         |  24.485 ms | 0.4338 ms | 0.4058 ms |  24.620 ms |  0.77 |    0.02 |         - |          NA |
| KindsDTickOnly |  10.143 ms | 0.1717 ms | 0.1606 ms |  10.137 ms |  0.32 |    0.01 |         - |          NA |
| KindsE2        |  36.574 ms | 0.9921 ms | 0.8795 ms |  36.828 ms |  1.15 |    0.03 |         - |          NA |
| KindsE64       |  41.501 ms | 1.4039 ms | 1.3132 ms |  41.133 ms |  1.30 |    0.05 |         - |          NA |
| KindsF1        |  23.096 ms | 0.3453 ms | 0.3230 ms |  23.186 ms |  0.72 |    0.02 |      61 B |          NA |
| KindsF8        | 119.804 ms | 3.1246 ms | 2.7698 ms | 120.430 ms |  3.76 |    0.11 |         - |          NA |
