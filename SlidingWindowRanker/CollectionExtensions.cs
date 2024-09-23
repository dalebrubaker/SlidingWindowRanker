namespace SlidingWindowRanker;

public static class CollectionExtensions
{
    public static bool IsSortedAscending<T>(this List<T> list) where T : IComparable<T>
    {
        if (list.Count == 0)
        {
            return true;
        }
        var prevItem = list[0];
        for (var i = 1; i < list.Count; i++)
        {
            var item = list[i];
            if (item.CompareTo(prevItem) < 0)
            {
                return false;
            }
            prevItem = item;
        }
        return true;
    }

    /// <summary>
    ///     Get the lower bound for value in the entire list
    ///     See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="list"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     the index of first element in the list that does not satisfy element less than value, or Count if no such
    ///     element is found
    /// </returns>
    public static int LowerBound<T>(this List<T> list, T value) where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(list);
        var low = 0;
        var high = list.Count;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (list[mid].CompareTo(value) < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }
        return low;
    }

    /// <summary>
    ///     Finds the first index in the sorted list where the element is not less than the specified value.
    /// </summary>
    /// <typeparam name="T">Type of elements in the list. Must implement IComparable&lt;T&gt;.</typeparam>
    /// <param name="list">The sorted list to search.</param>
    /// <param name="first">The starting index of the search range (inclusive).</param>
    /// <param name="last">The ending index of the search range (exclusive).</param>
    /// <param name="value">The value to compare.</param>
    /// <returns>
    ///     The index of the first element that is not less than the specified value.
    ///     If all elements are less, returns <paramref name="last" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the list is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if first or last are out of range,
    ///     or if first is greater than last.
    /// </exception>
    public static int LowerBound<T>(this List<T> list, int first, int last, T value) where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(list);
        if (first < 0 || first > list.Count || last < first || last > list.Count)
        {
            // ReSharper disable once NotResolvedInText
            throw new ArgumentOutOfRangeException("Index out of range.");
        }
        var low = first;
        var high = last;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (list[mid].CompareTo(value) < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }
        return low;
    }
}