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
    public void LowerBound_ShouldReturnCorrectIndex()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var result = list.LowerBound(3);
        Assert.Equal(2, result);
    }

    [Fact]
    public void LowerBound_ShouldReturnCorrectIndex_WhenElementExists()
    {
        // Arrange
        var list = new List<int> { 1, 3, 5, 7, 9 };
        var value = 5;

        // Act
        var result = list.LowerBound(0, list.Count, value);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void LowerBound_ShouldReturnCorrectIndex_WhenElementDoesNotExist()
    {
        // Arrange
        var list = new List<int> { 1, 3, 5, 7, 9 };
        var value = 6;

        // Act
        var result = list.LowerBound(0, list.Count, value);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void LowerBound_ShouldReturnZero_WhenValueIsLessThanAllElements()
    {
        // Arrange
        var list = new List<int> { 1, 3, 5, 7, 9 };
        var value = 0;

        // Act
        var result = list.LowerBound(0, list.Count, value);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void LowerBound_ShouldReturnCount_WhenValueIsGreaterThanAllElements()
    {
        // Arrange
        var list = new List<int> { 1, 3, 5, 7, 9 };
        var value = 10;

        // Act
        var result = list.LowerBound(0, list.Count, value);

        // Assert
        Assert.Equal(list.Count, result);
    }

    [Fact]
    public void UpperBound_ShouldReturnCorrectIndex()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var result = list.UpperBound(3);
        Assert.Equal(3, result);
    }

    [Fact]
    public void UpperBound_Array_ReturnsCorrectIndex()
    {
        int[] array = [1, 2, 2, 3, 4, 5];
        var result = array.UpperBound(2);
        Assert.Equal(3, result);
    }

    [Fact]
    public void UpperBound_Array_EmptyArray()
    {
        int[] array = [];
        var result = array.UpperBound(1);
        Assert.Equal(0, result);
    }

    [Fact]
    public void UpperBound_Array_AllElementsLessThanValue()
    {
        int[] array = [1, 2, 3];
        var result = array.UpperBound(4);
        Assert.Equal(3, result);
    }

    [Fact]
    public void UpperBound_Array_AllElementsGreaterThanValue()
    {
        int[] array = [2, 3, 4];
        var result = array.UpperBound(1);
        Assert.Equal(0, result);
    }

    [Fact]
    public void UpperBound_Array_SingleElement()
    {
        int[] array = [2];
        var result = array.UpperBound(2);
        Assert.Equal(1, result);
    }

    [Fact]
    public void UpperBound_Array_Range()
    {
        int[] array = [1, 2, 2, 3, 4, 5];
        var result = array.UpperBound(1, 4, 2);
        Assert.Equal(3, result);
    }

    [Fact]
    public void UpperBound_Array_Range_EmptyRange()
    {
        int[] array = [1, 2, 2, 3, 4, 5];
        var result = array.UpperBound(2, 2, 2);
        Assert.Equal(2, result);
    }

    [Fact]
    public void UpperBound_Array_Range_InvalidRange()
    {
        int[] array = [1, 2, 2, 3, 4, 5];
        Assert.Throws<ArgumentOutOfRangeException>(() => array.UpperBound(-1, 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.UpperBound(2, 7, 2));
    }
}