namespace SlidingWindowRanker;

public class SlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly List<Partition<T>> _partitions = [];

    /// <summary>
    /// The queue of all values so we know which one to remove at the left side of the window.
    /// </summary>
    private readonly Queue<T> _valueQueue;

    /// <summary>
    /// The size of the window. Normally this is the same as the number of initial values,
    /// but it can be set to a higher value if starting with little or no initial values.
    /// </summary>
    private readonly int _windowSize;

    /// <summary>
    /// Initializes a new instance of the SlidingWindowRanker class.
    /// </summary>
    /// <param name="initialValues">The initial values to populate the sliding window.</param>
    /// <param name="partitionCount">The number of partitions to divide the values into.</param>
    /// <param name="windowSize">Default -1 means to use initialValues.Count. Must be no smaller than initialValues</param>
    public SlidingWindowRanker(List<T> initialValues, int partitionCount, int windowSize = -1)
    {
        if (windowSize < 0)
        {
            windowSize = initialValues.Count;
        }
        _windowSize = windowSize;
        if (initialValues.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialValues), "The number of initial values must be greater than 0.");
        }
        if (partitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionCount), "The number of partitions must be greater than 0.");
        }
        _valueQueue = new Queue<T>(initialValues);
        var valuesPerPartition = _windowSize / partitionCount;
        for (var i = 0; i < partitionCount; i++)
        {
            var startIndex = i * valuesPerPartition;
            var valuesPerPartitionCount = initialValues.Count / partitionCount;
            if (i == partitionCount - 1)
            {
                // The last partition gets the remaining values
                valuesPerPartitionCount += initialValues.Count % partitionCount;
            }
            var partitionValues = initialValues.GetRange(startIndex, valuesPerPartitionCount);
            var partition = new Partition<T>(partitionValues, _windowSize / partitionCount)
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
        _valueQueue.Enqueue(value);
        int positionIndexForInsert;
        Partition<T> partitionForInsert;
        if (value.CompareTo(_partitions[^1].LowestValue) >= 0)
        {
            // Add the value to the end of the last partition
            positionIndexForInsert = _partitions.Count - 1;
            partitionForInsert = _partitions[positionIndexForInsert];
        }
        else
        {
            (partitionForInsert, positionIndexForInsert) = FindPartitionContaining(value);
        }
        var lowerBoundOwWithinPartition = partitionForInsert.GetLowerBoundWithinPartition(value);
        var lowerBound = partitionForInsert.LowerBound + lowerBoundOwWithinPartition;
        if (_valueQueue.Count < _windowSize)
        {
            // We are still filling the window, so we don't need to remove any values yet
            // Now we already know the result, so we could later have a different thread modify the partitions
            DoInsert(value, positionIndexForInsert, partitionForInsert);
            return (double)lowerBound / _valueQueue.Count;
        }

        var removeValue = _valueQueue.Dequeue();
        if (removeValue.CompareTo(value) < 0)
        {
            // After we do the insert and remove, the lower bound will be one less
            lowerBound--;
        }
        var result = (double)lowerBound / _valueQueue.Count;

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
        else
        {
            partitionForRemove.Remove(removeValue);
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
        var lowestValues = _partitions
            .Select(p => p.LowestValue)
            .ToList();
        var partitionIndex = lowestValues.LowerBound(value);
        var partition = _partitions[partitionIndex];
        return (partition, partitionIndex);
    }

    /// <summary>
    /// For debugging, return the values in all partitions.
    /// </summary>
    /// <returns></returns>
    internal List<T> GetValues()
    {
        return _partitions.SelectMany(p => p.Values).ToList();
    }

    public override string ToString()
    {
        return $"#values={_valueQueue.Count:N0}, #partitions={_partitions.Count} _windowSize={_windowSize:N0}";
    }
}