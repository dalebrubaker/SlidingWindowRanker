namespace SlidingWindowRanker;

internal partial class Partition<T> : IComparable<Partition<T>> where T : IComparable<T>
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
            throw new ArgumentOutOfRangeException(nameof(_partitionSize), "The partition size must be greater than 0.");
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
    /// The partition needs splitting if it has reached its capacity and the next Insert would cause it to grow.
    /// </summary>
    public bool NeedsSplitting => Values.Count == Values.Capacity;

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
    /// Split this partition in half and return the index of the partition to insert the new value.
    /// </summary>
    /// <param name="valueToInsert"></param>
    /// <returns>the Partition to insert AFTER this partition and true if <see cref="valueToInsert"/> was inserted into the new partition,
    /// or false if it was inserted into this partition</returns>
    public (Partition<T> rightPartition, bool isNewValueInRightPartition) SplitAndInsert(T valueToInsert)
    {
        // Split the partition in half
        var halfIndex = Values.Count / 2;
        var rightValues = Values.GetRange(halfIndex, Values.Count - halfIndex);
        Values.RemoveRange(halfIndex, Values.Count - halfIndex);

        // Leave room to grow. But note that for small partitions,
        // rightValues.Capacity may be a minimum of 4 here because of List.DefaultCapacity
        rightValues.Capacity = Math.Max(rightValues.Capacity, _partitionSize * 2); // Leave room to grow
        var rightPartition = new Partition<T>(rightValues, _partitionSize)
        {
            // The LowerBound of this partition doesn't change
            // The new partition starts after this partition and its is set BEFORE the new value is inserted so that the 
            LowerBound = LowerBound + Values.Count
        };

        // Now insert the new value into the correct partition
        bool isNewValueInRightPartition;
        if (valueToInsert.CompareTo(HighestValue) <= 0)
        {
            isNewValueInRightPartition = false;
            Insert(valueToInsert);
        }
        else
        {
            isNewValueInRightPartition = true;
            rightPartition.Insert(valueToInsert);
        }

        return (rightPartition, isNewValueInRightPartition);
    }

    public int GetLowerBoundWithinPartition(T value)
    {
        var lowerBound = Values.LowerBound(value);
        return lowerBound;
    }

    public override string ToString()
    {
        var values = Values.Count > 10 ? Values.Take(10).ToList() : Values;
        var valuesStr = string.Join(", ", values);
        if (Values.Count > 10)
        {
            valuesStr += "...";
        }
        return $"LowerBound={LowerBound:N0} #values={Values.Count:N0} {valuesStr} ";
    }
}