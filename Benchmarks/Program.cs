using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

var config = new DebugInProcessConfig();
//var summaryLowerBound = BenchmarkRunner.Run<BenchmarkLowerBound>();
var summaryUpperBound = BenchmarkRunner.Run<BenchmarkUpperBound>();