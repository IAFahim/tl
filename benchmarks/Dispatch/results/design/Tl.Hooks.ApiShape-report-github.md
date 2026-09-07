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
| DirectTicks            | Jit       | Empty                      | 11.01 ns | 0.071 ns | 0.051 ns | 11.00 ns |  1.00 |         - |          NA |
| InstanceSingle         | Jit       | Empty                      | 15.86 ns | 0.119 ns | 0.071 ns | 15.89 ns |  1.44 |         - |          NA |
| InstanceParamsFour     | Jit       | Empty                      | 14.22 ns | 0.070 ns | 0.055 ns | 14.20 ns |  1.29 |         - |          NA |
| ShellSingle            | Jit       | Empty                      | 15.06 ns | 0.047 ns | 0.036 ns | 15.06 ns |  1.37 |         - |          NA |
| ShellParamsFour        | Jit       | Empty                      | 14.27 ns | 0.151 ns | 0.118 ns | 14.27 ns |  1.30 |         - |          NA |
| PlaybackSingle         | Jit       | Empty                      | 21.68 ns | 0.096 ns | 0.075 ns | 21.67 ns |  1.97 |         - |          NA |
| PlaybackParamsFour     | Jit       | Empty                      | 19.46 ns | 0.050 ns | 0.039 ns | 19.45 ns |  1.77 |         - |          NA |
| PlaybackBackwardSingle | Jit       | Empty                      | 22.17 ns | 0.112 ns | 0.088 ns | 22.17 ns |  2.01 |         - |          NA |
| ClipHooksSingle        | Jit       | Empty                      | 20.45 ns | 0.066 ns | 0.051 ns | 20.46 ns |  1.86 |         - |          NA |
|                        |           |                            |          |          |          |          |       |           |             |
| DirectTicks            | NoTiering | DOTNET_TieredCompilation=0 | 12.88 ns | 0.054 ns | 0.043 ns | 12.89 ns |  1.00 |         - |          NA |
| InstanceSingle         | NoTiering | DOTNET_TieredCompilation=0 | 16.02 ns | 0.061 ns | 0.047 ns | 16.03 ns |  1.24 |         - |          NA |
| InstanceParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 13.90 ns | 0.052 ns | 0.040 ns | 13.88 ns |  1.08 |         - |          NA |
| ShellSingle            | NoTiering | DOTNET_TieredCompilation=0 | 14.61 ns | 0.057 ns | 0.045 ns | 14.61 ns |  1.13 |         - |          NA |
| ShellParamsFour        | NoTiering | DOTNET_TieredCompilation=0 | 13.34 ns | 0.019 ns | 0.015 ns | 13.34 ns |  1.04 |         - |          NA |
| PlaybackSingle         | NoTiering | DOTNET_TieredCompilation=0 | 31.10 ns | 0.058 ns | 0.045 ns | 31.09 ns |  2.41 |         - |          NA |
| PlaybackParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 27.73 ns | 0.030 ns | 0.024 ns | 27.73 ns |  2.15 |         - |          NA |
| PlaybackBackwardSingle | NoTiering | DOTNET_TieredCompilation=0 | 31.50 ns | 0.066 ns | 0.048 ns | 31.49 ns |  2.45 |         - |          NA |
| ClipHooksSingle        | NoTiering | DOTNET_TieredCompilation=0 | 29.96 ns | 0.034 ns | 0.025 ns | 29.97 ns |  2.33 |         - |          NA |
