using FluentAssertions;

namespace SlidingWindowRanker.Tests;

public class UnsafePartitionTests
{
    [Fact]
    public void Constructor_ShouldInitializeValues()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };

        // Act
        var partition = new UnsafePartition<int>(values);

        // Assert
        partition.Values.Should().Equal(values);
    }

    [Fact (Skip = "WIP")]
    public void NeedsSplitting_ShouldReturnTrue_WhenCapacityReached()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new UnsafePartition<int>(values);
        partition.Insert(4);
        partition.Insert(4);
        partition.IsFull.Should().BeFalse();
        partition.Insert(4);

        // Assert
        partition.IsFull.Should().BeTrue();
    }

    [Fact (Skip = "WIP")]
    public void SplittingPartitionAtBeginningOfPartition_ShouldMaintainCorrectLowerBounds()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new UnsafePartition<int>(values);
        partition.Insert(4);
        partition.Insert(4);
        partition.IsFull.Should().BeFalse();
        partition.Insert(4);
        partition.IsFull.Should().BeTrue();
        var partitionLowerBound = partition.LowerBound;

        // Act
        const int ValueToInsert = 0;
        var rightPartition = partition.SplitAndInsert(ValueToInsert);
        partition.LowerBound.Should().Be(partitionLowerBound, "the lower bound should not change");
        rightPartition.LowerBound.Should().Be(partitionLowerBound + partition.Count - 1, "Not adjusted yet");
        partition.Contains(ValueToInsert).Should().BeTrue("the value should be in the old (left) partition");
        partition.Values.IsSortedAscending().Should().BeTrue("the values should be sorted");
        rightPartition.Values.IsSortedAscending().Should().BeTrue("the values should be sorted");

        var lowerBoundOfValueInPartition = partition.Values.LowerBound(0);
        lowerBoundOfValueInPartition.Should().Be(0, "the value should be in the old (left) partition");
    }

    [Fact (Skip = "WIP")]
    public void SplittingPartitionAtEndOfPartition_ShouldMaintainCorrectLowerBounds()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new UnsafePartition<int>(values);
        partition.Insert(4);
        partition.Insert(4);
        partition.IsFull.Should().BeFalse();
        partition.Insert(4);
        partition.IsFull.Should().BeTrue();
        var partitionLowerBound = partition.LowerBound;

        // Act
        const int ValueToInsert = 4;
        var rightPartition = partition.SplitAndInsert(ValueToInsert);
        partition.LowerBound.Should().Be(partitionLowerBound);
        rightPartition.LowerBound.Should().Be(partitionLowerBound + partition.Count - 1, "Not adjusted yet");
        rightPartition.Contains(ValueToInsert).Should().BeTrue("the value should be in the old (left) partition");
        partition.Values.IsSortedAscending().Should().BeTrue("the values should be sorted");
        rightPartition.Values.IsSortedAscending().Should().BeTrue("the values should be sorted");

        var lowerBoundOfValueInPartition = partition.Values.LowerBound(0);
        lowerBoundOfValueInPartition.Should().Be(0, "the value should be in the old (left) partition");
    }

    [Fact (Skip = "WIP")]
    public void SplittingPartitionInMiddleOfPartition_ShouldMaintainCorrectLowerBounds()
    {
        // Arrange
        var values = new List<int> { 1, 2, 3 };
        var partition = new UnsafePartition<int>(values);
        partition.Insert(4);
        partition.Insert(4);
        partition.IsFull.Should().BeFalse();
        partition.Insert(4);
        partition.IsFull.Should().BeTrue();
        var partitionLowerBound = partition.LowerBound;

        // Act
        const int ValueToInsert = 2;
        var rightPartition = partition.SplitAndInsert(ValueToInsert);
        partition.LowerBound.Should().Be(partitionLowerBound);
        rightPartition.LowerBound.Should().Be(partitionLowerBound + partition.Count - 1, "Not adjusted yet");
        partition.Contains(ValueToInsert).Should().BeTrue("the value should be in the old (left) partition");
        partition.Values.IsSortedAscending().Should().BeTrue("the values should be sorted");
        rightPartition.Values.IsSortedAscending().Should().BeTrue("the values should be sorted");

        var lowerBoundOfValueInPartition = partition.Values.LowerBound(0);
        lowerBoundOfValueInPartition.Should().Be(0, "the value should be in the old (left) partition");
    }

    [Fact]
    public void Insert_ShouldAddValueInSortedOrderClosestToRight()
    {
        // Arrange
        var values = new List<int> { 1, 3, 5 };
        var partition = new UnsafePartition<int>(values);

        // Act
        partition.Insert(4);

        // Assert
        partition.Values.Should().Equal(new List<int> { 1, 3, 4, 5 });
    }

    [Fact]
    public void Insert_ShouldAddValueInSortedOrderClosestToLeft()
    {
        // Arrange
        var values = new List<int> { 1, 3, 5 };
        var partition = new UnsafePartition<int>(values);

        // Act
        partition.Insert(2);

        // Assert
        partition.Values.Should().Equal(new List<int> { 1, 2, 3, 5 });
    }

    [Fact]
    public void Remove_ShouldRemoveValue()
    {
        // Arrange
        var values = new List<int> { 1, 3, 5 };
        var partition = new UnsafePartition<int>(values);

        // Act
        partition.Remove(3);

        // Assert
        partition.Values.Should().Equal(new List<int> { 1, 5 });
    }
}