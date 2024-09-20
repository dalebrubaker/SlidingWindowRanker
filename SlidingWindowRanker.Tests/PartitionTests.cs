namespace SlidingWindowRanker.Tests;

public class PartitionTests
{
    [Fact]
    public void Constructor_ShouldInitializeValues()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };

        // Act
        var partition = new Partition<int>(values);

        // Assert
        Assert.Equal(values, partition.Values);
        Assert.Equal(6, partition.Values.Capacity); // Capacity should be double the initial count
    }

    [Fact]
    public void NeedsSplitting_ShouldReturnTrue_WhenCapacityReached()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new Partition<int>(values);
        partition.Insert(4, 3);
        partition.Insert(4, 4);
        partition.Insert(4, 5);

        // Act
        var needsSplitting = partition.NeedsSplitting;

        // Assert
        Assert.True(needsSplitting);
    }

    [Fact]
    public void Insert_ShouldAddValueInSortedOrder()
    {
        // Arrange
        var values = new List<int> { 1, 3, 5 };
        var partition = new Partition<int>(values);

        // Act
        partition.Insert(4, 2);

        // Assert
        Assert.Equal([1, 3, 4, 5], partition.Values);
    }

    [Fact]
    public void Remove_ShouldRemoveValue()
    {
        // Arrange
        var values = new List<int> { 1, 3, 5 };
        var partition = new Partition<int>(values);

        // Act
        partition.Remove(3);

        // Assert
        Assert.Equal([1, 5], partition.Values);
    }
}