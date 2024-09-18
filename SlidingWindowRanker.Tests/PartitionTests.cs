﻿namespace SlidingWindowRanker.Tests;

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
    public void LowestValue_ShouldReturnFirstValue()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new Partition<int>(values);

        // Act
        var lowestValue = partition.LowestValue;

        // Assert
        Assert.Equal(1, lowestValue);
    }

    [Fact]
    public void HighestValue_ShouldReturnLastValue()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new Partition<int>(values);

        // Act
        var highestValue = partition.HighestValue;

        // Assert
        Assert.Equal(3, highestValue);
    }

    [Fact]
    public void NeedsSplitting_ShouldReturnTrue_WhenCapacityReached()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new Partition<int>(values);
        partition.Insert(4);
        partition.Insert(4);
        partition.Insert(4);

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
        partition.Insert(4);

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

    [Fact]
    public void Split_ShouldDivideValuesCorrectly()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3, 4, 5 };
        var partition = new Partition<int>(values);

        // Act
        var rightPartition = partition.Split(2);

        // Assert
        Assert.Equal([1, 2], partition.Values);
        Assert.Equal([3, 4, 5], rightPartition.Values);
    }

    [Fact]
    public void SplitAtEndValue_ShouldDivideValuesCorrectly()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3, 4, 5 };
        var partition = new Partition<int>(values);

        // Act
        var rightPartition = partition.Split(5);

        // Assert
        Assert.Equal([1, 2, 3, 4, 5], partition.Values);
        Assert.Equal([], rightPartition.Values);
    }
}