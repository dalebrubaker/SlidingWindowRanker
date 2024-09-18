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
    public void GetRank_ThrowsException_ForEmptyInitialValues()
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

    // TODO Test when no values are available at the start
    // TODO Test random values, ascending values and descending values
}