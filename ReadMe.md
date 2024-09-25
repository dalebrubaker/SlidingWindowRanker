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

## Usage

Here's a simple example of how to use Sliding Window Ranker:

```csharp
var ranker = new SlidingWindowRanker<double>(initialValues);
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

| Method           | GetRankCount | WindowSize | Mean      | Error    | StdDev    | Ratio | RatioSD | Rank |
|----------------- |------------- |----------- |----------:|---------:|----------:|------:|--------:|-----:|
| RankValuesSafe   | 100000       | 1000       |  22.55 ms | 0.217 ms |  0.169 ms |  0.99 |    0.02 |    1 |
| RankValuesUnsafe | 100000       | 1000       |  22.89 ms | 0.445 ms |  0.477 ms |  1.00 |    0.03 |    1 |
|                  |              |            |           |          |           |       |         |      |
| RankValuesSafe   | 100000       | 10000      |  26.89 ms | 0.524 ms |  0.514 ms |  0.98 |    0.03 |    1 |
| RankValuesUnsafe | 100000       | 10000      |  27.43 ms | 0.548 ms |  0.652 ms |  1.00 |    0.03 |    1 |
|                  |              |            |           |          |           |       |         |      |
| RankValuesUnsafe | 100000       | 100000     |  25.59 ms | 0.510 ms |  0.664 ms |  1.00 |    0.04 |    1 |
| RankValuesSafe   | 100000       | 100000     |  25.75 ms | 0.510 ms |  0.824 ms |  1.01 |    0.04 |    1 |
|                  |              |            |           |          |           |       |         |      |
| RankValuesSafe   | 1000000      | 1000       | 224.04 ms | 2.412 ms |  2.256 ms |  1.00 |    0.02 |    1 |
| RankValuesUnsafe | 1000000      | 1000       | 224.94 ms | 3.207 ms |  3.000 ms |  1.00 |    0.02 |    1 |
|                  |              |            |           |          |           |       |         |      |
| RankValuesSafe   | 1000000      | 10000      | 267.47 ms | 4.743 ms |  4.437 ms |  1.00 |    0.02 |    1 |
| RankValuesUnsafe | 1000000      | 10000      | 268.81 ms | 3.059 ms |  2.555 ms |  1.00 |    0.01 |    1 |
|                  |              |            |           |          |           |       |         |      |
| RankValuesSafe   | 1000000      | 100000     | 420.95 ms | 8.287 ms | 11.618 ms |  0.99 |    0.04 |    1 |
| RankValuesUnsafe | 1000000      | 100000     | 426.24 ms | 8.475 ms | 11.881 ms |  1.00 |    0.04 |    1 |                - |                - |     200 B |        1.00 |
