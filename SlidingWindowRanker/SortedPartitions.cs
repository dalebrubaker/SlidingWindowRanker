using System.Diagnostics;

namespace SlidingWindowRanker;

public partial class SortedPartitions<T> : IDisposable where T : IComparable<T>
{
    public List<IPartition<T>> Partitions { get; set; }

    public int CountPartitionSplits { get; private set; }

    public int CountPartitionRemoves { get; private set; }

    public void Dispose()
    {
        foreach (var partition in Partitions)
        {
            partition.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns the lower bound of the value over all the partitions in this class.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>the lower bound of the <see cref="value"/> over all the partitions in this class.</returns>
    public int GetLowerBound(T value)
    {
        var partitionIndexForInsert = FindPartitionContaining(value);
        var partitionForInsert = Partitions[partitionIndexForInsert];
        var indexWithinPartitionForInsert = partitionForInsert.GetLowerBoundWithinPartition(value);
        var lowerBound = partitionForInsert.LowerBound + indexWithinPartitionForInsert;
        return lowerBound;
    }

    /// <summary>
    /// Insert <see cref="valueToInsert"/> into the correct partition and remove <see cref="valueToRemove"/> from the correct partition.
    /// </summary>
    /// <param name="valueToInsert"></param>
    /// <param name="valueToRemove"></param>
    /// <returns>the lower bound of the <see cref="valueToInsert"> over all the partitions in this class/></returns>
    public void InsertAndRemoveValues(T valueToInsert, T valueToRemove = default)
    {
#if DEBUG
        _debugMessageRemove = null;
        _debugMessageInsert = null;
#endif
        var isDoingRemove = !EqualityComparer<T>.Default.Equals(valueToRemove, default);
        var partitionIndexForInsert = FindPartitionContaining(valueToInsert);
        var beginIncrementsIndex = DoInsert(valueToInsert, ref partitionIndexForInsert);
        var beginDecrementsIndex = Partitions.Count;
        if (isDoingRemove)
        {
            var partitionIndexForRemove = FindPartitionContaining(valueToRemove);
            beginDecrementsIndex = DoRemove(partitionIndexForRemove, ref partitionIndexForInsert, valueToRemove, ref beginIncrementsIndex);
        }
        AdjustPartitionsLowerBounds(beginIncrementsIndex, beginDecrementsIndex);
    }

    /// <summary>
    /// Insert the value, splitting the partition if necessary.
    /// </summary>
    /// <param name="valueToInsert"></param>
    /// <param name="partitionIndexForInsert">This can change to the split partition</param>
    /// <returns>the beginIncrementsIndex - the index above which index must be incremented</returns>
    private int DoInsert(T valueToInsert, ref int partitionIndexForInsert)
    {
        var partitionForInsert = Partitions[partitionIndexForInsert];
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
        var partitionForRemove = Partitions[partitionIndexForRemove];
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

    private bool SplitPartition(IPartition<T> partitionForInsert, int partitionIndexForInsert, T valueToInsert)
    {
        CountPartitionSplits++;
        var (rightPartition, isSplitIntoRightPartition) = partitionForInsert.SplitAndInsert(valueToInsert);
        Partitions.Insert(partitionIndexForInsert + 1, rightPartition);
#if DEBUG
        _debugMessageInsert = $"Split partitionForInsert={partitionForInsert} and inserted it at partitionIndexForInsert={partitionIndexForInsert}";
#endif
        return isSplitIntoRightPartition;
    }

    private void RemovePartition(int partitionIndexForRemove, IPartition<T> partitionForRemove)
    {
        Partitions.RemoveAt(partitionIndexForRemove);
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
                var partition = Partitions[i];
                partition.LowerBound++;
            }
        }
        else
        {
            for (var i = beginDecrementsIndex; i < beginIncrementsIndex; i++)
            {
                var partition = Partitions[i];
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
        if (partitionIndex >= Partitions.Count)
        {
            // Must be in the last partition
            partitionIndex = Partitions.Count - 1;
        }
        return partitionIndex;
    }

    private int LowerBound(T value)
    {
        var low = 0;
        var high = Partitions.Count;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            var partition = Partitions[mid];
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
        return $"#partitions={Partitions.Count} #splits={CountPartitionSplits:N0} #removes={CountPartitionRemoves:N0}";
    }
}