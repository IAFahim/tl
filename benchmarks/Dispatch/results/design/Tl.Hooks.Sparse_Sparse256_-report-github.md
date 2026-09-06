```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method     | Mean      | Error     | StdDev    | Ratio |
|----------- |----------:|----------:|----------:|------:|
| Binary     | 24.074 ns | 0.0510 ns | 0.0426 ns |  1.00 |
| Linear     | 18.366 ns | 0.1275 ns | 0.1064 ns |  0.76 |
| FlatSwitch | 17.874 ns | 0.0619 ns | 0.0517 ns |  0.74 |
| Radix8     |  9.571 ns | 0.0523 ns | 0.0463 ns |  0.40 |
| Radix4     | 16.161 ns | 0.0704 ns | 0.0588 ns |  0.67 |
| DenseFp    |  6.035 ns | 0.0270 ns | 0.0239 ns |  0.25 |
