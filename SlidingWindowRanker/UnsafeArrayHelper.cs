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
}