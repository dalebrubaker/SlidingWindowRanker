namespace SlidingWindowRanker;

/// <summary>
/// Partial class so we can do Unit Testing on private methods in th test project
/// </summary>
/// <typeparam name="T"></typeparam>
public class SlidingWindowRanker<T> : SlidingWindowRankerBase<T> where T : IComparable<T>
{
    /// <summary>
    /// Initializes a new instance of the SlidingWindowRanker class.
    /// </summary>
    /// <param name="initialValues">The initial values to populate the sliding window.</param>
    /// <param name="partitionCount">The number of partitions to divide the values into. If less than or equal to zero,
    ///     use the square root of the given or calculated window size, which is usually optimal or close to it.</param>
    /// <param name="windowSize">Default -1 means to use initialValues.Count. Must be no smaller than initialValues</param>
    /// <param name="isSorted">true means the initialValues have already been sorted, thus preventing an additional sort here</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public SlidingWindowRanker(List<T> initialValues, int partitionCount = -1, int windowSize = -1, bool isSorted = false)
    {
        if (windowSize < 0)
        {
            // Use wants to default to the size of the initial values
            windowSize = initialValues.Count;
        }
        _windowSize = windowSize;
        if (_windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize),
                "The window size must be greater than 0, in order to have values to rank against.");
        }
        if (partitionCount <= 0)
        {
            partitionCount = (int)Math.Sqrt(_windowSize);
        }
        if (partitionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionCount),
                "The partition count must be at least 1, in order to have values to rank against.");
        }
        _valueQueue = new Queue<T>(initialValues);
        _isQueueFull = _valueQueue.Count >= _windowSize;
        List<T> values;
        if (!isSorted)
        {
            // Sort the initial values so we can divide them into partitions
            // But be friendly to the caller, so sort a new list and leave the given list unchanged
            values = [..initialValues];
            values.Sort();
        }
        else
        {
            values = initialValues;
        }
        int partitionSize;
        if (_windowSize % 2 == 0)
        {
            // An even number of values in the window
            partitionSize = _windowSize / partitionCount;
        }
        else
        {
            // Add 1 to _windowSize so we can round up on the integer division. E.g. 5 values and 3 partitions
            // should have values per partition of [2, 2, 1] not [1, 1, 1]
            partitionSize = (_windowSize + 1) / partitionCount;
        }
        for (var i = 0; i < partitionCount; i++)
        {
            var startIndex = i * partitionSize;

            // Last partition gets the remaining values
            var getRangeCount = i == partitionCount - 1 ? values.Count - startIndex : partitionSize;
            var partitionValues = values.GetRange(startIndex, getRangeCount);
            var partition = new Partition<T>(partitionValues, partitionSize)
            {
                LowerBound = startIndex
            };
            _partitions.Add(partition);
        }
    }
}