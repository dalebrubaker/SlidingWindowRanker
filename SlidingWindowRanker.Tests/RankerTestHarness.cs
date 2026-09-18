using FluentAssertions;

namespace SlidingWindowRanker.Tests;

/// <summary>
/// A deliberately naive List-based model of the sliding window, used as the oracle for every
/// mutating operation. Every method here is the obvious O(N) implementation of the documented semantics.
/// </summary>
internal sealed class ReferenceSlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly List<T> _values;
    private readonly int _windowSize;
    private bool _hasLastEvictedValue;
    private T? _lastEvictedValue;

    internal ReferenceSlidingWindowRanker(IEnumerable<T> initialValues, int windowSize)
    {
        _values = initialValues.ToList();
        _windowSize = windowSize;
    }

    internal IReadOnlyList<T> Values => _values.ToList();

    internal int Count => _values.Count;

    /// <summary>Adds to the newest end, evicting the oldest when the window overflows.</summary>
    internal void Add(T value)
    {
        _hasLastEvictedValue = false;
        _values.Add(value);
        if (_values.Count > _windowSize)
        {
            _lastEvictedValue = _values[0];
            _hasLastEvictedValue = true;
            _values.RemoveAt(0);
        }
    }

    /// <summary>Adds, then ranks against the UPDATED window.</summary>
    internal double GetRank(T value)
    {
        Add(value);
        return GetRankNoAdd(value);
    }

    /// <summary>
    /// Ranks without mutating. An empty window divides by zero and yields NaN, which is what the
    /// library has always done, so the model reproduces it rather than hiding it.
    /// </summary>
    internal double GetRankNoAdd(T value)
    {
        return _values.Count(v => v.CompareTo(value) < 0) / (double)_values.Count;
    }

    /// <summary>
    /// Undoes the most recent add: removes the NEWEST value and restores the value that add evicted, if any.
    /// </summary>
    internal T RemoveLast()
    {
        var value = _values[^1];
        _values.RemoveAt(_values.Count - 1);
        if (_hasLastEvictedValue)
        {
            _values.Insert(0, _lastEvictedValue!);
            _lastEvictedValue = default;
            _hasLastEvictedValue = false;
        }
        return value;
    }

    /// <summary>Remove the newest, then add once and rank against the updated window.</summary>
    internal double ReplaceLastAndGetRank(T value)
    {
        RemoveLast();
        return GetRank(value);
    }
}

internal static class RankerInvariantChecker
{
    /// <summary>
    /// Asserts everything that must hold after any operation: the chronological sequence matches
    /// <paramref name="expectedFifoValues"/> exactly, the partitions hold the same multiset, are globally
    /// sorted, and carry correct lower bounds, and the rank denominator matches the retained population.
    /// </summary>
    internal static void AssertInvariants<T>(
        SlidingWindowRanker<T> ranker,
        IReadOnlyCollection<T> expectedFifoValues)
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
        ranker.Count.Should().Be(queue.Count, "Count must expose the retained population");
        queue.Count.Should().BeLessThanOrEqualTo(ranker.TestWindowSize);
    }

    internal static void AssertState<T>(
        SlidingWindowRanker<T> ranker,
        IReadOnlyCollection<T> expectedFifoValues,
        int observationsSeen)
        where T : IComparable<T>
    {
        AssertInvariants(ranker, expectedFifoValues);
        if (observationsSeen >= ranker.TestWindowSize)
        {
            ranker.TestQueueValues.Count.Should().Be(ranker.TestWindowSize, "a filled finite window must stay full");
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
