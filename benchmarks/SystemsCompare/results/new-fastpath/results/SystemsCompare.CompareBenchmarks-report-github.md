```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method | Job        | Toolchain                | IterationCount | IterationTime | WarmupCount | Mean       | Error    | StdDev   | Median     | Allocated |
|------- |----------- |------------------------- |--------------- |-------------- |------------ |-----------:|---------:|---------:|-----------:|----------:|
| NewA   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   951.6 μs |  7.35 μs |  6.88 μs |   953.9 μs |         - |
| NewB   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   939.8 μs |  9.66 μs |  9.49 μs |   938.1 μs |         - |
| NewC   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           |   829.4 μs |  1.63 μs |  1.60 μs |   829.5 μs |         - |
| NewD   | Job-DFACHR | InProcessNoEmitToolchain | 16             | 200ms         | 8           | 1,441.8 μs |  9.72 μs |  9.09 μs | 1,437.1 μs |      26 B |
| NewA   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   952.6 μs |  8.92 μs |  7.45 μs |   954.2 μs |       4 B |
| NewB   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   922.6 μs |  8.92 μs |  7.91 μs |   919.2 μs |       4 B |
| NewC   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     |   842.3 μs | 10.70 μs | 10.01 μs |   847.6 μs |       3 B |
| NewD   | InProcess  | InProcessEmitToolchain   | Default        | Default       | Default     | 1,447.5 μs | 11.49 μs |  9.60 μs | 1,442.2 μs |       7 B |
