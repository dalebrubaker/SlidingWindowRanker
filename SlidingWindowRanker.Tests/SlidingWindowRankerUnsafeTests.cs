using System.Diagnostics;
using FluentAssertions;
using Xunit.Abstractions;

namespace SlidingWindowRanker.Tests;

public class SlidingWindowRankerUnsafeUnsafeTests
{
    private readonly ITestOutputHelper _output;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SlidingWindowRankerUnsafeUnsafeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForValueInMiddle()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(3);

        ranker.TestValues.Should().BeEquivalentTo(new[] { 2, 3, 3, 4, 5 });

        // The first value (1) is removed and the 3 is inserted, leaving the values [2, 3, 3, 4, 5]
        rank.Should().BeApproximately(0.2, 1);
    }

    [Fact]
    public void GetRank_ReturnsZero_ForSmallestValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(0);
        rank.Should().Be(0.0);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForNewHighValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(6);

        ranker.TestValues.Should().BeEquivalentTo(new[] { 2, 3, 4, 5, 6 });

        rank.Should().Be(4 / 5.0);
    }

    [Fact]
    public void GetRank_UpdatesCorrectly_AfterAddingAndRemovingValues()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        ranker.GetRank(6); // Add 6, remove 1
        var rank = ranker.GetRank(0); // Add 0, remove 2
        rank.Should().Be(0.0);
    }

    [Fact]
    public void GetRank_ThrowsException_ForEmptyInitialValuesAndNoGivenWidth()
    {
        var initialValues = new List<int>();
        Action act = () => new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetRank_ReturnsZero_ForEmptyInitialValues_WithWindowSizeOf10()
    {
        var initialValues = new List<int>();
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 1, 10);
        var rank = ranker.GetRank(5);

        // Since there are no initial values, the rank should be 0.0
        rank.Should().Be(0.0);

        var rank5 = ranker.GetRank(6);
        ranker.TestValues.Should().BeEquivalentTo(new[] { 5, 6 });
        rank5.Should().Be(0.5);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForAscendingValues()
    {
        var initialValues = Enumerable.Range(1, 10).ToList();
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(5);

        var expected = ExpectedRank(ranker, 5);
        rank.Should().Be(expected);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForAscendingValuesInLargeList()
    {
        const int NumberOfTestValues = 1000; // 40000; //  1000;// 100;
        const int NumberOfPartitions = 10; // 200; // 10  // 1;
        const int WindowSize = NumberOfTestValues / 10;

        var valuesToRank = new List<int>(NumberOfTestValues);
        for (var i = 0; i < NumberOfTestValues; i++)
        {
            valuesToRank.Add(i);
        }
        var indexToSplit = NumberOfTestValues - WindowSize;
        var initialValues = valuesToRank.GetRange(indexToSplit, WindowSize).ToList();
        valuesToRank.RemoveRange(indexToSplit, valuesToRank.Count - indexToSplit);
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, NumberOfPartitions);
        for (var index = indexToSplit - 1; index >= 0; index--)
        {
            ranker.DebugGuardPartitionLowerBoundValuesAreCorrect();
            var value = valuesToRank[index];
            if (index is 880 or 870)
            {
            }
            var rank = ranker.GetRank(value);
            //ranker.DebugGuardPartitionLowerBoundValuesAreCorrect();
            var expected = ExpectedRank(ranker, value);
            rank.Should().BeApproximately(expected, 3);
            if (ranker.CountPartitionSplits > 0)
            {
                _output.WriteLine($"ranker.CountPartitionSplits={ranker.CountPartitionSplits}");
            }
            if (ranker.CountPartitionRemoves > 0)
            {
                _output.WriteLine($"ranker.CountPartitionRemoves={ranker.CountPartitionRemoves}");
            }
        }
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForDescendingValues()
    {
        var initialValues = Enumerable.Range(1, 10).Reverse().ToList();
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(5);
        var expected = ExpectedRank(ranker, 5);
        rank.Should().Be(expected);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForRandomValues()
    {
        var initialValues = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3 };
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(5);
        var expected = ExpectedRank(ranker, 5);
        rank.Should().Be(expected);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForRandomValuesInLargeList()
    {
        const int NumberOfTestValues = 10000; // 1000;// 100;
        const int NumberOfPartitions = 10; // 200; // 1;
        const int WindowSize = NumberOfTestValues / 10; // * 2 / 10;

        var random = new Random();
        var valuesToRank = new List<int>(NumberOfTestValues);
        for (var i = 0; i < NumberOfTestValues; i++)
        {
            var value = (int)(random.NextDouble() * 100);
            valuesToRank.Add(value);
        }
        var indexToSplit = NumberOfTestValues - WindowSize;
        var initialValues = valuesToRank.GetRange(indexToSplit, WindowSize).ToList();
        valuesToRank.RemoveRange(indexToSplit, valuesToRank.Count - indexToSplit);
        var stopWatch = Stopwatch.StartNew();
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, NumberOfPartitions);
        for (var index = indexToSplit - 1; index >= 0; index--)
        {
            var value = valuesToRank[index];
            var rank = ranker.GetRank(value);
            var expected = ExpectedRank(ranker, value);
            //rank.Should().BeApproximately(expected, 2);
        }
        var elapsed = stopWatch.ElapsedMilliseconds;
        _output.WriteLine($"Elapsed time: {elapsed} ms");
    }

    private double ExpectedRank(SlidingWindowRankerUnsafe<int> ranker, int value)
    {
        var values = ranker.TestValues;
        var isSortedAscending = values.IsSortedAscending();
        isSortedAscending.Should().BeTrue();
        var lowerValuesCount = values.Count(v => v < value);
        var rank = lowerValuesCount / (double)values.Count;
        return rank;
    }

    [Fact]
    public void DebugTest()
    {
        // Initial values copied from the benchmark
        const int WindowSize = 10;
        var initialValuesStr =
            "74.4,43.6,58.6,16.5,52.9,72.6,63.9,0.9,54.6,99.1,28.8,55,34.2,1.6,55,36.4,32.9,46.9,60.5,4.8,30.1,63,72.2,88.7,72.8,52,49.3,90.8,39.9,11.9,65.9,7.3,49.4,20.2,4.8,19.3,66.1,83.7,45.5,32.4,37.7,89.9,59.9,84.6,4.7,30.5,89.9,97.3,92.2,59.4,63.8,51.5,42.8,86.5,58.6,6.3,13,41.7,71.3,11.5,57.9,79.1,64.3,77.2,47.6,36.8,35.7,80.6,12.8,68,83.4,18.8,28,29,88.7,44.4,44.8,58.1,83.1,44.2,72.1,11.7,60.3,20.5,35.3,47.8,80.6,31,33.9,25.8,89.5,49.5,44,11.4,86.8,18.1,18.5,41.5,39.6,31.7";
        //"60.4,2.3,6.3,45.8,20.1,6.2,10,58.9,38.1,77.8,31.8,59.9,80.7,31.5,47.5,91.5,81.7,80.5,6,13.7,81.9,32.7,60,96,36.2,68.4,15.4,12.7,58.1,95.1,60.5,65.3,25.6,56.1,77,64.1,13,26.2,55.2,25.1,46.8,0.3,22.7,36.8,37.3,48.4,49,7.7,64.1,5.6,43.4,81.7,6.8,28.8,58.2,60.7,12.1,94,48.8,47.5,18.9,26.7,51.7,4.4,87.3,60.9,93,95.3,14.5,57.2,7.9,53.7,74.8,30.7,4.6,54.9,87.9,62.2,56.6,86,87.1,42.2,35.4,76,62,24.7,17.7,51.9,71.5,79.1,33.3,8.1,26.1,99.6,38.3,30.6,91.5,53.3,38.5,97.4";

        var valuesToRank = initialValuesStr.Split(',').Select(double.Parse).ToList();
        var indexToSplit = valuesToRank.Count - WindowSize;
        var initialValues = valuesToRank.GetRange(indexToSplit, WindowSize).ToList();
        valuesToRank.RemoveRange(indexToSplit, valuesToRank.Count - indexToSplit);
        var ranker = new SlidingWindowRankerUnsafe<double>(initialValues);
        for (var index = WindowSize; index < valuesToRank.Count; index++)
        {
            var value = valuesToRank[index];
            if (index == 60)
            {
            }
            var rank = ranker.GetRank(value);
        }
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForMaxValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(int.MaxValue);

        ranker.TestValues.Should().BeEquivalentTo(new[] { 2, 3, 4, 5, int.MaxValue });

        rank.Should().Be(4 / 5.0);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForMinValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(int.MinValue);

        ranker.TestValues.Should().BeEquivalentTo(new[] { int.MinValue, 2, 3, 4, 5 });

        rank.Should().Be(0.0);
    }
}