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
    private List<double> _getRankValues;
    private SlidingWindowRanker<double> _ranker;

    [Params(1000000)]
    public int GetRankCount { get; set; }

    private int TotalTestValues => GetRankCount + WindowSize;

    [Params(10000, 100000)]
    public int WindowSize { get; set; }

    //[Params(0.5, 0.75, 1.0, 1.25)]
    //public double PartitionsMultipleOfDefault { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random();
        var valuesToRank = new List<double>(TotalTestValues);
        for (var i = 0; i < TotalTestValues; i++)
        {
            var value = random.NextDouble() * 100;
            value = Math.Round(value, 1); // for easier debugging
            valuesToRank.Add(value);
        }
        s_ValuesToRankStr = string.Join(',', valuesToRank);
        var initialValues = valuesToRank.Take(WindowSize).ToList();
        _getRankValues = valuesToRank.GetRange(0, GetRankCount).ToList();
        _ranker = new SlidingWindowRanker<double>(initialValues, -1, WindowSize);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public void RankValues()
    {
        for (var index = 0; index < GetRankCount; index++)
        {
            var value = _getRankValues[index];
            var rank = _ranker.GetRank(value);
        }
    }
}