```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method | Job        | Toolchain                | IterationCount | IterationTime | WarmupCount | Mean       | Error    | StdDev   | Median     | Allocated |
|------- |----------- |------------------------- |--------------- |-------------- |------------ |-----------:|---------:|---------:|-----------:|----------:|
| NewA   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           | 1,035.7 μs | 21.82 μs | 21.43 μs | 1,023.5 μs |         - |
| NewB   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   997.0 μs |  3.58 μs |  3.52 μs |   997.7 μs |      18 B |
| NewC   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   936.3 μs | 13.14 μs | 12.90 μs |   937.4 μs |      16 B |
| NewD   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           | 1,832.2 μs |  3.22 μs |  3.16 μs | 1,832.5 μs |      33 B |
| NewA   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     | 1,032.2 μs | 11.55 μs | 10.81 μs | 1,025.3 μs |       7 B |
| NewB   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   982.6 μs |  7.62 μs |  6.76 μs |   979.9 μs |       7 B |
| NewC   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   919.8 μs |  1.56 μs |  1.30 μs |   919.7 μs |       3 B |
| NewD   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     | 1,885.7 μs | 19.50 μs | 18.24 μs | 1,892.3 μs |       7 B |
