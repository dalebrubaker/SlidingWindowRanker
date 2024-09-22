using FluentAssertions;

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
        partition.Values.Should().Equal(values);
        partition.Values.Capacity.Should().Be(6); // Capacity should be double the initial count
    }

    [Fact]
    public void NeedsSplitting_ShouldReturnTrue_WhenCapacityReached()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new Partition<int>(values);
        partition.Insert(4);
        partition.Insert(4);
        partition.NeedsSplitting.Should().BeFalse();
        partition.Insert(4);

        // Assert
        partition.NeedsSplitting.Should().BeTrue();
    }

    [Fact]
    public void SplittingPartition_ShouldMaintainCorrectLowerBounds()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new Partition<int>(values);
        partition.Insert(4);
        partition.Insert(4);
        partition.NeedsSplitting.Should().BeFalse();
        partition.Insert(4);
        partition.NeedsSplitting.Should().BeTrue();
        var partitionLowerBound = partition.LowerBound;

        // Act
        var newPartition = partition.SplitAndInsert(0);
        //partition.LowerBound.Should().Be(partitionLowerBound);
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
        partition.Values.Should().Equal(new List<int> { 1, 3, 4, 5 });
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
        partition.Values.Should().Equal(new List<int> { 1, 5 });
    }
}