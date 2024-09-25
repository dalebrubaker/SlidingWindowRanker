// ReSharper disable ConvertToAutoProperty
// ReSharper disable InconsistentNaming
namespace SlidingWindowRanker;

internal partial class Partition<T> : IComparable<IPartition<T>> where T : IComparable<T>
{
    internal int Test_PartitionSize => _partitionSize;
    internal int Test_PartitionCapacity => Values.Capacity;

    internal int Test_LowerBound => LowerBound;
    internal List<T> Test_Values => Values;
}