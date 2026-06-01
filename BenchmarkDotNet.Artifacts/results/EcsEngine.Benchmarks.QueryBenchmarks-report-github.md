```

BenchmarkDotNet v0.14.0, Fedora Linux 43 (Workstation Edition)
Intel Core i5-8400 CPU 2.80GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores
.NET SDK 10.0.108
  [Host]     : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2


```
| Method                     | EntityCount | Mean        | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |------------ |------------:|---------:|---------:|-------:|----------:|
| **QueryEach_PositionVelocity** | **100**         |    **256.2 ns** |  **1.97 ns** |  **1.84 ns** | **0.0253** |     **120 B** |
| **QueryEach_PositionVelocity** | **1000**        |  **1,230.5 ns** |  **8.74 ns** |  **8.17 ns** | **0.0248** |     **120 B** |
| **QueryEach_PositionVelocity** | **10000**       | **11,323.3 ns** | **60.70 ns** | **53.81 ns** | **0.0153** |     **120 B** |
