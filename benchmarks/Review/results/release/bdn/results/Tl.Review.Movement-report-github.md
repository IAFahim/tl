```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-VRYZFP : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method       | Clips | Sequential | Mean       | Error     | StdDev    | Median     | Ratio | Allocated | Alloc Ratio |
|------------- |------ |----------- |-----------:|----------:|----------:|-----------:|------:|----------:|------------:|
| **BeforeSingle** | **16**    | **False**      |  **46.105 ns** | **0.3420 ns** | **0.2262 ns** |  **46.021 ns** |  **1.00** |         **-** |          **NA** |
| BeforeBatch  | 16    | False      |  41.156 ns | 0.0386 ns | 0.0279 ns |  41.162 ns |  0.89 |         - |          NA |
| AfterSingle  | 16    | False      |  41.464 ns | 0.0528 ns | 0.0382 ns |  41.452 ns |  0.90 |         - |          NA |
| AfterBatch   | 16    | False      |  29.077 ns | 0.0648 ns | 0.0506 ns |  29.071 ns |  0.63 |         - |          NA |
|              |       |            |            |           |           |            |       |           |             |
| **BeforeSingle** | **16**    | **True**       |  **20.324 ns** | **0.0616 ns** | **0.0481 ns** |  **20.310 ns** |  **1.00** |         **-** |          **NA** |
| BeforeBatch  | 16    | True       |  14.180 ns | 0.2407 ns | 0.1879 ns |  14.103 ns |  0.70 |         - |          NA |
| AfterSingle  | 16    | True       |  19.834 ns | 0.0664 ns | 0.0480 ns |  19.834 ns |  0.98 |         - |          NA |
| AfterBatch   | 16    | True       |   8.715 ns | 0.0415 ns | 0.0324 ns |   8.711 ns |  0.43 |         - |          NA |
|              |       |            |            |           |           |            |       |           |             |
| **BeforeSingle** | **512**   | **False**      | **266.225 ns** | **2.7725 ns** | **2.1646 ns** | **265.901 ns** |  **1.00** |         **-** |          **NA** |
| BeforeBatch  | 512   | False      | 254.043 ns | 0.2492 ns | 0.1802 ns | 254.054 ns |  0.95 |         - |          NA |
| AfterSingle  | 512   | False      |  72.111 ns | 0.1112 ns | 0.0868 ns |  72.132 ns |  0.27 |         - |          NA |
| AfterBatch   | 512   | False      |  52.642 ns | 0.0828 ns | 0.0646 ns |  52.656 ns |  0.20 |         - |          NA |
|              |       |            |            |           |           |            |       |           |             |
| **BeforeSingle** | **512**   | **True**       | **176.041 ns** | **0.2129 ns** | **0.1662 ns** | **176.033 ns** |  **1.00** |         **-** |          **NA** |
| BeforeBatch  | 512   | True       | 177.007 ns | 0.2346 ns | 0.1832 ns | 176.913 ns |  1.01 |         - |          NA |
| AfterSingle  | 512   | True       |  62.060 ns | 0.0766 ns | 0.0507 ns |  62.075 ns |  0.35 |         - |          NA |
| AfterBatch   | 512   | True       |  14.545 ns | 0.0551 ns | 0.0430 ns |  14.543 ns |  0.08 |         - |          NA |
