using System.Collections;

namespace SlidingWindowRanker;

/// <summary>
/// This class is based on Link.cs from .Net Core source code.
/// Instead of maintaining data from 0 to Count at the left of Capacity, this class keeps data centered.
/// Fields for right and left are maintained.
/// Inserts move data to the right or to the left, whichever is closer to the insertion point.
/// Removes also move data from the right or left, whichever is closer to the removal point.
/// The point is to reduce copy time by as much as 50%.
/// </summary>
public class LinkCentered<T> : IList<T>, IList, IReadOnlyList<T>
{
    private const int DefaultCapacity = 4;

    internal T[] _items; // Do not rename (binary serialization)
    internal int _size; // Do not rename (binary serialization)
    internal int _version; // Do not rename (binary serialization)

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    public void Add(T item)
    {
        throw new NotSupportedException();
    }

    public int Add(object value)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotSupportedException();
    }

    public bool Contains(object value)
    {
        throw new NotImplementedException();
    }

    public int IndexOf(object value)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, object value)
    {
        throw new NotImplementedException();
    }

    public void Remove(object value)
    {
        throw new NotImplementedException();
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

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public int Count => _size;
    int ICollection<T>.Count => _size;
    
    int IReadOnlyCollection<T>.Count => _size;

    public bool IsSynchronized { get; }
    public object SyncRoot { get; }

    public bool IsReadOnly { get; }
    

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

    public bool IsFixedSize { get; }

    public T this[int index]
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value;
    }

}