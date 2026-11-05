```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method        | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Mean        | Error     | StdDev    | Median      | Allocated |
|-------------- |----------- |----------------------- |--------------- |-------------- |------------ |------------:|----------:|----------:|------------:|----------:|
| A_Folded      | Job-TZECNT | Default                | 16             | 200ms         | 8           |    45.28 μs |  0.194 μs |  0.190 μs |    45.27 μs |         - |
| B_Apply       | Job-TZECNT | Default                | 16             | 200ms         | 8           | 7,664.18 μs | 69.465 μs | 68.224 μs | 7,636.15 μs |         - |
| B_PreparePlay | Job-TZECNT | Default                | 16             | 200ms         | 8           | 7,575.58 μs | 23.698 μs | 21.008 μs | 7,568.64 μs |         - |
| B_Chunk128    | Job-TZECNT | Default                | 16             | 200ms         | 8           | 7,730.87 μs | 44.500 μs | 43.705 μs | 7,729.26 μs |         - |
| C_Frame       | Job-TZECNT | Default                | 16             | 200ms         | 8           |    84.61 μs |  2.647 μs |  2.600 μs |    83.98 μs |         - |
| D_FourPairs   | Job-TZECNT | Default                | 16             | 200ms         | 8           |   313.99 μs |  0.410 μs |  0.342 μs |   313.98 μs |         - |
| A_Folded      | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     |    43.67 μs |  0.834 μs |  0.780 μs |    44.00 μs |         - |
| B_Apply       | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | 7,618.45 μs | 11.337 μs | 10.604 μs | 7,622.03 μs |      15 B |
| B_PreparePlay | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | 7,618.93 μs | 14.953 μs | 11.674 μs | 7,617.84 μs |      11 B |
| B_Chunk128    | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | 7,698.24 μs | 18.588 μs | 14.512 μs | 7,692.65 μs |      26 B |
| C_Frame       | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     |    85.30 μs |  1.071 μs |  0.949 μs |    85.20 μs |         - |
| D_FourPairs   | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     |   322.93 μs |  5.493 μs |  6.106 μs |   320.99 μs |       2 B |
