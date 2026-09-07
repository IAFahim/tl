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
| DirectTicks            | Jit       | Empty                      | 11.82 ns | 0.080 ns | 0.053 ns | 11.82 ns |  1.00 |    0.01 |         - |          NA |
| InstanceSingle         | Jit       | Empty                      | 25.88 ns | 0.215 ns | 0.168 ns | 25.92 ns |  2.19 |    0.02 |         - |          NA |
| InstanceParamsFour     | Jit       | Empty                      | 20.07 ns | 0.132 ns | 0.103 ns | 20.04 ns |  1.70 |    0.01 |         - |          NA |
| ShellSingle            | Jit       | Empty                      | 21.20 ns | 0.073 ns | 0.048 ns | 21.22 ns |  1.79 |    0.01 |         - |          NA |
| ShellParamsFour        | Jit       | Empty                      | 19.20 ns | 0.052 ns | 0.037 ns | 19.21 ns |  1.62 |    0.01 |         - |          NA |
| PlaybackSingle         | Jit       | Empty                      | 23.21 ns | 0.076 ns | 0.059 ns | 23.21 ns |  1.96 |    0.01 |         - |          NA |
| PlaybackParamsFour     | Jit       | Empty                      | 20.64 ns | 0.145 ns | 0.113 ns | 20.61 ns |  1.75 |    0.01 |         - |          NA |
| PlaybackBackwardSingle | Jit       | Empty                      | 23.45 ns | 0.161 ns | 0.126 ns | 23.44 ns |  1.98 |    0.01 |         - |          NA |
| HubDispatch            | Jit       | Empty                      | 27.67 ns | 0.144 ns | 0.112 ns | 27.65 ns |  2.34 |    0.01 |         - |          NA |
|                        |           |                            |          |          |          |          |       |         |           |             |
| DirectTicks            | NoTiering | DOTNET_TieredCompilation=0 | 14.24 ns | 0.051 ns | 0.040 ns | 14.24 ns |  1.00 |    0.00 |         - |          NA |
| InstanceSingle         | NoTiering | DOTNET_TieredCompilation=0 | 28.45 ns | 0.107 ns | 0.071 ns | 28.47 ns |  2.00 |    0.01 |         - |          NA |
| InstanceParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 22.76 ns | 0.104 ns | 0.081 ns | 22.77 ns |  1.60 |    0.01 |         - |          NA |
| ShellSingle            | NoTiering | DOTNET_TieredCompilation=0 | 24.79 ns | 0.187 ns | 0.146 ns | 24.79 ns |  1.74 |    0.01 |         - |          NA |
| ShellParamsFour        | NoTiering | DOTNET_TieredCompilation=0 | 21.81 ns | 0.101 ns | 0.079 ns | 21.79 ns |  1.53 |    0.01 |         - |          NA |
| PlaybackSingle         | NoTiering | DOTNET_TieredCompilation=0 | 36.72 ns | 0.242 ns | 0.175 ns | 36.69 ns |  2.58 |    0.01 |         - |          NA |
| PlaybackParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 27.52 ns | 0.222 ns | 0.173 ns | 27.52 ns |  1.93 |    0.01 |         - |          NA |
| PlaybackBackwardSingle | NoTiering | DOTNET_TieredCompilation=0 | 35.45 ns | 0.261 ns | 0.204 ns | 35.39 ns |  2.49 |    0.02 |         - |          NA |
| HubDispatch            | NoTiering | DOTNET_TieredCompilation=0 | 37.20 ns | 0.196 ns | 0.130 ns | 37.22 ns |  2.61 |    0.01 |         - |          NA |
