using FluentAssertions;

namespace SlidingWindowRanker.Tests;

public class SlidingWindowRankerRandomizedTests
{
    [Fact]
    public void DeterministicRandomStreams_AgreeWithReferenceAfterEveryOperation()
    {
        for (var seed = 0; seed < 32; seed++)
        {
            var random = new Random(seed);
            var windowSize = random.Next(1, 65);
            var initialCount = random.Next(windowSize + 1);
            var partitionCount = random.Next(1, Math.Min(windowSize + 3, 16));
            var initial = Enumerable.Range(0, initialCount)
                .Select(_ => NextDuplicateHeavyDouble(random))
                .ToList();
            if (seed % 2 == 0)
            {
                initial.Reverse();
            }

            var ranker = new SlidingWindowRanker<double>(initial, partitionCount, windowSize);
            var reference = new ReferenceSlidingWindowRanker<double>(initial, windowSize);
            RankerInvariantChecker.AssertState(ranker, reference.Values, initialCount);

            for (var operation = 0; operation < 2_000; operation++)
            {
                var value = NextDuplicateHeavyDouble(random);
                RankerInvariantChecker.AssertStep(
                    ranker,
                    reference,
                    value,
                    initialCount + operation + 1);
            }
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void PartitionRemovalRelativeToInsertion_WithSplitsAndDuplicates_AgreesWithReference(int partitionCount)
    {
        const int windowSize = 12;
        for (var seed = 100; seed < 112; seed++)
        {
            var random = new Random(seed);
            // FIFO order is chosen so the first three evictions are 3, 0, and 2.
            var initial = new List<double> { 3, 0, 2, 1, 0, 1, 2, 3, 0, 1, 2, 3 };
            var ranker = new SlidingWindowRanker<double>(initial, partitionCount, windowSize);
            var reference = new ReferenceSlidingWindowRanker<double>(initial, windowSize);

            for (var operation = 0; operation < 2_000; operation++)
            {
                // The first three operations deliberately put the removed value after,
                // before, and equal to the insertion value's partition respectively.
                // Later operations force splits, singleton partition removal, and
                // duplicate runs that cross partition boundaries.
                var value = (operation % 11) switch
                {
                    0 => -1000 - operation,
                    1 => 1000 + operation,
                    2 or 3 or 4 => operation % 4,
                    _ => random.Next(-3, 8)
                };
                RankerInvariantChecker.AssertStep(
                    ranker,
                    reference,
                    value,
                    initial.Count + operation + 1);
            }

            if (partitionCount >= 3)
            {
                ranker.CountPartitionSplits.Should().BeGreaterThan(0);
                ranker.CountPartitionRemoves.Should().BeGreaterThan(0);
            }
        }
    }

    private static double NextDuplicateHeavyDouble(Random random)
    {
        return random.Next(10) switch
        {
            0 => 0.0,
            1 => -0.0,
            2 or 3 or 4 => random.Next(-3, 4),
            5 => 6198.600613839286,
            _ => Math.Round((random.NextDouble() - 0.5) * 1000, 4)
        };
    }
}
