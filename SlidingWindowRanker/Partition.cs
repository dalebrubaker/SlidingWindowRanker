namespace SlidingWindowRanker;

public class Partition<T> where T : IComparable<T>
{
    public Partition(List<T> values)
    {
        Values = values;
        Values.Capacity = Math.Max(Values.Capacity, values.Count * 2); // Leave room to grow
    }

    public List<T> Values { get; }

    public T LowestValue => Values[0];

    public T HighestValue => Values[^1];

    public bool IsEmpty => Values.Count == 0;

    /// <summary>
    /// The partition needs splitting if it is has reached its capacity and the next Insert would cause it to grow.
    /// </summary>
    public bool NeedsSplitting => Values.Count == Values.Capacity;

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
}