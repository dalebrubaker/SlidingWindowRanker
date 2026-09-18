using FluentAssertions;

namespace SlidingWindowRanker.Tests;

/// <summary>
/// Tests for SlidingWindowStats.ReplaceLastAndGetZScore, whose contract is that the result is
/// identical to what GetZScore would have returned had the replaced value never been added.
/// </summary>
public class SlidingWindowStatsReplaceLastTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(64)]
    public void ReplaceLastAndGetZScore_MatchesAFreshlyBuiltPriorOnlyWindow(int windowSize)
    {
        var random = new Random(windowSize * 104_729);
        var stats = new SlidingWindowStats<double>(windowSize);

        // The prior-only history, maintained in lockstep, so we can rebuild a pristine window on demand
        var history = new List<double>();

        for (var bar = 0; bar < 300; bar++)
        {
            var priorWindow = TakeWindow(history, windowSize);

            // The first update of the bar goes in via GetZScore
            var firstValue = Math.Round(random.NextDouble() * 20 - 10, 2);
            var firstZScore = stats.GetZScore(firstValue);
            firstZScore.Should().Be(BuildPriorOnly(priorWindow, windowSize).GetZScore(firstValue),
                $"bar={bar} first update must score against the prior window");

            // Subsequent updates of the SAME bar must each score against the SAME prior window
            for (var update = 0; update < 4; update++)
            {
                var value = Math.Round(random.NextDouble() * 20 - 10, 2);
                var expected = BuildPriorOnly(priorWindow, windowSize).GetZScore(value);
                var actual = stats.ReplaceLastAndGetZScore(value);

                actual.Should().Be(expected, $"bar={bar} update={update} value={value} windowSize={windowSize}");
                stats.Count.Should().Be(Math.Min(priorWindow.Count + 1, windowSize),
                    "replacement never changes the number of observations");
                RankerInvariantChecker.AssertInvariants(stats, TakeWindow([..priorWindow, value], windowSize));
            }

            // The bar closes holding whatever its last value was
            history.Add(stats.TestQueueValues[^1]);
        }
    }

    [Fact]
    public void ReplaceLastAndGetZScore_PartialWindow_ScoresAgainstThePriorValuesOnly()
    {
        var stats = new SlidingWindowStats<double>(100, [1.0, 2.0, 3.0, 4.0]);

        stats.GetZScore(50.0);
        stats.Count.Should().Be(5);

        var expected = new SlidingWindowStats<double>(100, [1.0, 2.0, 3.0, 4.0]).GetZScore(99.0);
        stats.ReplaceLastAndGetZScore(99.0).Should().Be(expected);
        stats.Count.Should().Be(5, "the partial window did not grow");
        stats.TestQueueValues.Should().Equal(1.0, 2.0, 3.0, 4.0, 99.0);
    }

    [Fact]
    public void ReplaceLastAndGetZScore_FewerThanTwoPriorValues_ReturnsZeroAndStillAdds()
    {
        var stats = new SlidingWindowStats<double>(10);

        stats.GetZScore(5.0).Should().Be(0, "an empty window has fewer than two values");
        stats.Count.Should().Be(1);

        // Removing the newest leaves zero prior values, so the z-score is 0 and the value is still added
        stats.ReplaceLastAndGetZScore(7.0).Should().Be(0);
        stats.Count.Should().Be(1);
        stats.TestQueueValues.Should().Equal(7.0);

        stats.GetZScore(8.0).Should().Be(0, "one prior value is still fewer than two");
        stats.Count.Should().Be(2);

        // Now there is exactly one prior value after the removal, so still 0
        stats.ReplaceLastAndGetZScore(9.0).Should().Be(0);
        stats.Count.Should().Be(2);
        stats.TestQueueValues.Should().Equal(7.0, 9.0);
    }

    [Fact]
    public void ReplaceLastAndGetZScore_ZeroIqr_ReturnsZeroAndStillAdds()
    {
        var stats = new SlidingWindowStats<double>(10, [4.0, 4.0, 4.0, 4.0, 4.0]);

        stats.GetZScore(1000.0).Should().Be(0, "the prior window has a zero IQR");
        stats.ReplaceLastAndGetZScore(2000.0).Should().Be(0, "the prior window still has a zero IQR");
        stats.Count.Should().Be(6);
        stats.TestQueueValues.Should().Equal(4.0, 4.0, 4.0, 4.0, 4.0, 2000.0);
    }

    [Fact]
    public void ReplaceLastAndGetZScore_EmptyWindow_ThrowsAndLeavesStateIntact()
    {
        var stats = new SlidingWindowStats<double>(10);

        var act = () => stats.ReplaceLastAndGetZScore(1.0);

        act.Should().Throw<SlidingWindowRankerException>().WithMessage("*ReplaceLastAndGetZScore*");
        stats.Count.Should().Be(0);
        RankerInvariantChecker.AssertInvariants(stats, []);
    }

    [Fact]
    public void ReplaceLastAndGetZScore_RepeatedReplacement_DoesNotEvictOlderObservations()
    {
        const int windowSize = 20;
        var initial = Enumerable.Range(0, windowSize).Select(i => (double)i).ToList();
        var stats = new SlidingWindowStats<double>(initial, windowSize: windowSize);

        var expected = new SlidingWindowStats<double>([..initial.Take(windowSize - 1)], windowSize: windowSize)
            .GetZScoreNoAdd(123.0);

        for (var i = 0; i < 500; i++)
        {
            stats.ReplaceLastAndGetZScore(123.0).Should().Be(expected);
            stats.Count.Should().Be(windowSize);
            stats.TestQueueValues.Take(windowSize - 1).Should().Equal(initial.Take(windowSize - 1));
        }
    }

    [Theory]
    [InlineData(30, 42)]
    [InlineData(30, 127)]
    [InlineData(60, 1257)]
    public void ReplaceLastAndGetZScore_BruZScoreReplayData_DoesNotCorruptPartitions(int windowSize, int seed)
    {
        var (warmup, stream) = BruZScoreReplayHelper.BuildWarmupAndStream(windowSize, seed, 20_000);
        var stats = new SlidingWindowStats<double>(warmup, windowSize: windowSize);
        var reference = new ReferenceSlidingWindowRanker<double>(warmup, windowSize);

        for (var i = 0; i < stream.Count; i++)
        {
            var magnitude = stream[i];
            if (i % 4 == 0)
            {
                _ = stats.GetZScore(magnitude);
                reference.Add(magnitude);
            }
            else
            {
                _ = stats.ReplaceLastAndGetZScore(magnitude);
                reference.RemoveLast();
                reference.Add(magnitude);
            }

            stats.Count.Should().Be(windowSize);
            RankerInvariantChecker.AssertInvariants(stats, reference.Values);
        }
    }

    /// <summary>
    /// The last <paramref name="windowSize"/> values of the closed-bar history, which is exactly
    /// what the window holds before the current bar contributes anything.
    /// </summary>
    private static List<double> TakeWindow(List<double> history, int windowSize)
    {
        var skip = Math.Max(0, history.Count - windowSize);
        return history.Skip(skip).ToList();
    }

    private static SlidingWindowStats<double> BuildPriorOnly(List<double> priorWindow, int windowSize)
    {
        return new SlidingWindowStats<double>(priorWindow, windowSize: windowSize);
    }
}
