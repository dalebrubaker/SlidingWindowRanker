namespace SlidingWindowRanker;

public class SlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly List<Partition<T>> _partitions = [];
    private readonly Queue<T> _valueQueue;
    private readonly int _windowSize;

    public SlidingWindowRanker(List<T> initialValues, int partitionCount)
    {
        _windowSize = initialValues.Count;
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

    public double GetCdf(T value)
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
        var result = (double)lowerBoundAfterRemove / _windowSize;

        // Now we already know the result, so we could later have a different thread modify the partitions

        DoInsertAndRemove(value, removeValue, positionIndexForInsert, partitionForInsert);

        return result;
    }

    private void DoInsertAndRemove(T value, T removeValue, int positionIndexForInsert, Partition<T> partitionForInsert)
    {
        // We must DoRemove() BEFORE we DoInsert() because positionIndexForInsert may change
        positionIndexForInsert = DoRemove(removeValue, positionIndexForInsert);
        DoInsert(value, positionIndexForInsert, partitionForInsert);
    }

    private void DoInsert(T value, int positionIndexForInsert, Partition<T> partitionForInsert)
    {
        if (partitionForInsert.NeedsSplitting)
        {
            var rightPartition = partitionForInsert.Split(positionIndexForInsert);
            _partitions.Insert(positionIndexForInsert + 1, rightPartition);
        }
        partitionForInsert.Insert(value); // An O(1) operation because we are adding to the end of the list
    }

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