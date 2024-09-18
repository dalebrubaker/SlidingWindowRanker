namespace SlidingWindowRanker;

public class Partition<T>(List<T> values) where T : IComparable<T>
{
    public List<T> Values { get; } = values;

    public T LowestValue => Values[0];

    public T HighestValue => Values[^1];

    public bool IsEmpty => Values.Count == 0;

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
        // TODO: Does this change _values.Length? 
        var rightValues = Values.GetRange(splitIndex, Values.Count - splitIndex);
        var rightPartition = new Partition<T>(rightValues);
        Values.RemoveRange(splitIndex, Values.Count - splitIndex);
        return rightPartition;
    }
}