namespace SlidingWindowRanker;

internal partial class Partition<T> : IPartition<T> where T : IComparable<T>
{
    private readonly int _partitionSize;

    public Partition(List<T> values, int partitionSize = -1)
    {
        _partitionSize = partitionSize;
        if (_partitionSize < 0)
        {
            _partitionSize = values.Count;
        }
        if (_partitionSize == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionSize), "The partition size must be greater than 0.");
        }
        Values = values;
        Values.Capacity = Math.Max(Values.Capacity, _partitionSize * 2); // Leave room to grow
    }

    public List<T> Values { get; }

    /// <summary>
    ///     This is the lower bound for the entire window of the lowest value in the partition.
    ///     Every Add operation will reset this value for every partition that is affected by adding the new value
    ///     and by removing the oldest queued value.
    /// </summary>
    public int LowerBound { get; set; }

    /// <summary>
    /// Return the lowest value in the partition or null if the partition is empty.
    /// </summary>
    public T LowestValue => Values.Count == 0 ? default : Values[0];

    /// <summary>
    /// Return the highest value in the partition or null if the partition is empty.
    /// </summary>
    public T HighestValue => Values.Count == 0 ? default : Values[^1];

    public int Count => Values.Count;

    /// <summary>
    /// The partition needs to be split if it has reached its capacity and the next Insert would cause it to grow.
    /// </summary>
    public bool IsFull => Values.Count == Values.Capacity;

    public int CompareTo(Partition<T> other)
    {
        return other == null ? 0 : LowerBound.CompareTo(other.LowerBound);
    }

    public void Insert(T value)
    {
        var index = Values.LowerBound(value);
        if (index > Values.Count)
        {
            // avoid a crash if our LowerBound came in with count
            index = Values.Count;
        }
        Values.Insert(index, value);
#if DEBUG
        if (!Values.IsSortedAscending())
        {
            throw new SlidingWindowRankerException("The value was not inserted at the expected index.");
        }
#endif
    }

    public void Remove(T value)
    {
        var index = Values.LowerBound(value);
        var existingValue = Values[index];
        if (existingValue.CompareTo(value) != 0)
        {
            throw new SlidingWindowRankerException("The value to remove was not found in the partition.");
        }
        Values.RemoveAt(index);
    }

    /// <summary>
    /// Split this partition at the index.
    /// If <see cref="valueToInsert"/> is greater than or equal to the highest value in this partition, we add it to the right partition,
    /// because we can't allow it to be empty. Otherwise, we add it to this partition.
    /// </summary>
    /// <param name="valueToInsert"></param>
    /// <returns>the Partition to insert AFTER this partition.</returns>
    public Partition<T> SplitAndInsert(T valueToInsert)
    {
        var splitIndex = Values.LowerBound(valueToInsert);
        var isSplittingAtEnd = splitIndex == Values.Count;
        var rightValues = Values.GetRange(splitIndex, Values.Count - splitIndex);
        Values.RemoveRange(splitIndex, Values.Count - splitIndex);

        // Leave room to grow. But note that for small partitions,
        // rightValues.Capacity may be a minimum of 4 here because of List.DefaultCapacity
        rightValues.Capacity = Math.Max(rightValues.Capacity, _partitionSize * 2); // Leave room to grow
        var rightPartition = new Partition<T>(rightValues, _partitionSize);

        // The LowerBound of this partition doesn't change
        // The new partition starts after this partition
        // BUT ignore the Insert below because AdjustPartitionsLowerBounds needs to do the incrementing/decrementing properly
        rightPartition.LowerBound = LowerBound + Values.Count;
        if (isSplittingAtEnd)
        {
            // We must add the value into the right partition because we can't allow it to be empty
            rightPartition.Insert(valueToInsert);
        }
        else
        {
            Insert(valueToInsert);
        }
        return rightPartition;
    }

    public int GetLowerBoundWithinPartition(T value)
    {
        var lowerBound = Values.LowerBound(value);
        return lowerBound;
    }

    public bool Contains(T value)
    {
        if (Values.Count == 0)
        {
            return false;
        }
        return value.CompareTo(Values[0]) >= 0 && value.CompareTo(Values[^1]) <= 0;
    }

    public override string ToString()
    {
        var values = Values.Count > 10 ? Values.Take(10).ToList() : Values;
        var valuesStr = string.Join(", ", values);
        if (Values.Count > 10)
        {
            valuesStr += "...";
        }
        return $"LowerBound={LowerBound:N0} #values={Values.Count:N0}: {valuesStr} ";
    }
}