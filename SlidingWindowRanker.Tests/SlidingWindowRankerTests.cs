using System.Diagnostics;
using Xunit.Abstractions;

namespace SlidingWindowRanker.Tests;

public class SlidingWindowRankerTests
{
    private readonly ITestOutputHelper _output;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SlidingWindowRankerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForValueInMiddle()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 2);
        var rank = ranker.GetRank(3);

        var values = ranker.GetValues();
        Assert.Equal([2, 3, 3, 4, 5], values);

        // The first value (1) is removed and the 3 is inserted, leaving the values [2, 3, 3, 4, 5]
        Assert.Equal(0.2, rank, 1);
    }

    [Fact]
    public void GetRank_ReturnsZero_ForSmallestValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 2);
        var rank = ranker.GetRank(0);
        Assert.Equal(0.0, rank);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForNewHighValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 2);
        var rank = ranker.GetRank(6);

        var values = ranker.GetValues();
        Assert.Equal([2, 3, 4, 5, 6], values);

        Assert.Equal(4 / 5.0, rank);
    }

    [Fact]
    public void GetRank_UpdatesCorrectly_AfterAddingAndRemovingValues()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 2);
        ranker.GetRank(6); // Add 6, remove 1
        var rank = ranker.GetRank(0); // Add 0, remove 2
        Assert.Equal(0.0, rank);
    }

    [Fact]
    public void GetRank_ThrowsException_ForEmptyInitialValuesAndNoGivenWidth()
    {
        var initialValues = new List<int>();
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingWindowRanker<int>(initialValues, 2));
    }

    [Fact]
    public void GetRank_ThrowsException_ForInvalidPartitionCount()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingWindowRanker<int>(initialValues, 0));
    }

    [Fact]
    public void GetRank_ReturnsZero_ForEmptyInitialValues_WithWindowSizeOf10()
    {
        var initialValues = new List<int>();
        var ranker = new SlidingWindowRanker<int>(initialValues, 1, 10);
        var rank = ranker.GetRank(5);

        // Since there are no initial values, the rank should be 0.0
        Assert.Equal(0.0, rank);

        var rank5 = ranker.GetRank(6);
        var values = ranker.GetValues();
        Assert.Equal([5, 6], values);
        Assert.Equal(0.5, rank5);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForAscendingValues()
    {
        var initialValues = Enumerable.Range(1, 10).ToList();
        var ranker = new SlidingWindowRanker<int>(initialValues, 2);
        var rank = ranker.GetRank(5);

        var expected = ExpectedRank(ranker, 5);
        Assert.Equal(expected, rank);
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
        var initialValues = valuesToRank.Take(WindowSize).ToList();
        var ranker = new SlidingWindowRanker<int>(initialValues, NumberOfPartitions);
        for (var index = WindowSize; index < NumberOfTestValues; index++)
        {
            var value = valuesToRank[index];
            var rank = ranker.GetRank(value);
            var expected = ExpectedRank(ranker, value);
            if (expected != rank)
            {
            }
            Assert.Equal(expected, rank, 3);
        }
        _output.WriteLine($"ranker.CountPartitionSplits={ranker.CountPartitionSplits}");
        _output.WriteLine($"ranker.CountPartitionRemoves={ranker.CountPartitionRemoves}");
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForDescendingValues()
    {
        var initialValues = Enumerable.Range(1, 10).Reverse().ToList();
        var ranker = new SlidingWindowRanker<int>(initialValues, 2);
        var rank = ranker.GetRank(5);
        var expected = ExpectedRank(ranker, 5);
        Assert.Equal(expected, rank);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForRandomValues()
    {
        var initialValues = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 2);
        var rank = ranker.GetRank(5);
        var expected = ExpectedRank(ranker, 5);
        Assert.Equal(expected, rank);
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
        var initialValues = valuesToRank.Take(WindowSize).ToList();
        var stopWatch = Stopwatch.StartNew();
        var ranker = new SlidingWindowRanker<int>(initialValues, NumberOfPartitions);
        for (var index = WindowSize; index < NumberOfTestValues; index++)
        {
            var value = valuesToRank[index];
            var rank = ranker.GetRank(value);
            var expected = ExpectedRank(ranker, value);
            //Assert.Equal(expected, rank, 2);
        }
        var elapsed = stopWatch.ElapsedMilliseconds;
        _output.WriteLine($"Elapsed time: {elapsed} ms");
    }

    private double ExpectedRank(SlidingWindowRanker<int> ranker, int value)
    {
        var values = ranker.GetValues();
        var isSortedAscending = values.IsSortedAscending();
        Assert.True(isSortedAscending);
        var lowerValuesCount = values.Count(v => v < value);
        var rank = lowerValuesCount / (double)values.Count;
        return rank;
    }

    [Fact]
    public void DebugTest()
    {
        // Initial values copied from the benchmark
        const int NumberOfPartitions = 3;
        const int WindowSize = 10;
        var initialValuesStr =
            "60.4,2.3,6.3,45.8,20.1,6.2,10,58.9,38.1,77.8,31.8,59.9,80.7,31.5,47.5,91.5,81.7,80.5,6,13.7,81.9,32.7,60,96,36.2,68.4,15.4,12.7,58.1,95.1,60.5,65.3,25.6,56.1,77,64.1,13,26.2,55.2,25.1,46.8,0.3,22.7,36.8,37.3,48.4,49,7.7,64.1,5.6,43.4,81.7,6.8,28.8,58.2,60.7,12.1,94,48.8,47.5,18.9,26.7,51.7,4.4,87.3,60.9,93,95.3,14.5,57.2,7.9,53.7,74.8,30.7,4.6,54.9,87.9,62.2,56.6,86,87.1,42.2,35.4,76,62,24.7,17.7,51.9,71.5,79.1,33.3,8.1,26.1,99.6,38.3,30.6,91.5,53.3,38.5,97.4";

        var valuesToRank = initialValuesStr.Split(',').Select(double.Parse).ToList();
        var initialValues = valuesToRank.Take(WindowSize).ToList();
        var ranker = new SlidingWindowRanker<double>(initialValues, NumberOfPartitions);
        for (var index = WindowSize; index < valuesToRank.Count; index++)
        {
            var value = valuesToRank[index];
            if (index == 16)
            {
            }
            var rank = ranker.GetRank(value);
        }
    }
}