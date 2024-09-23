using System.Runtime.InteropServices;

namespace SlidingWindowRanker;

internal unsafe class UnsafePartition<T> : IPartition<T> where T : unmanaged, IComparable<T>
{
    private readonly int _capacity;
    private readonly T* _values;

    public UnsafePartition(int initialCapacity)
    {
        _capacity = initialCapacity;
        _values = (T*)Marshal.AllocHGlobal(_capacity * sizeof(T));
        Count = 0;
        LowerBound = 0;
    }

    public int LowerBound { get; set; }

    public T LowestValue => Count > 0 ? _values[0] : default;

    public T HighestValue => Count > 0 ? _values[Count - 1] : default;

    public int Count { get; private set; }

    public bool IsFull => Count >= _capacity;

    public int CompareTo(Partition<T> other)
    {
        // Implement comparison logic based on your requirements
        return 0;
    }

    public void Insert(T value)
    {
        if (IsFull)
        {
            // Handle partition splitting or resizing
            throw new InvalidOperationException("Partition is full.");
        }

        // Insert value in sorted order. In this case we don't need LowerBound because we insert anywhere within a range of duplicate values. 
        var index = UnsafeArrayHelper.BinarySearch(_values, 0, Count, value);
        if (index < 0)
        {
            index = ~index; // Get the insertion point
        }

        // Shift elements to make space
        for (var i = Count; i > index; i--)
        {
            _values[i] = _values[i - 1];
        }

        _values[index] = value;
        Count++;
    }

    public void Remove(T value)
    {
        // Remove value and maintain sorted order. In this case we don't need LowerBound because we remove anywhere within a range of duplicate values. 
        var index = UnsafeArrayHelper.BinarySearch(_values, 0, Count, value);
        if (index < 0)
        {
            throw new SlidingWindowRankerException($"Value {value} not found in partition.");
        }

        // Shift elements to fill the gap
        for (var i = index; i < Count - 1; i++)
        {
            _values[i] = _values[i + 1];
        }
        Count--;
    }

    public Partition<T> SplitAndInsert(T valueToInsert)
    {
        // Implement split logic
        return null; // Placeholder
    }

    public int GetLowerBoundWithinPartition(T value)
    {
        // Implement logic to find lower bound
        return 0; // Placeholder
    }

    public bool Contains(T value)
    {
        var index = UnsafeArrayHelper.BinarySearch(_values, 0, Count, value);
        return index >= 0;
    }

    ~UnsafePartition()
    {
        if (_values != null)
        {
            Marshal.FreeHGlobal((IntPtr)_values);
        }
    }
}