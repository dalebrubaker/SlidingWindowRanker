using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SlidingWindowRanker.Tests")]

namespace SlidingWindowRanker;

/// <summary>
/// Partial class so we can do Unit Testing on private methods in th test project
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class SlidingWindowRankerBase<T> : IDisposable where T : IComparable<T>
{
    protected readonly SortedPartitions<T> SortedPartitions = new();

    protected bool _isQueueFull;

    /// <summary>
    /// The queue of all values so we know which one to remove at the left edge of the window.
    /// They are NOT sorted. They are in the order in which they were added.
    /// </summary>
    protected Queue<T> _valueQueue;

    /// <summary>
    /// The size of the window. Normally this is the same as the number of initial values,
    /// but it can be set to a higher value if starting with little or no initial values.
    /// </summary>
    protected int _windowSize;

    public int CountPartitionSplits => SortedPartitions.CountPartitionSplits;

    public int CountPartitionRemoves => SortedPartitions.CountPartitionRemoves;

    public void Dispose()
    {
        SortedPartitions.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns the rank of the specified value, as a fraction of the total number of values in the window
    /// that are LESS THAN the given value.
    /// This is Cumulative Distribution Function (CDF) value for the specified value
    /// except that CDF is normally defined as LESS THAN OR EQUAL rather than LESS THAN.
    /// So the values returned will be in the range ([0, 1] NOT inclusive of 1) rather than [0, 1] inclusive.
    ///
    /// The given value given is added to the right side of the window and the oldest value is removed from the left side
    /// of the window. The result is what would be calculated based on the values in the window AFTER the add/remove.
    /// But we determine the result BEFORE we do the add/remove so we can later have a different thread
    /// or threads do the insert and/or remove. Finally, we adjust the partition LowerBound values to reflect the insert and remove.
    /// </summary>
    /// <param name="valueToInsert">The value to calculate the Rank for.</param>
    /// <returns>The fraction of values in the window that are less than the specified value.</returns>
    public double GetRank(T valueToInsert)
    {
        var valueToRemove = _isQueueFull ? _valueQueue.Dequeue() : default;
        _valueQueue.Enqueue(valueToInsert);
        if (!_isQueueFull && _valueQueue.Count >= _windowSize)
        {
            _isQueueFull = true;
        }
        SortedPartitions.InsertAndRemoveValues(valueToInsert, valueToRemove);

        var lowerBound = SortedPartitions.GetLowerBound(valueToInsert);
        var rank = _isQueueFull
            ? (double)lowerBound / _windowSize
            : (double)lowerBound / _valueQueue.Count; // Use _valueQueue.Count instead of _windowSize when the window is not yet full
        return rank;
    }

    public override string ToString()
    {
        return $"_windowSize={_windowSize:N0} #values={_valueQueue.Count:N0} #{SortedPartitions}";
    }
}