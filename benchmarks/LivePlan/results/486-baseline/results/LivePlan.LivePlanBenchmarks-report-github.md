```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TZECNT : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method | Job        | Toolchain              | IterationCount | IterationTime | WarmupCount | Shape                | Mean       | Error    | StdDev   | Median     | Allocated |
|------- |----------- |----------------------- |--------------- |-------------- |------------ |--------------------- |-----------:|---------:|---------:|-----------:|----------:|
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **ColumnOne**            |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | ColumnOne            |   993.2 μs |  8.02 μs |  7.50 μs |   994.5 μs |         - |
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **ColumnRuns8**          |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | ColumnRuns8          |   971.6 μs | 15.11 μs | 14.14 μs |   968.2 μs |       9 B |
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **ColumnAlternating8**   |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | ColumnAlternating8   | 5,062.1 μs | 37.90 μs | 31.64 μs | 5,065.0 μs |      36 B |
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **ColumnRuns8Waves**     |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | ColumnRuns8Waves     |   961.5 μs |  4.13 μs |  3.66 μs |   960.6 μs |         - |
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **Colum(...)Waves [23]** |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | Colum(...)Waves [23] | 5,096.2 μs | 32.39 μs | 30.30 μs | 5,105.5 μs |      32 B |

Benchmarks with issues:
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=ColumnOne]
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=ColumnRuns8]
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=ColumnAlternating8]
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=ColumnRuns8Waves]
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=Colum(...)Waves [23]]
