using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SlidingWindowRanker.Tests")]

namespace SlidingWindowRanker;

internal class Partition<T> : IComparable<Partition<T>> where T : IComparable<T>
{
    public Partition(List<T> values, int partitionSize = -1)
    {
        if (partitionSize < 0)
        {
            partitionSize = values.Count;
        }
        Values = values;
        Values.Capacity = Math.Max(Values.Capacity, partitionSize * 2); // Leave room to grow
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
        Values.Insert(index, value);
    }

    public void Remove(T value)
    {
        var index = Values.LowerBound(value);
        Debug.Assert(Values[index].CompareTo(value) == 0); // There must always be a value to remove
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

    public override string ToString()
    {
        return $"#values={Values.Count:N0} LowerBound={LowerBound:N0}";
    }
}