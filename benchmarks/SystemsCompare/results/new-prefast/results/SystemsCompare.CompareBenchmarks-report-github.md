```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method | Job        | Toolchain                | IterationCount | IterationTime | WarmupCount | Mean       | Error    | StdDev   | Median     | Allocated |
|------- |----------- |------------------------- |--------------- |-------------- |------------ |-----------:|---------:|---------:|-----------:|----------:|
| NewA   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           | 1,036.0 μs | 15.60 μs | 15.32 μs | 1,037.0 μs |         - |
| NewB   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   991.3 μs |  5.31 μs |  5.21 μs |   988.9 μs |      16 B |
| NewC   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   915.7 μs |  3.19 μs |  2.83 μs |   915.2 μs |      16 B |
| NewD   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           | 1,867.2 μs |  3.97 μs |  3.90 μs | 1,865.9 μs |      30 B |
| NewA   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     | 1,039.6 μs | 13.16 μs | 11.67 μs | 1,043.9 μs |         - |
| NewB   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   989.0 μs | 16.54 μs | 15.47 μs |   996.4 μs |       3 B |
| NewC   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   918.8 μs | 11.28 μs | 10.55 μs |   912.6 μs |       4 B |
| NewD   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     | 1,846.3 μs | 27.40 μs | 24.29 μs | 1,846.2 μs |       7 B |
