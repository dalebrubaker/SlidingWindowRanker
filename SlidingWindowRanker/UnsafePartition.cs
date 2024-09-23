using System.Runtime.InteropServices;

namespace SlidingWindowRanker;

internal unsafe class UnsafePartition<T> : IPartition<T> where T : unmanaged, IComparable<T>
{
    private readonly int _capacity;
    private readonly int _capacityLeft;
    private readonly int _capacityRight;
    private readonly int _partitionSize;
    private GCHandle _bufferHandle;
    private T* _bufferPtr;
    private bool _disposed;
    private int _left;
    private int _right;

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
        _bufferPtr = (T*)Marshal.AllocHGlobal(_capacity * sizeof(T));
        if (_bufferPtr == null)
        {
            throw new OutOfMemoryException("Failed to allocate memory for partition.");
        }
        var valuesHandle = GCHandle.Alloc(values.ToArray(), GCHandleType.Pinned);
        var valuesArrayPtr = (T*)valuesHandle.AddrOfPinnedObject();
        if (valuesArrayPtr == null)
        {
            throw new InvalidOperationException("Failed to get pointer to values.");
        }
        for (var i = 0; i < values.Count; i++)
        {
            var bufferIndex = _left + i;
            _bufferPtr[bufferIndex] = valuesArrayPtr[i];
        }
        valuesHandle.Free();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public int LowerBound { get; set; }

    public T LowestValue => _bufferPtr[_left];

    public T HighestValue => _bufferPtr[_right];

    public int Count => _right - _left + 1;

    public bool IsFull => _left == _capacityLeft || _right == _capacityRight;

    public int CompareTo(Partition<T> other)
    {
        return other == null ? 0 : LowerBound.CompareTo(other.LowerBound);
    }

    public void Insert(T value)
    {
        if (IsFull)
        {
            throw new InvalidOperationException("Partition is full. You must split the partition before inserting a new value.");
        }
        var index = UnsafeArrayHelper.BinarySearch(_bufferPtr, _left, Count, value);
        if (index < 0)
        {
            index = ~index; // Get the insertion point
        }
        index--; // insert before the binary search value

        var distanceToLeft = index - _capacityLeft;
        var distanceToRight = _capacityRight - index;
        // Shift existing values to the right or to the left, whichever is closer, then insert the new value
        if (distanceToLeft < distanceToRight)
        {
            for (var i = _left; i <= index; i++)
            {
                _bufferPtr[i - 1] = _bufferPtr[i];
            }
            _left--;
            _bufferPtr[index] = value;
        }
        else
        {
            for (var i = _right; i > index; i--)
            {
                _bufferPtr[i + 1] = _bufferPtr[i];
            }
            _right++;
            _bufferPtr[index + 1] = value;
        }
    }

    public void Remove(T value)
    {
        if (Count <= 1)
        {
            throw new SlidingWindowRankerException("Partition has only one value which cannot be removed. Remove the partition instead.");
        }

        var index = UnsafeArrayHelper.BinarySearch(_bufferPtr, 0, Count, value);
        if (index < 0)
        {
            throw new SlidingWindowRankerException($"Value {value} not found in partition.");
        }

        for (var i = index; i < Count - 1; i++)
        {
            _bufferPtr[i] = _bufferPtr[i + 1];
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
        var index = UnsafeArrayHelper.BinarySearch(_bufferPtr, 0, Count, value);
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
                result.Add(_bufferPtr[i]);
            }
            return result;
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (_bufferPtr != null)
            {
                Marshal.FreeHGlobal((IntPtr)_bufferPtr);
                _bufferPtr = null; // Avoid dangling pointer
            }
            if (_bufferHandle.IsAllocated)
            {
                _bufferHandle.Free();
            }
            _disposed = true;
        }
    }

    ~UnsafePartition()
    {
        Dispose(false);
    }
}