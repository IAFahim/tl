```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method     | Mean        | Error    | StdDev   | Ratio | RatioSD |
|----------- |------------:|---------:|---------:|------:|--------:|
| Binary     |    36.28 ns | 0.063 ns | 0.056 ns |  1.00 |    0.00 |
| Linear     | 1,034.29 ns | 2.160 ns | 1.804 ns | 28.50 |    0.06 |
| FlatSwitch |    49.09 ns | 0.511 ns | 0.478 ns |  1.35 |    0.01 |
| Radix8     |    19.84 ns | 0.093 ns | 0.078 ns |  0.55 |    0.00 |
| Radix4     |    25.53 ns | 0.082 ns | 0.069 ns |  0.70 |    0.00 |
| DenseFp    |    11.13 ns | 0.048 ns | 0.043 ns |  0.31 |    0.00 |
