using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SlidingWindowRanker.Tests")]

namespace SlidingWindowRanker;

/// <summary>
/// Partial class so we can do Unit Testing on private methods in th test project
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class SlidingWindowRankerBase<T> : IDisposable where T : IComparable<T>
{
    protected List<IPartition<T>> _partitions = [];

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

    protected bool _isQueueFull;

    public int CountPartitionSplits { get; private set; }

    public int CountPartitionRemoves { get; private set; }

    public void Dispose()
    {
        foreach (var partition in _partitions)
        {
            partition.Dispose();
        }
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
#if DEBUG
        _debugMessageRemove = null;
        _debugMessageInsert = null;
#endif
        var partitionIndexForInsert = FindPartitionContaining(valueToInsert);
        var beginIncrementsIndex = DoInsert(valueToInsert, ref partitionIndexForInsert);
        var partitionIndexForRemove = _isQueueFull ? FindPartitionContaining(valueToRemove) : int.MaxValue;
        var beginDecrementsIndex = DoRemove(partitionIndexForRemove, ref partitionIndexForInsert, valueToRemove, ref beginIncrementsIndex);
        AdjustPartitionsLowerBounds(beginIncrementsIndex, beginDecrementsIndex);
        var partitionForInsert = _partitions[partitionIndexForInsert];

        // Now get the rank
        var indexWithinPartitionForInsert = partitionForInsert.GetLowerBoundWithinPartition(valueToInsert);
        var lowerBound = partitionForInsert.LowerBound + indexWithinPartitionForInsert;
        var rank = _isQueueFull 
            ? (double)lowerBound / _windowSize 
            : (double)lowerBound / _valueQueue.Count; // Use _valueQueue.Count instead of _windowSize when the window is not yet full
        return rank;
    }

    /// <summary>
    /// Removes the specified value from the window, either by removing within a partition or by removing the partition.
    /// </summary>
    /// <param name="partitionIndexForRemove"></param>
    /// <param name="partitionIndexForInsert"></param>
    /// <param name="valueToRemove"></param>
    /// <param name="beginIncrementsIndex"></param>
    /// <returns>the beginDecrementIndex - the index above which index must be decremented</returns>
    private int DoRemove(int partitionIndexForRemove, ref int partitionIndexForInsert, T valueToRemove, ref int beginIncrementsIndex)
    {
        if (!_isQueueFull)
        {
            return _partitions.Count; // No removal
        }
        var partitionForRemove = _partitions[partitionIndexForRemove];
        if (partitionForRemove.Count == 1)
        {
            // The partition holding the value to remove will be empty after the remove
            // But don't remove the partition because we are about to insert a value into it
            RemovePartition(partitionIndexForRemove, partitionForRemove);
            if (beginIncrementsIndex > partitionIndexForRemove)
            {
                beginIncrementsIndex--;
                partitionIndexForInsert--;
            }
            return partitionIndexForRemove;
        }
        DoRemove(valueToRemove, partitionForRemove);
        return partitionIndexForRemove + 1;
    }

    /// <summary>
    /// Insert the value, splitting the partition if necessary.
    /// </summary>
    /// <param name="valueToInsert"></param>
    /// <param name="partitionIndexForInsert">This can change to the split partition</param>
    /// <returns>the beginIncrementsIndex - the index above which index must be incremented</returns>
    private int DoInsert(T valueToInsert, ref int partitionIndexForInsert)
    {
        var partitionForInsert = _partitions[partitionIndexForInsert];
        if (partitionForInsert.IsFull)
        {
            var isSplitIntoRightPartition = SplitPartition(partitionForInsert, partitionIndexForInsert, valueToInsert);
            if (isSplitIntoRightPartition)
            {
                // The value to insert is the highest value in the partition, so we must insert it into the right partition
                partitionIndexForInsert++;
            }
        }
        else
        {
            partitionForInsert.Insert(valueToInsert);
        }
        return partitionIndexForInsert + 1;
    }

    private bool SplitPartition(IPartition<T> partitionForInsert, int partitionIndexForInsert, T valueToInsert)
    {
        CountPartitionSplits++;
        var (rightPartition, isSplitIntoRightPartition) = partitionForInsert.SplitAndInsert(valueToInsert);
        _partitions.Insert(partitionIndexForInsert + 1, rightPartition);
#if DEBUG
        _debugMessageInsert = $"Split partitionForInsert={partitionForInsert} and inserted it at partitionIndexForInsert={partitionIndexForInsert}";
#endif
        return isSplitIntoRightPartition;
    }

    private void RemovePartition(int partitionIndexForRemove, IPartition<T> partitionForRemove)
    {
        _partitions.RemoveAt(partitionIndexForRemove);
#if DEBUG
        _debugMessageRemove = $"Removed _partitionForRemove={partitionForRemove} at partitionForRemoveIndex={partitionIndexForRemove}";
#endif
        CountPartitionRemoves++;
    }

    /// <summary>
    /// Reflect the insertion and removal of values in the partitions.
    /// An insertion will increment the LowerBound of all partitions to the right of the partition holding the inserted value.
    /// A removal will decrement the LowerBound of all partitions to the right of the partition holding the inserted value.
    /// </summary>
    /// <param name="beginIncrementsIndex">The partition index where we begin increments</param>
    /// <param name="beginDecrementsIndex">The partition index where we begin decrements</param>
    private void AdjustPartitionsLowerBounds(int beginIncrementsIndex, int beginDecrementsIndex)
    {
        if (beginIncrementsIndex < beginDecrementsIndex)
        {
            for (var i = beginIncrementsIndex; i < beginDecrementsIndex; i++)
            {
                var partition = _partitions[i];
                partition.LowerBound++;
            }
        }
        else
        {
            for (var i = beginDecrementsIndex; i < beginIncrementsIndex; i++)
            {
                var partition = _partitions[i];
                partition.LowerBound--;
            }
        }
        DebugGuardPartitionLowerBoundValuesAreCorrect();
    }

    /// <summary>
    /// Inserts a new value into the specified partition.
    /// The partition holding value will be split if it is full (meaning it has reached its capacity).
    /// The capacity of a partition is double the initial count of values in the partition.
    /// </summary>
    /// <param name="valueToInsert">The value to insert.</param>
    /// <param name="partitionForInsert"></param>
    private void DoInsert(T valueToInsert, IPartition<T> partitionForInsert)
    {
        Debug.Assert(!partitionForInsert.IsFull, "Must have been split before we get here");
        partitionForInsert.Insert(valueToInsert);
#if DEBUG
        _debugMessageInsert = $"Inserted value into partitionForInsert={partitionForInsert}";
#endif
    }

    /// <summary>
    /// Removes a value from the partitions.
    /// The partition holding _valueToRemove will be removed when is emptied.
    /// </summary>
    /// <param name="valueToRemove">The value to remove.</param>
    /// <param name="partitionForRemove"></param>
    /// <returns>The index of the partition where the value was removed, or -1 if no value was removed.</returns>
    private void DoRemove(T valueToRemove, IPartition<T> partitionForRemove)
    {
        Debug.Assert(partitionForRemove.Count > 1, "Partition should have been removed before we get here");
#if DEBUG
        if (valueToRemove.CompareTo(partitionForRemove.HighestValue) > 0)
        {
            throw new SlidingWindowRankerException("The value to remove above the HighestValue in the window.");
        }
        if (valueToRemove.CompareTo(partitionForRemove.LowestValue) < 0)
        {
            throw new SlidingWindowRankerException("The value to remove is below the LowestValue of the window.");
        }
#endif
        partitionForRemove.Remove(valueToRemove);
#if DEBUG
        _debugMessageRemove = $"Removed  value in _partitionForRemove={partitionForRemove}";
#endif
    }

    /// <summary>
    /// Finds the partition containing the specified value.
    /// </summary>
    /// <param name="value">The value to find the partition for.</param>
    /// <returns>The partition where we want to remove or insert the value.</returns>
    private int FindPartitionContaining(T value)
    {
        var partitionIndex = LowerBound(value);
        if (partitionIndex >= _partitions.Count)
        {
            // Must be in the last partition
            partitionIndex = _partitions.Count - 1;
        }
        return partitionIndex;
    }

    private int LowerBound(T value)
    {
        var low = 0;
        var high = _partitions.Count;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            var partition = _partitions[mid];
            if (partition.HighestValue.CompareTo(value) < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }
        return low;
    }

    public override string ToString()
    {
        return
            $"_windowSize={_windowSize:N0} #values={_valueQueue.Count:N0} #partitions={_partitions.Count} #splits={CountPartitionSplits:N0} #removes={CountPartitionRemoves:N0}";
    }
}