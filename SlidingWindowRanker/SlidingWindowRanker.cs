using System.Diagnostics;

namespace SlidingWindowRanker;

public class SlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly List<Partition<T>> _partitions = [];

    /// <summary>
    /// The queue of all values so we know which one to remove at the left side of the window.
    /// They are NOT sorted. They are in the order in which they were added.
    /// </summary>
    private readonly Queue<T> _valueQueue;

    /// <summary>
    /// The size of the window. Normally this is the same as the number of initial values,
    /// but it can be set to a higher value if starting with little or no initial values.
    /// </summary>
    private readonly int _windowSize;

    /// <summary>
    /// The index within the partition where _valueToInsert will be inserted.
    /// We calculate this early in GetRank() so we can later have a different thread or threads do the insert and/or remove
    /// </summary>
    private int _lowerBoundWithinInsertPartition;

    /// <summary>
    /// The index at which we inserted a new value.
    /// </summary>
    private int _partitionIndexForInsert;

    /// <summary>
    /// The index at which we removed an old value, or null if no value was removed
    /// </summary>
    private int? _partitionIndexForRemove;

    /// <summary>
    /// _valueToInsert is cached so a future implementation could have a different thread do the insert
    /// </summary>
    private T _valueToInsert;

    /// <summary>
    /// _valueToRemove is cached so a future implementation could have a different thread do the insert
    /// </summary>
    private T _valueToRemove;

    /// <summary>
    /// Initializes a new instance of the SlidingWindowRanker class.
    /// </summary>
    /// <param name="initialValues">The initial values to populate the sliding window.</param>
    /// <param name="partitionCount">The number of partitions to divide the values into.</param>
    /// <param name="windowSize">Default -1 means to use initialValues.Count. Must be no smaller than initialValues</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public SlidingWindowRanker(List<T> initialValues, int partitionCount, int windowSize = -1)
    {
        if (windowSize < 0)
        {
            windowSize = initialValues.Count;
        }
        _windowSize = windowSize;
        if (_windowSize == 0)
        {
            throw new ArgumentException(nameof(windowSize), "The window size must be greater than 0.");
        }
        if (partitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionCount), "The number of partitions must be greater than 0.");
        }
        _valueQueue = new Queue<T>(initialValues);

        // Sort the initial values so we can divide them into partitions
        // But be friendly to the caller, so sort a new list and leave the given list unchanged
        var values = new List<T>(initialValues);
        values.Sort();
        var valuesPerPartition = _windowSize / partitionCount;
        for (var i = 0; i < partitionCount; i++)
        {
            var startIndex = i * valuesPerPartition;
            var valuesPerPartitionCount = values.Count / partitionCount;
            if (i == partitionCount - 1)
            {
                // The last partition gets the remaining values
                valuesPerPartitionCount += values.Count % partitionCount;
            }
            var partitionValues = values.GetRange(startIndex, valuesPerPartitionCount);
            var partition = new Partition<T>(partitionValues, _windowSize / partitionCount)
            {
                LowerBound = startIndex
            };
            _partitions.Add(partition);
        }
    }

    private bool IsQueueFull => _valueQueue.Count >= _windowSize;

    /// <summary>
    /// Returns the rank of the specified value, as a fraction of the total number of values in the window
    /// that are LESS THAN the given value.
    /// This is Cumulative Distribution Function (CDF) value for the specified value
    /// except that CDF is normally defined as LESS THAN OR EQUAL rather than LESS THAN.
    /// So the values returned will be in the range [0, 1] NOT inclusive of 1) rather than [0, 1] inclusive.
    ///
    /// The given value given is added to the right side of the window and the oldest value is removed from the left side
    /// of the window. The result is what would be calculated based on the values in the window AFTER the add/remove.
    /// But we cheat a bit and determine the result before we do the add/remove so we can later have a different thread
    /// or threads do the insert and/or remove.
    /// Also, we do not much work if the given value is the same one that comes out of the window.
    ///  
    /// </summary>
    /// <param name="value">The value to calculate the Rank for.</param>
    /// <returns>The fraction of values in the window that are less than the specified value.</returns>
    public double GetRank(T value)
    {
        _valueToRemove = IsQueueFull ? _valueQueue.Dequeue() : default;
        _valueQueue.Enqueue(value);
        _valueToInsert = value;
        var (rank, partitionForInsert) = CalculateRankBeforeDoingInsertAndRemove();
        if (!IsQueueFull || _valueToInsert.CompareTo(_valueToRemove) != 0)
        {
            // If the value we are inserting is the same as the value we are removing, we don't need to do anything more
            DoInsertAndRemove(partitionForInsert);
            AdjustPartitionsLowerBounds();
        }
        return rank;
    }

    /// <summary>
    /// Calculate the rank BEFORE doing insert and remove,
    /// so we can later have a different thread or threads do the insert and/or remove
    /// </summary>
    /// <returns></returns>
    private (double rank, Partition<T> partitionForInsert) CalculateRankBeforeDoingInsertAndRemove()
    {
        Partition<T> partitionForInsert;
        if (_valueToInsert.CompareTo(_partitions[^1].LowestValue) >= 0)
        {
            // Add the value to the end of the last partition
            _partitionIndexForInsert = _partitions.Count - 1;
            partitionForInsert = _partitions[_partitionIndexForInsert];
        }
        else
        {
            (partitionForInsert, _partitionIndexForInsert) = FindPartitionContaining(_valueToInsert);
        }
        _lowerBoundWithinInsertPartition = partitionForInsert.GetLowerBoundWithinPartition(_valueToInsert);
        var lowerBound = partitionForInsert.LowerBound + _lowerBoundWithinInsertPartition;
        if (IsQueueFull && _valueToRemove?.CompareTo(_valueToInsert) < 0)
        {
            // After we do the insert and remove, the lower bound will be one less
            lowerBound--;
        }

        // We use _valueQueue.Count instead of _windowSize because the window may not be full yet
        var rank = (double)lowerBound / _valueQueue.Count;
        return (rank, partitionForInsert);
    }

    /// <summary>
    /// Reflect the insertion and removal of values in the partitions.
    /// An insertion will increment the LowerBound of all partitions to the right of the partition holding the inserted value.
    /// A removal will decrement the LowerBound of all partitions to the right of the partition holding the inserted value.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void AdjustPartitionsLowerBounds()
    {
        var startIndex = _partitionIndexForRemove == null
            ? _partitionIndexForInsert
            : Math.Min(_partitionIndexForInsert, _partitionIndexForRemove.Value);
        startIndex++; // We don't need to adjust the partition holding the  value
        for (var i = startIndex; i < _partitions.Count; i++)
        {
            var isDecrement = _partitionIndexForRemove == null || i > _partitionIndexForRemove.Value;
            var isIncrement = i > _partitionIndexForInsert;
            var netChange = (isIncrement ? 1 : 0) - (isDecrement ? 1 : 0);
        }
    }

    /// <summary>
    /// Inserts a new value and removes an old value from the partitions.
    /// The partition holding removeValue will be removed when it is empty.
    /// The partition holding value will be split if it is full (meaning it has reached its capacity).
    /// The capacity of a partition is double the initial count of values in the partition.
    /// </summary>
    /// <param name="partitionForInsert">The partition to insert the new value into.</param>
    private void DoInsertAndRemove(Partition<T> partitionForInsert)
    {
        if (!IsQueueFull)
        {
            // We are still filling the window, so we don't need to remove any values yet
            // Now we already know the result, so we could later have a different thread modify the partitions
            DoInsert(partitionForInsert);
            _partitionIndexForRemove = null;
            return;
        }

        // We must DoRemove() BEFORE we DoInsert() because positionIndexForInsert may change
        DoRemove();
        DoInsert(partitionForInsert);
    }

    /// <summary>
    /// Inserts a new value into the specified partition.
    /// The partition holding value will be split if it is full (meaning it has reached its capacity).
    /// The capacity of a partition is double the initial count of values in the partition.
    /// </summary>
    /// <param name="partitionForInsert">The partition to insert the new value into.</param>
    private void DoInsert(Partition<T> partitionForInsert)
    {
        if (partitionForInsert.NeedsSplitting)
        {
            var rightPartition = partitionForInsert.Split(_partitionIndexForInsert);
            _partitions.Insert(_partitionIndexForInsert + 1, rightPartition);
        }
        partitionForInsert.Insert(_valueToInsert); // An O(1) operation because we are adding to the end of the list
    }

    /// <summary>
    /// Removes a value from the partitions.
    /// The partition holding _valueToRemove will be removed when is emptied.
    /// </summary>
    private void DoRemove()
    {
        Debug.Assert(_valueToRemove != null);
        (var partitionForRemove, _partitionIndexForRemove) = FindPartitionContaining(_valueToRemove);

        // For removals, unlike insertions, we must remove the value from the partition that contains it
        if (_valueToRemove.CompareTo(partitionForRemove.LowestValue) < 0)
        {
            // Must be in the previous partition
            Debug.Assert(_partitionIndexForRemove != null);
            _partitionIndexForRemove--;
            partitionForRemove = _partitions[_partitionIndexForRemove.Value];
        }

        Debug.Assert(_valueToRemove.CompareTo(partitionForRemove.LowestValue) >= 0);
        Debug.Assert(_valueToRemove.CompareTo(partitionForRemove.HighestValue) <= 0);
        if (partitionForRemove.Count == 1)
        {
            // Remove the partition if it only contains the value we are removing
            _partitions.Remove(partitionForRemove);
            if (_partitionIndexForRemove < _partitionIndexForInsert)
            {
                _partitionIndexForInsert--;
            }
        }
        else
        {
            partitionForRemove.Remove(_valueToRemove);
        }
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
        if (partitionIndex >= _partitions.Count)
        {
            // Must be in the last partition
            partitionIndex = _partitions.Count - 1;
        }
        else if (partitionIndex > 0)
        {
            // the LowestValue could also be in the position to the left
            var adjacentLeftPosition = _partitions[partitionIndex - 1];
            if (adjacentLeftPosition.HighestValue.CompareTo(value) >= 0)
            {
                partitionIndex--;
            }
        }
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