using System.Collections.Concurrent;
using FluentAssertions;
using Xunit.Abstractions;

namespace SlidingWindowRanker.Tests;

[Trait("Category", "ConcurrencyCharacterization")]
public class SlidingWindowRankerConcurrencyTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ConcurrentGetRankCalls_OnOneInstance_ExposeDocumentedUnsafeBehavior()
    {
        const int workerCount = 8;
        const int operationsPerWorker = 5_000;
        const int windowSize = 31;
        var ranker = new SlidingWindowRanker<double>(
            Enumerable.Range(0, windowSize).Select(i => (double)(i % 5)).ToList(),
            partitionCount: 11,
            windowSize: windowSize);
        var exceptions = new ConcurrentQueue<Exception>();
        using var start = new Barrier(workerCount);

        var workers = Enumerable.Range(0, workerCount)
            .Select(worker => Task.Factory.StartNew(
                () =>
                {
                    start.SignalAndWait();
                    for (var i = 0; i < operationsPerWorker; i++)
                    {
                        try
                        {
                            ranker.GetRank((worker * 17 + i) % 13);
                        }
                        catch (Exception exception)
                        {
                            exceptions.Enqueue(exception);
                        }
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(20));
        var invariantViolation = HasInvariantViolation(ranker);
        output.WriteLine($"Exceptions: {exceptions.Count}; invariant violation after completion: {invariantViolation}");
        foreach (var group in exceptions.GroupBy(exception => exception.GetType().Name).OrderBy(group => group.Key))
        {
            output.WriteLine($"{group.Key}: {group.Count()}");
        }
        if (exceptions.TryPeek(out var firstException))
        {
            output.WriteLine($"First exception: {firstException.GetType().Name}: {firstException.Message}");
        }
        var firstRankerException = exceptions.OfType<SlidingWindowRankerException>().FirstOrDefault();
        if (firstRankerException is not null)
        {
            output.WriteLine($"First ranker exception: {firstRankerException.Message}");
        }

        (exceptions.Count > 0 || invariantViolation).Should().BeTrue(
            "the type explicitly requires callers to serialize access to one instance");
    }

    private static bool HasInvariantViolation(SlidingWindowRanker<double> ranker)
    {
        try
        {
            var partitions = ranker.TestPartitions;
            var combined = partitions.SelectMany(partition => partition.Values).ToList();
            if (partitions.Count == 0
                || combined.Count != ranker.TestQueueValues.Count
                || ranker.TestRankDenominator != combined.Count
                || combined.Count > ranker.TestWindowSize
                || !combined.IsSortedAscending())
            {
                return true;
            }

            var lowerBound = 0;
            foreach (var partition in partitions)
            {
                if (!partition.Values.IsSortedAscending() || partition.LowerBound != lowerBound)
                {
                    return true;
                }
                lowerBound += partition.Count;
            }

            return !ranker.TestQueueValues.Order().SequenceEqual(combined.Order());
        }
        catch
        {
            return true;
        }
    }
}
