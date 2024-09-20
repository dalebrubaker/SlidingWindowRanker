using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using SlidingWindowRanker;

namespace Benchmarks;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class BenchmarkSlidingWindowRanker
{
    private static string s_ValuesToRankStr;
    private SlidingWindowRanker<double> _ranker;
    private List<double> _valuesToRank;

    //[Params(1000, 10000)]
    [Params(40000)]
    public int NumberOfTestValues { get; set; }

    [Params(200)]
    public int NumberOfPartitions { get; set; }

    private int WindowSize => NumberOfTestValues / 10;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random();
        _valuesToRank = new List<double>(NumberOfTestValues);
        for (var i = 0; i < NumberOfTestValues; i++)
        {
            var value = random.NextDouble() * 100;
            value = Math.Round(value, 1); // for easier debugging
            _valuesToRank.Add(value);
        }
        s_ValuesToRankStr = string.Join(',', _valuesToRank);
        var initialValues = _valuesToRank.Take(WindowSize).ToList();
        _ranker = new SlidingWindowRanker<double>(initialValues, NumberOfPartitions);
    }

    [Benchmark]
    public void RankValues()
    {
        for (var index = WindowSize; index < NumberOfTestValues; index++)
        {
            var value = _valuesToRank[index];
            var rank = _ranker.GetRank(value);
        }
    }
}