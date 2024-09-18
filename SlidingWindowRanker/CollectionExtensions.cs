﻿namespace SlidingWindowRanker;

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

    public static bool IsSortedDescending<T>(this List<T> list) where T : IComparable<T>
    {
        if (list.Count == 0)
        {
            return true;
        }
        var prevItem = list[0];
        for (var i = 1; i < list.Count; i++)
        {
            var item = list[i];
            if (item.CompareTo(prevItem) > 0)
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
        return list.LowerBound(0, list.Count, value);
    }

    /// <summary>
    ///     Get the lower bound for value in the range from first to last.
    ///     See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="first">This first index to search, must be 0 or higher</param>
    /// <param name="last">The index one higher than the highest index in the range (e.g. Count)</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     the index of first element in the range [first, last) that does not satisfy element less than value, or last
    ///     (Count) if no such element is
    ///     found
    /// </returns>
    public static int LowerBoundOriginal<T>(this List<T> array, int first, int last, T value) where T : IComparable<T>
    {
        if (first < 0)
        {
            throw new ArgumentException($"first={first:N0} must not be negative", nameof(first));
        }
        var count = array.Count;
        if (last > count)
        {
            throw new ArgumentException($"last={last:N0} must not be higher than {count:N0}", nameof(last));
        }
        count = last - first;
        while (count > 0)
        {
            var step = count / 2;
            var i = first + step;
            var arrayValue = array[i];
            var compareTo = arrayValue.CompareTo(value);
            if (compareTo < 0)
            {
                first = ++i;
                count -= step + 1;
            }
            else
            {
                count = step;
            }
        }

        return first;
    }

    /// <summary>
    ///     Get the upper bound for value in the entire list
    ///     See https://en.cppreference.com/w/cpp/algorithm/upper_bound
    /// </summary>
    /// <param name="list"></param>
    /// <param name="value"></param>
    /// <returns>
    ///     the index of first element in the list such that value is less than element, or Count if no such element is
    ///     found
    /// </returns>
    public static int UpperBound<T>(this List<T> list, T value) where T : IComparable<T>
    {
        return UpperBound(list, 0, list.Count, value);
    }

    // Arrays below here
    /// <summary>
    ///     Get the lower bound for value in the entire array
    ///     See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     the index of first element in the array that does not satisfy element less than value, or Count if no such
    ///     element is found
    /// </returns>
    public static int LowerBound<T>(this T[] array, T value) where T : IComparable<T>
    {
        return LowerBound(array, 0, array.Length, value);
    }

    /// <summary>
    ///     Get the lower bound for value in the range from first to last.
    ///     See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="first">This first index to search, must be 0 or higher</param>
    /// <param name="last">The index one higher than the highest index in the range (e.g. Count)</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     the index of first element in the range [first, last) that does not satisfy element less than value, or last
    ///     (Count) if no such element is
    ///     found
    /// </returns>
    public static int LowerBound<T>(this T[] array, int first, int last, T value) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        if (first < 0 || first > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(first), "First index is out of range.");
        }
        if (last < first || last > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(last), "Last index is out of range.");
        }
        var low = first;
        var high = last;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (array[mid].CompareTo(value) < 0)
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
    ///     From o1 Mini. CoPilot did it wrong.
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
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }
        if (first < 0 || first > list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(first), "First index is out of range.");
        }
        if (last < first || last > list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(last), "Last index is out of range.");
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

    /// <summary>
    ///     Get the upper bound for value in the entire array
    ///     See https://en.cppreference.com/w/cpp/algorithm/upper_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <returns>
    ///     the index of first element in the array such that value is less than element, or Count if no such element is
    ///     found
    /// </returns>
    public static int UpperBound<T>(this T[] array, T value) where T : IComparable<T>
    {
        return UpperBound(array, 0, array.Length, value);
    }

    /// <summary>
    ///     Get the upper bound for value in the range from first to last.
    ///     See https://en.cppreference.com/w/cpp/algorithm/upper_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="first">This first index to search, must be 0 or higher</param>
    /// <param name="last">The index one higher than the highest index in the range (e.g. Count)</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     the index of first element in the range [first, last) such that value is less than element, or last (Count) if
    ///     no such element is found
    /// </returns>
    public static int UpperBoundOriginal<T>(this T[] array, int first, int last, T value) where T : IComparable<T>
    {
        if (first < 0)
        {
            throw new ArgumentException($"first={first:N0} must not be negative", nameof(first));
        }
        var count = array.Length;
        if (last > count)
        {
            throw new ArgumentException($"last={last:N0} must not be higher than {count:N0}", nameof(last));
        }
        count = last - first;
        while (count > 0)
        {
            var step = count / 2;
            var i = first + step;
            var arrayValue = array[i];
            var compareTo = value.CompareTo(arrayValue);
            if (compareTo >= 0)
            {
                first = ++i;
                count -= step + 1;
            }
            else
            {
                count = step;
            }
        }

        return first;
    }

    /// <summary>
    ///     From o1 Mini
    ///     Finds the first index in the sorted array where the element is not less than the specified value.
    /// </summary>
    /// <typeparam name="T">Type of elements in the array. Must implement IComparable&lt;T&gt;.</typeparam>
    /// <param name="array">The sorted array to search.</param>
    /// <param name="first">The starting index of the search range (inclusive).</param>
    /// <param name="last">The ending index of the search range (exclusive).</param>
    /// <param name="value">The value to compare.</param>
    /// <param name="comparer"></param>
    /// <returns>
    ///     The index of the first element that is not less than the specified value.
    ///     If all elements are less, returns <paramref name="last" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the array is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if first or last are out of range,
    ///     or if first is greater than last.
    /// </exception>
    public static int UpperBound<T>(this T[] array, int first, int last, T value) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        if (first < 0 || first > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(first), "First index is out of range.");
        }
        if (last < first || last > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(last), "Last index is out of range.");
        }
        var low = first;
        var high = last;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (array[mid].CompareTo(value) <= 0)
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
    ///     Finds the first index in the sorted list where the element is greater than the specified value.
    ///     From o1 Mini
    /// </summary>
    /// <typeparam name="T">Type of elements in the list. Must implement IComparable&lt;T&gt;.</typeparam>
    /// <param name="list">The sorted list to search.</param>
    /// <param name="first">The starting index of the search range (inclusive).</param>
    /// <param name="last">The ending index of the search range (exclusive).</param>
    /// <param name="value">The value to compare.</param>
    /// <returns>
    ///     The index of the first element that is greater than the specified value.
    ///     If no such element exists, returns <paramref name="last" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the list is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if first or last are out of range,
    ///     or if first is greater than last.
    /// </exception>
    public static int UpperBound<T>(this List<T> list, int first, int last, T value) where T : IComparable<T>
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }
        if (first < 0 || first > list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(first), "First index is out of range.");
        }
        if (last < first || last > list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(last), "Last index is out of range.");
        }
        var low = first;
        var high = last;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (list[mid].CompareTo(value) <= 0)
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
    ///     Get the upper bound for value in the range from first to last.
    ///     See https://en.cppreference.com/w/cpp/algorithm/upper_bound
    /// </summary>
    /// <param name="list"></param>
    /// <param name="first">This first index to search, must be 0 or higher</param>
    /// <param name="last">The index one higher than the highest index in the range (e.g. Count)</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     the index of first element in the range [first, last) such that value is less than element, or last (Count) if
    ///     no such element is found
    /// </returns>
    public static int UpperBoundOriginal<T>(this List<T> list, int first, int last, T value) where T : IComparable<T>
    {
        if (first < 0)
        {
            throw new ArgumentException($"first={first:N0} must not be negative", nameof(first));
        }
        var count = list.Count;
        if (last > count)
        {
            throw new ArgumentException($"last={last:N0} must not be higher than {count:N0}", nameof(last));
        }
        count = last - first;
        while (count > 0)
        {
            var step = count / 2;
            var i = first + step;
            var arrayValue = list[i];
            var compareTo = value.CompareTo(arrayValue);
            if (compareTo >= 0)
            {
                first = ++i;
                count -= step + 1;
            }
            else
            {
                count = step;
            }
        }

        return first;
    }
}