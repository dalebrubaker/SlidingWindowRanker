using FluentAssertions;

namespace SlidingWindowRanker.Tests;

public class SlidingWindowRankerCorrectnessTests
{
    [Fact]
    public void PartialWindow_FirstReachesWindowSize_RetainsEveryValue()
    {
        var ranker = new SlidingWindowRanker<int>(
            initialValues: [10, 20],
            partitionCount: 2,
            windowSize: 3);

        var rank = ranker.GetRank(30);

        rank.Should().Be(2.0 / 3.0);
        ranker.TestQueueValues.Should().Equal(10, 20, 30);
        ranker.TestValues.Should().Equal(10, 20, 30);
        ranker.TestRankDenominator.Should().Be(3);
    }

    [Fact]
    public void EmptySeed_WithExplicitWindowSize_FillsAndSlidesCorrectly()
    {
        const int windowSize = 4;
        var ranker = new SlidingWindowRanker<double>([], partitionCount: 2, windowSize: windowSize);
        var reference = new ReferenceSlidingWindowRanker<double>([], windowSize);
        var stream = new[] { 0.0, -2.0, 2.0, -0.0, 7.0, -7.0 };

        for (var i = 0; i < stream.Length; i++)
        {
            RankerInvariantChecker.AssertStep(ranker, reference, stream[i], i + 1);
        }
    }

    [Fact]
    public void EveryInitialCount_FromZeroThroughWindowSize_IsCorrect()
    {
        const int windowSize = 8;
        for (var initialCount = 0; initialCount <= windowSize; initialCount++)
        {
            var initial = Enumerable.Range(0, initialCount).Select(i => 100 - i).ToList();
            var ranker = new SlidingWindowRanker<int>(initial, partitionCount: 3, windowSize: windowSize);
            var reference = new ReferenceSlidingWindowRanker<int>(initial, windowSize);
            RankerInvariantChecker.AssertState(ranker, reference.Values, initialCount);

            for (var operation = 0; operation < 20; operation++)
            {
                var value = operation % 4 == 0 ? 0 : operation % 7;
                RankerInvariantChecker.AssertStep(
                    ranker,
                    reference,
                    value,
                    initialCount + operation + 1);
            }
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 3)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(3, 3)]
    public void SmallWindowAndPartitionCounts_AgreeWithReference(int windowSize, int partitionCount)
    {
        var ranker = new SlidingWindowRanker<int>([], partitionCount, windowSize);
        var reference = new ReferenceSlidingWindowRanker<int>([], windowSize);
        var stream = new[] { 0, 0, 1, -1, 1, 0, -1, 2, 2, -2 };

        for (var i = 0; i < stream.Length; i++)
        {
            RankerInvariantChecker.AssertStep(ranker, reference, stream[i], i + 1);
        }
    }

    [Theory]
    [InlineData(2, 2, 1)]
    [InlineData(3, 2, 2)]
    [InlineData(6, 2, 3)]
    public void PartitionSizesOneTwoAndThree_AgreeWithReference(
        int windowSize,
        int partitionCount,
        int expectedPartitionSize)
    {
        var initial = Enumerable.Range(0, windowSize).Reverse().ToList();
        var ranker = new SlidingWindowRanker<int>(initial, partitionCount, windowSize);
        var reference = new ReferenceSlidingWindowRanker<int>(initial, windowSize);

        ranker.TestPartitions[0].Test_PartitionSize.Should().Be(expectedPartitionSize);
        for (var i = 0; i < 30; i++)
        {
            RankerInvariantChecker.AssertStep(ranker, reference, i % 5 - 2, windowSize + i + 1);
        }
    }

    [Fact]
    public void InitialValues_AreChronologicalOldestToNewest_NotReverseChronological()
    {
        var initial = new List<int> { 30, 20, 10 };
        var ranker = new SlidingWindowRanker<int>(initial, partitionCount: 2, windowSize: 3);
        var reference = new ReferenceSlidingWindowRanker<int>(initial, 3);

        RankerInvariantChecker.AssertStep(ranker, reference, 25, observationsSeen: 4);

        ranker.TestQueueValues.Should().Equal(20, 10, 25);
    }

    [Fact]
    public void DuplicateBoundaryValues_AndEqualEviction_AgreeWithReference()
    {
        const int windowSize = 9;
        var initial = new List<double> { -2, 0, 1, 1, 1, 2, 2, 3, 3 };
        var stream = new[] { -2.0, 0.0, -0.0, 1.0, 1.0, 2.0, 3.0, 3.0, -2.0, 2.0, 2.0, 2.0 };
        var ranker = new SlidingWindowRanker<double>(initial, partitionCount: 5, windowSize: windowSize);
        var reference = new ReferenceSlidingWindowRanker<double>(initial, windowSize);

        for (var i = 0; i < stream.Length; i++)
        {
            RankerInvariantChecker.AssertStep(ranker, reference, stream[i], initial.Count + i + 1);
        }
    }

    [Fact]
    public void AllEqualValues_RemainCorrectAcrossManySplitsAndRemovals()
    {
        const int windowSize = 7;
        var initial = Enumerable.Repeat(5.0, windowSize).ToList();
        var ranker = new SlidingWindowRanker<double>(initial, partitionCount: 7, windowSize: windowSize);
        var reference = new ReferenceSlidingWindowRanker<double>(initial, windowSize);

        for (var i = 0; i < 2_000; i++)
        {
            RankerInvariantChecker.AssertStep(ranker, reference, 5.0, initial.Count + i + 1);
        }
    }

    [Fact]
    public void PartitionCreationSplittingAndCompleteRemoval_PreserveExactState()
    {
        const int windowSize = 6;
        var initial = new List<int> { -30, -20, -10, 10, 20, 30 };
        var stream = Enumerable.Range(0, 100).Select(i => i % 2 == 0 ? 100 + i : -100 - i).ToList();
        var ranker = new SlidingWindowRanker<int>(initial, partitionCount: 6, windowSize: windowSize);
        var reference = new ReferenceSlidingWindowRanker<int>(initial, windowSize);

        for (var i = 0; i < stream.Count; i++)
        {
            RankerInvariantChecker.AssertStep(ranker, reference, stream[i], initial.Count + i + 1);
        }

        ranker.CountPartitionSplits.Should().BeGreaterThan(0);
        ranker.CountPartitionRemoves.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LongRunningDuplicateHeavySignedDoubleStream_AgreesWithReference()
    {
        const int windowSize = 31;
        var initial = Enumerable.Range(0, 13).Select(i => (double)(i % 5 - 2)).ToList();
        var ranker = new SlidingWindowRanker<double>(initial, partitionCount: 11, windowSize: windowSize);
        var reference = new ReferenceSlidingWindowRanker<double>(initial, windowSize);

        for (var i = 0; i < 25_000; i++)
        {
            var value = (i % 17) switch
            {
                0 => 0.0,
                1 => -0.0,
                2 => 6198.600613839286,
                3 => -6198.600613839286,
                _ => (i * 17 % 13) - 6
            };
            RankerInvariantChecker.AssertStep(ranker, reference, value, initial.Count + i + 1);
        }
    }

    [Fact]
    public void InitialPopulationLargerThanWindow_IsRejected()
    {
        var action = () => new SlidingWindowRanker<int>([1, 2, 3], partitionCount: 1, windowSize: 2);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("windowSize");
    }

    [Fact]
    public void MaxValueWindow_DoesNotOverflowPartitionSizeCalculation()
    {
        var ranker = new SlidingWindowRanker<int>([1, 2, 3], windowSize: int.MaxValue);

        ranker.TestPartitions[0].Test_PartitionSize.Should().Be(46_341);
    }
}
