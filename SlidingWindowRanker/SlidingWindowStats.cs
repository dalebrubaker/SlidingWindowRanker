using System.Numerics;

namespace SlidingWindowRanker;

/// <summary>
/// Extends SlidingWindowRanker with percentile and IQR-based robust z-score methods.
/// T must be numeric (INumber&lt;T&gt;) to support median/IQR arithmetic.
///
/// All stat methods read from the same partitioned sorted structure that SlidingWindowRanker
/// maintains, so the sliding window slides correctly across millions of bars at O(√N) per bar.
/// </summary>
public class SlidingWindowStats<T> : SlidingWindowRanker<T>
    where T : IComparable<T>, INumber<T>
{
    /// <summary>
    /// Scale factor that converts IQR into a consistent estimator of the standard deviation σ,
    /// equivalent to multiplying MAD by 1.4826.
    ///
    /// Derivation (for a standard normal distribution N(0, σ²)):
    ///   • Q75 = +0.6745σ,  Q25 = −0.6745σ  →  IQR = 1.3490σ
    ///   • MAD = median(|xᵢ − median|) = 0.6745σ
    ///   • IQR = 2 × MAD  (exactly, by symmetry)
    ///   • Therefore: σ = IQR / 1.3490 = 0.7413 × IQR
    ///
    /// The MAD-based robust z-score uses denominator = 1.4826 × MAD.
    /// This IQR-based form uses          denominator = 0.7413 × IQR.
    /// Both yield the same σ estimate for normal data: 0.7413 × IQR = 1.4826 × MAD.
    ///
    /// Numerically: 0.7413 = 1.4826 / 2 = 1 / (2 × Φ⁻¹(0.75))
    /// where Φ⁻¹(0.75) ≈ 0.6745 is the 75th percentile of N(0,1).
    /// </summary>
    public const double IQRScale = 0.7413;

    public SlidingWindowStats(int windowSize, List<T> initialValues = null, bool isSorted = false)
        : base(windowSize, initialValues, isSorted)
    {
    }

    public SlidingWindowStats(List<T> initialValues, int partitionCount = -1, int windowSize = -1, bool isSorted = false)
        : base(initialValues, partitionCount, windowSize, isSorted)
    {
    }

    /// <summary>
    /// Returns the value at the given percentile rank from the current window without interpolation.
    /// The selected zero-based index is floor(p × Count), clamped to [0, Count - 1].
    /// Consequently, p below zero returns the minimum and p greater than or equal to one returns the maximum.
    /// O(log √N) — binary search over partitions, then direct index into partition.Values.
    /// </summary>
    public T GetValueAtRank(double p)
    {
        if (_rankDenominator < 1)
        {
            return default;
        }
        var totalCount = (int)_rankDenominator;
        var targetIndex = (int)(p * _rankDenominator);
        targetIndex = Math.Clamp(targetIndex, 0, totalCount - 1);
        var low = 0;
        var high = _partitions.Count - 1;
        while (low < high)
        {
            var mid = (low + high) / 2;
            var partition = _partitions[mid];
            if (partition.LowerBound + partition.Count <= targetIndex)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }
        var foundPartition = _partitions[low];
        var localIndex = targetIndex - foundPartition.LowerBound;
        localIndex = Math.Clamp(localIndex, 0, foundPartition.Count - 1);
        return foundPartition.Values[localIndex];
    }

    /// <summary>
    /// Returns the median of the current window. For even-sized windows this is the
    /// average of the two middle values, matching the standard statistical definition.
    /// O(log √N).
    /// </summary>
    public T GetMedian()
    {
        var totalCount = (int)_rankDenominator;
        if (totalCount == 0)
        {
            return default;
        }
        if (totalCount % 2 == 1)
        {
            return GetValueAtSortedIndex(totalCount / 2);
        }
        var lower = GetValueAtSortedIndex(totalCount / 2 - 1);
        var upper = GetValueAtSortedIndex(totalCount / 2);
        return (lower + upper) / (T.One + T.One);
    }

    /// <summary>
    /// Returns the value at the given zero-based sorted index. O(log √N).
    /// </summary>
    private T GetValueAtSortedIndex(int sortedIndex)
    {
        var totalCount = (int)_rankDenominator;
        sortedIndex = Math.Clamp(sortedIndex, 0, totalCount - 1);
        var low = 0;
        var high = _partitions.Count - 1;
        while (low < high)
        {
            var mid = (low + high) / 2;
            var partition = _partitions[mid];
            if (partition.LowerBound + partition.Count <= sortedIndex)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }
        var foundPartition = _partitions[low];
        var localIndex = sortedIndex - foundPartition.LowerBound;
        localIndex = Math.Clamp(localIndex, 0, foundPartition.Count - 1);
        return foundPartition.Values[localIndex];
    }

    /// <summary>Returns the 25th percentile of the current window. O(log √N).</summary>
    public T GetQ25()
    {
        return GetValueAtRank(0.25);
    }

    /// <summary>Returns the 75th percentile of the current window. O(log √N).</summary>
    public T GetQ75()
    {
        return GetValueAtRank(0.75);
    }

    /// <summary>Returns the interquartile range (Q75 − Q25) as a double. O(log √N).</summary>
    public double GetIQR()
    {
        var q25 = GetQ25();
        var q75 = GetQ75();
        return double.CreateChecked(q75) - double.CreateChecked(q25);
    }

    /// <summary>
    /// Computes the IQR-based robust z-score of <paramref name="value"/> against the CURRENT window
    /// (prior values), then adds the value to the window (removing oldest). Total cost O(√N).
    /// z = (value − median) / (IQRScale × IQR).
    /// Returns 0 when the window has fewer than 2 values or when IQR is zero.
    /// </summary>
    public double GetZScore(T value)
    {
        if (_rankDenominator < 2)
        {
            Add(value);
            return 0;
        }
        var median = GetMedian();
        var iqr = GetIQR();
        Add(value);
        if (iqr == 0)
        {
            return 0;
        }
        return (double.CreateChecked(value) - double.CreateChecked(median)) / (IQRScale * iqr);
    }

    /// <summary>
    /// Replaces the newest value in the window with <paramref name="value"/> and returns its IQR-based
    /// robust z-score, atomically from the caller's point of view.
    ///
    /// The ordering is: undo the add that placed the newest value (removing it and restoring whatever it
    /// evicted), score <paramref name="value"/> against the values that REMAIN, which are exactly the prior
    /// values, then add <paramref name="value"/> to the window. The result is therefore identical to what
    /// <see cref="GetZScore"/> would have returned had the replaced value never been added at all.
    /// Calling this repeatedly for successive updates of the same realtime bar leaves the window holding
    /// exactly one observation for that bar and always scores against the same prior window.
    ///
    /// Partial windows, fewer than two prior values, and a zero IQR behave exactly as in <see cref="GetZScore"/>:
    /// the value is still added and 0 is returned. Note that the count used for the "fewer than two" test is
    /// the count AFTER the newest value has been removed. Total cost O(√N).
    /// </summary>
    /// <param name="value">The value that replaces the newest value in the window.</param>
    /// <returns>z = (value − median) / (IQRScale × IQR) over the prior window, or 0.</returns>
    /// <exception cref="SlidingWindowRankerException">The window is empty, so there is no value to replace.
    /// The window is not modified when this is thrown. See <see cref="SlidingWindowRanker{T}.RemoveLast"/>
    /// for the int.MaxValue limitation.</exception>
    public double ReplaceLastAndGetZScore(T value)
    {
        RemoveLastCore(nameof(ReplaceLastAndGetZScore));
        return GetZScore(value);
    }

    /// <summary>
    /// Same as GetZScore but does NOT update the window. O(log √N).
    /// </summary>
    public double GetZScoreNoAdd(T value)
    {
        if (_rankDenominator < 2)
        {
            return 0;
        }
        var median = GetMedian();
        var iqr = GetIQR();
        if (iqr == 0)
        {
            return 0;
        }
        return (double.CreateChecked(value) - double.CreateChecked(median)) / (IQRScale * iqr);
    }
}
