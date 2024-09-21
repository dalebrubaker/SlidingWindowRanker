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

    [Params(100000)]
    public int NumberOfTestValues { get; set; }

    [Params(8)]
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
    }

    [Benchmark]
    public void RankValues()
    {
        var valuesToRank = new List<double>(_valuesToRank);
        var initialValues = valuesToRank.Take(WindowSize).ToList();
        _ranker = new SlidingWindowRanker<double>(initialValues, NumberOfPartitions);
        for (var index = WindowSize; index < NumberOfTestValues; index++)
        {
            var value = _valuesToRank[index];
            var rank = _ranker.GetRank(value);
        }
    }
}