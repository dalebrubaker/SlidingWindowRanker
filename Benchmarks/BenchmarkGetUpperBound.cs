using BenchmarkDotNet.Attributes;
using SlidingWindowRanker;

namespace Benchmarks;

/*
 *

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
Intel Core i7-14700, 1 CPU, 28 logical and 20 physical cores
.NET SDK 8.0.400
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method                    | Mean     | Error    | StdDev   |
|-------------------------- |---------:|---------:|---------:|
| TestGetUpperBoundO1Mini   | 34.32 ms | 0.518 ms | 0.837 ms |
| TestGetUpperBoundOriginal | 34.18 ms | 0.226 ms | 0.200 ms |

 */

public class BenchmarkGetUpperBound
{
    private const int NumberOfIterations = 1000000;
    private readonly List<double> _list = new(NumberOfIterations);

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < NumberOfIterations; i++)
        {
            var random = new Random();
            _list.Add(random.NextDouble());
        }
    }

    [Benchmark]
    public void TestGetUpperBoundO1Mini()
    {
        for (var i = 0; i < NumberOfIterations - 1; i++)
        {
            var index = _list.GetUpperBound(_list[i]);
        }
    }

    [Benchmark]
    public void TestGetUpperBoundOriginal()
    {
        for (var i = 0; i < NumberOfIterations - 1; i++)
        {
            var index = _list.GetUpperBoundOriginal(0, _list.Count, _list[i]);
        }
    }
}