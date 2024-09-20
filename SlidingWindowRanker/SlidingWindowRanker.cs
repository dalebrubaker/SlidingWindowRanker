using System.Diagnostics;
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

    private int _beginIndexForLowerBoundInsertIncrements;
    private int _beginIndexForLowerBoundRemoveDecrements;

    /// <summary>
    /// The index  in the partition where we did <see cref="DoInsert"/> at which we inserted a new value,
    /// or null if no value was inserted
    /// This is determined during <see cref="CalculateRankBeforeDoingInsertAndRemove"/>
    /// and saved for the later <see cref="DoInsert"/> 
    /// </summary>
    private int _indexWithinPartitionForInsert;

    private Partition<T> _partitionForInsert;
    private int _partitionForInsertIndex;
    private Partition<T> _partitionForRemove;
    private int _partitionInsertedIndex;

    private int _partitionRemovedIndex;

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
        _partitionForInsert = null;
        _partitionForRemove = null;
        _beginIndexForLowerBoundInsertIncrements = int.MaxValue; // No value yet
        _beginIndexForLowerBoundRemoveDecrements = int.MaxValue; // No value yet
        _partitionRemovedIndex = int.MaxValue; // No value yet
        _partitionInsertedIndex = int.MaxValue; // No value yet

        _indexWithinPartitionForInsert = int.MinValue; // a bad value so we throw if it never gets set
        var rank = CalculateRankBeforeDoingInsertAndRemove();
        Debug.Assert(_indexWithinPartitionForInsert != int.MinValue, "Must set _indexInPartitionForInsert before calling DoInsert");
        DoInsert();
        DoRemove();
        AdjustPartitionsLowerBounds();
        return rank;
    }

    /// <summary>
    /// Calculate the rank BEFORE doing insert and remove,
    /// so we can later have a different thread or threads do the insert and/or remove
    /// </summary>
    /// <returns></returns>
    private double CalculateRankBeforeDoingInsertAndRemove()
    {
        if (_valueToInsert.CompareTo(_partitions[^1].LowestValue) >= 0)
        {
            // Add the value to the end of the last partition
            _partitionForInsert = _partitions[^1];
            _partitionForInsertIndex = _partitions.Count - 1;
            if (_valueToInsert.CompareTo(_partitions[^1].HighestValue) >= 0)
            {
                // No matter what happens, a lower value will be removed and _valueToInsert will be added at the end, so we know the result without doing more work
                var result = (_valueQueue.Count - 1) / (double)_valueQueue.Count;
                _indexWithinPartitionForInsert = _partitionForInsert.GetLowerBoundWithinPartition(_valueToInsert);
                return result;
            }
        }
        else
        {
            (_partitionForInsert, _partitionForInsertIndex) = FindPartitionContaining(_valueToInsert);
        }
        _indexWithinPartitionForInsert = _partitionForInsert.GetLowerBoundWithinPartition(_valueToInsert);
        var lowerBound = _partitionForInsert.LowerBound + _indexWithinPartitionForInsert;
        if (IsQueueFull && _valueToRemove?.CompareTo(_valueToInsert) < 0)
        {
            // After we do the insert and remove, the lower bound will be one less
            lowerBound--;
        }

        // We use _valueQueue.Count instead of _windowSize because the window may not be full yet
        var rank = (double)lowerBound / _valueQueue.Count;
        return rank;
    }

    /// <summary>
    /// Reflect the insertion and removal of values in the partitions.
    /// An insertion will increment the LowerBound of all partitions to the right of the partition holding the inserted value.
    /// A removal will decrement the LowerBound of all partitions to the right of the partition holding the inserted value.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void AdjustPartitionsLowerBounds()
    {
        DebugGuardIsLowerBoundAscending();
        if (_partitionRemovedIndex < _beginIndexForLowerBoundInsertIncrements)
        {
            // The removed partition was 1 below the insert partition, so we need to increment the insert partition
            _beginIndexForLowerBoundInsertIncrements--;
            _ = _valueToInsert;
            _ = _valueToRemove;
        }
        var startIndex = Math.Min(_beginIndexForLowerBoundInsertIncrements, _beginIndexForLowerBoundRemoveDecrements);
        for (var i = startIndex; i < _partitions.Count; i++)
        {
            var netChange = 0;
            if (i >= _beginIndexForLowerBoundInsertIncrements)
            {
                netChange++;
            }
            if (i >= _beginIndexForLowerBoundRemoveDecrements)
            {
                netChange--;
            }
            if (netChange == 0)
            {
                // From here on there will be no net change
                break;
            }
            var partition = _partitions[i];
            partition.LowerBound += netChange;
#if DEBUG
            if (i == 0)
            {
                if (partition.LowerBound != 0)
                {
                    throw new SlidingWindowRankerException("The LowerBound of the first partition is not 0.");
                }
            }
            else
            {
                var previousPartition = _partitions[i - 1];
                if (partition.LowerBound != previousPartition.LowerBound + previousPartition.Count)
                {
                    throw new SlidingWindowRankerException("LowerBound is incorrect!");
                }
            }
#endif
        }
        //DebugGuardPartitionLowerBoundValuesAreCorrect();
    }

    /// <summary>
    /// Inserts a new value into the specified partition.
    /// The partition holding value will be split if it is full (meaning it has reached its capacity).
    /// The capacity of a partition is double the initial count of values in the partition.
    /// </summary>
    private void DoInsert()
    {
        if (_partitionForInsert.NeedsSplitting)
        {
            SplitPartitionAndDoInsert();
        }
        else
        {
            _partitionForInsert.Insert(_valueToInsert, _indexWithinPartitionForInsert);
        }
        _partitionForInsert = null; // no longer needed
        _indexWithinPartitionForInsert = int.MinValue; // no longer valid
        _beginIndexForLowerBoundInsertIncrements = _partitionForInsertIndex + 1;
        if (_partitionRemovedIndex < _beginIndexForLowerBoundInsertIncrements)
        {
            // A partition was already removed. Add to it here so AdjustPartitionsLowerBounds will decrement the LowerBound
            // The removed partition was 1 below the insert partition, so we need to increment the insert partition
            _beginIndexForLowerBoundInsertIncrements--;
        }
        _partitionForInsertIndex = int.MinValue; // no longer valid
    }

    private void SplitPartitionAndDoInsert()
    {
        CountPartitionSplits++;
        var rightPartition = _partitionForInsert.SplitAndInsert(_valueToInsert, _indexWithinPartitionForInsert);
        _partitions.Insert(_partitionForInsertIndex + 1, rightPartition);
        rightPartition.LowerBound = _partitionForInsert.LowerBound + _partitionForInsert.Count;
        _partitionForInsertIndex++;
        _beginIndexForLowerBoundInsertIncrements = _partitionForInsertIndex + 1;
#if DEBUG
        _splitPartitionMessages.Add($"Split _partitionForInsert={_partitionForInsert} "
                                    + $"at _partitionForInsertIndex={_partitionForInsertIndex} "
                                    + $"_beginIndexForLowerBoundInsertIncrements={_beginIndexForLowerBoundInsertIncrements}");
#endif
    }

    /// <summary>
    /// Removes a value from the partitions.
    /// The partition holding _valueToRemove will be removed when is emptied.
    /// </summary> 
    private void DoRemove()
    {
        if (!IsQueueFull)
        {
            // We don't start removing values until the queue is full
            _beginIndexForLowerBoundRemoveDecrements = int.MaxValue; // No decrements needed
            return;
        }
        Debug.Assert(_valueToRemove != null);
        (_partitionForRemove, var partitionForRemoveIndex) = FindPartitionContaining(_valueToRemove);
        if (_valueToRemove.CompareTo(_partitionForRemove.HighestValue) > 0)
        {
            throw new SlidingWindowRankerException("The value to remove above the HighestValue in the window.");
        }
        if (_valueToRemove.CompareTo(_partitionForRemove.LowestValue) < 0)
        {
            throw new SlidingWindowRankerException("The value to remove is below the LowestValue of the window.");
        }
        if (_partitionForRemove.Count == 1)
        {
            RemovePartition(partitionForRemoveIndex);
        }
        else
        {
            _partitionForRemove.Remove(_valueToRemove);

            // Decrement LowerBound for all partitions to the right of the partition holding the removed value
            _beginIndexForLowerBoundRemoveDecrements = partitionForRemoveIndex + 1;
        }
    }

    private void RemovePartition(int partitionForRemoveIndex)
    {
        // Remove the partition if it only contains the value we are removing
        _partitions.Remove(_partitionForRemove);
        CountPartitionRemoves++;

        // We removed this partition, so decrement starting at this index
        _beginIndexForLowerBoundRemoveDecrements = partitionForRemoveIndex;
        _partitionRemovedIndex = partitionForRemoveIndex;

#if DEBUG
        _removePartitionMessages.Add($"Removed _partitionForRemove={_partitionForRemove} "
                                     + $"at partitionForRemoveIndex={partitionForRemoveIndex} "
                                     + $"_beginIndexForLowerBoundRemoveDecrements={_beginIndexForLowerBoundRemoveDecrements}");
#endif
    }

    /// <summary>
    /// Finds the partition containing the specified value.
    /// </summary>
    /// <param name="value">The value to find the partition for.</param>
    /// <returns>The partition where we want to remove or insert the value.</returns>
    private (Partition<T> partition, int partitionIndex) FindPartitionContaining(T value)
    {
        DebugGuardIsLowerBoundAscending();
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