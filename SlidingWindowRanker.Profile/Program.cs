﻿using System.Diagnostics;
using SlidingWindowRanker;


const int NumberOfTestValues = 400000;
const int NumberOfPartitions = 128;
const int WindowSize = NumberOfTestValues / 10;
var valuesToRank = new List<double>(NumberOfTestValues);
for (var i = 0; i < NumberOfTestValues; i++)
{
    var random = new Random();
    var value = random.NextDouble() * 100;
    value = Math.Round(value, 1); // for easier debugging
    valuesToRank.Add(value);
}
var initialValues = valuesToRank.Take(WindowSize).ToList();
var ranker = new SlidingWindowRanker<double>(initialValues, NumberOfPartitions);     
Console.WriteLine("Ready to start ranking values...");
Console.WriteLine("Press any key to start...");
//Console.ReadKey();
var stopwatch = Stopwatch.StartNew();
var counter = 0;
var sum = 0.0;
for (var index = WindowSize; index < NumberOfTestValues; index++)
{
    var value = valuesToRank[index];
    var rank = ranker.GetRank(value);
    counter++;
    sum += rank;
}
var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
Console.WriteLine($"Done in {elapsedMilliseconds} ms. counter={counter:N0} sum={sum} Press any key to exit.");
Console.ReadKey();
