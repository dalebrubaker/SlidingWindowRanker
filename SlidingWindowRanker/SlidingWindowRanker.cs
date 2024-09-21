using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SlidingWindowRanker.Tests")]

namespace SlidingWindowRanker;

/// <summary>
/// Partial class so we can do Unit Testing on private methods in th test project
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class SlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly List<Partition<T>> _partitions = [];

    /// <summary>
    /// The queue of all values so we know which one to remove at the left edge of the window.
    /// They are NOT sorted. They are in the order in which they were added.
    /// </summary>
    private readonly Queue<T> _valueQueue;

    /// <summary>
    /// The size of the window. Normally this is the same as the number of initial values,
    /// but it can be set to a higher value if starting with little or no initial values.
    /// </summary>
    private readonly int _windowSize;

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
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public SlidingWindowRanker(List<T> initialValues, int partitionCount, int windowSize = -1)
    {
        if (windowSize < 0)
        {
            // Use wants to default to the size of the initial values
            windowSize = initialValues.Count;
        }
        _windowSize = windowSize;
        if (_windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize),
                "The window size must be greater than 0, in order to have values to rank against.");
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

        int partitionSize;
        if (_windowSize % 2 == 0)
        {
            // An even number of values in the window
            partitionSize = _windowSize / partitionCount;
        }
        else
        {
            // Add 1 to _windowSize so we can round up on the integer division. E.g. 5 values and 3 partitions
            // should have values per partition of [2, 2, 1] not [1, 1, 1]
            partitionSize = (_windowSize + 1) / partitionCount;
        }
        for (var i = 0; i < partitionCount; i++)
        {
            var startIndex = i * partitionSize;
            // Last partition gets the remaining values
            var getRangeCount = i == partitionCount - 1 ? values.Count - startIndex : partitionSize;
            var partitionValues = values.GetRange(startIndex, getRangeCount);
            var partition = new Partition<T>(partitionValues, partitionSize)
            {
                LowerBound = startIndex
            };
            _partitions.Add(partition);
        }
    }

    private bool IsQueueFull => _valueQueue.Count >= _windowSize;

    public int CountPartitionSplits { get; private set; }

    public int CountPartitionRemoves { get; private set; }

    /// <summary>
    /// Returns the rank of the specified value, as a fraction of the total number of values in the window
    /// that are LESS THAN the given value.
    /// This is Cumulative Distribution Function (CDF) value for the specified value
    /// except that CDF is normally defined as LESS THAN OR EQUAL rather than LESS THAN.
    /// So the values returned will be in the range [0, 1] NOT inclusive of 1) rather than [0, 1] inclusive.
    ///
    /// The given value given is added to the right side of the window and the oldest value is removed from the left side
    /// of the window. The result is what would be calculated based on the values in the window AFTER the add/remove.
    /// But we determine the result BEFORE we do the add/remove so we can later have a different thread
    /// or threads do the insert and/or remove. Finally, we adjust the partition LowerBound values to reflect the insert and remove.
    /// </summary>
    /// <param name="value">The value to calculate the Rank for.</param>
    /// <returns>The fraction of values in the window that are less than the specified value.</returns>
    public double GetRank(T value)
    {
        _valueToRemove = IsQueueFull ? _valueQueue.Dequeue() : default;
        _valueQueue.Enqueue(value);
        _valueToInsert = value;
#if DEBUG
        _debugMessageRemove = null;
        _debugMessageInsert = null;
#endif

        // We use _valueQueue.Count instead of _windowSize because the window may not be full yet
        var rank = CalculateRankBeforeDoingInsertAndRemove(_valueToInsert, _valueToRemove, _valueQueue.Count);

        // 20240924 We can do Remove first or Insert first, but we must do both before AdjustPartitionsLowerBounds
        var (partitionIndexChangedByInsert, partitionIndexSplitInserted) = DoInsert(_valueToInsert);
        var partitionIndexChangedByRemove = DoRemove(_valueToRemove);
        AdjustPartitionsLowerBounds(partitionIndexChangedByInsert, partitionIndexSplitInserted, partitionIndexChangedByRemove);
        return rank;
    }

    /// <summary>
    /// Calculate the rank BEFORE doing insert and remove,
    /// so we can later have a different thread or threads do the insert and/or remove
    /// </summary>
    /// <param name="valueToInsert">The value to insert</param>
    /// <param name="valueToRemove">The value to remove, which will be null until the queue is full</param>
    /// <param name="countValues">The number of values in _valuesQueue. We use _valueQueue.Count instead of _windowSize
    /// because the window may not be full yet </param>
    /// <returns></returns>
    private double CalculateRankBeforeDoingInsertAndRemove(T valueToInsert, T valueToRemove, int countValues)
    {
        Partition<T> partitionForInsert;
        if (valueToInsert.CompareTo(_partitions[^1].LowestValue) >= 0)
        {
            // The value will be added to the end of the last partition
            partitionForInsert = _partitions[^1];
            if (valueToInsert.CompareTo(_partitions[^1].HighestValue) >= 0)
            {
                // No matter what happens, a lower value will be removed and valueToInsert will be added at the end,
                // so we know the result without doing more work
                var result = (countValues - 1) / (double)countValues;
                return result;
            }
        }
        else
        {
            (partitionForInsert, _) = FindPartitionContaining(valueToInsert);
        }
        var indexWithinPartitionForInsert = partitionForInsert.GetLowerBoundWithinPartition(valueToInsert);
        var lowerBound = partitionForInsert.LowerBound + indexWithinPartitionForInsert;
        if (IsQueueFull && valueToRemove?.CompareTo(valueToInsert) < 0)
        {
            // After we do the insert and remove, the lower bound will be one less
            lowerBound--;
        }
        var rank = (double)lowerBound / countValues;
        return rank;
    }

    /// <summary>
    /// Reflect the insertion and removal of values in the partitions.
    /// An insertion will increment the LowerBound of all partitions to the right of the partition holding the inserted value.
    /// A removal will decrement the LowerBound of all partitions to the right of the partition holding the inserted value.
    /// </summary>
    /// <param name="partitionIndexChangedByInsert"></param>
    /// <param name="partitionIndexSplitInserted"></param>
    /// <param name="partitionIndexChangedByRemove">-1 means no remove happened</param>
    private void AdjustPartitionsLowerBounds(int partitionIndexChangedByInsert, int partitionIndexSplitInserted = -1,
        int partitionIndexChangedByRemove = -1)
    {
#if DEBUG
        for (var i = 1; i < _partitions.Count; i++)
        {
            var partition = _partitions[i];
            var previousPartition = _partitions[i - 1];
            if (previousPartition.HighestValue.CompareTo(partition.LowestValue) > 0)
            {
                _ = _debugMessageInsert;
                _ = _debugMessageRemove;
                throw new SlidingWindowRankerException(
                    "The HighestValue of the previous partition is greater than the LowestValue of the current partition.");
            }
        }
#endif
        if (partitionIndexChangedByInsert < 0 && partitionIndexChangedByRemove >= 0)
        {
            // happens on unit test only
            partitionIndexChangedByInsert = _partitions.Count - 1;
        }
        var lowestPartitionChanged = partitionIndexChangedByInsert;
        var highestPartitionChanged = partitionIndexChangedByInsert;
        if (partitionIndexChangedByRemove >= 0)
        {
            lowestPartitionChanged = Math.Min(partitionIndexChangedByRemove, lowestPartitionChanged);
            highestPartitionChanged = Math.Max(partitionIndexChangedByRemove, highestPartitionChanged);
        }
        if (partitionIndexSplitInserted >= 0)
        {
            lowestPartitionChanged = Math.Min(partitionIndexSplitInserted, lowestPartitionChanged);
            highestPartitionChanged = Math.Max(partitionIndexSplitInserted, highestPartitionChanged);
        }
        highestPartitionChanged = Math.Min(highestPartitionChanged, _partitions.Count - 1);
        for (var i = lowestPartitionChanged; i <= highestPartitionChanged; i++)
        {
            var partition = _partitions[i];
            partition.LowerBound = i == 0 ? 0 : _partitions[i - 1].LowerBound + _partitions[i - 1].Count;
        }
        DebugGuardPartitionLowerBoundValuesAreCorrect();
    }

    /// <summary>
    /// Inserts a new value into the specified partition.
    /// The partition holding value will be split if it is full (meaning it has reached its capacity).
    /// The capacity of a partition is double the initial count of values in the partition.
    /// </summary>
    private (int partitionIndexChangedByInsert, int partitionIndexSplitInserted) DoInsert(T valueToInsert)
    {
        var (partitionForInsert, partitionIndexChangedByInsert) = FindPartitionContaining(valueToInsert);
        var partitionIndexSplitInserted = -1;
        if (partitionForInsert.NeedsSplitting)
        {
            CountPartitionSplits++;
            var rightPartition = partitionForInsert.SplitAndInsert(valueToInsert);
            _partitions.Insert(partitionIndexChangedByInsert + 1, rightPartition);
            partitionIndexSplitInserted = partitionIndexChangedByInsert + 1;
#if DEBUG
            _debugMessageInsert = $"Split partitionForInsert={partitionForInsert} "
                                  + $"at partitionIndexChangedByInsert={partitionIndexChangedByInsert} "
                                  + $"partitionIndexSplitInserted={partitionIndexSplitInserted}";
#endif
        }
        else
        {
            partitionForInsert.Insert(valueToInsert);
#if DEBUG
            _debugMessageInsert = $"Inserted value into partitionForInsert={partitionForInsert} "
                                  + $"at partitionIndexChangedByInsert={partitionIndexChangedByInsert}";
#endif
        }
        return (partitionIndexChangedByInsert, partitionIndexSplitInserted);
    }

    /// <summary>
    /// Removes a value from the partitions.
    /// The partition holding _valueToRemove will be removed when is emptied.
    /// </summary>
    /// <param name="valueToRemove">The value to remove.</param>
    /// <returns>The index of the partition where the value was removed, or -1 if no value was removed.</returns>
    private int DoRemove(T valueToRemove)
    {
        if (!IsQueueFull)
        {
            // We don't start removing values until the queue is full
            return -1; // no remove happened
        }
        var (partitionForRemove, partitionIndexChangedByRemove) = FindPartitionContaining(valueToRemove);
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
        if (partitionForRemove.Count == 1)
        {
            // Remove the partition if it only contains the value we are removing
            _partitions.RemoveAt(partitionIndexChangedByRemove);
            CountPartitionRemoves++;
        }
        else
        {
            partitionForRemove.Remove(valueToRemove);
        }
#if DEBUG
        _debugMessageRemove = $"Removed  value in _partitionForRemove={partitionForRemove} "
                              + $"at partitionForRemoveIndex={partitionIndexChangedByRemove}";
#endif
        return partitionIndexChangedByRemove;
    }

    /// <summary>
    /// Finds the partition containing the specified value.
    /// </summary>
    /// <param name="value">The value to find the partition for.</param>
    /// <returns>The partition where we want to remove or insert the value.</returns>
    private (Partition<T> partition, int partitionIndex) FindPartitionContaining(T value)
    {
        var partitionIndex = LowerBound(value);
        if (partitionIndex >= _partitions.Count)
        {
            // Must be in the last partition
            partitionIndex = _partitions.Count - 1;
        }
        var partition = _partitions[partitionIndex];
        return (partition, partitionIndex);
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
        return $"#values={_valueQueue.Count:N0}, #partitions={_partitions.Count} _windowSize={_windowSize:N0}";
    }
}