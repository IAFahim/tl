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
| DirectTicks            | Jit       | Empty                      | 11.59 ns | 0.266 ns | 0.192 ns | 11.50 ns |  1.00 |    0.02 |         - |          NA |
| InstanceSingle         | Jit       | Empty                      | 22.36 ns | 0.370 ns | 0.267 ns | 22.42 ns |  1.93 |    0.04 |         - |          NA |
| InstanceParamsFour     | Jit       | Empty                      | 17.66 ns | 0.062 ns | 0.045 ns | 17.65 ns |  1.52 |    0.02 |         - |          NA |
| ShellSingle            | Jit       | Empty                      | 18.43 ns | 0.039 ns | 0.030 ns | 18.43 ns |  1.59 |    0.03 |         - |          NA |
| ShellParamsFour        | Jit       | Empty                      | 17.40 ns | 0.208 ns | 0.163 ns | 17.39 ns |  1.50 |    0.03 |         - |          NA |
| PlaybackSingle         | Jit       | Empty                      | 20.60 ns | 0.419 ns | 0.303 ns | 20.59 ns |  1.78 |    0.04 |         - |          NA |
| PlaybackParamsFour     | Jit       | Empty                      | 18.50 ns | 0.126 ns | 0.083 ns | 18.49 ns |  1.60 |    0.03 |         - |          NA |
| PlaybackBackwardSingle | Jit       | Empty                      | 20.13 ns | 0.364 ns | 0.284 ns | 20.08 ns |  1.74 |    0.04 |         - |          NA |
| HubDispatch            | Jit       | Empty                      | 23.07 ns | 0.085 ns | 0.067 ns | 23.04 ns |  1.99 |    0.03 |         - |          NA |
|                        |           |                            |          |          |          |          |       |         |           |             |
| DirectTicks            | NoTiering | DOTNET_TieredCompilation=0 | 13.71 ns | 0.176 ns | 0.138 ns | 13.70 ns |  1.00 |    0.01 |         - |          NA |
| InstanceSingle         | NoTiering | DOTNET_TieredCompilation=0 | 24.66 ns | 0.162 ns | 0.126 ns | 24.67 ns |  1.80 |    0.02 |         - |          NA |
| InstanceParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 19.80 ns | 0.114 ns | 0.082 ns | 19.78 ns |  1.44 |    0.02 |         - |          NA |
| ShellSingle            | NoTiering | DOTNET_TieredCompilation=0 | 21.10 ns | 0.075 ns | 0.059 ns | 21.11 ns |  1.54 |    0.02 |         - |          NA |
| ShellParamsFour        | NoTiering | DOTNET_TieredCompilation=0 | 19.09 ns | 0.082 ns | 0.059 ns | 19.10 ns |  1.39 |    0.01 |         - |          NA |
| PlaybackSingle         | NoTiering | DOTNET_TieredCompilation=0 | 27.62 ns | 0.131 ns | 0.102 ns | 27.58 ns |  2.02 |    0.02 |         - |          NA |
| PlaybackParamsFour     | NoTiering | DOTNET_TieredCompilation=0 | 24.15 ns | 0.178 ns | 0.139 ns | 24.14 ns |  1.76 |    0.02 |         - |          NA |
| PlaybackBackwardSingle | NoTiering | DOTNET_TieredCompilation=0 | 28.58 ns | 0.158 ns | 0.123 ns | 28.59 ns |  2.09 |    0.02 |         - |          NA |
| HubDispatch            | NoTiering | DOTNET_TieredCompilation=0 | 30.93 ns | 0.171 ns | 0.133 ns | 30.97 ns |  2.26 |    0.02 |         - |          NA |
