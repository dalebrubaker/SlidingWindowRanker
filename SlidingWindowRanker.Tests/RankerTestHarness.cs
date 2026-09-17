using FluentAssertions;

namespace SlidingWindowRanker.Tests;

internal sealed class ReferenceSlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly Queue<T> _values;
    private readonly int _windowSize;

    internal ReferenceSlidingWindowRanker(IEnumerable<T> initialValues, int windowSize)
    {
        _values = new Queue<T>(initialValues);
        _windowSize = windowSize;
    }

    internal IReadOnlyList<T> Values => _values.ToList();

    internal double GetRank(T value)
    {
        _values.Enqueue(value);
        if (_values.Count > _windowSize)
        {
            _values.Dequeue();
        }

        var sorted = _values.Order().ToList();
        return sorted.Count(v => v.CompareTo(value) < 0) / (double)sorted.Count;
    }
}

internal static class RankerInvariantChecker
{
    internal static void AssertState<T>(
        SlidingWindowRanker<T> ranker,
        IReadOnlyCollection<T> expectedFifoValues,
        int observationsSeen)
        where T : IComparable<T>
    {
        var partitions = ranker.TestPartitions;
        var combined = partitions.SelectMany(p => p.Values).ToList();
        var queue = ranker.TestQueueValues;

        partitions.Should().NotBeEmpty();
        queue.Count.Should().Be(combined.Count, "the FIFO and partitions represent the same population");
        queue.Order().Should().Equal(combined.Order(), "the FIFO and partition multisets must be identical");
        queue.Should().Equal(expectedFifoValues, "FIFO order defines which observation is evicted next");

        var expectedLowerBound = 0;
        for (var i = 0; i < partitions.Count; i++)
        {
            var partition = partitions[i];
            partition.Values.Should().BeInAscendingOrder($"partition {i} must be sorted");
            partition.LowerBound.Should().Be(expectedLowerBound, $"partition {i} must start after its predecessor");
            expectedLowerBound += partition.Count;
        }

        combined.Should().BeInAscendingOrder("the concatenated partitions must be globally sorted");
        expectedLowerBound.Should().Be(queue.Count);
        ranker.TestRankDenominator.Should().Be(queue.Count, "rank denominator must equal retained population");
        queue.Count.Should().BeLessThanOrEqualTo(ranker.TestWindowSize);
        if (observationsSeen >= ranker.TestWindowSize)
        {
            queue.Count.Should().Be(ranker.TestWindowSize, "a filled finite window must stay full");
        }
    }

    internal static void AssertStep<T>(
        SlidingWindowRanker<T> ranker,
        ReferenceSlidingWindowRanker<T> reference,
        T value,
        int observationsSeen)
        where T : IComparable<T>
    {
        var expectedRank = reference.GetRank(value);
        var actualRank = ranker.GetRank(value);

        actualRank.Should().Be(expectedRank, $"rank must match the reference after inserting {value}");
        AssertState(ranker, reference.Values, observationsSeen);
    }
}
