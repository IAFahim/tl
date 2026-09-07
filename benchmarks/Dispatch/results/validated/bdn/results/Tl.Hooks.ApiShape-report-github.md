```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method                 | Job       | EnvironmentVariables       | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------- |---------- |--------------------------- |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| DirectTicks            | Jit       | Empty                      | 11.02 ns | 0.096 ns | 0.063 ns | 11.02 ns |  1.00 |    0.01 |         - |          NA |
| InstanceSingle         | Jit       | Empty                      | 15.55 ns | 0.028 ns | 0.022 ns | 15.55 ns |  1.41 |    0.01 |         - |          NA |
| InstanceParamsFour     | Jit       | Empty                      | 14.02 ns | 0.045 ns | 0.033 ns | 14.02 ns |  1.27 |    0.01 |         - |          NA |
| ShellSingle            | Jit       | Empty                      | 15.32 ns | 0.116 ns | 0.090 ns | 15.33 ns |  1.39 |    0.01 |         - |          NA |
| ShellParamsFour        | Jit       | Empty                      | 13.81 ns | 0.075 ns | 0.059 ns | 13.80 ns |  1.25 |    0.01 |         - |          NA |
| PlaybackSingle         | Jit       | Empty                      | 21.49 ns | 0.404 ns | 0.316 ns | 21.49 ns |  1.95 |    0.03 |         - |          NA |
| PlaybackParamsFour     | Jit       | Empty                      | 20.14 ns | 0.079 ns | 0.057 ns | 20.14 ns |  1.83 |    0.01 |         - |          NA |
| PlaybackBackwardSingle | Jit       | Empty                      | 22.11 ns | 0.080 ns | 0.058 ns | 22.12 ns |  2.01 |    0.01 |         - |          NA |
| ClipHooksSingle        | Jit       | Empty                      | 20.82 ns | 0.070 ns | 0.055 ns | 20.81 ns |  1.89 |    0.01 |         - |          NA |
|                        |           |                            |          |          |          |          |       |         |           |             |
| DirectTicks            | NoTiering | DOTNET_TieredCompilation=0 | 12.89 ns | 0.017 ns | 0.013 ns | 12.88 ns |  1.00 |    0.00 |         - |          NA |
| InstanceSingle         | NoTiering | DOTNET_TieredCompilation=0 | 16.05 ns | 0.033 ns | 0.026 ns | 16.04 ns |  1.25 |    0.00 |         - |          NA |
| InstanceParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 13.92 ns | 0.036 ns | 0.028 ns | 13.92 ns |  1.08 |    0.00 |         - |          NA |
| ShellSingle            | NoTiering | DOTNET_TieredCompilation=0 | 14.93 ns | 0.044 ns | 0.032 ns | 14.92 ns |  1.16 |    0.00 |         - |          NA |
| ShellParamsFour        | NoTiering | DOTNET_TieredCompilation=0 | 13.32 ns | 0.028 ns | 0.021 ns | 13.32 ns |  1.03 |    0.00 |         - |          NA |
| PlaybackSingle         | NoTiering | DOTNET_TieredCompilation=0 | 31.11 ns | 0.064 ns | 0.046 ns | 31.10 ns |  2.41 |    0.00 |         - |          NA |
| PlaybackParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 27.59 ns | 0.088 ns | 0.068 ns | 27.60 ns |  2.14 |    0.01 |         - |          NA |
| PlaybackBackwardSingle | NoTiering | DOTNET_TieredCompilation=0 | 31.28 ns | 0.086 ns | 0.062 ns | 31.29 ns |  2.43 |    0.01 |         - |          NA |
| ClipHooksSingle        | NoTiering | DOTNET_TieredCompilation=0 | 29.97 ns | 0.051 ns | 0.040 ns | 29.98 ns |  2.33 |    0.00 |         - |          NA |
