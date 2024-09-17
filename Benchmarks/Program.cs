using BenchmarkDotNet.Running;
using Benchmarks;

var config = new BenchmarkDotNet.Configs.DebugInProcessConfig();
var summary = BenchmarkRunner.Run<BenchmarkGetLowerBound>(config);