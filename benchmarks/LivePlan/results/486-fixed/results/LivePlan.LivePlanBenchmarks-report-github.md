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
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | ColumnOne            |   968.4 μs |  9.93 μs |  9.29 μs |   970.6 μs |         - |
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **ColumnRuns8**          |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | ColumnRuns8          |   955.6 μs | 13.03 μs | 12.19 μs |   953.2 μs |         - |
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **ColumnAlternating8**   |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | ColumnAlternating8   | 3,804.2 μs | 20.55 μs | 17.16 μs | 3,810.5 μs |         - |
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **ColumnRuns8Waves**     |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | ColumnRuns8Waves     |   926.4 μs |  3.93 μs |  3.68 μs |   927.6 μs |       4 B |
| **Apply**  | **Job-TZECNT** | **Default**                | **16**             | **200ms**         | **8**           | **Colum(...)Waves [23]** |         **NA** |       **NA** |       **NA** |         **NA** |        **NA** |
| Apply  | InProcess  | InProcessEmitToolchain | Default        | Default       | Default     | Colum(...)Waves [23] | 3,803.8 μs | 18.91 μs | 16.76 μs | 3,804.8 μs |      18 B |

Benchmarks with issues:
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=ColumnOne]
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=ColumnRuns8]
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=ColumnAlternating8]
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=ColumnRuns8Waves]
  LivePlanBenchmarks.Apply: Job-TZECNT(IterationCount=16, IterationTime=200ms, WarmupCount=8) [Shape=Colum(...)Waves [23]]
