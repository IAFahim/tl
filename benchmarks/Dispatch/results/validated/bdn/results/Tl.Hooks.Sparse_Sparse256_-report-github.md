```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method     | Mean      | Error     | StdDev    | Ratio |
|----------- |----------:|----------:|----------:|------:|
| Binary     | 23.510 ns | 0.0445 ns | 0.0395 ns |  1.00 |
| Linear     | 18.401 ns | 0.1474 ns | 0.1307 ns |  0.78 |
| FlatSwitch | 17.856 ns | 0.0338 ns | 0.0283 ns |  0.76 |
| Radix8     | 10.042 ns | 0.0428 ns | 0.0379 ns |  0.43 |
| Radix4     | 16.103 ns | 0.0596 ns | 0.0528 ns |  0.68 |
| DenseFp    |  6.018 ns | 0.0267 ns | 0.0237 ns |  0.26 |
