using FluentAssertions;
using Xunit.Abstractions;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace SlidingWindowRanker.Tests;

public class SlidingWindowRankerLowLevelTests
{
    private readonly ITestOutputHelper _output;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SlidingWindowRankerLowLevelTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Remove_ShouldRemovePartitionWhenPartitionSize1()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 5);
        ranker.TestPartitions.Count.Should().Be(5);
        var partition1 = ranker.TestPartitions[1] as Partition<int>;
        partition1.Test_PartitionSize.Should().Be(1);
        partition1.Test_LowerBound.Should().Be(1);

        // Act
        var valueToRemove = 1;
        ranker.Test_DoRemove(valueToRemove);

        // Assert
        ranker.TestPartitions.Count.Should().Be(4, "The first partition was removed.");
        ranker.CountPartitionRemoves.Should().Be(1);
    }

    [Fact]
    public void Remove_ShouldKeepPartitionWhenPartitionSize2()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 3);
        ranker.TestPartitions.Count.Should().Be(3);
        var partition1 = ranker.TestPartitions[1] as Partition<int>;
        partition1.Test_PartitionSize.Should().Be(2);
        partition1.Test_LowerBound.Should().Be(2);

        // Act
        var valueToRemove = 3;
        ranker.Test_DoRemove(valueToRemove);

        // Assert
        ranker.TestPartitions.Count.Should().Be(3, "The partition was not removed because it is not empty.");
        ranker.CountPartitionRemoves.Should().Be(0);
    }

    [Fact]
    public void InsertWithoutSplitShouldSetCorrectIncrementPosition()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2, 4, 5 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 1);
        ranker.TestPartitions.Count.Should().Be(1);
        var partition0 = ranker.TestPartitions[0];

        // Act
        var valueToInsert = 3;
        ranker.Test_DoInsert(valueToInsert);

        // Assert
        ranker.CountPartitionSplits.Should().Be(0);
        partition0.Values.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void InsertWithSplitShouldWorkCorrectlyAddingToEndOfSplitPartition()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 1);
        ranker.TestPartitions.Count.Should().Be(1);
        var partition0 = ranker.TestPartitions[0];

        // Act
        ranker.Test_DoInsert(3);
        ranker.Test_DoInsert(5);
        partition0.IsFull.Should().BeTrue();
        ranker.Test_DoInsert(4);

        // Assert
        ranker.CountPartitionSplits.Should().Be(1);
        ranker.TestValues.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void InsertWithSplitShouldWorkCorrectlyAddingToEndOfNewPartition()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 1);
        ranker.TestPartitions.Count.Should().Be(1);
        var partition0 = ranker.TestPartitions[0];

        // Act
        ranker.Test_DoInsert(3);
        ranker.Test_DoInsert(4);
        partition0.IsFull.Should().BeTrue();
        ranker.Test_DoInsert(5);

        // Assert
        ranker.CountPartitionSplits.Should().Be(1);
        ranker.TestValues.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void InsertWithSplitShouldWorkCorrectlyPartitionSize1()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2, 4 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 3);
        ranker.TestPartitions.Count.Should().Be(3);
        var partition0 = ranker.TestPartitions[0] as Partition<int>;
        partition0.Test_PartitionSize.Should().Be(1);
        partition0.Test_PartitionCapacity.Should().Be(2);

        // Act
        ranker.Test_DoInsert(3);
        ranker.Test_DoInsert(3);

        // Assert
        ranker.CountPartitionSplits.Should().Be(1);
        ranker.TestValues.Should().BeEquivalentTo(new[] { 1, 2, 3, 3, 4 });
    }
}