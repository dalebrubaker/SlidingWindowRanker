using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using SlidingWindowRanker;

namespace Benchmarks;

//[MemoryDiagnoser]
//[ThreadingDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class BenchmarkSlidingWindowRanker
{
    private static string s_ValuesToRankStr;
    private SlidingWindowRanker<double> _rankerSafe;
    private SlidingWindowRankerUnsafe<double> _rankerUnsafe;
    private List<double> _valuesToRank;

    [Params(1000, 10000, 100000, 1000000)]
    public int NumberOfTestValues { get; set; }

    private int WindowSize => NumberOfTestValues / 10;

    [Params(1, 16,  -1)]
    public int NumberOfPartitions { get; set; }

    //[Params(0.5, 0.75, 1.0, 1.25)]
    //public double PartitionsMultipleOfDefault { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random();
        var valuesToRank = new List<double>(NumberOfTestValues);
        for (var i = 0; i < NumberOfTestValues; i++)
        {
            var value = random.NextDouble() * 100;
            value = Math.Round(value, 1); // for easier debugging
            valuesToRank.Add(value);
        }
        s_ValuesToRankStr = string.Join(',', valuesToRank);
        _valuesToRank = [..valuesToRank];
        var initialValues = _valuesToRank.Take(WindowSize).ToList();
        //var numberOfPartitions = (int)(NumberOfPartitions * MultipleOfNumberOfPartitions);
        _rankerSafe = new SlidingWindowRanker<double>(initialValues, NumberOfPartitions);
        _rankerUnsafe = new SlidingWindowRankerUnsafe<double>(initialValues, NumberOfPartitions);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _rankerSafe?.Dispose();
        _rankerUnsafe?.Dispose();
    }

    [Benchmark]
    public void RankValuesSafe()
    {
        for (var index = WindowSize; index < NumberOfTestValues; index++)
        {
            var value = _valuesToRank[index];
            var rank = _rankerSafe.GetRank(value);
        }
    }

    [Benchmark(Baseline = true)]
    public void RankValuesUnsafe()
    {
        for (var index = WindowSize; index < NumberOfTestValues; index++)
        {
            var value = _valuesToRank[index];
            var rank = _rankerUnsafe.GetRank(value);
        }
    }
}