using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using SlidingWindowRanker;

namespace Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class BenchmarkSlidingWindowRanker
{
    private List<double> _initialValues;
    private List<double> _values;
    private SlidingWindowRanker<double> _ranker;
    private SlidingWindowStats<double> _stats;

    [Params(100000, 1000000)]
    public int GetRankCount { get; set; }

    private int TotalTestValues => GetRankCount + WindowSize;

    [Params(1000, 10000, 100000)]
    public int WindowSize { get; set; }

    // [Params(0.75, 1.0, 1.25)]
    public double PartitionsMultipleOfDefault { get; set; } = 1.0;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var valuesToRank = new List<double>(TotalTestValues);
        for (var i = 0; i < TotalTestValues; i++)
        {
            var value = random.NextDouble() * 100;
            value = Math.Round(value, 1); // for easier debugging
            valuesToRank.Add(value);
        }
        _initialValues = valuesToRank.Take(WindowSize).ToList();
        _values = valuesToRank.Skip(WindowSize).Take(GetRankCount).ToList();
    }

    [IterationSetup(Targets = [nameof(RankValues), nameof(AddValues)])]
    public void SetupRanker()
    {
        var partitionCount = (int)(Math.Sqrt(WindowSize) * PartitionsMultipleOfDefault);
        _ranker = new SlidingWindowRanker<double>(_initialValues, partitionCount, WindowSize);
    }

    [IterationSetup(Targets = [nameof(ZScoreValues), nameof(ZScoreNoAddValues)])]
    public void SetupStats()
    {
        var partitionCount = (int)(Math.Sqrt(WindowSize) * PartitionsMultipleOfDefault);
        _stats = new SlidingWindowStats<double>(_initialValues, partitionCount, WindowSize);
    }

    [Benchmark(Baseline = true)]
    public double RankValues()
    {
        var total = 0.0;
        for (var index = 0; index < GetRankCount; index++)
        {
            total += _ranker.GetRank(_values[index]);
        }
        return total;
    }

    [Benchmark]
    public void AddValues()
    {
        for (var index = 0; index < GetRankCount; index++)
        {
            _ranker.Add(_values[index]);
        }
    }

    [Benchmark]
    public double ZScoreValues()
    {
        var total = 0.0;
        for (var index = 0; index < GetRankCount; index++)
        {
            total += _stats.GetZScore(_values[index]);
        }
        return total;
    }

    [Benchmark]
    public double ZScoreNoAddValues()
    {
        var total = 0.0;
        for (var index = 0; index < GetRankCount; index++)
        {
            total += _stats.GetZScoreNoAdd(_values[index]);
        }
        return total;
    }
}
