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

| Method     | GetRankCount | WindowSize | Mean      | Error    | StdDev   | Rank |
|----------- |------------- |----------- |----------:|---------:|---------:|-----:|
| RankValues | 100000       | 1000       |  23.81 ms | 0.099 ms | 0.088 ms |    1 |
| RankValues | 100000       | 100000     |  25.81 ms | 0.091 ms | 0.081 ms |    2 |
| RankValues | 100000       | 10000      |  26.52 ms | 0.078 ms | 0.073 ms |    3 |
| RankValues | 1000000      | 1000       | 237.77 ms | 0.644 ms | 0.603 ms |    4 |
| RankValues | 1000000      | 10000      | 265.42 ms | 1.560 ms | 1.460 ms |    5 |
| RankValues | 1000000      | 100000     | 374.91 ms | 2.407 ms | 2.251 ms |    6 |

GetRankCount is the number of times GetRank is called in the benchmark.
WindowSize is the size of the window in the benchmark, i.e., the number of values against which each new value is
ranked.
The number of partitions K defaults to the square root of the window size (which is usually close to optimal) but can be
specified if desired.
