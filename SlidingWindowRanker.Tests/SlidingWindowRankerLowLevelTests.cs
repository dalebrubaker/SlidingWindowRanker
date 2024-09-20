using FluentAssertions;
using Xunit.Abstractions;

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
        var partition1 = ranker.TestPartitions[1];
        partition1.Test_PartitionSize.Should().Be(1);
        partition1.Test_LowerBound.Should().Be(1);
        ranker.TestPartitionIndexChangedByRemove.Should().Be(0);

        // Act
        var valueToRemove = 1;
        ranker.Test_DoRemove(valueToRemove);

        // Assert
        ranker.TestPartitionIndexChangedByRemove.Should().Be(0,
            "The removed value was in the first partition (index 0), which was removed, "
            + "so we start decrementing LowerBound values at 0, which had been partition 1.");
        ranker.TestPartitions.Count.Should().Be(4, "The first partition was removed.");
        ranker.CountPartitionRemoves.Should().Be(1);
        ranker.Test_AdjustPartitionsLowerBounds(false, true);
    }

    [Fact]
    public void Remove_ShouldKeepPartitionWhenPartitionSize2()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2, 3, 4, 5 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 3);
        ranker.TestPartitions.Count.Should().Be(3);
        var partition1 = ranker.TestPartitions[1];
        partition1.Test_PartitionSize.Should().Be(2);
        partition1.Test_LowerBound.Should().Be(2);

        // Act
        var valueToRemove = 3;
        ranker.Test_DoRemove(valueToRemove);

        // Assert
        ranker.TestPartitionIndexChangedByRemove.Should().Be(1,
            "The removed value was in the second partition (index 1), "
            + "and we start decrementing LowerBound values above that.");
        ranker.TestPartitions.Count.Should().Be(3, "The partition was not removed because it is not empty.");
        ranker.CountPartitionRemoves.Should().Be(0);
        ranker.Test_AdjustPartitionsLowerBounds(false, true);
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
        ranker.TestPartitionIndexChangedByInsert.Should().Be(0,
            "We start fixing LowerBound values at the partition index where the insert was done.");
        ranker.CountPartitionSplits.Should().Be(0);
        partition0.Values.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
        ranker.Test_AdjustPartitionsLowerBounds(true, false);
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
        partition0.NeedsSplitting.Should().BeTrue();
        ranker.Test_DoInsert(4);

        // Assert
        ranker.CountPartitionSplits.Should().Be(1);
        ranker.TestPartitionIndexChangedByInsert.Should().Be(0,
            "We start fixing LowerBound values at the partition index where the insert was done,"
            + "and Splitting fixed LowerBound in the added partition.");
        ranker.TestValues.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
        ranker.Test_AdjustPartitionsLowerBounds(true, false);
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
        partition0.NeedsSplitting.Should().BeTrue();
        ranker.Test_DoInsert(5);

        // Assert
        ranker.CountPartitionSplits.Should().Be(1);
        ranker.TestPartitionIndexChangedByInsert.Should().Be(0,
            "We start fixing LowerBound values at the partition index where the insert was done,"
            + "and Splitting fixed LowerBound in the added partition.");
        ranker.TestPartitionIndexInserted.Should().Be(1,
            "The new partition was inserted at index 1.");
        ranker.TestValues.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
        ranker.Test_AdjustPartitionsLowerBounds(true, false);
    }

    [Fact]
    public void InsertWithSplitShouldWorkCorrectlyPartitionSize1()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2, 4 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 3);
        ranker.TestPartitions.Count.Should().Be(3);
        var partition0 = ranker.TestPartitions[0];
        partition0.Test_PartitionSize.Should().Be(1);
        partition0.Test_PartitionCapacity.Should().Be(2);

        // Act
        ranker.Test_DoInsert(3);
        ranker.Test_DoInsert(3);

        // Assert
        ranker.CountPartitionSplits.Should().Be(1);
        ranker.TestPartitionIndexChangedByInsert.Should().Be(2,
            "We start fixing LowerBound values at the partition index where the insert was done,"
            + "and Splitting fixed LowerBound in the added partition.");
        ranker.TestValues.Should().BeEquivalentTo(new[] { 1, 2, 3, 3, 4 });
        ranker.Test_AdjustPartitionsLowerBounds(true, false);
    }

    [Fact]
    public void TestAdjustPartitionsLowerBoundsInSamePartitionAsInsert()
    {
        // Arrange
        var initialValues = new List<int> { 1, 2, 3, 4, 5, 6 };
        var ranker = new SlidingWindowRanker<int>(initialValues, 6);
        ranker.TestPartitions.Count.Should().Be(6);
        var partition0 = ranker.TestPartitions[0];
        partition0.Test_PartitionSize.Should().Be(1);
        partition0.Test_PartitionCapacity.Should().Be(2);

        // Act
        ranker.Test_DoRemove(1);
        ranker.Test_DoInsert(2);

        // Assert
        ranker.Test_AdjustPartitionsLowerBounds(true, true);
    }
}