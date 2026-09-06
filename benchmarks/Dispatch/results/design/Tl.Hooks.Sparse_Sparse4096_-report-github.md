```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method     | Mean        | Error    | StdDev   | Ratio | RatioSD |
|----------- |------------:|---------:|---------:|------:|--------:|
| Binary     |    36.39 ns | 0.236 ns | 0.209 ns |  1.00 |    0.01 |
| Linear     | 1,031.33 ns | 0.678 ns | 0.529 ns | 28.34 |    0.16 |
| FlatSwitch |    49.31 ns | 0.490 ns | 0.459 ns |  1.35 |    0.01 |
| Radix8     |    19.87 ns | 0.144 ns | 0.128 ns |  0.55 |    0.00 |
| Radix4     |    25.15 ns | 0.136 ns | 0.120 ns |  0.69 |    0.00 |
| DenseFp    |    11.20 ns | 0.029 ns | 0.024 ns |  0.31 |    0.00 |
