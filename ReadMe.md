# Sliding Window Ranker

Sliding Window Ranker is a C# library that provides efficient algorithms for ranking elements within a sliding window
over a sequence of data. This can be useful for various applications such as real-time data analysis, signal processing,
and more.

## Features

- Support high-performance ranking of a generic List of values in a window of size N where a new value is added to the
  right side of the window and the oldest one is removed from the left side of the window.
- The window size defaults to the count of the initial values provided to the constructor, if any. The windows size is
  the number of values you want to rank against. If you set this to int.MaxValue new values will be added to the window
  but old ones will never be removed.
- The number of partitions K defaults to the square root of the window size (which is usually close to optimal) but can
  be specified if desired.
- By removing earlier data, ranking is no longer against "stale" data. But specifying a window size of int.MaxValue
  causes earlier values to never drop off.
- The fraction returned is the Cumulative Distribution Function (CDF) value for the specified value except that CDF is
  normally defined as LESS THAN OR EQUAL rather than LESS THAN. So, the values returned will be in the range ([0, 1] NOT
  inclusive of 1) rather than [0, 1] inclusive.
- The fraction returned can be multiplied by 10 to get a decile rank or by 100 to get a percentile rank.
- For realtime bars that update many times before they close, `RemoveLast`, `ReplaceLastAndGetRank` and
  `SlidingWindowStats.ReplaceLastAndGetZScore` replace the newest observation instead of appending another one,
  so a bar contributes exactly one observation however many times it updates.
- This program is NOT thread-safe. If you need to use it in a multi-threaded environment, you will need to provide your
  own synchronization.

## Usage

Here's a simple example of how to use Sliding Window Ranker:

```csharp
var ranker = new SlidingWindowRanker<double>(initialValues);
var ranker = new SlidingWindowRanker<double>(windowSize);
var ranker = new SlidingWindowRanker<double>(windowSize, initialValues);

var rank = ranker.GetRank(value);
var rank = ranker.GetRankNoAdd(value);
ranker.Add(value); // update the window without calculating a rank

var removed = ranker.RemoveLast();              // undo the most recent observation
var rank = ranker.ReplaceLastAndGetRank(value); // replace the most recent observation
int n = ranker.Count;                           // number of values currently in the window
```

## Realtime bars — replacing the newest observation

`GetRank`, `Add` and `GetZScore` each treat their argument as a **new** observation. A realtime bar that
updates many times before it closes would therefore contribute one observation per tick instead of one
observation per bar. `RemoveLast` and the `ReplaceLast...` methods exist for exactly this case: track the
absolute bar index, call `GetRank`/`GetZScore` on the first update of a bar, and call the matching
`ReplaceLast...` method on every later update of that same bar.

```csharp
if (barAbsolute != _lastBarAbsolute)
{
    _lastBarAbsolute = barAbsolute;
    rank = ranker.GetRank(value);              // a new bar: a new observation
}
else
{
    rank = ranker.ReplaceLastAndGetRank(value); // the same bar updating: replace its observation
}
```

### `T RemoveLast()`

Undoes the most recent observation. It removes the **newest** value from the right edge and, when the add
that placed it there evicted the oldest value from the left edge, **restores that evicted value**. The
window is therefore left exactly as it was before that add, which is what makes replacement exact rather
than approximate. It returns the removed value, so the observation can be handed to another ranker — the
sign-change case where a value moves between a plus ranker and a minus ranker.

Only the single most recent add can be undone. Calling `RemoveLast` again removes whatever is then newest,
but there is no longer an eviction to restore, so the window shrinks by one. In other words, the first call
rewinds an add and further calls trim observations off the newest end.

It throws a `SlidingWindowRankerException` when the window is empty, leaving the window unmodified. When the
window size is `int.MaxValue` the chronological sequence is deliberately not retained, because nothing is
ever evicted, so only the single newest value can be removed; a second consecutive `RemoveLast` throws.

`CountPartitionSplits` and `CountPartitionRemoves` count work actually performed and are never rolled back.

### `double ReplaceLastAndGetRank(T value)`

`RemoveLast` followed by `GetRank`, so the returned rank is identical to what `GetRank(value)` would have
returned had the replaced value never been added at all. The number of values in the window and their
chronological order are unchanged, and no additional value is evicted. The rank keeps the same
"rank AFTER insertion" semantics as `GetRank`: the fraction of the updated window that is less than `value`,
where the updated window already contains `value` and no longer contains the value it replaced.

Repeated calls for successive updates of the same bar always leave the window holding exactly one
observation for that bar and always rank against the same set of prior observations.

Cost is O(log √N) to locate each partition plus O(√N) to update it — the same order as an add.

## Constructor options:
* Optional List{T} initialValues: The initial values to load into the window, in chronological FIFO order from oldest to newest. This list is NOT modified. Defaults to an empty list. Reverse-chronological input changes eviction order and is not inferred or reversed automatically.
* Optional int windowSize: The width of the window of values to rank against. Defaults to initialValues.Count if they are supplied. 
    * int.MaxValue means no values will ever be removed from the window, so it grows forever.
* Optional int partitionCount. The number of partitions into which the window is divided (for faster performance). Defaults to the square root of windowSize, which is usually close to optimal.
* Optional bool isSorted: Flag to indicate that the initial values are already ascending by value, saving sorting work when set to true. Defaults to false. Because the same input also defines FIFO order, `isSorted: true` is only appropriate when the oldest-to-newest sequence is value-sorted too.
* AFTER the defaults are applied, exceptions are thrown if the window size is less than 1, the window size is smaller than the initial population, or the partition count is less than 1.

## SlidingWindowStats — IQR-Based Robust Z-Score

`SlidingWindowStats<T>` extends `SlidingWindowRanker<T>` with percentile and z-score methods that read from the same partitioned sorted structure. The window slides correctly across millions of bars at O(√N) per bar — same as ranking.

```csharp
var stats = new SlidingWindowStats<double>(windowSize);
double median = stats.GetMedian();           // O(log √N)
double q25    = stats.GetQ25();              // O(log √N)
double q75    = stats.GetQ75();              // O(log √N)
double iqr    = stats.GetIQR();              // O(log √N)
double z      = stats.GetZScore(value);      // O(√N) — adds value to window
double zPeek  = stats.GetZScoreNoAdd(value); // O(log √N) — no window update
int n         = stats.Count;                  // current window count

double zReplace = stats.ReplaceLastAndGetZScore(value); // O(√N) — replaces the newest observation
```

`ReplaceLastAndGetZScore` is the z-score counterpart of `ReplaceLastAndGetRank`, for realtime bars that
update many times before they close. The ordering is: undo the add that placed the newest value (removing
it and restoring whatever it evicted), score `value` against the values that remain — which are exactly the
prior observations — then add `value` to the window. The result is therefore identical to what
`GetZScore(value)` would have returned had the replaced value never been added at all.

Partial windows, fewer than two prior values, and a zero IQR behave exactly as in `GetZScore`: the value is
still added and `0` is returned. Note that the count used for the "fewer than two" test is the count after
the newest value has been undone. It throws a `SlidingWindowRankerException` when the window is empty,
leaving the window unmodified.

Percentile values use the zero-based index `floor(p × Count)`, clamped to the available range, without interpolation.

**Z-score formula:** `z = (value − median) / (0.7413 × IQR)`

The `0.7413` constant makes IQR a consistent estimator of σ (standard deviation), equivalent to the MAD-based formula `(value − median) / (1.4826 × MAD)` for normally distributed data:

- For N(0, σ²): Q75 = +0.6745σ, Q25 = −0.6745σ → IQR = 1.3490σ
- MAD = median(|xᵢ − median|) = 0.6745σ → IQR = 2 × MAD
- Therefore σ = IQR / 1.3490 = 0.7413 × IQR
- And: 1.4826 × MAD = 0.7413 × IQR

Generic constraint: `T : IComparable<T>, INumber<T>` (.NET 7+ generic math).

## Contributing

Contributions to Sliding Window Ranker are welcome! If you have an idea for a new feature or have found a bug, please
open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE] file for details.

## Contact

For any questions or inquiries, please contact me at [brubaker.dale@gmail.com](mailto:brubaker.dale@gmail.com).

## Benchmarks (from BenchmarkDotNet)

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
Intel Core i7-14700, 1 CPU, 28 logical and 20 physical cores
.NET SDK 8.0.400
[Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

| Method     | GetRankCount | WindowSize | Mean      | Error    | StdDev   | Rank |
|----------- |------------- |----------- |----------:|---------:|---------:|-----:|
| RankValues | 100000       | 1000       |  23.81 ms | 0.099 ms | 0.088 ms |    1 |
| RankValues | 100000       | 100000     |  25.81 ms | 0.091 ms | 0.081 ms |    2 |
| RankValues | 100000       | 10000      |  26.52 ms | 0.078 ms | 0.073 ms |    3 |
| RankValues | 1000000      | 1000       | 237.77 ms | 0.644 ms | 0.603 ms |    4 |
| RankValues | 1000000      | 10000      | 265.42 ms | 1.560 ms | 1.460 ms |    5 |
| RankValues | 1000000      | 100000     | 374.91 ms | 2.407 ms | 2.251 ms |    6 |

* GetRankCount is the number of times GetRank is called in the benchmark after the window has been filled with initial values.
* WindowSize is the size of the window in the benchmark, i.e., the number of values against which each new value is
ranked.
* The number of partitions are the default: the square root of the window size (which is usually close to optimal).
