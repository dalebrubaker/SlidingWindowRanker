// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace SlidingWindowRanker;

public partial class SlidingWindowRanker<T> where T : IComparable<T>
{
#if DEBUG
    private string _debugMessageInsert;
    private string _debugMessageRemove;
#endif

    internal List<Partition<T>> TestPartitions => _partitions;
    internal List<T> TestValues => GetValues();

    internal int TestPartitionIndexChangedByInsert => _partitionIndexChangedByInsert;

    internal int TestPartitionIndexChangedByRemove => _partitionIndexChangedByRemove;

    internal int TestPartitionIndexInserted => _partitionIndexInserted;

    /// <summary>
    /// For debugging, return the values in all partitions.
    /// </summary>
    /// <returns></returns>
    internal List<T> GetValues()
    {
        return _partitions.SelectMany(p => p.Values).ToList();
    }

    internal void Test_DoInsert(T valueToInsert)
    {
        InitializeState();
        _valueToInsert = valueToInsert;
        DoInsert();
    }

    internal void Test_DoRemove(T valueToRemove)
    {
        InitializeState();
        _valueToRemove = valueToRemove;
        DoRemove();
    }

    internal void Test_AdjustPartitionsLowerBounds(bool didInsert, bool didRemove)
    {
        _partitionForInsert = null;
        _partitionForRemove = null;
#if DEBUG
        _debugMessageRemove = null;
        _debugMessageInsert = null;
#endif

        if (!didInsert)
        {
            _partitionIndexChangedByInsert = -1; // Not set yet
            _partitionIndexInserted = -1; // Not set yet
        }
        if (!didRemove)
        {
            _partitionIndexChangedByRemove = -1; // Not set yet
        }
        AdjustPartitionsLowerBounds();
    }

    /// <summary>
    /// Debug code to ensure the LowerBound of every partition is correct.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global -- Used in tests
    internal void DebugGuardPartitionLowerBoundValuesAreCorrect()
    {
#if DEBUG
        if (_partitions[0].LowerBound != 0)
        {
            throw new SlidingWindowRankerException("The LowerBound of the first partition is not 0.");
        }

        for (var i = 1; i < _partitions.Count; i++)
        {
            var partition = _partitions[i];
            var priorPartition = _partitions[i - 1];
            if (partition.LowerBound != priorPartition.LowerBound + priorPartition.Count)
            {
                _ = CountPartitionRemoves;
                _ = CountPartitionSplits;
                _ = _debugMessageRemove;
                _ = _debugMessageInsert;
                throw new SlidingWindowRankerException($"The LowerBound of partition={i} is not correct.");
            }
        }
#endif
    }

    private void DebugGuardIsLowerBoundAscending()
    {
#if DEBUG
        var lowerBoundsList = _partitions.Select(p => p.LowerBound).ToList();
        var isSortedAscending = lowerBoundsList.IsSortedAscending();
        if (!isSortedAscending)
        {
            throw new SlidingWindowRankerException("The LowerBounds of the partitions are not sorted ascending.");
        }
#endif
    }

    private void DebugPartitionValuesAreSortedAscending()
    {
#if DEBUG
        for (var i = 0; i < _partitions.Count; i++)
        {
            var partition = _partitions[i];
            if (!partition.Values.IsSortedAscending())
            {
                throw new SlidingWindowRankerException("The values in the partition are not sorted ascending.");
            }
        }
#endif
    }
}