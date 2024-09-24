namespace SlidingWindowRanker;

internal static unsafe class UnsafeArrayHelper
{
    public static int BinarySearch<T>(T* array, int index, int length, T value) where T : unmanaged, IComparable<T>
    {
        var low = array + index;
        var high = array + index + length - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var cmp = mid->CompareTo(value);

            if (cmp == 0)
            {
                return (int)(mid - array);
            }
            if (cmp < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        return ~(int)(low - array); // If not found, return bitwise complement of the insertion point
    }

    /// <summary>
    ///     Get the lower bound for value in the entire array
    ///     See https://en.cppreference.com/w/cpp/algorithm/lower_bound
    /// </summary>
    /// <param name="array"></param>
    /// <param name="length"></param>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>
    ///     the index of first element in the array that does not satisfy element less than value, or length if no such element is found
    /// </returns>
    public static int LowerBound<T>(T* array, int index, int length, T value) where T : unmanaged, IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(array);
        var low = array + index;
        var high = array + index + length - 1;
        while (low < high)
        {
            var mid = low + ((high - low) >> 1);
            if (mid->CompareTo(value) < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }
        return (int)(low - array);
    }
}