namespace SlidingWindowRanker.Tests;

public class CollectionExtensionTests
{
    [Fact]
    public void IsSortedAscending_ShouldReturnTrue_ForEmptyList()
    {
        var list = new List<int>();
        var result = list.IsSortedAscending();
        Assert.True(result);
    }

    [Fact]
    public void IsSortedAscending_ShouldReturnTrue_ForSortedAscendingList()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var result = list.IsSortedAscending();
        Assert.True(result);
    }

    [Fact]
    public void IsSortedAscending_ShouldReturnFalse_ForUnsortedList()
    {
        var list = new List<int> { 5, 3, 1, 4, 2 };
        var result = list.IsSortedAscending();
        Assert.False(result);
    }

    [Fact]
    public void GetLowerBound_ShouldReturnCorrectIndex()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var result = list.GetLowerBound(3);
        Assert.Equal(2, result);
    }

    [Fact]
    public void GetUpperBound_ShouldReturnCorrectIndex()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var result = list.GetUpperBound(3);
        Assert.Equal(3, result);
    }
}