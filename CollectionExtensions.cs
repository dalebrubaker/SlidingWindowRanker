using System;
using System.Collections.Generic;
using System.IO.Compression;
using NLog;

namespace BruSoftware.SharedServices.ExtensionMethods;

public static class CollectionExtensions
{
    private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Check that the Lengths match and that each value in each collection matches, in the same order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array1"></param>
    /// <param name="array2"></param>
    /// <returns></returns>
    public static bool SequenceEqualFast<T>(this T[] array1, T[] array2)
    {
        if (array1 == null && array2 == null)
        {
            return true;
        }
        if (array1 == null || array2 == null)
        {
            return false;
        }
        if (array1.Length != array2.Length)
        {
            return false;
        }
        for (var i = 0; i < array1.Length; i++)
        {
            if (!array1[i].Equals(array2[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Check that the Counts match and that each value in each collection matches, in the same order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection1"></param>
    /// <param name="collection2"></param>
    /// <returns></returns>
    public static bool SequenceEqualFast<T>(this IList<T> collection1, IList<T> collection2)
    {
        if (collection1.Count != collection2.Count)
        {
            return false;
        }
        for (var i = 0; i < collection1.Count; i++)
        {
            var item1 = collection1[i];
            var item2 = collection2[i];
            if (!item1.Equals(item2))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Compress bytes using zlib and return the zipped bytes
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="compressionLevel"></param>
    /// <returns></returns>
    public static byte[] ToZippedBytes(this byte[] bytes, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        return ZipCodec.Compress(bytes, compressionLevel);
    }

    /// <summary>
    /// Decompress bytes using zlib and return the unzipped bytes
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static byte[] FromZippedBytes(this byte[] bytes)
    {
        return ZipCodec.Decompress(bytes);
    }

    /// <summary>
    /// Thanks to http://stackoverflow.com/questions/943635/getting-a-sub-array-from-an-existing-array
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="index"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        var result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }

    /// <summary>
    /// Add 1 to the length of the existing array, or start a new one
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    public static T[] IncrementLength<T>(this T[] array)
    {
        if (array == null || array.Length == 0)
        {
            return new T[1];
        }
        var result = new T[array.Length + 1];
        Array.Copy(array, result, array.Length);
        return result;
    }

    /// <summary>
    /// Remove the element at index from the array and return a new shorter array without the element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static T[] RemoveElement<T>(this T[] array, int index)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        if (index < 0 || index >= array.Length)
        {
            throw new ArgumentException($"index {index} doesn't fit in array.Length={array.Length}");
        }
        var result = new T[array.Length - 1];
        if (index > 0)
        {
            Array.Copy(array, result, index);
        }
        var countAfterIndex = array.Length - index - 1;
        if (countAfterIndex > 0)
        {
            Array.Copy(array, index + 1, result, index, countAfterIndex);
        }
        return result;
    }

    /// <summary>
    /// Extend the length of the existing array to accommodate index if necessary, or start a new one. Set item into the result at index.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="index"></param>
    /// <param name="item">the item to add at index</param>
    public static T[] SetWithExtendIfNeeded<T>(this T[] array, int index, T item)
    {
        var newLength = index + 1;
        var result = new T[newLength];
        if (array != null && array.Length > 0)
        {
            Array.Copy(array, result, array.Length);
        }
        result[index] = item;
        return result;
    }

    /// <summary>
    /// Append an item to an existing array, or start a new one
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="item"></param>
    public static T[] AppendItem<T>(this T[] array, T item)
    {
        if (array == null || array.Length == 0)
        {
            return new[] { item };
        }
        var result = new T[array.Length + 1];
        Array.Copy(array, result, array.Length);
        result[^1] = item;
        return result;
    }

    /// <summary>
    /// Convert an array or list of double to an array of floats.
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static float[] ToFloatArray(this IList<double> values)
    {
        var floats = new float[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            floats[i] = (float)values[i];
        }
        return floats;
    }

    /// <summary>
    /// The .Net IEnumerable LastOrDefault necessarily runs entirely through the list to find the last one!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T LastOrDefault<T>(this IList<T> list)
    {
        if (list.Count == 0)
        {
            return default;
        }
        return list[^1];
    }

    /// <summary>
    /// Set all array elements to default(T). Ref https://stackoverflow.com/questions/1407715/how-to-quickly-zero-out-an-array/1407729
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    public static T[] Clear<T>(this T[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = default;
        }
        return array;
    }

    /// <summary>
    /// Return <c>true</c> if all values are default (0 or null)
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static bool IsAllDefault<T>(this IEnumerable<T> values) where T : IEquatable<T>
    {
        foreach (var value in values)
        {
            if (!value.Equals(default))
            {
                return false;
            }
        }
        return true;
    }

    public static long CountDefaults<T>(this IEnumerable<T> values) where T : IEquatable<T>
    {
        var result = 0L;
        foreach (var value in values)
        {
            if (value.Equals(default))
            {
                result++;
            }
        }
        return result;
    }

    /// <summary>
    /// Thanks to https://stackoverflow.com/questions/200574/linq-equivalent-of-foreach-for-ienumerablet
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumeration"></param>
    /// <param name="action"></param>
    public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
    {
        foreach (var item in enumeration)
        {
            action(item);
        }
    }

    public static bool IsSortedAscending<T>(this IList<T> list) where T : IComparable<T>
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

    public static bool IsSortedDescending<T>(this IList<T> list) where T : IComparable<T>
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
    /// Get the lower bound for value in the entire array
    /// See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>the index of first element in the array that does not satisfy element less than value, or Count if no such element is found</returns>
    public static int GetLowerBound<T>(this IList<T> array, T value) where T : IComparable<T>
    {
        return GetLowerBound(array, 0, array.Count, value);
    }

    /// <summary>
    /// Get the lower bound for value in the range from first to last.
    /// See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="first">This first index to search, must be 0 or higher</param>
    /// <param name="last">The index one higher than the highest index in the range (e.g. Count)</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    /// the index of first element in the range [first, last) that does not satisfy element less than value, or last (Count) if no such element is
    /// found
    /// </returns>
    public static int GetLowerBound<T>(this IList<T> array, int first, int last, T value) where T : IComparable<T>
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
    /// Get the upper bound for value in the entire array
    /// See https://en.cppreference.com/w/cpp/algorithm/upper_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <returns>the index of first element in the array such that value is less than element, or Count if no such element is found</returns>
    public static int GetUpperBound<T>(this IList<T> array, T value) where T : IComparable<T>
    {
        return GetUpperBound(array, 0, array.Count, value);
    }

    /// <summary>
    /// Get the upper bound for value in the range from first to last.
    /// See https://en.cppreference.com/w/cpp/algorithm/upper_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="first">This first index to search, must be 0 or higher</param>
    /// <param name="last">The index one higher than the highest index in the range (e.g. Count)</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>the index of first element in the range [first, last) such that value is less than element, or last (Count) if no such element is found</returns>
    public static int GetUpperBound<T>(this IList<T> array, int first, int last, T value) where T : IComparable<T>
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

    // Arrays below here
    /// <summary>
    /// Get the lower bound for value in the entire array
    /// See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>the index of first element in the array that does not satisfy element less than value, or Count if no such element is found</returns>
    public static int GetLowerBound<T>(this T[] array, T value) where T : IComparable<T>
    {
        return GetLowerBound(array, 0, array.Length, value);
    }

    /// <summary>
    /// Get the lower bound for value in the range from first to last.
    /// See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="first">This first index to search, must be 0 or higher</param>
    /// <param name="last">The index one higher than the highest index in the range (e.g. Count)</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    /// the index of first element in the range [first, last) that does not satisfy element less than value, or last (Count) if no such element is
    /// found
    /// </returns>
    public static int GetLowerBound<T>(this T[] array, int first, int last, T value) where T : IComparable<T>
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
    /// Get the upper bound for value in the entire array
    /// See https://en.cppreference.com/w/cpp/algorithm/upper_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <returns>the index of first element in the array such that value is less than element, or Count if no such element is found</returns>
    public static int GetUpperBound<T>(this T[] array, T value) where T : IComparable<T>
    {
        return GetUpperBound(array, 0, array.Length, value);
    }

    /// <summary>
    /// Get the upper bound for value in the range from first to last.
    /// See https://en.cppreference.com/w/cpp/algorithm/upper_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="first">This first index to search, must be 0 or higher</param>
    /// <param name="last">The index one higher than the highest index in the range (e.g. Count)</param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>the index of first element in the range [first, last) such that value is less than element, or last (Count) if no such element is found</returns>
    public static int GetUpperBound<T>(this T[] array, int first, int last, T value) where T : IComparable<T>
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
}