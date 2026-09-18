using System.Collections;
using System.Runtime.CompilerServices;

namespace SlidingWindowRanker;

/// <summary>
/// Minimal ring deque used to retain observations in chronological order.
/// Unlike <see cref="Queue{T}"/> it can also remove the NEWEST observation in O(1),
/// which is what <see cref="SlidingWindowRanker{T}.RemoveLast"/> needs.
///
/// Index arithmetic deliberately avoids the modulo operator and mirrors the branch-based
/// wrap-around used by <see cref="Queue{T}"/>, so the steady-state Add/Remove cost of the
/// backtest path is unchanged.
/// </summary>
internal sealed class ValueDeque<T> : IEnumerable<T>
{
    /// <summary>
    /// The deque never needs to hold more than the window size, plus the single transient
    /// value that exists between the insert and the eviction of an <see cref="SlidingWindowRanker{T}"/> add.
    /// Growth is capped there so a full window allocates its buffer once instead of doubling past it.
    /// </summary>
    private readonly int _maxCapacity;

    private T[] _buffer;
    private int _head;

    /// <param name="values">The initial values, ordered oldest to newest.</param>
    /// <param name="maxCapacity">The largest number of values that will ever be retained,
    /// or 0 when there is no known bound.</param>
    internal ValueDeque(IEnumerable<T> values, int maxCapacity = 0)
    {
        _maxCapacity = maxCapacity;
        var initialCapacity = values is ICollection<T> collection ? collection.Count : 0;
        _buffer = new T[Math.Max(4, initialCapacity)];
        foreach (var value in values)
        {
            AddLast(value);
        }
    }

    internal int Count { get; private set; }

    /// <summary>
    /// The newest value. Only valid when <see cref="Count"/> is greater than zero.
    /// </summary>
    internal T Last => _buffer[PhysicalIndex(Count - 1)];

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return _buffer[PhysicalIndex(i)];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddLast(T value)
    {
        if (Count == _buffer.Length)
        {
            Grow();
        }
        _buffer[PhysicalIndex(Count)] = value;
        Count++;
    }

    /// <summary>
    /// Adds a value at the OLDEST end, which is how a value evicted by the previous add is restored.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddFirst(T value)
    {
        if (Count == _buffer.Length)
        {
            Grow();
        }
        _head--;
        if (_head < 0)
        {
            _head = _buffer.Length - 1;
        }
        _buffer[_head] = value;
        Count++;
    }

    /// <summary>
    /// Removes and returns the OLDEST value. The caller must ensure the deque is not empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T RemoveFirst()
    {
        var value = _buffer[_head];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _buffer[_head] = default;
        }
        _head++;
        if (_head == _buffer.Length)
        {
            _head = 0;
        }
        Count--;
        if (Count == 0)
        {
            _head = 0;
        }
        return value;
    }

    /// <summary>
    /// Removes and returns the NEWEST value. The caller must ensure the deque is not empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T RemoveLast()
    {
        var index = PhysicalIndex(Count - 1);
        var value = _buffer[index];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _buffer[index] = default;
        }
        Count--;
        if (Count == 0)
        {
            _head = 0;
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int PhysicalIndex(int logicalIndex)
    {
        var index = _head + logicalIndex;
        if (index >= _buffer.Length)
        {
            index -= _buffer.Length;
        }
        return index;
    }

    private void Grow()
    {
        var requiredCapacity = Count + 1;
        var newCapacity = Math.Max(4L, (long)_buffer.Length * 2);
        if (_maxCapacity > 0 && newCapacity > _maxCapacity)
        {
            // Never allocate beyond what the window can hold
            newCapacity = _maxCapacity;
        }
        if (newCapacity > Array.MaxLength)
        {
            newCapacity = Array.MaxLength;
        }
        if (newCapacity < requiredCapacity)
        {
            newCapacity = requiredCapacity;
        }
        var newBuffer = new T[newCapacity];
        for (var i = 0; i < Count; i++)
        {
            newBuffer[i] = _buffer[PhysicalIndex(i)];
        }
        _buffer = newBuffer;
        _head = 0;
    }
}
