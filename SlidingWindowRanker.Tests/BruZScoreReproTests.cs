using FluentAssertions;

namespace SlidingWindowRanker.Tests;

/// <summary>
/// Regression tests for partition-index corruption seen from BruZScorePlusMinus:
/// window 30/60, natural-log magnitudes, duplicate deltas, long GetZScore streams.
/// </summary>
public class BruZScoreReproTests
{
    [Fact]
    public void GetZScore_FormerFailureIteration11654_Seed42_DoesNotThrow()
    {
        const int windowSize = 30;
        const int seed = 42;
        const int formerlyFailingIteration = 11654;
        var (warmup, stream) = BruZScoreReplayHelper.BuildWarmupAndStream(windowSize, seed, formerlyFailingIteration);
        var stats = new SlidingWindowStats<double>(warmup, windowSize: windowSize);

        for (var i = 0; i < formerlyFailingIteration; i++)
        {
            ApplyBruZScoreStep(stats, stream[i]);
            AssertPartitionInvariants(stats);
        }
    }

    [Theory]
    [InlineData(30, 42)]
    [InlineData(30, 127)]
    [InlineData(60, 42)]
    [InlineData(60, 1257)]
    public void GetZScore_LogMagnitudeStream_DoesNotCorruptPartitions(int windowSize, int seed)
    {
        var rng = new Random(seed);
        var buffer = new List<double>(windowSize);
        while (buffer.Count < windowSize)
        {
            var delta = NextDelta(rng);
            if (delta > 0)
            {
                buffer.Add(Math.Log(delta));
            }
        }

        var stats = new SlidingWindowStats<double>(buffer, windowSize: windowSize);
        for (var i = 0; i < 50_000; i++)
        {
            var delta = NextDelta(rng);
            if (delta <= 0)
            {
                continue;
            }

            var magnitude = Math.Log(delta);
            ApplyBruZScoreStep(stats, magnitude);

            stats.Count.Should().BeLessThanOrEqualTo(windowSize);
            AssertPartitionInvariants(stats);
        }
    }

    [Fact]
    public void GetZScore_DuplicateLogMagnitudes_Window30_TriggersPartitionRemoves()
    {
        const int windowSize = 30;
        var initial = Enumerable.Repeat(Math.Log(100.0), windowSize).ToList();
        var stats = new SlidingWindowStats<double>(initial, windowSize: windowSize);

        for (var i = 0; i < 10_000; i++)
        {
            var magnitude = i % 3 == 0 ? Math.Log(100.0) : Math.Log(50.0 + i % 20);
            _ = stats.GetZScore(magnitude);
            AssertPartitionInvariants(stats);
        }
    }

    private static void ApplyBruZScoreStep(SlidingWindowStats<double> stats, double magnitude)
    {
        var iqr = stats.GetIQR();
        if (iqr == 0)
        {
            stats.GetRank(magnitude);
        }
        else
        {
            _ = stats.GetZScore(magnitude);
        }
    }

    private static double NextDelta(Random rng)
    {
        return rng.NextDouble() switch
        {
            < 0.1 => rng.Next(1, 5),
            < 0.2 => rng.Next(1000, 5000),
            _ => rng.Next(5, 500)
        };
    }

    private static void AssertPartitionInvariants(SlidingWindowStats<double> stats)
    {
        var partitions = stats.TestPartitions;
        partitions.Count.Should().BeGreaterThan(0);
        partitions[0].LowerBound.Should().Be(0);

        var total = 0;
        for (var i = 0; i < partitions.Count; i++)
        {
            var partition = partitions[i];
            partition.Values.IsSortedAscending().Should().BeTrue($"partition {i}");
            if (i > 0)
            {
                partition.LowerBound.Should().Be(partitions[i - 1].LowerBound + partitions[i - 1].Count);
            }
            total += partition.Count;
        }

        total.Should().Be(stats.Count);
        stats.TestValues.IsSortedAscending().Should().BeTrue();
    }
}
