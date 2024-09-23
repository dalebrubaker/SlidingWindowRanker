using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

// ReSharper disable once JoinDeclarationAndInitializer
DebugInProcessConfig config;
#if DEBUG
config = new DebugInProcessConfig();
#else
config = null;
#endif
var summarySlidingWindowRanker = BenchmarkRunner.Run<BenchmarkSlidingWindowRanker>(config);