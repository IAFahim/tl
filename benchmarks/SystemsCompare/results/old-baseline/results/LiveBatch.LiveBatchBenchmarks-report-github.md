```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method        | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Mean        | Error    | StdDev   | Median      | Allocated |
|-------------- |----------- |----------------------- |--------------- |-------------- |------------ |------------:|---------:|---------:|------------:|----------:|
| A_Folded      | Job-TZECNT | Default                | 16             | 200ms         | 8           |    44.54 μs | 0.814 μs | 0.800 μs |    44.80 μs |         - |
| B_Apply       | Job-TZECNT | Default                | 16             | 200ms         | 8           | 7,594.49 μs | 6.030 μs | 5.923 μs | 7,592.99 μs |         - |
| B_PreparePlay | Job-TZECNT | Default                | 16             | 200ms         | 8           | 7,562.15 μs | 5.655 μs | 5.290 μs | 7,561.87 μs |         - |
| B_Chunk128    | Job-TZECNT | Default                | 16             | 200ms         | 8           | 7,698.67 μs | 7.572 μs | 7.437 μs | 7,698.01 μs |         - |
| C_Frame       | Job-TZECNT | Default                | 16             | 200ms         | 8           |    82.03 μs | 0.242 μs | 0.202 μs |    82.03 μs |         - |
| D_FourPairs   | Job-TZECNT | Default                | 16             | 200ms         | 8           |   315.90 μs | 2.539 μs | 2.494 μs |   314.46 μs |         - |
| A_Folded      | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     |    43.17 μs | 0.626 μs | 0.555 μs |    42.81 μs |         - |
| B_Apply       | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | 7,610.40 μs | 2.486 μs | 2.076 μs | 7,610.53 μs |       3 B |
| B_PreparePlay | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | 7,609.08 μs | 3.533 μs | 2.758 μs | 7,609.13 μs |      24 B |
| B_Chunk128    | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | 7,690.94 μs | 2.762 μs | 2.583 μs | 7,690.36 μs |      11 B |
| C_Frame       | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     |    81.43 μs | 0.247 μs | 0.219 μs |    81.38 μs |         - |
| D_FourPairs   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     |   314.32 μs | 0.256 μs | 0.227 μs |   314.37 μs |       1 B |
