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
| DirectTicks            | Jit       | Empty                      | 11.68 ns | 0.150 ns | 0.108 ns | 11.66 ns |  1.00 |    0.01 |         - |          NA |
| InstanceSingle         | Jit       | Empty                      | 22.51 ns | 0.125 ns | 0.098 ns | 22.51 ns |  1.93 |    0.02 |         - |          NA |
| InstanceParamsFour     | Jit       | Empty                      | 18.25 ns | 0.082 ns | 0.054 ns | 18.27 ns |  1.56 |    0.01 |         - |          NA |
| ShellSingle            | Jit       | Empty                      | 19.30 ns | 0.089 ns | 0.069 ns | 19.30 ns |  1.65 |    0.02 |         - |          NA |
| ShellParamsFour        | Jit       | Empty                      | 17.80 ns | 0.077 ns | 0.060 ns | 17.80 ns |  1.52 |    0.01 |         - |          NA |
| PlaybackSingle         | Jit       | Empty                      | 21.92 ns | 0.090 ns | 0.071 ns | 21.93 ns |  1.88 |    0.02 |         - |          NA |
| PlaybackParamsFour     | Jit       | Empty                      | 19.31 ns | 0.100 ns | 0.078 ns | 19.32 ns |  1.65 |    0.02 |         - |          NA |
| PlaybackBackwardSingle | Jit       | Empty                      | 21.56 ns | 0.210 ns | 0.164 ns | 21.55 ns |  1.85 |    0.02 |         - |          NA |
| HubDispatch            | Jit       | Empty                      | 24.33 ns | 0.164 ns | 0.128 ns | 24.29 ns |  2.08 |    0.02 |         - |          NA |
|                        |           |                            |          |          |          |          |       |         |           |             |
| DirectTicks            | NoTiering | DOTNET_TieredCompilation=0 | 13.93 ns | 0.079 ns | 0.057 ns | 13.96 ns |  1.00 |    0.01 |         - |          NA |
| InstanceSingle         | NoTiering | DOTNET_TieredCompilation=0 | 25.18 ns | 0.122 ns | 0.088 ns | 25.19 ns |  1.81 |    0.01 |         - |          NA |
| InstanceParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 20.36 ns | 0.076 ns | 0.060 ns | 20.36 ns |  1.46 |    0.01 |         - |          NA |
| ShellSingle            | NoTiering | DOTNET_TieredCompilation=0 | 21.25 ns | 0.097 ns | 0.075 ns | 21.25 ns |  1.53 |    0.01 |         - |          NA |
| ShellParamsFour        | NoTiering | DOTNET_TieredCompilation=0 | 19.23 ns | 0.092 ns | 0.072 ns | 19.23 ns |  1.38 |    0.01 |         - |          NA |
| PlaybackSingle         | NoTiering | DOTNET_TieredCompilation=0 | 28.71 ns | 0.246 ns | 0.192 ns | 28.69 ns |  2.06 |    0.02 |         - |          NA |
| PlaybackParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 24.19 ns | 0.106 ns | 0.083 ns | 24.17 ns |  1.74 |    0.01 |         - |          NA |
| PlaybackBackwardSingle | NoTiering | DOTNET_TieredCompilation=0 | 28.21 ns | 0.177 ns | 0.138 ns | 28.24 ns |  2.03 |    0.01 |         - |          NA |
| HubDispatch            | NoTiering | DOTNET_TieredCompilation=0 | 32.45 ns | 0.215 ns | 0.168 ns | 32.46 ns |  2.33 |    0.01 |         - |          NA |
