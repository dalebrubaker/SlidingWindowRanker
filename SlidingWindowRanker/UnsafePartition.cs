using System.Runtime.InteropServices;

namespace SlidingWindowRanker;

internal unsafe class UnsafePartition<T> : IPartition<T>, IDisposable where T : unmanaged, IComparable<T>
{
    private readonly int _capacity;
    private readonly int _left;
    private readonly int _partitionSize;
    private readonly int _right;
    private bool _disposed;
    private GCHandle _valuesHandle;
    private T* _valuesPtr;
    private readonly int _capacityLeft;
    private readonly int _capacityRight;

    public UnsafePartition(List<T> values, int partitionSize = -1)
    {
        _partitionSize = partitionSize < 0 ? values.Count : partitionSize;
        if (_partitionSize == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionSize), "The partition size must be greater than 0.");
        }
        _capacity = _partitionSize * 2; // Leave room to grow
        _capacityLeft = 0;
        _capacityRight = _capacity - 1;

        // Center the initial values in this partition
        var middle = _capacity / 2;
        _left = middle - _partitionSize / 2;
        _right = _left + _partitionSize - 1;

        // Pin the values array
        _valuesHandle = GCHandle.Alloc(values.ToArray(), GCHandleType.Pinned);
        _valuesPtr = (T*)Marshal.AllocHGlobal(_capacity * sizeof(T));
        if (_valuesPtr == null)
        {
            throw new OutOfMemoryException("Failed to allocate memory for partition.");
        }
        var valuesArrayPtr = (T*)_valuesHandle.AddrOfPinnedObject();
        if (valuesArrayPtr == null)
        {
            throw new InvalidOperationException("Failed to get pointer to values.");
        }
        for (var i = _left; i <= _right; i++)
        {
            _valuesPtr[i] = valuesArrayPtr[i];
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public int LowerBound { get; set; }

    public T LowestValue => _valuesPtr[_left];

    public T HighestValue => _valuesPtr[_right];

    public int Count => _right - _left + 1;

    public bool IsFull => Count >= _capacity;

    public int CompareTo(Partition<T> other)
    {
        return other == null ? 0 : LowerBound.CompareTo(other.LowerBound);
    }

    public void Insert(T value)
    {
        if (IsFull)
        {
            throw new InvalidOperationException("Partition is full.");
        }

        var index = UnsafeArrayHelper.BinarySearch(_valuesPtr, 0, Count, value);
        if (index < 0)
        {
            index = ~index; // Get the insertion point
        }

        for (var i = Count; i > index; i--)
        {
            _valuesPtr[i] = _valuesPtr[i - 1];
        }
        _valuesPtr[index] = value;
        // Increment Count
    }

    public void Remove(T value)
    {
        if (Count <= 1)
        {
            throw new SlidingWindowRankerException("Partition has only one value which cannot be removed. Remove the partition instead.");
        }

        var index = UnsafeArrayHelper.BinarySearch(_valuesPtr, 0, Count, value);
        if (index < 0)
        {
            throw new SlidingWindowRankerException($"Value {value} not found in partition.");
        }

        for (var i = index; i < Count - 1; i++)
        {
            _valuesPtr[i] = _valuesPtr[i + 1];
        }
        // Decrement Count
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
        var index = UnsafeArrayHelper.BinarySearch(_valuesPtr, 0, Count, value);
        return index >= 0;
    }

    public List<T> Values
    {
        get
        {
            if (Count == 0)
            {
                return new List<T>();
            }
            var result = new List<T>(Count);
            for (var i = _left; i <= _right; i++)
            {
                result.Add(_valuesPtr[i]);
            }
            return result;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (_valuesPtr != null)
            {
                Marshal.FreeHGlobal((IntPtr)_valuesPtr);
                _valuesPtr = null; // Avoid dangling pointer
            }
            if (_valuesHandle.IsAllocated)
            {
                _valuesHandle.Free();
            }
            _disposed = true;
        }
    }

    ~UnsafePartition()
    {
        Dispose(false);
    }
}