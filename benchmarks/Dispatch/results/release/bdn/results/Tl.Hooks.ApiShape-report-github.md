```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method                 | Job       | EnvironmentVariables       | Mean     | Error    | StdDev   | Median   | Ratio | Allocated | Alloc Ratio |
|----------------------- |---------- |--------------------------- |---------:|---------:|---------:|---------:|------:|----------:|------------:|
| DirectTicks            | Jit       | Empty                      | 10.57 ns | 0.040 ns | 0.029 ns | 10.57 ns |  1.00 |         - |          NA |
| InstanceSingle         | Jit       | Empty                      | 16.28 ns | 0.042 ns | 0.033 ns | 16.28 ns |  1.54 |         - |          NA |
| InstanceParamsFour     | Jit       | Empty                      | 14.85 ns | 0.037 ns | 0.024 ns | 14.86 ns |  1.40 |         - |          NA |
| ShellSingle            | Jit       | Empty                      | 15.46 ns | 0.082 ns | 0.064 ns | 15.47 ns |  1.46 |         - |          NA |
| ShellParamsFour        | Jit       | Empty                      | 14.06 ns | 0.103 ns | 0.081 ns | 14.06 ns |  1.33 |         - |          NA |
| PlaybackSingle         | Jit       | Empty                      | 22.58 ns | 0.099 ns | 0.077 ns | 22.56 ns |  2.14 |         - |          NA |
| PlaybackParamsFour     | Jit       | Empty                      | 19.89 ns | 0.033 ns | 0.026 ns | 19.89 ns |  1.88 |         - |          NA |
| PlaybackBackwardSingle | Jit       | Empty                      | 21.88 ns | 0.051 ns | 0.040 ns | 21.88 ns |  2.07 |         - |          NA |
| ClipHooksSingle        | Jit       | Empty                      | 19.64 ns | 0.080 ns | 0.058 ns | 19.64 ns |  1.86 |         - |          NA |
|                        |           |                            |          |          |          |          |       |           |             |
| DirectTicks            | NoTiering | DOTNET_TieredCompilation=0 | 12.97 ns | 0.025 ns | 0.020 ns | 12.98 ns |  1.00 |         - |          NA |
| InstanceSingle         | NoTiering | DOTNET_TieredCompilation=0 | 16.00 ns | 0.040 ns | 0.031 ns | 16.01 ns |  1.23 |         - |          NA |
| InstanceParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 14.40 ns | 0.027 ns | 0.019 ns | 14.40 ns |  1.11 |         - |          NA |
| ShellSingle            | NoTiering | DOTNET_TieredCompilation=0 | 14.66 ns | 0.031 ns | 0.024 ns | 14.66 ns |  1.13 |         - |          NA |
| ShellParamsFour        | NoTiering | DOTNET_TieredCompilation=0 | 14.09 ns | 0.070 ns | 0.054 ns | 14.08 ns |  1.09 |         - |          NA |
| PlaybackSingle         | NoTiering | DOTNET_TieredCompilation=0 | 30.33 ns | 0.053 ns | 0.042 ns | 30.32 ns |  2.34 |         - |          NA |
| PlaybackParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 25.38 ns | 0.040 ns | 0.029 ns | 25.39 ns |  1.96 |         - |          NA |
| PlaybackBackwardSingle | NoTiering | DOTNET_TieredCompilation=0 | 30.08 ns | 0.062 ns | 0.048 ns | 30.08 ns |  2.32 |         - |          NA |
| ClipHooksSingle        | NoTiering | DOTNET_TieredCompilation=0 | 29.04 ns | 0.130 ns | 0.101 ns | 29.04 ns |  2.24 |         - |          NA |
