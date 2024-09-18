namespace SlidingWindowRanker.Tests;

public class SlidingWindowRankerTests
{
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
    public void GetRank_ThrowsException_ForEmptyInitialValuesAndNoGivenWidt()
    {
        var initialValues = new List<int>();
        Assert.Throws<ArgumentException>(() => new SlidingWindowRanker<int>(initialValues, 2));
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

    private double ExpectedRank(SlidingWindowRanker<int> ranker, int value)
    {
        var values = ranker.GetValues();
        var isSortedAscending = values.IsSortedAscending();
        Assert.True(isSortedAscending);
        var lowerValuesCount = values.Count(v => v < value);
        var rank = lowerValuesCount / (double)values.Count;
        return rank;
    }
}