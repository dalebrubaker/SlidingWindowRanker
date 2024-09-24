namespace SlidingWindowRanker;

public interface IPartition<T> : IDisposable where T : IComparable<T>
{
    /// <summary>
    ///     This is the lower bound for the entire window of the lowest value in the partition.
    ///     Every Add operation will reset this value for every partition that is affected by adding the new value
    ///     and by removing the oldest queued value.
    /// </summary>
    int LowerBound { get; set; }

    /// <summary>
    /// Return the lowest value in the partition or null if the partition is empty.
    /// </summary>
    T LowestValue { get; }

    /// <summary>
    /// Return the highest value in the partition or null if the partition is empty.
    /// </summary>
    T HighestValue { get; }

    /// <summary>
    /// The number of values currently in the partition. Starts out at half the capacity.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// The partition need to be split if it has reached its capacity and the next Insert would cause it to grow.
    /// </summary>
    bool IsFull { get; }

    List<T> Values { get; }

    int CompareTo(IPartition<T> other);

    void Insert(T value);

    void Remove(T value);

    /// <summary>
    /// Split this partition at the index.
    /// If <see cref="valueToInsert"/> is greater than or equal to the highest value in this partition, we add it to the right partition,
    /// because we can't allow it to be empty. Otherwise, we add it to this partition.
    /// </summary>
    /// <param name="valueToInsert"></param>
    /// <returns>the Partition to insert AFTER this partition.</returns>
    IPartition<T> SplitAndInsert(T valueToInsert);

    int GetLowerBoundWithinPartition(T value);

    bool Contains(T value);
}