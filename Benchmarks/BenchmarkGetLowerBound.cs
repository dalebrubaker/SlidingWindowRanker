﻿using BenchmarkDotNet.Attributes;
using SlidingWindowRanker;

namespace Benchmarks;


/*
 *
 *BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
Intel Core i7-14700, 1 CPU, 28 logical and 20 physical cores
.NET SDK 8.0.400
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method                    | Mean     | Error    | StdDev   |
|-------------------------- |---------:|---------:|---------:|
| TestGetLowerBoundO1Mini   | 32.89 ms | 0.028 ms | 0.026 ms |
| TestGetLowerBoundOriginal | 35.83 ms | 0.097 ms | 0.090 ms |

 * 
 */


public class BenchmarkGetLowerBound
{
    const int NumberOfIterations = 1000000;
    readonly List<double> _list = new List<double>(NumberOfIterations);

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
    public void TestGetLowerBoundO1Mini()
    {
        for (int i = 0; i < NumberOfIterations - 1; i++)
        {
            var index = _list.GetLowerBound(_list[i]);
        }
    }
    
    [Benchmark]
    public void TestGetLowerBoundOriginal()
    {
        for (int i = 0; i < NumberOfIterations - 1; i++)
        {
            var index = _list.GetLowerBoundOriginal<double>(0, _list.Count, _list[i]);
        }
    }

}