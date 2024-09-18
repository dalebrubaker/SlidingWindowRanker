namespace SlidingWindowRanker;

public class SlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly List<Partition<T>> _partitions = [];

    /// <summary>
    /// The queue of all values so we know which one to remove at the left side of the window.
    /// </summary>
    private readonly Queue<T> _valueQueue;

    /// <summary>
    /// Initializes a new instance of the SlidingWindowRanker class.
    /// </summary>
    /// <param name="initialValues">The initial values to populate the sliding window.</param>
    /// <param name="partitionCount">The number of partitions to divide the values into.</param>
    public SlidingWindowRanker(List<T> initialValues, int partitionCount)
    {
        _valueQueue = new Queue<T>(initialValues);
        var valuesPerPartition = initialValues.Count / partitionCount;
        for (var i = 0; i < partitionCount; i++)
        {
            var startIndex = i * valuesPerPartition;
            var count = Math.Max(valuesPerPartition, initialValues.Count - startIndex);
            var partitionValues = initialValues.GetRange(startIndex, count);
            var partition = new Partition<T>(partitionValues)
            {
                LowerBound = startIndex
            };
            _partitions.Add(partition);
        }
    }

    /// <summary>
    /// Returns the rank of the specified value, as a fraction of representing the total number of values
    /// in the window that are LESS THAN the given value.
    /// This is Cumulative Distribution Function (CDF) value for the specified value
    /// except that CDF is normally defined as LESS THAN OR EQUAL rather than LESS THAN.
    /// So the values returned will be in the range [0, 1] NOT inclusive of 1) rather than [0, 1] inclusive.
    ///
    /// The value given is added to the right side of the window and the oldest value is removed from the left side
    /// of the window. The result is calculated based on the values in the window AFTER the add/remove.
    /// 
    /// </summary>
    /// <param name="value">The value to calculate the Rank for.</param>
    /// <returns>The fraction of values in the window that are less than the specified value.</returns>
    public double GetRank(T value)
    {
        // Returns the fraction of values in the window that are Less Than to the specified value.
        var removeValue = _valueQueue.Dequeue();
        _valueQueue.Enqueue(value);
        var (partitionForInsert, positionIndexForInsert) = FindPartitionContaining(value);
        var lowerBoundOwWithinPartition = partitionForInsert.GetLowerBoundWithinPartition(value);
        var lowerBound = partitionForInsert.LowerBound + lowerBoundOwWithinPartition;
        var lowerBoundAfterRemove = lowerBound;
        if (removeValue.CompareTo(value) < 0)
        {
            // After we do the insert and remove, the lower bound will be one less
            lowerBoundAfterRemove--;
        }
        var result = (double)lowerBoundAfterRemove / _valueQueue.Count;

        // Now we already know the result, so we could later have a different thread modify the partitions

        DoInsertAndRemove(value, removeValue, positionIndexForInsert, partitionForInsert);

        return result;
    }

    /// <summary>
    /// Inserts a new value and removes an old value from the partitions.
    /// The partition holding removeValue will be removed when it is empty.
    /// The partition holding value will be split if it is full (meaning it has reached its capacity).
    /// The capacity of a partition is double the initial count of values in the partition.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    /// <param name="removeValue">The value to remove.</param>
    /// <param name="positionIndexForInsert">The index at which to insert the new value.</param>
    /// <param name="partitionForInsert">The partition to insert the new value into.</param>
    private void DoInsertAndRemove(T value, T removeValue, int positionIndexForInsert, Partition<T> partitionForInsert)
    {
        // We must DoRemove() BEFORE we DoInsert() because positionIndexForInsert may change
        positionIndexForInsert = DoRemove(removeValue, positionIndexForInsert);
        DoInsert(value, positionIndexForInsert, partitionForInsert);
    }

    /// <summary>
    /// Inserts a new value into the specified partition.
    /// The partition holding value will be split if it is full (meaning it has reached its capacity).
    /// The capacity of a partition is double the initial count of values in the partition.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    /// <param name="positionIndexForInsert">The index at which to insert the new value.</param>
    /// <param name="partitionForInsert">The partition to insert the new value into.</param>
    private void DoInsert(T value, int positionIndexForInsert, Partition<T> partitionForInsert)
    {
        if (partitionForInsert.NeedsSplitting)
        {
            var rightPartition = partitionForInsert.Split(positionIndexForInsert);
            _partitions.Insert(positionIndexForInsert + 1, rightPartition);
        }
        partitionForInsert.Insert(value); // An O(1) operation because we are adding to the end of the list
    }

    /// <summary>
    /// Removes a value from the partitions.
    /// The partition holding removeValue will be removed when it is empty.
    /// </summary>
    /// <param name="removeValue">The value to remove.</param>
    /// <param name="positionIndexForInsert">The index at which the new value will be inserted.</param>
    /// <returns>The updated index at which the new value will be inserted.</returns>
    private int DoRemove(T removeValue, int positionIndexForInsert)
    {
        var (partitionForRemove, positionIndexForRemove) = FindPartitionContaining(removeValue);
        if (partitionForRemove.Count == 1)
        {
            // Remove the partition if it only contains the value we are removing
            _partitions.Remove(partitionForRemove);
            if (positionIndexForRemove < positionIndexForInsert)
            {
                positionIndexForInsert--;
            }
        }
        return positionIndexForInsert;
    }

    /// <summary>
    /// Finds the partition containing the specified value.
    /// </summary>
    /// <param name="value">The value to find the partition for.</param>
    /// <returns>A tuple containing the partition and its index.</returns>
    private (Partition<T> partition, int partitionIndex) FindPartitionContaining(T value)
    {
        var partitionIndex = FindPartitionIndex(value);
        if (partitionIndex < 0 || partitionIndex >= _partitions.Count)
        {
            throw new SlidingWindowRankerException($"partitionIndex={partitionIndex} is out of range.");
        }
        var partition = _partitions[partitionIndex];
        return (partition, partitionIndex);
    }

    /// <summary>
    /// Finds the index of the partition that should contain the specified value.
    /// </summary>
    /// <param name="value">The value to find the partition index for.</param>
    /// <returns>The index of the partition that should contain the value, or -1 if no suitable partition is found.</returns>
    private int FindPartitionIndex(T value)
    {
        var low = 0;
        var high = _partitions.Count - 1;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            var partition = _partitions[mid];
            if (partition.LowerBound.CompareTo(value) < 0)
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
}