```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method | Job        | Toolchain                | IterationCount | IterationTime | WarmupCount | Mean       | Error    | StdDev   | Median     | Allocated |
|------- |----------- |------------------------- |--------------- |-------------- |------------ |-----------:|---------:|---------:|-----------:|----------:|
| NewA   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   944.5 μs | 13.29 μs | 12.43 μs |   945.8 μs |         - |
| NewB   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   908.8 μs |  3.58 μs |  2.99 μs |   908.0 μs |      16 B |
| NewC   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   837.1 μs |  5.79 μs |  5.69 μs |   836.0 μs |         - |
| NewD   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           | 1,457.9 μs | 25.92 μs | 25.46 μs | 1,450.1 μs |      28 B |
| NewA   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   932.9 μs |  1.67 μs |  1.48 μs |   933.1 μs |       1 B |
| NewB   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   916.9 μs | 11.08 μs | 11.38 μs |   911.6 μs |       3 B |
| NewC   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   833.5 μs |  3.66 μs |  2.86 μs |   833.2 μs |       3 B |
| NewD   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     | 1,442.5 μs |  1.20 μs |  0.93 μs | 1,442.5 μs |       7 B |
