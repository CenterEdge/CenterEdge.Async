using BenchmarkDotNet.Running;
using CenterEdge.Async.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(LegacyComparison).Assembly).Run(args);
