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
| DirectTicks            | Jit       | Empty                      | 11.42 ns | 0.073 ns | 0.048 ns | 11.42 ns |  1.00 |    0.01 |         - |          NA |
| InstanceSingle         | Jit       | Empty                      | 23.52 ns | 0.190 ns | 0.148 ns | 23.55 ns |  2.06 |    0.01 |         - |          NA |
| InstanceParamsFour     | Jit       | Empty                      | 18.76 ns | 0.157 ns | 0.114 ns | 18.79 ns |  1.64 |    0.01 |         - |          NA |
| ShellSingle            | Jit       | Empty                      | 19.52 ns | 0.063 ns | 0.049 ns | 19.51 ns |  1.71 |    0.01 |         - |          NA |
| ShellParamsFour        | Jit       | Empty                      | 18.06 ns | 0.113 ns | 0.082 ns | 18.02 ns |  1.58 |    0.01 |         - |          NA |
| PlaybackSingle         | Jit       | Empty                      | 18.88 ns | 0.121 ns | 0.095 ns | 18.88 ns |  1.65 |    0.01 |         - |          NA |
| PlaybackParamsFour     | Jit       | Empty                      | 17.88 ns | 0.094 ns | 0.068 ns | 17.89 ns |  1.57 |    0.01 |         - |          NA |
| PlaybackBackwardSingle | Jit       | Empty                      | 18.39 ns | 0.110 ns | 0.086 ns | 18.37 ns |  1.61 |    0.01 |         - |          NA |
| HubDispatch            | Jit       | Empty                      | 24.75 ns | 0.128 ns | 0.100 ns | 24.76 ns |  2.17 |    0.01 |         - |          NA |
|                        |           |                            |          |          |          |          |       |         |           |             |
| DirectTicks            | NoTiering | DOTNET_TieredCompilation=0 | 13.67 ns | 0.180 ns | 0.141 ns | 13.64 ns |  1.00 |    0.01 |         - |          NA |
| InstanceSingle         | NoTiering | DOTNET_TieredCompilation=0 | 25.80 ns | 0.206 ns | 0.149 ns | 25.81 ns |  1.89 |    0.02 |         - |          NA |
| InstanceParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 20.72 ns | 0.156 ns | 0.122 ns | 20.72 ns |  1.52 |    0.02 |         - |          NA |
| ShellSingle            | NoTiering | DOTNET_TieredCompilation=0 | 23.15 ns | 0.113 ns | 0.089 ns | 23.12 ns |  1.69 |    0.02 |         - |          NA |
| ShellParamsFour        | NoTiering | DOTNET_TieredCompilation=0 | 20.18 ns | 0.141 ns | 0.110 ns | 20.17 ns |  1.48 |    0.02 |         - |          NA |
| PlaybackSingle         | NoTiering | DOTNET_TieredCompilation=0 | 30.90 ns | 0.122 ns | 0.088 ns | 30.90 ns |  2.26 |    0.02 |         - |          NA |
| PlaybackParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 24.99 ns | 0.178 ns | 0.139 ns | 25.02 ns |  1.83 |    0.02 |         - |          NA |
| PlaybackBackwardSingle | NoTiering | DOTNET_TieredCompilation=0 | 30.06 ns | 0.151 ns | 0.118 ns | 30.02 ns |  2.20 |    0.02 |         - |          NA |
| HubDispatch            | NoTiering | DOTNET_TieredCompilation=0 | 34.49 ns | 0.100 ns | 0.078 ns | 34.49 ns |  2.52 |    0.03 |         - |          NA |
