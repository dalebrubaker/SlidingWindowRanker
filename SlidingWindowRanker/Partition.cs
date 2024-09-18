namespace SlidingWindowRanker;

public class Partition<T> : IComparable<Partition<T>> where T : IComparable<T>
{
    public Partition(List<T> values)
    {
        Values = values;
        Values.Capacity = Math.Max(Values.Capacity, values.Count * 2); // Leave room to grow
    }

    public List<T> Values { get; }

    /// <summary>
    ///     This is the lower bound for the entire window of the lowest value in the partition.
    ///     Every Add operation will reset this value for every partition that is affected by adding the new value
    ///     and by removing the oldest queued value.
    /// </summary>
    public int LowerBound { get; set; }

    public T LowestValue => Values[0];

    public T HighestValue => Values[^1];

    public int Count => Values.Count;

    /// <summary>
    ///     The partition needs splitting if it is has reached its capacity and the next Insert would cause it to grow.
    /// </summary>
    public bool NeedsSplitting => Values.Count == Values.Capacity;

    public int CompareTo(Partition<T>? other)
    {
        return other == null ? 0 : LowerBound.CompareTo(other.LowerBound);
    }

    public void Insert(T value)
    {
        var index = Values.LowerBound(value);
        Values.Insert(index, value);
    }

    public void Remove(T value)
    {
        var index = Values.LowerBound(value);
        Values.RemoveAt(index);
    }

    public Partition<T> Split(int splitIndex)
    {
        var rightValues = Values.GetRange(splitIndex, Values.Count - splitIndex);
        var rightPartition = new Partition<T>(rightValues);
        Values.RemoveRange(splitIndex, Values.Count - splitIndex);
        return rightPartition;
    }

    public int GetLowerBoundWithinPartition(T value)
    {
        var lowerBound = Values.LowerBound(value);
        return lowerBound;
    }
}