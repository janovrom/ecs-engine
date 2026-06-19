using BenchmarkDotNet.Running;
using EcsEngine.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(QueryBenchmarks).Assembly).Run(args);
