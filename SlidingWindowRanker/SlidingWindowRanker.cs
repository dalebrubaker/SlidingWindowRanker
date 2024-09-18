namespace SlidingWindowRanker;

public class SlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly int _windowSize;
    private readonly int _maxPartitionSize;
    private readonly List<Partition<T>> _partitions;
    private readonly Queue<(T value, Partition<T> partition)> _valueQueue;

    public SlidingWindowRanker(int windowSize, int maxPartitionSize)
    {
        _windowSize = windowSize;
        _maxPartitionSize = maxPartitionSize;
        _partitions = new List<Partition<T>>();
        _valueQueue = new Queue<(T value, Partition<T> partition)>(windowSize);
    }

    public void Add(T value)
    {
        // Step 1: Find the target partition
        int partitionIndex = FindPartitionIndex(value);
        Partition<T> partition;
        if (partitionIndex == -1)
        {
            // Create a new partition
            partition = new Partition<T>(new List<T> { value });
            _partitions.Insert(0, partition);
        }
        else
        {
            partition = _partitions[partitionIndex];
            partition.Insert(value);
            // Step 3: Split partition if necessary
            if (partition.NeedsSplitting)
            {
                int splitIndex = partition.Values.LowerBound(0, partition.Values.Count, value);
                var rightPartition = partition.Split(splitIndex);
                _partitions.RemoveAt(partitionIndex);
                _partitions.Insert(partitionIndex, partition);
                _partitions.Insert(partitionIndex + 1, rightPartition);
                // Update partition reference if needed
                partition = value.CompareTo(rightPartition.LowestValue) < 0 ? partition : rightPartition;
            }
        }
        // Step 2: Record in the queue
        _valueQueue.Enqueue((value, partition));
        // Step 4: Maintain window size
        if (_valueQueue.Count > _windowSize)
        {
            var (oldValue, oldPartition) = _valueQueue.Dequeue();
            oldPartition.Remove(oldValue);
            // Remove empty partitions
            if (oldPartition.IsEmpty)
            {
                _partitions.Remove(oldPartition);
            }
        }
    }

    private int FindPartitionIndex(T value)
    {
        int low = 0;
        int high = _partitions.Count - 1;
        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            var partition = _partitions[mid];
            if (value.CompareTo(partition.LowestValue) < 0)
            {
                high = mid - 1;
            }
            else if (value.CompareTo(partition.HighestValue) > 0)
            {
                low = mid + 1;
            }
            else
            {
                return mid;
            }
        }
        return high; // Return the index where a new partition should be inserted
    }

    // Implement GetPercentile, GetLowerBoundFraction, etc.
}