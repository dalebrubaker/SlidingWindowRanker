using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

var config = new DebugInProcessConfig();
//var summaryLowerBound = BenchmarkRunner.Run<BenchmarkGetLowerBound>();
var summaryUpperBound = BenchmarkRunner.Run<BenchmarkGetUpperBound>();