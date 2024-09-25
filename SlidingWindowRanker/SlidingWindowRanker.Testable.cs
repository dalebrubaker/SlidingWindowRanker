// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace SlidingWindowRanker;

public partial class SlidingWindowRankerBase<T> where T : IComparable<T>
{
    internal List<IPartition<T>> TestPartitions => SortedPartitions.TestPartitions;
    internal List<T> TestValues => TestPartitions.SelectMany(p => p.Values).ToList();

    internal void Test_DoInsert(T valueToInsert)
    {
        SortedPartitions.Test_DoInsert(valueToInsert);
    }

    internal void Test_DoRemove(T valueToRemove)
    {
        SortedPartitions.Test_DoRemove(valueToRemove);
    }

    /// <summary>
    /// Debug code to ensure the LowerBound of every partition is correct.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global -- Used in tests
    internal void DebugGuardPartitionLowerBoundValuesAreCorrect()
    {
#if DEBUG
        SortedPartitions.DebugGuardPartitionLowerBoundValuesAreCorrect();
#endif
    }
}