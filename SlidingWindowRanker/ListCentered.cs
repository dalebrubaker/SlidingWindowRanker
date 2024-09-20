using System.Collections;

namespace SlidingWindowRanker;

/// <summary>
/// This class is based on List.cs from .Net Core source code.
/// Instead of maintaining data from 0 to Count at the left of Capacity, this class keeps data centered.
/// Fields for right and left are maintained.
/// Inserts move data to the right or to the left, whichever is closer to the insertion point.
/// Removes also move data from the right or left, whichever is closer to the removal point.
/// The point is to reduce copy time by as much as 50%.
/// </summary>
public class ListCentered<T> : IList<T>, IList, IReadOnlyList<T>
{
    private const int DefaultCapacity = 4;

#pragma warning disable CA1825 // avoid the extra generic instantiation for Array.Empty<T>()
    private static readonly T[] s_emptyArray = new T[0];
#pragma warning restore CA1825

    internal T[] _items; // Do not rename (binary serialization)
    internal int _size; // Do not rename (binary serialization)
    internal int _version; // Do not rename (binary serialization)

    // Constructs a List. The list is initially empty and has a capacity
    // of zero. Upon adding the first element to the list the capacity is
    // increased to DefaultCapacity, and then increased in multiples of two
    // as required.

    public ListCentered()
    {
        _items = s_emptyArray;
    }

    // Constructs a ListCentered with a given initial capacity. The list is
    // initially empty, but will have room for the given number of elements
    // before any reallocations are required.
    //
    public ListCentered(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _items = capacity == 0 ? s_emptyArray : new T[capacity];
    }

    // Constructs a List, copying the contents of the given collection. The
    // size and capacity of the new list will both be equal to the size of the
    // given collection.
    //
    public ListCentered(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (collection is ICollection<T> c)
        {
            var count = c.Count;
            if (count == 0)
            {
                _items = s_emptyArray;
            }
            else
            {
                _items = new T[count];
                c.CopyTo(_items, 0);
                _size = count;
            }
        }
        else
        {
            _items = s_emptyArray;
            using var en = collection!.GetEnumerator();
            while (en.MoveNext())
            {
                Add(en.Current);
            }
        }
    }

    // Gets and sets the capacity of this list.  The capacity is the size of
    // the internal array used to hold items.  When set, the internal
    // array of the list is reallocated to the given capacity.
    //
    public int Capacity
    {
        get => _items.Length;
        set
        {
            if (value < _size)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, _size);
            }
            if (value != _items.Length)
            {
                if (value > 0)
                {
                    var newItems = new T[value];
                    if (_size > 0)
                    {
                        Array.Copy(_items, newItems, _size);
                    }
                    _items = newItems;
                }
                else
                {
                    _items = s_emptyArray;
                }
            }
        }
    }

    public int Add(object value)
    {
        throw new NotSupportedException();
    }

    public bool Contains(object value)
    {
        throw new NotSupportedException();
    }

    public int IndexOf(object value)
    {
        throw new NotSupportedException();
    }

    public void Insert(int index, object value)
    {
        throw new NotSupportedException();
    }

    public void Remove(object value)
    {
        throw new NotSupportedException();
    }

    public void CopyTo(Array array, int index)
    {
        throw new NotSupportedException();
    }

    public int Count => _size;

    // Is this List synchronized (thread-safe)?
    bool ICollection.IsSynchronized => false;

    // Synchronization root for this object.
    object ICollection.SyncRoot => this;

    bool IList.IsFixedSize => false;

    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    public void Add(T item)
    {
        throw new NotSupportedException();
    }

    public void Clear()
    {
        throw new NotSupportedException();
    }

    public bool Contains(T item)
    {
        throw new NotSupportedException();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    public bool Remove(T item)
    {
        throw new NotSupportedException();
    }

    int ICollection<T>.Count => _size;

    public bool IsReadOnly => false;

    public int IndexOf(T item)
    {
        throw new NotSupportedException();
    }

    public void Insert(int index, T item)
    {
        throw new NotSupportedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    public T this[int index]
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    int IReadOnlyCollection<T>.Count => _size;
}