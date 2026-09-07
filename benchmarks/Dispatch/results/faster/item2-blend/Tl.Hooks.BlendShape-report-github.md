```

BenchmarkDotNet v0.15.8, Linux Omarchy
Intel Core i9-14900K 0.80GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.400
  [Host]    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Jit       : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  NoTiering : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=12  IterationTime=250ms  WarmupCount=16  

```
| Method             | Job       | EnvironmentVariables       | Mean      | Error    | StdDev   | Median    | Allocated |
|------------------- |---------- |--------------------------- |----------:|---------:|---------:|----------:|----------:|
| StackSingle        | Jit       | Empty                      |  38.06 ns | 0.166 ns | 0.130 ns |  38.03 ns |         - |
| StackBatchFour     | Jit       | Empty                      |  21.63 ns | 0.173 ns | 0.135 ns |  21.67 ns |         - |
| BufferSingle       | Jit       | Empty                      |  30.95 ns | 0.441 ns | 0.344 ns |  30.86 ns |         - |
| BufferBatchFour    | Jit       | Empty                      |  20.38 ns | 0.159 ns | 0.124 ns |  20.37 ns |         - |
| ZeroBlendSingle    | Jit       | Empty                      |  25.91 ns | 0.078 ns | 0.052 ns |  25.93 ns |         - |
| ZeroBlendBatchFour | Jit       | Empty                      |  14.80 ns | 0.074 ns | 0.057 ns |  14.80 ns |         - |
| StackSingle        | NoTiering | DOTNET_TieredCompilation=0 | 208.36 ns | 0.923 ns | 0.668 ns | 208.32 ns |         - |
| StackBatchFour     | NoTiering | DOTNET_TieredCompilation=0 | 190.53 ns | 2.189 ns | 1.582 ns | 189.71 ns |         - |
| BufferSingle       | NoTiering | DOTNET_TieredCompilation=0 | 200.57 ns | 0.129 ns | 0.085 ns | 200.57 ns |         - |
| BufferBatchFour    | NoTiering | DOTNET_TieredCompilation=0 | 188.76 ns | 0.285 ns | 0.188 ns | 188.72 ns |         - |
| ZeroBlendSingle    | NoTiering | DOTNET_TieredCompilation=0 |  37.42 ns | 0.541 ns | 0.422 ns |  37.27 ns |         - |
| ZeroBlendBatchFour | NoTiering | DOTNET_TieredCompilation=0 |  22.87 ns | 0.111 ns | 0.087 ns |  22.85 ns |         - |
