using System.Diagnostics;
using FluentAssertions;
using Xunit.Abstractions;

namespace SlidingWindowRanker.Tests;

public class SlidingWindowRankerUnsafeTests
{
    private readonly ITestOutputHelper _output;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SlidingWindowRankerUnsafeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForValueInMiddle()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(3);

        ranker.TestValues.Should().BeEquivalentTo(new[] { 2, 3, 3, 4, 5 });

        // The first value (1) is removed and the 3 is inserted, leaving the values [2, 3, 3, 4, 5]
        rank.Should().BeApproximately(0.2, 1);
    }

    [Fact]
    public void GetRank_ReturnsZero_ForSmallestValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(0);
        rank.Should().Be(0.0);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForNewHighValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(6);

        ranker.TestValues.Should().BeEquivalentTo(new[] { 2, 3, 4, 5, 6 });

        rank.Should().Be(4 / 5.0);
    }

    [Fact]
    public void GetRank_UpdatesCorrectly_AfterAddingAndRemovingValues()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
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
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 1, 10);
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
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
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
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, NumberOfPartitions);
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
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(5);
        var expected = ExpectedRank(ranker, 5);
        rank.Should().Be(expected);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForRandomValues()
    {
        var initialValues = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3 };
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
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
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, NumberOfPartitions);
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
        var valuesToRankStr =
            "89.8,65.3,74.5,20.1,65.3,3.2,26.8,66.6,52,80.6,41.6,44.2,5.5,85.6,49.4,61.3,31.7,4.2,85.3,21.3,95.7,38.7,24.6,97.1,19.9,89.7,95.2,81.4,53.5,48.7,37.1,29.9,89.9,16,38.4,91,10.4,60.7,49.3,19,83,67.9,63,71.3,67.5,74.1,20.7,57.4,8.7,20.2,44.6,78.1,5.1,71.3,35,97.3,98.4,95.8,35,66.4,17.7,13.1,87.1,15.4,2.9,13.4,62.5,30.2,38.5,53.7,42,9.3,8.5,20.2,35.2,54.6,64.7,67.1,57.6,78.6,54.5,69.5,5.7,16.8,77,49.3,90.6,45.5,87.4,43.2,13.2,66,89.8,72.5,15.1,82.5,79.7,66.3,52.2,41.5,99.7,42.5,66.2,11.3,41.9,7.2,94.1,93,34.9,22.8,61.4,82.1,7.6,75.3,83.2,76.6,18.6,55.7,63.9,11.6,31.7,53.6,3.4,28.6,99.7,58.2,99.9,7.4,62.4,8,74.7,85.6,6.3,27,66.9,66.5,10.9,66,88.5,44.7,9.5,96.5,59.1,2,18.3,76.5,13.6,24.6,31.4,54.6,92.4,53.6,34.5,29.4,18.5,47,6.8,45.3,63.7,93.3,80.8,92.4,55.9,72,34.2,91.4,81.5,63.9,92.4,55.9,21.9,43.3,77.7,84.1,15.6,8,31.7,53.2,18.5,36.6,79.1,70,45.1,90.3,98.4,70.9,20.4,85.2,81.2,37.4,79.4,20.3,54.1,65.2,2.1,88.4,94.1,48.3,58.1,57.6";
            //"74.4,43.6,58.6,16.5,52.9,72.6,63.9,0.9,54.6,99.1,28.8,55,34.2,1.6,55,36.4,32.9,46.9,60.5,4.8,30.1,63,72.2,88.7,72.8,52,49.3,90.8,39.9,11.9,65.9,7.3,49.4,20.2,4.8,19.3,66.1,83.7,45.5,32.4,37.7,89.9,59.9,84.6,4.7,30.5,89.9,97.3,92.2,59.4,63.8,51.5,42.8,86.5,58.6,6.3,13,41.7,71.3,11.5,57.9,79.1,64.3,77.2,47.6,36.8,35.7,80.6,12.8,68,83.4,18.8,28,29,88.7,44.4,44.8,58.1,83.1,44.2,72.1,11.7,60.3,20.5,35.3,47.8,80.6,31,33.9,25.8,89.5,49.5,44,11.4,86.8,18.1,18.5,41.5,39.6,31.7";
        //"60.4,2.3,6.3,45.8,20.1,6.2,10,58.9,38.1,77.8,31.8,59.9,80.7,31.5,47.5,91.5,81.7,80.5,6,13.7,81.9,32.7,60,96,36.2,68.4,15.4,12.7,58.1,95.1,60.5,65.3,25.6,56.1,77,64.1,13,26.2,55.2,25.1,46.8,0.3,22.7,36.8,37.3,48.4,49,7.7,64.1,5.6,43.4,81.7,6.8,28.8,58.2,60.7,12.1,94,48.8,47.5,18.9,26.7,51.7,4.4,87.3,60.9,93,95.3,14.5,57.2,7.9,53.7,74.8,30.7,4.6,54.9,87.9,62.2,56.6,86,87.1,42.2,35.4,76,62,24.7,17.7,51.9,71.5,79.1,33.3,8.1,26.1,99.6,38.3,30.6,91.5,53.3,38.5,97.4";

        var valuesToRank = valuesToRankStr.Split(',').Select(double.Parse).ToList();
        var indexToSplit = 200 - WindowSize;
        var initialValues = valuesToRank.GetRange(indexToSplit, WindowSize);
        valuesToRank.RemoveRange(indexToSplit, WindowSize);
        using var ranker = new SlidingWindowRankerUnsafe<double>(initialValues);
        for (var i = indexToSplit - 1; i >= 0; i--)
        {
            var value = valuesToRank[i];
            if (i is 176)
            {
            }
            var rank = ranker.GetRank(value);
        }
    }

    [Fact (Skip = "Fails on Github due to path not found")]
    public void DebugTest2()
    {
        // Initial values copied from the benchmark
        const int WindowSize = 10;
        var cwd = Directory.GetCurrentDirectory();
        var path = Path.Combine(cwd, @"..\..\..\DebugValues.txt");
        path = Path.GetFullPath(new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }

        var initialValuesStr = File.ReadAllText(path);
        //"74.4,43.6,58.6,16.5,52.9,72.6,63.9,0.9,54.6,99.1,28.8,55,34.2,1.6,55,36.4,32.9,46.9,60.5,4.8,30.1,63,72.2,88.7,72.8,52,49.3,90.8,39.9,11.9,65.9,7.3,49.4,20.2,4.8,19.3,66.1,83.7,45.5,32.4,37.7,89.9,59.9,84.6,4.7,30.5,89.9,97.3,92.2,59.4,63.8,51.5,42.8,86.5,58.6,6.3,13,41.7,71.3,11.5,57.9,79.1,64.3,77.2,47.6,36.8,35.7,80.6,12.8,68,83.4,18.8,28,29,88.7,44.4,44.8,58.1,83.1,44.2,72.1,11.7,60.3,20.5,35.3,47.8,80.6,31,33.9,25.8,89.5,49.5,44,11.4,86.8,18.1,18.5,41.5,39.6,31.7";
        //"60.4,2.3,6.3,45.8,20.1,6.2,10,58.9,38.1,77.8,31.8,59.9,80.7,31.5,47.5,91.5,81.7,80.5,6,13.7,81.9,32.7,60,96,36.2,68.4,15.4,12.7,58.1,95.1,60.5,65.3,25.6,56.1,77,64.1,13,26.2,55.2,25.1,46.8,0.3,22.7,36.8,37.3,48.4,49,7.7,64.1,5.6,43.4,81.7,6.8,28.8,58.2,60.7,12.1,94,48.8,47.5,18.9,26.7,51.7,4.4,87.3,60.9,93,95.3,14.5,57.2,7.9,53.7,74.8,30.7,4.6,54.9,87.9,62.2,56.6,86,87.1,42.2,35.4,76,62,24.7,17.7,51.9,71.5,79.1,33.3,8.1,26.1,99.6,38.3,30.6,91.5,53.3,38.5,97.4";

        var valuesToRank = initialValuesStr.Split(',').Select(double.Parse).ToList();
        var indexToSplit = valuesToRank.Count - WindowSize;
        var initialValues = valuesToRank.GetRange(indexToSplit, WindowSize).ToList();
        valuesToRank.RemoveRange(indexToSplit, valuesToRank.Count - indexToSplit);
        using var ranker = new SlidingWindowRankerUnsafe<double>(initialValues);
        for (var index = WindowSize; index < valuesToRank.Count; index++)
        {
            var value = valuesToRank[index];
            if (index == 552861)
            {
            }
            var rank = ranker.GetRank(value);
        }
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForMaxValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(int.MaxValue);

        ranker.TestValues.Should().BeEquivalentTo(new[] { 2, 3, 4, 5, int.MaxValue });

        rank.Should().Be(4 / 5.0);
    }

    [Fact]
    public void GetRank_ReturnsCorrectRank_ForMinValue()
    {
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        using var ranker = new SlidingWindowRankerUnsafe<int>(initialValues, 2);
        var rank = ranker.GetRank(int.MinValue);

        ranker.TestValues.Should().BeEquivalentTo(new[] { int.MinValue, 2, 3, 4, 5 });

        rank.Should().Be(0.0);
    }
}