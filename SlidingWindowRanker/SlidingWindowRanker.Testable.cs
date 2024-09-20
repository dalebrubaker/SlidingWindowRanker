// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace SlidingWindowRanker;

public partial class SlidingWindowRanker<T> where T : IComparable<T>
{
    private readonly List<string> _removePartitionMessages = [];
    private readonly List<string> _splitPartitionMessages = [];

    internal List<Partition<T>> TestPartitions => _partitions;
    internal List<T> TestValues => GetValues();

    /// <summary>
    /// From this index up to the highest partition, increment <see cref="LowerBound"/>
    /// to account for <see cref="DoInsert"/> in a lower partition
    /// </summary>
    internal int Test_BeginIndexForLowerBoundInsertIncrements => _beginIndexForLowerBoundInsertIncrements;

    /// <summary>
    /// From this index up to the highest partition, decrement <see cref="LowerBound"/>
    /// to account for <see cref="DoRemove"/> in a lower partition
    /// </summary>
    internal int Test_BeginIndexForLowerBoundRemoveDecrements => _beginIndexForLowerBoundRemoveDecrements;

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
        _valueToInsert = valueToInsert;
        (_partitionForInsert, _partitionForInsertIndex) = FindPartitionContaining(_valueToInsert);
        _indexWithinPartitionForInsert = _partitionForInsert.GetLowerBoundWithinPartition(_valueToInsert);
        DoInsert();
    }

    internal void Test_DoRemove(T valueToRemove)
    {
        _valueToRemove = valueToRemove;
        DoRemove();
    }

    internal void Test_AdjustPartitionsLowerBounds(bool didInsert, bool didRemove)
    {
        if (!didInsert)
        {
            _beginIndexForLowerBoundInsertIncrements = int.MaxValue;
            _partitionInsertedIndex = int.MaxValue;
        }
        if (!didRemove)
        {
            _beginIndexForLowerBoundRemoveDecrements = int.MaxValue;
            _partitionRemovedIndex = int.MaxValue;
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
                _ = _removePartitionMessages;
                _ = _splitPartitionMessages;
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
}