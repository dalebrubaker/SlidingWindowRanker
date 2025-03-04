﻿using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SlidingWindowRanker.Tests")]

// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace SlidingWindowRanker;

/// <summary>
/// partial so we can test the private methods.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class SlidingWindowRanker<T> where T : IComparable<T>
{
    internal List<Partition<T>> TestPartitions => _partitions;
    internal List<T> TestValues => _partitions.SelectMany(p => p.Values).ToList();

    internal void Test_DoInsert(T valueToInsert)
    {
#if DEBUG
        _debugMessageRemove = null;
        _debugMessageInsert = null;
#endif
        var partitionIndexForInsert = FindPartitionContaining(valueToInsert);
        var partitionForInsert = _partitions[partitionIndexForInsert];
        if (partitionForInsert.IsFull)
        {
            SplitPartition(partitionForInsert, partitionIndexForInsert, valueToInsert);
        }
        else
        {
            DoInsert(valueToInsert, partitionForInsert);
        }
    }

    internal void Test_DoRemove(T valueToRemove)
    {
#if DEBUG
        _debugMessageRemove = null;
        _debugMessageInsert = null;
#endif
        var partitionIndexForRemove = FindPartitionContaining(valueToRemove);
        var partitionForRemove = _partitions[partitionIndexForRemove];
        if (partitionForRemove.Count == 1)
        {
            // The partition holding the value to remove will be empty after the remove
            RemovePartition(partitionIndexForRemove, partitionForRemove);
        }
        else
        {
            DoRemove(valueToRemove, partitionForRemove);
        }
    }

    /// <summary>
    /// Debug code to ensure the LowerBound of every partition is correct.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global -- Used in tests
    internal void DebugGuardPartitionLowerBoundValuesAreCorrect()
    {
#if DEBUG
        if (_partitions.Count == 0)
        {
            return;
        }
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

#if DEBUG
    private int _debugCounter;
    private string _debugMessageInsert;
    private string _debugMessageRemove;
#endif
}