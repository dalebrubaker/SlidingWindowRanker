using System.Diagnostics;
using SlidingWindowRanker;

const int NumberOfTestValues = 1000000;
const int WindowSize = NumberOfTestValues / 10;
//const int NumberOfPartitions = -1; // use Sqrt(WindowSize) as default
var valuesToRank = new List<double>(NumberOfTestValues);
var random = new Random();
for (var i = 0; i < NumberOfTestValues; i++)
{
    var value = random.NextDouble() * 100;
    value = Math.Round(value, 1); // for easier debugging
    valuesToRank.Add(value);
}
var valuesToRankStr = string.Join(',', valuesToRank);
var indexToSplit = NumberOfTestValues - WindowSize;
var initialValues = valuesToRank.GetRange(indexToSplit, WindowSize);
valuesToRank.RemoveRange(indexToSplit, WindowSize);
var ranker = new SlidingWindowRanker<double>(initialValues);
//using var ranker = new SlidingWindowRankerUnsafe<double>(initialValues);
Console.WriteLine("Ready to start ranking values...");
Console.WriteLine("Press any key to start...");
Console.ReadKey();
var stopwatch = Stopwatch.StartNew();
var counter = 0;
for (var i = indexToSplit - 1; i >= 0; i--)
{
    var value = valuesToRank[i];
    if (i == 3233)
    {
    }
    var rank = ranker.GetRank(value);
    counter++;
}
var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
var countSplits = ranker.CountPartitionSplits;
var countRemovePartitions = ranker.CountPartitionRemoves;
Console.WriteLine($"Done in {elapsedMilliseconds} ms. #ranks={counter:N0} "
                  + $"countRemovePartitions={countRemovePartitions:N0} countSplits={countSplits:N0} Press any key to exit.");
Console.ReadKey();