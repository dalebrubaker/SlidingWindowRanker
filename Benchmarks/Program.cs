using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;
// ReSharper disable ExpressionIsAlwaysNull

// ReSharper disable once JoinDeclarationAndInitializer
DebugInProcessConfig config;
#if DEBUG
config = new DebugInProcessConfig();
#else 
config = null;
#endif
//var summaryLowerBound = BenchmarkRunner.Run<BenchmarkLowerBound>();
//var summaryUpperBound = BenchmarkRunner.Run<BenchmarkUpperBound>();
var summarySlidingWindowRanker = BenchmarkRunner.Run<BenchmarkSlidingWindowRanker>(config);
