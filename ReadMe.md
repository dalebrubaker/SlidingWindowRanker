# Sliding Window Ranker

Sliding Window Ranker is a C# library that provides efficient algorithms for ranking elements within a sliding window
over a sequence of data. This can be useful for various applications such as real-time data analysis, signal processing,
and more.

## Features

- Support high-performance ranking of a generic List of values in a window of size N where a new value is added to the
  right side of the window and the oldest one is removed from the left side of the window.
- The window size defaults to the count of the initial values provided to the constructor, but can be smaller, even
  zero, if fewer values are available.
- The number of partitions K defaults to the square root of the window size (which is usually close to optimal) but can
  be specified if desired.
- By removing earlier data, ranking is no longer against "stale" data. But specifying a window size of int.MaxValue
  causes earlier values to never drop off.
- The fraction returned is the Cumulative Distribution Function (CDF) value for the specified value except that CDF is
  normally defined as LESS THAN OR EQUAL rather than LESS THAN. So, the values returned will be in the range ([0, 1] NOT
  inclusive of 1) rather than [0, 1] inclusive.
- The fraction returned can be multiplied by 10 to get a decile rank or by 100 to get a percentile rank.
- An alternative version called SlidingWindowRankerUnsafe is available that is significantly faster. It is unsafe only in the sense that it uses pointers like C or C++ programs. You should call Dispose() on this version of the ranker to do a better job of disposing of unmanaged memory. This version may use more memory because a significant speed advantage is that it inserts and removes to the smallest side of the window.

## Usage

Here's a simple example of how to use Sliding Window Ranker:

```csharp
var ranker = new SlidingWindowRanker<double>(initialValues);
var rank = ranker.GetRank(value);

// or
using var ranker = new SlidingWindowRankerUnsafe<double>(initialValues);
var rank = ranker.GetRank(value);


```

## Contributing

We welcome contributions to Sliding Window Ranker! If you have an idea for a new feature or have found a bug, please
open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE] file for details.

## Contact

For any questions or inquiries, please contact us at [brubaker.dale@gmail.com](mailto:brubaker.dale@gmail.com).

## Benchmarks (from BenchmarkDotNet)

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
Intel Core i7-14700, 1 CPU, 28 logical and 20 physical cores
.NET SDK 8.0.400
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

  | Method           | NumberOfTestValues | PartitionsMultipleOfDefault  | Mean     | Error   | StdDev  | Ratio | RatioSD | Rank | Completed Work Items | Lock Contentions | Allocated | Alloc Ratio |
|----------------- |------------------- |----------------------------- |---------:|--------:|--------:|------:|--------:|-----:|---------------------:|-----------------:|----------:|------------:|
| RankValuesUnsafe | 1000000            | 0.5                          | 293.5 ms | 5.36 ms | 5.02 ms |  1.00 |    0.02 |    1 |                    - |                - |     200 B |        1.00 |
| RankValuesSafe   | 1000000            | 0.5                          | 295.4 ms | 5.00 ms | 6.33 ms |  1.01 |    0.03 |    1 |                    - |                - |     200 B |        1.00 |
|                  |                    |                              |          |         |         |       |         |      |                      |                  |           |             |
| RankValuesUnsafe | 1000000            | 0.75                         | 285.1 ms | 1.53 ms | 1.36 ms |  1.00 |    0.01 |    1 |                    - |                - |     200 B |        1.00 |
| RankValuesSafe   | 1000000            | 0.75                         | 301.8 ms | 3.33 ms | 3.11 ms |  1.06 |    0.01 |    2 |                    - |                - |     200 B |        1.00 |
|                  |                    |                              |          |         |         |       |         |      |                      |                  |           |             |
| RankValuesUnsafe | 1000000            | 1                            | 293.3 ms | 5.84 ms | 5.73 ms |  1.00 |    0.03 |    1 |                    - |                - |     200 B |        1.00 |
| RankValuesSafe   | 1000000            | 1                            | 314.7 ms | 4.27 ms | 3.78 ms |  1.07 |    0.02 |    2 |                    - |                - |     200 B |        1.00 |
|                  |                    |                              |          |         |         |       |         |      |                      |                  |           |             |
| RankValuesUnsafe | 1000000            | 1.25                         | 311.2 ms | 5.00 ms | 4.43 ms |  1.00 |    0.02 |    1 |                    - |                - |     200 B |        1.00 |
| RankValuesSafe   | 1000000            | 1.25                         | 327.2 ms | 3.10 ms | 2.75 ms |  1.05 |    0.02 |    2 |                    - |                - |     200 B |        1.00 |
