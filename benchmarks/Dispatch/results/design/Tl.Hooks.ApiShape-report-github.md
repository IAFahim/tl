```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method             | Job       | EnvironmentVariables       | Mean     | Error    | StdDev   | Median   | Ratio | Allocated | Alloc Ratio |
|------------------- |---------- |--------------------------- |---------:|---------:|---------:|---------:|------:|----------:|------------:|
| DirectTicks        | Jit       | Empty                      | 11.04 ns | 0.055 ns | 0.040 ns | 11.04 ns |  1.00 |         - |          NA |
| InstanceSingle     | Jit       | Empty                      | 15.71 ns | 0.062 ns | 0.037 ns | 15.72 ns |  1.42 |         - |          NA |
| InstanceParamsFour | Jit       | Empty                      | 14.01 ns | 0.057 ns | 0.044 ns | 14.00 ns |  1.27 |         - |          NA |
| ShellSingle        | Jit       | Empty                      | 15.29 ns | 0.148 ns | 0.116 ns | 15.30 ns |  1.38 |         - |          NA |
| ShellParamsFour    | Jit       | Empty                      | 13.26 ns | 0.106 ns | 0.076 ns | 13.27 ns |  1.20 |         - |          NA |
|                    |           |                            |          |          |          |          |       |           |             |
| DirectTicks        | NoTiering | DOTNET_TieredCompilation=0 | 12.89 ns | 0.025 ns | 0.019 ns | 12.89 ns |  1.00 |         - |          NA |
| InstanceSingle     | NoTiering | DOTNET_TieredCompilation=0 | 15.88 ns | 0.057 ns | 0.044 ns | 15.87 ns |  1.23 |         - |          NA |
| InstanceParamsFour | NoTiering | DOTNET_TieredCompilation=0 | 14.08 ns | 0.221 ns | 0.173 ns | 14.11 ns |  1.09 |         - |          NA |
| ShellSingle        | NoTiering | DOTNET_TieredCompilation=0 | 15.60 ns | 0.066 ns | 0.052 ns | 15.59 ns |  1.21 |         - |          NA |
| ShellParamsFour    | NoTiering | DOTNET_TieredCompilation=0 | 14.22 ns | 0.190 ns | 0.148 ns | 14.26 ns |  1.10 |         - |          NA |
