using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SlidingWindowRanker;

public partial class SlidingWindowRanker<T> where T : IComparable<T>
{
    internal readonly List<Partition<T>> _partitions = [];

    /// <summary>
    /// All retained values in chronological order (oldest first), so we know which one to remove
    /// at the left edge of the window and which one is the newest at the right edge.
    /// They are NOT sorted. They are in the order in which they were added.
    /// Not populated when <see cref="_windowSize"/> is int.MaxValue, because nothing is ever evicted.
    /// </summary>
    private readonly ValueDeque<T> _valueDeque;

    /// <summary>
    /// The size of the window. Normally this is the same as the number of initial values,
    /// but it can be set to a higher value if starting with little or no initial values.
    /// </summary>
    private readonly int _windowSize;

    private bool _isQueueFull;

    /// <summary>
    /// The newest value, tracked only when <see cref="_windowSize"/> is int.MaxValue and therefore
    /// <see cref="_valueDeque"/> is not populated. See <see cref="RemoveLast"/>.
    /// </summary>
    private T _newestValue;

    private bool _hasNewestValue;

    /// <summary>
    /// The value evicted from the left edge by the most recent add, if that add evicted one.
    /// <see cref="RemoveLast"/> restores it, so that undoing an add restores the window exactly.
    /// </summary>
    private T _lastEvictedValue;

    private bool _hasLastEvictedValue;

    internal double _rankDenominator;

    /// <summary>
    /// Initializes a new instance of the SlidingWindowRanker class.
    /// The window size is set to the number of initial values.
    /// The partition count is calculated as the square root of the window size.
    /// </summary>
    /// <param name="windowSize">-1 means to use initialValues.Count. Must be no smaller than initialValues.
    /// int.MaxValue means to never remove a value from the left edge of the window.</param>
    /// <param name="initialValues">The initial values to populate the sliding window, ordered oldest to newest, if not null.</param>
    /// <param name="isSorted">true means the initialValues are ascending by value as well as oldest-to-newest,
    /// preventing an additional sort here.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public SlidingWindowRanker(int windowSize, List<T> initialValues = null, bool isSorted = false)
        : this(initialValues ?? [], -1, windowSize, isSorted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the SlidingWindowRanker class.
    /// </summary>
    /// <param name="initialValues">The initial values to populate the sliding window, ordered oldest to newest.</param>
    /// <param name="partitionCount">The number of partitions to divide the values into. If less than or equal to zero,
    ///     use the square root of the given or calculated window size, which is usually optimal or close to it.</param>
    /// <param name="windowSize">-1 means to use initialValues.Count. Must be no smaller than initialValues.
    /// int.MaxValue means to never remove a value from the left edge of the window.</param>
    /// <param name="isSorted">true means the initialValues are ascending by value as well as oldest-to-newest,
    /// preventing an additional sort here.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public SlidingWindowRanker(List<T> initialValues, int partitionCount = -1, int windowSize = -1, bool isSorted = false)
    {
        _rankDenominator = initialValues.Count;
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
        if (initialValues.Count > _windowSize)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize),
                "The window size must be at least the number of initial values.");
        }
        if (partitionCount < 1)
        {
            partitionCount = (int)Math.Sqrt(_windowSize);
        }
        if (partitionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionCount),
                "The partition count must be at least 1, in order to have values to rank against.");
        }
        if (_windowSize == int.MaxValue)
        {
            // Nothing is ever evicted, so retaining every value would waste time and memory.
            // Only the newest value is retained, which is all RemoveLast needs.
            _valueDeque = new ValueDeque<T>([]);
            if (initialValues.Count > 0)
            {
                _newestValue = initialValues[^1];
                _hasNewestValue = true;
            }
        }
        else
        {
            // Cap growth at the window size plus the single transient value that exists
            // between the insert and the eviction within AddValue
            _valueDeque = new ValueDeque<T>(initialValues, _windowSize + 1);
        }
        _isQueueFull = _valueDeque.Count >= _windowSize;
        List<T> values;
        if (!isSorted)
        {
            // Sort the initial values so we can divide them into partitions
            // But be friendly to the caller, so sort a new list and leave the given list unchanged
            values = [..initialValues];
            values.Sort();
        }
        else
        {
            values = initialValues;
        }
        int partitionSize;
        if (_windowSize % 2 == 0)
        {
            // An even number of values in the window
            partitionSize = Math.Max(1, _windowSize / partitionCount);
        }
        else
        {
            // Add 1 to _windowSize so we can round up on the integer division. E.g. 5 values and 3 partitions
            // should have values per partition of [2, 2, 1] not [1, 1, 1]
            partitionSize = Math.Max(1, (int)(((long)_windowSize + 1) / partitionCount));
        }
        if (values.Count == 0)
        {
            // We need at least one partition
            var emptyPartition = new Partition<T>(new List<T>(), partitionSize)
            {
                LowerBound = 0
            };
            _partitions.Add(emptyPartition);
            return;
        }
        var valuesAddedIntoPartitionsThusFar = 0;
        var countRemainingValues = values.Count;
        while (countRemainingValues > 0)
        {
            var getRangeCount = Math.Min(partitionSize, values.Count - valuesAddedIntoPartitionsThusFar);
            var partitionValues = values.GetRange(valuesAddedIntoPartitionsThusFar, getRangeCount);
            var partition = new Partition<T>(partitionValues, partitionSize)
            {
                LowerBound = valuesAddedIntoPartitionsThusFar
            };
            _partitions.Add(partition);
            valuesAddedIntoPartitionsThusFar += getRangeCount;
            countRemainingValues = values.Count - valuesAddedIntoPartitionsThusFar;
        }
        if (_partitions.Count == 0)
        {
            // We need at least one partition
            var emptyPartition = new Partition<T>(new List<T>(), partitionSize)
            {
                LowerBound = 0
            };
            _partitions.Add(emptyPartition);
        }
    }

    public int CountPartitionSplits { get; private set; }

    public int CountPartitionRemoves { get; private set; }

    /// <summary>
    /// The number of values currently in the window, which is the denominator of every rank.
    /// Useful for callers to check warmup readiness and to guard <see cref="RemoveLast"/>.
    /// When the window size is int.MaxValue nothing is ever evicted, so this is the number of values seen so far.
    /// </summary>
    public int Count => (int)_rankDenominator;

    /// <summary>
    /// Returns the rank of the specified value, as a fraction of the total number of values in the window
    /// that are LESS THAN the given value.
    /// This is Cumulative Distribution Function (CDF) value for the specified value
    /// except that CDF is normally defined as LESS THAN OR EQUAL rather than LESS THAN.
    /// So the values returned will be in the range ([0, 1] NOT inclusive of 1) rather than [0, 1] inclusive.
    ///
    /// The given value given is added to the right side of the window and the oldest value is removed from the left side
    /// of the window. The result is what would be calculated based on the values in the window AFTER the add/remove.
    /// The insert and removal are completed before the rank is calculated, so the returned rank describes
    /// the updated window.
    /// </summary>
    /// <param name="valueToInsert">The value to calculate the Rank for.</param>
    /// <returns>The fraction of values in the window that are less than the specified value.</returns>
    public double GetRank(T valueToInsert)
    {
        var partitionIndexForInsert = AddValue(valueToInsert);
        var partitionForInsert = _partitions[partitionIndexForInsert];

        // Now get the rank
        var indexWithinPartitionForInsert = partitionForInsert.GetLowerBoundWithinPartition(valueToInsert);
        var lowerBound = partitionForInsert.LowerBound + indexWithinPartitionForInsert;
        var rank = lowerBound / _rankDenominator;
        return rank;
    }

    /// <summary>
    /// Adds a value to the right side of the window and removes the oldest value when the window is full,
    /// without calculating a rank.
    /// </summary>
    /// <param name="valueToInsert">The value to add to the sliding window.</param>
    public void Add(T valueToInsert)
    {
        AddValue(valueToInsert);
    }

    /// <summary>
    /// Updates the window and returns the final partition index containing the inserted value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddValue(T valueToInsert)
    {
        if (_windowSize == int.MaxValue)
        {
            // When _windowSize is int.MaxValue, we don't need to waste time and memory using the deque
            // The denominator of the rank is the number of values seen so far
            _rankDenominator++;
            _newestValue = valueToInsert;
            _hasNewestValue = true;
        }
        else
        {
            _valueDeque.AddLast(valueToInsert);
        }
#if DEBUG
        _debugMessageRemove = null;
        _debugMessageInsert = null;
        if (valueToInsert?.ToString() == "0")
        {
            _debugCounter++;
        }
#endif

        // Only the most recent add can be undone, so any earlier eviction is now permanent
        _hasLastEvictedValue = false;
        var partitionIndexForInsert = FindPartitionContaining(valueToInsert);
        var beginIncrementsIndex = DoInsert(valueToInsert, ref partitionIndexForInsert);
        var beginDecrementsIndex = DoRemove(ref partitionIndexForInsert, ref beginIncrementsIndex);
        AdjustPartitionsLowerBounds(beginIncrementsIndex, beginDecrementsIndex);
        return partitionIndexForInsert;
    }

    /// <summary>
    /// Returns the rank of the specified value, just like <see cref="GetRank(T)"/> except that the value
    /// is not added to right edge of the window nor is a value removed from the left edge.
    /// </summary>
    /// <param name="valueToCheck"></param>
    /// <returns></returns>
    public double GetRankNoAdd(T valueToCheck)
    {
        var partitionIndexForInsert = FindPartitionContaining(valueToCheck);
        var partitionForInsert = _partitions[partitionIndexForInsert];

        // Now get the rank
        var indexWithinPartitionForInsert = partitionForInsert.GetLowerBoundWithinPartition(valueToCheck);
        var lowerBound = partitionForInsert.LowerBound + indexWithinPartitionForInsert;
        var rank = lowerBound / _rankDenominator;
        return rank;
    }

    /// <summary>
    /// Undoes the most recent observation: removes the NEWEST value from the right edge of the window and,
    /// when the add that placed it there evicted the oldest value from the left edge, restores that evicted
    /// value. The window is therefore left EXACTLY as it was before that add.
    ///
    /// This exists for realtime bars that update many times before they close. Call this to undo the
    /// observation contributed by the previous update of the bar, so the bar contributes exactly one
    /// observation once it closes. <see cref="ReplaceLastAndGetRank"/> does the undo-and-redo in one step.
    ///
    /// Only the single most recent add can be undone this way. Calling this again removes the value that is
    /// then newest, but there is no longer an eviction to restore, so the window shrinks by one. In other
    /// words the first call rewinds an add, and further calls trim observations off the newest end.
    ///
    /// <see cref="CountPartitionSplits"/> and <see cref="CountPartitionRemoves"/> are cumulative counters of
    /// work actually performed, so they are never rolled back.
    ///
    /// O(log sqrt(N)) to locate each partition plus O(sqrt(N)) to update it, the same cost as an add.
    /// </summary>
    /// <returns>The value that was removed, which is the value most recently added.</returns>
    /// <exception cref="SlidingWindowRankerException">The window is empty, so there is no value to remove.
    /// The window is not modified when this is thrown.
    ///
    /// Also thrown when the window size is int.MaxValue and this is called twice without an intervening add.
    /// That mode deliberately does not retain the chronological sequence, because nothing is ever evicted,
    /// so only the single newest value is available to remove.</exception>
    public T RemoveLast()
    {
        return RemoveLastCore(nameof(RemoveLast));
    }

    /// <summary>
    /// Replaces the newest value in the window with <paramref name="valueToInsert"/> and returns the rank
    /// of <paramref name="valueToInsert"/>, atomically from the point of view of the caller.
    ///
    /// Equivalent to <see cref="RemoveLast"/> followed by <see cref="GetRank"/>, so the result is identical
    /// to what <see cref="GetRank"/> would have returned had the replaced value never been added at all:
    /// <list type="bullet">
    /// <item>The number of values in the window and their chronological order are unchanged. The replacement
    /// takes the place in the sequence held by the value it replaces, so no additional value is evicted.</item>
    /// <item>The returned rank uses the same "rank AFTER insertion" semantics as <see cref="GetRank"/>:
    /// it is the fraction of the updated window that is LESS THAN <paramref name="valueToInsert"/>,
    /// where the updated window already contains <paramref name="valueToInsert"/> and no longer contains
    /// the value it replaced.</item>
    /// </list>
    /// Calling this repeatedly for successive updates of the same realtime bar always leaves the window
    /// holding exactly one observation for that bar, whatever value it last had, and always ranks against
    /// the same set of prior observations.
    /// </summary>
    /// <param name="valueToInsert">The value that replaces the newest value in the window.</param>
    /// <returns>The fraction of values in the updated window that are less than the specified value.</returns>
    /// <exception cref="SlidingWindowRankerException">The window is empty, so there is no value to replace.
    /// The window is not modified when this is thrown. See <see cref="RemoveLast"/> for the int.MaxValue limitation.</exception>
    public double ReplaceLastAndGetRank(T valueToInsert)
    {
        RemoveLastCore(nameof(ReplaceLastAndGetRank));
        return GetRank(valueToInsert);
    }

    /// <summary>
    /// The shared implementation of <see cref="RemoveLast"/>, naming the caller in any exception message.
    /// </summary>
    private protected T RemoveLastCore(string operationName)
    {
        // Validate before mutating anything, so a failure cannot corrupt the window
        var valueToRemove = RemoveNewestFromSequence(operationName);
        RemoveValueFromPartitions(valueToRemove);
        if (_hasLastEvictedValue)
        {
            // The add being undone evicted the oldest value, so put it back at the left edge
            var valueToRestore = _lastEvictedValue;
            _lastEvictedValue = default;
            _hasLastEvictedValue = false;
            InsertValueIntoPartitions(valueToRestore);
            _valueDeque.AddFirst(valueToRestore);
            _rankDenominator = _valueDeque.Count;
            _isQueueFull = _valueDeque.Count >= _windowSize;
        }
        return valueToRemove;
    }

    /// <summary>
    /// Removes the newest value from the chronological sequence and updates the denominator and
    /// queue-full state, leaving the partitions to the caller.
    /// </summary>
    private T RemoveNewestFromSequence(string operationName)
    {
        if (_windowSize == int.MaxValue)
        {
            if (!_hasNewestValue)
            {
                throw new SlidingWindowRankerException(
                    $"{operationName} requires a newest value to remove. When the window size is int.MaxValue only the "
                    + "single newest value is retained, because nothing is ever evicted, so the newest value cannot be "
                    + "removed twice without an intervening add.");
            }
            var newestValue = _newestValue;
            _newestValue = default;
            _hasNewestValue = false;
            _rankDenominator--;
            return newestValue;
        }
        if (_valueDeque.Count == 0)
        {
            throw new SlidingWindowRankerException(
                $"{operationName} requires at least one value in the window, but the window is empty.");
        }
        var valueToRemove = _valueDeque.RemoveLast();
        _rankDenominator = _valueDeque.Count;

        // Dropping below the window size means the next add refills the window instead of evicting
        _isQueueFull = _valueDeque.Count >= _windowSize;
        return valueToRemove;
    }

    /// <summary>
    /// Removes a value from the sorted partitions and repairs the lower bounds, with nothing inserted.
    /// </summary>
    private void RemoveValueFromPartitions(T valueToRemove)
    {
        var partitionIndexForRemove = FindPartitionContaining(valueToRemove);
        var partitionForRemove = _partitions[partitionIndexForRemove];
        int beginDecrementsIndex;
        if (partitionForRemove.Count == 1
            && _partitions.Count > 1) // don't remove the last partition. We need at least one partition, but it can be empty
        {
            // The partition holding the value to remove will be empty after the remove
            RemovePartition(partitionIndexForRemove, partitionForRemove);

            // The partition that followed the removed one now sits at partitionIndexForRemove
            beginDecrementsIndex = partitionIndexForRemove;
        }
        else
        {
            DoRemove(valueToRemove, partitionForRemove);
            beginDecrementsIndex = partitionIndexForRemove + 1;
        }

        // Nothing was inserted, so every partition at or after the removal point shifts down by one
        AdjustPartitionsLowerBounds(_partitions.Count, beginDecrementsIndex);
    }

    /// <summary>
    /// Inserts a value into the sorted partitions and repairs the lower bounds, with nothing removed.
    /// </summary>
    private void InsertValueIntoPartitions(T valueToInsert)
    {
        var partitionIndexForInsert = FindPartitionContaining(valueToInsert);
        var beginIncrementsIndex = DoInsert(valueToInsert, ref partitionIndexForInsert);

        // Nothing was removed, so every partition after the insertion point shifts up by one
        AdjustPartitionsLowerBounds(beginIncrementsIndex, _partitions.Count);
    }

    /// <summary>
    /// Removes the specified value from the window, either by removing within a partition or by removing the partition.
    /// </summary>
    /// <param name="partitionIndexForInsert"></param>
    /// <param name="beginIncrementsIndex"></param>
    /// <returns>the beginDecrementIndex - the index above which index must be decremented</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int DoRemove(ref int partitionIndexForInsert, ref int beginIncrementsIndex)
    {
        if (_windowSize == int.MaxValue)
        {
            // No need to use the deque
            return _partitions.Count; // No removal
        }
        if (!_isQueueFull)
        {
            _rankDenominator = _valueDeque.Count;
            if (_valueDeque.Count < _windowSize)
            {
                // We don't remove anything because the window is not full
                return _partitions.Count;
            }
            _isQueueFull = true;
            // The insertion that first fills a partial window belongs in the window.
            // Removal starts with the next insertion, when the queue exceeds the window size.
            return _partitions.Count;
        }
        var valueToRemove = _valueDeque.RemoveFirst();
        _lastEvictedValue = valueToRemove;
        _hasLastEvictedValue = true;
#if DEBUG
        if (valueToRemove?.ToString() == "0")
        {
            _debugCounter--;
        }
#endif
        var partitionIndexForRemove = FindPartitionContaining(valueToRemove);
        var partitionForRemove = _partitions[partitionIndexForRemove];
        if (partitionForRemove.Count == 1
            && _partitions.Count > 1) // don't remove the last partition. We need at least one partition, but it can be empty
        {
            // The partition holding the value to remove will be empty after the remove
            RemovePartition(partitionIndexForRemove, partitionForRemove);
            if (partitionIndexForRemove < partitionIndexForInsert)
            {
                partitionIndexForInsert--;
            }
            if (beginIncrementsIndex > partitionIndexForRemove)
            {
                beginIncrementsIndex--;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SplitPartition(Partition<T> partitionForInsert, int partitionIndexForInsert, T valueToInsert)
    {
        CountPartitionSplits++;
        var (rightPartition, isSplitIntoRightPartition) = partitionForInsert.SplitAndInsert(valueToInsert);
        _partitions.Insert(partitionIndexForInsert + 1, rightPartition);
#if DEBUG
        _debugMessageInsert = $"Split partitionForInsert={partitionForInsert} and inserted it at partitionIndexForInsert={partitionIndexForInsert}";
#endif
        return isSplitIntoRightPartition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemovePartition(int partitionIndexForRemove, Partition<T> partitionForRemove)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdjustPartitionsLowerBounds(int beginIncrementsIndex, int beginDecrementsIndex)
    {
        if (beginIncrementsIndex < beginDecrementsIndex)
        {
            for (var i = beginIncrementsIndex; i < beginDecrementsIndex; i++)
            {
                _partitions[i].LowerBound++;
            }
        }
        else
        {
            for (var i = beginDecrementsIndex; i < beginIncrementsIndex; i++)
            {
                _partitions[i].LowerBound--;
            }
        }
#if DEBUG
        DebugGuardPartitionLowerBoundValuesAreCorrect();
#endif
    }

    /// <summary>
    /// Inserts a new value into the specified partition.
    /// The partition holding value will be split if it is full (meaning it has reached its capacity).
    /// The capacity of a partition is double the initial count of values in the partition.
    /// </summary>
    /// <param name="valueToInsert">The value to insert.</param>
    /// <param name="partitionForInsert"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DoInsert(T valueToInsert, Partition<T> partitionForInsert)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DoRemove(T valueToRemove, Partition<T> partitionForRemove)
    {
        Debug.Assert(partitionForRemove.Count > 1 || _partitions.Count == 1,
            "Partition should have been removed before we get here unless we are keeping one empty partition");
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            $"_windowSize={_windowSize:N0} #values={Count:N0} #partitions={_partitions.Count} #splits={CountPartitionSplits:N0} #removes={CountPartitionRemoves:N0}";
    }
}
