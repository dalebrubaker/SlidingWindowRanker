using FluentAssertions;

namespace SlidingWindowRanker.Tests;

public class CollectionExtensionTests
{
    [Fact]
    public void IsSortedAscending_ShouldReturnTrue_ForEmptyList()
    {
        var list = new List<int>();
        var result = list.IsSortedAscending();
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSortedAscending_ShouldReturnTrue_ForSortedAscendingList()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var result = list.IsSortedAscending();
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSortedAscending_ShouldReturnFalse_ForUnsortedList()
    {
        var list = new List<int> { 5, 3, 1, 4, 2 };
        var result = list.IsSortedAscending();
        result.Should().BeFalse();
    }

    [Fact]
    public void LowerBound_ShouldReturnCorrectIndex()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var result = list.LowerBound(3);
        result.Should().Be(2);
    }

    [Fact]
    public void LowerBound_ShouldReturnCorrectIndex_WhenElementExists()
    {
        var list = new List<int> { 1, 3, 5, 7, 9 };
        var value = 5;
        var result = list.LowerBound(value);
        result.Should().Be(2);
    }

    [Fact]
    public void LowerBound_ShouldReturnCorrectIndex_WhenElementDoesNotExist()
    {
        var list = new List<int> { 1, 3, 5, 7, 9 };
        var value = 4;
        var result = list.LowerBound(value);
        result.Should().Be(2);
    }
}