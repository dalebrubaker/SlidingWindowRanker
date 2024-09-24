using System.Runtime.InteropServices;

namespace SlidingWindowRanker;

internal unsafe partial class PartitionUnsafe<T> : IPartition<T> where T : unmanaged, IComparable<T>
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

    public PartitionUnsafe(List<T> values, int partitionSize = -1)
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
        var count = values.Count;
        _left = middle - count / 2;
        _right = _left + count - 1;

        _bufferPtr = (T*)Marshal.AllocHGlobal(_capacity * sizeof(T));
        if (_bufferPtr == null)
        {
            throw new OutOfMemoryException("Failed to allocate memory for partition.");
        }
        if (count > 0)
        {
            LoadInitialValuesIntoBuffer(values, count);
        }
    }

    private void LoadInitialValuesIntoBuffer(List<T> values, int count)
    {
        // Pin the values array
        var valuesHandle = GCHandle.Alloc(values.ToArray(), GCHandleType.Pinned);
        var valuesArrayPtr = (T*)valuesHandle.AddrOfPinnedObject();
        if (valuesArrayPtr == null)
        {
            throw new InvalidOperationException("Failed to get pointer to values.");
        }
        for (var i = 0; i < count; i++)
        {
            var bufferIndex = _left + i;
            *(_bufferPtr + bufferIndex) = *(valuesArrayPtr + i);
        }
        valuesHandle.Free();
    }

    public List<string> BufferValues
    {
        get
        {
            if (Count == 0)
            {
                return [];
            }
            var result = new List<string>(_capacity);
            for (var i = _capacityLeft; i <= _capacityRight; i++)
            {
                if (i < _left || i > _right)
                {
                    result.Add("N/A");
                    continue;
                }
                var value = *(_bufferPtr + i);
                result.Add(value.ToString());
            }
            return result;
        }
    }

    public void Dispose()
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
        GC.SuppressFinalize(this);
    }

    public int LowerBound { get; set; }

    public T LowestValue => *(_bufferPtr + _left);

    public T HighestValue => *(_bufferPtr + _right);

    public int Count => _right - _left + 1;

    public bool IsFull => _left == _capacityLeft || _right == _capacityRight;

    public int CompareTo(IPartition<T> other)
    {
        return other == null ? 0 : LowerBound.CompareTo(other.LowerBound);
    }

    public void Insert(T value)
    {
#if DEBUG
        if (IsFull)
        {
            throw new InvalidOperationException("Partition is full. You must split the partition before inserting a new value.");
        }
#endif
        var indexIntoBuffer = UnsafeArrayHelper.BinarySearch(_bufferPtr, _left, Count, value);
        if (indexIntoBuffer < 0)
        {
            indexIntoBuffer = ~indexIntoBuffer; // Get the insertion point
        }

        // Shift existing values to the right or to the left, whichever is fewer, then insert the new value
        var distanceToLeft = indexIntoBuffer - _left;
        var distanceToRight = _right - indexIntoBuffer;
        bool moveRight; 
        if (distanceToRight == distanceToLeft)
        {
            // Shift where we have more room in the buffer
            var roomToLeft = _left - _capacityLeft;
            var roomToRight = _capacityRight - _right;
            moveRight = roomToRight > roomToLeft;
        }
        else
        {
            moveRight = distanceToRight < distanceToLeft;
        }
        if (moveRight)
        {
            ShiftValuesToRightFromInsertionPoint(indexIntoBuffer);
            *(_bufferPtr + indexIntoBuffer) = value;
        }
        else
        {
            ShiftValuesToLeftFromInsertionPoint(indexIntoBuffer);
            *(_bufferPtr + indexIntoBuffer - 1) = value;
        }
    }

    private void ShiftValuesToRightFromInsertionPoint(int indexIntoBuffer)
    {
        for (var i = _right; i >= indexIntoBuffer; i--)
        {
            *(_bufferPtr + i + 1) = *(_bufferPtr + i);
        }
        _right++;
    }

    private void ShiftValuesToLeftFromInsertionPoint(int indexIntoBuffer)
    {
        for (var i = _left; i < indexIntoBuffer; i++)
        {
            *(_bufferPtr + i - 1) = *(_bufferPtr + i);
        }
        _left--;
    }

    public void Remove(T value)
    {
        if (Count <= 1)
        {
            throw new SlidingWindowRankerException("Partition has only one value which cannot be removed. Remove the partition instead.");
        }

        var indexIntoBuffer = UnsafeArrayHelper.BinarySearch(_bufferPtr, _left, Count, value);
        if (indexIntoBuffer < 0)
        {
            throw new SlidingWindowRankerException($"Value {value} not found in partition.");
        }

        var distanceToLeft = indexIntoBuffer - _capacityLeft;
        var distanceToRight = _capacityRight - indexIntoBuffer;
        // Remove the value and shift the remaining values to the left or to the right, whichever is closer
        if (distanceToLeft > distanceToRight)
        {
            // Shift to the right
            for (var i = indexIntoBuffer; i < _right; i++)
            {
                *(_bufferPtr + i) = *(_bufferPtr + i + 1);
            }
            _right--;
        }
        else
        {
            // Shift to the left
            for (var i = indexIntoBuffer; i > _left; i--)
            {
                *(_bufferPtr + i) = *(_bufferPtr + i - 1);
            }
            _left++;
        }
    }

    public IPartition<T> SplitAndInsert(T valueToInsert)
    {
        var indexIntoBuffer = UnsafeArrayHelper.BinarySearch(_bufferPtr, _left, Count, valueToInsert);
        if (indexIntoBuffer < 0)
        {
            indexIntoBuffer = ~indexIntoBuffer; // Get the insertion point
        }
        var isSplittingAtEnd = indexIntoBuffer >= _right;
        var countValuesToGet = _right - indexIntoBuffer + 1;
        var rightValues = GetRange(indexIntoBuffer, countValuesToGet);
        _right -= countValuesToGet; // Effectively is RemoveRange
        if (_left == _capacityLeft)
        {
            // This partition is still full because we are up against the left side. Center the values
            var middle = _capacity / 2 - 1;
            var count = Count;
            var newLeft = middle - count / 2;
            var countToShift = newLeft - _left;
            for (int i = _left; i <  _left + countToShift; i++)
            {
                *(_bufferPtr + i + countToShift) = *(_bufferPtr + i);
            }
            _left += countToShift;
            _right += countToShift;
        }

        // Leave room to grow. But note that for small partitions,
        // rightValues.Capacity may be a minimum of 4 here because of List.DefaultCapacity
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
        var index = UnsafeArrayHelper.LowerBound(_bufferPtr, _left, Count, value);
        index -= _left;
        return index;
    }

    public bool Contains(T value)
    {
        var index = UnsafeArrayHelper.BinarySearch(_bufferPtr, _left, Count, value);
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
                result.Add(*(_bufferPtr + i));
            }
            return result;
        }
    }

    private List<T> GetRange(int indexIntoBuffer, int count)
    {
        var result = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            result.Add(*(_bufferPtr + indexIntoBuffer + i));
        }
        return result;
    }

    ~PartitionUnsafe()
    {
        Dispose();
    }
}