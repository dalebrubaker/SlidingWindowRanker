using FluentAssertions;

namespace SlidingWindowRanker.Tests;

/// <summary>
/// Tests for the realtime-bar APIs added in 1.4.0: RemoveLast, ReplaceLastAndGetRank
/// and SlidingWindowStats.ReplaceLastAndGetZScore.
/// </summary>
public class SlidingWindowRankerReplaceLastTests
{
    [Fact]
    public void RemoveLast_EmptyWindow_ThrowsAndLeavesStateIntact()
    {
        var ranker = new SlidingWindowRanker<double>(5);

        var act = () => ranker.RemoveLast();

        act.Should().Throw<SlidingWindowRankerException>().WithMessage("*RemoveLast*");
        ranker.Count.Should().Be(0);
        RankerInvariantChecker.AssertInvariants(ranker, []);

        // The failure must not have corrupted anything, so the ranker still works
        ranker.GetRank(1.0).Should().Be(0);
        RankerInvariantChecker.AssertInvariants(ranker, [1.0]);
    }

    [Fact]
    public void ReplaceLastAndGetRank_EmptyWindow_ThrowsAndLeavesStateIntact()
    {
        var ranker = new SlidingWindowRanker<double>(5);

        var act = () => ranker.ReplaceLastAndGetRank(3.0);

        act.Should().Throw<SlidingWindowRankerException>().WithMessage("*ReplaceLastAndGetRank*");
        ranker.Count.Should().Be(0);
        RankerInvariantChecker.AssertInvariants(ranker, []);
    }

    [Fact]
    public void RemoveLast_AfterDrainingTheWindow_ThrowsAndLeavesStateIntact()
    {
        var ranker = new SlidingWindowRanker<double>([3.0, 1.0, 2.0], windowSize: 3);

        ranker.RemoveLast().Should().Be(2.0);
        ranker.RemoveLast().Should().Be(1.0);
        ranker.RemoveLast().Should().Be(3.0);
        ranker.Count.Should().Be(0);
        RankerInvariantChecker.AssertInvariants(ranker, []);

        var act = () => ranker.RemoveLast();
        act.Should().Throw<SlidingWindowRankerException>();
        RankerInvariantChecker.AssertInvariants(ranker, []);
    }

    [Fact]
    public void RemoveLast_ReturnsNewestAndLeavesOlderObservationsUntouched()
    {
        var ranker = new SlidingWindowRanker<double>([5.0, 1.0, 4.0], windowSize: 6);
        ranker.Add(9.0);
        ranker.Add(0.5);

        ranker.RemoveLast().Should().Be(0.5, "the NEWEST observation is removed, not the oldest");
        RankerInvariantChecker.AssertInvariants(ranker, [5.0, 1.0, 4.0, 9.0]);

        ranker.RemoveLast().Should().Be(9.0);
        RankerInvariantChecker.AssertInvariants(ranker, [5.0, 1.0, 4.0]);
    }

    [Fact]
    public void RemoveLast_OnFullWindow_UnfillsSoTheNextAddDoesNotEvict()
    {
        var ranker = new SlidingWindowRanker<double>([1.0, 2.0, 3.0], windowSize: 3);
        ranker.Count.Should().Be(3);

        ranker.RemoveLast().Should().Be(3.0);
        RankerInvariantChecker.AssertInvariants(ranker, [1.0, 2.0]);

        // Refilling must NOT evict 1.0
        ranker.Add(7.0);
        RankerInvariantChecker.AssertInvariants(ranker, [1.0, 2.0, 7.0]);

        // And the window is full again, so the next add evicts normally
        ranker.Add(8.0);
        RankerInvariantChecker.AssertInvariants(ranker, [2.0, 7.0, 8.0]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(64)]
    public void ReplaceLastAndGetRank_MatchesRemoveNewestThenAddOnce(int windowSize)
    {
        var random = new Random(windowSize * 7919);
        var ranker = new SlidingWindowRanker<double>(windowSize);
        var reference = new ReferenceSlidingWindowRanker<double>([], windowSize);

        for (var i = 0; i < 500; i++)
        {
            var seed = Math.Round(random.NextDouble() * 10, 1);
            RankerInvariantChecker.AssertInvariants(ranker, reference.Values);
            ranker.GetRank(seed).Should().Be(reference.GetRank(seed));

            // Several realtime updates of the same bar
            for (var update = 0; update < 3; update++)
            {
                var value = Math.Round(random.NextDouble() * 10, 1);
                var expected = reference.ReplaceLastAndGetRank(value);
                var actual = ranker.ReplaceLastAndGetRank(value);
                actual.Should().Be(expected, $"window={windowSize} i={i} update={update} value={value}");
                RankerInvariantChecker.AssertInvariants(ranker, reference.Values);
            }
        }
    }

    [Fact]
    public void ReplaceLastAndGetRank_RepeatedReplacement_DoesNotChangeCountOrEvictOlderObservations()
    {
        const int windowSize = 5;
        var ranker = new SlidingWindowRanker<double>([1.0, 2.0, 3.0, 4.0, 5.0], windowSize: windowSize);

        for (var i = 0; i < 1_000; i++)
        {
            ranker.ReplaceLastAndGetRank(i % 2 == 0 ? 100.0 + i : -100.0 - i);
            ranker.Count.Should().Be(windowSize, "replacement never changes the number of observations");
            ranker.TestQueueValues.Take(4).Should().Equal([1.0, 2.0, 3.0, 4.0], "older observations are never evicted");
        }
    }

    [Fact]
    public void ReplaceLastAndGetRank_WithTheSameValue_LeavesTheWindowUnchanged()
    {
        var initial = new List<double> { 4.0, 4.0, 1.0, 9.0, 4.0 };
        var ranker = new SlidingWindowRanker<double>(initial, windowSize: 5);
        var before = ranker.TestQueueValues;
        var firstRank = ranker.GetRankNoAdd(4.0);

        for (var i = 0; i < 10; i++)
        {
            ranker.ReplaceLastAndGetRank(4.0).Should().Be(firstRank);
            ranker.TestQueueValues.Should().Equal(before);
            RankerInvariantChecker.AssertInvariants(ranker, before);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    public void ReplaceLastAndGetRank_ReplacingMinAndMax_MatchesReference(int windowSize)
    {
        var ranker = new SlidingWindowRanker<double>(windowSize);
        var reference = new ReferenceSlidingWindowRanker<double>([], windowSize);
        foreach (var value in Enumerable.Range(0, windowSize).Select(i => (double)i))
        {
            ranker.GetRank(value).Should().Be(reference.GetRank(value));
        }

        foreach (var replacement in new[] { double.MinValue, double.MaxValue, 0.0, windowSize - 1.0, -1.0 })
        {
            ranker.ReplaceLastAndGetRank(replacement).Should().Be(reference.ReplaceLastAndGetRank(replacement),
                $"replacing with {replacement} in a window of {windowSize}");
            RankerInvariantChecker.AssertInvariants(ranker, reference.Values);
        }
    }

    [Fact]
    public void RemoveLast_WindowSizeOne_RoundTripsRepeatedly()
    {
        var ranker = new SlidingWindowRanker<double>(1);

        for (var i = 0; i < 100; i++)
        {
            ranker.GetRank(i).Should().Be(0, "a single-value window always ranks its own value at 0");
            ranker.Count.Should().Be(1);
            RankerInvariantChecker.AssertInvariants(ranker, [(double)i]);

            ranker.RemoveLast().Should().Be(i);
            ranker.Count.Should().Be(0);
            RankerInvariantChecker.AssertInvariants(ranker, []);
        }
    }

    [Fact]
    public void ReplaceLastAndGetRank_HeavyDuplicates_MatchesReference()
    {
        const int windowSize = 12;
        var random = new Random(20260918);
        var ranker = new SlidingWindowRanker<double>(windowSize);
        var reference = new ReferenceSlidingWindowRanker<double>([], windowSize);

        for (var i = 0; i < 5_000; i++)
        {
            var value = random.Next(0, 3);
            ranker.GetRank(value).Should().Be(reference.GetRank(value));
            RankerInvariantChecker.AssertInvariants(ranker, reference.Values);

            var replacement = random.Next(0, 3);
            ranker.ReplaceLastAndGetRank(replacement).Should().Be(reference.ReplaceLastAndGetRank(replacement));
            RankerInvariantChecker.AssertInvariants(ranker, reference.Values);
        }
    }

    [Fact]
    public void MixedOperations_RandomizedStreams_AgreeWithReferenceAfterEveryOperation()
    {
        for (var seed = 0; seed < 32; seed++)
        {
            var random = new Random(seed);
            var windowSize = random.Next(1, 33);
            var initialCount = random.Next(windowSize + 1);
            var partitionCount = random.Next(1, Math.Min(windowSize + 3, 12));
            var initial = Enumerable.Range(0, initialCount).Select(_ => NextDuplicateHeavyDouble(random)).ToList();

            var ranker = new SlidingWindowRanker<double>(initial, partitionCount, windowSize);
            var reference = new ReferenceSlidingWindowRanker<double>(initial, windowSize);
            RankerInvariantChecker.AssertInvariants(ranker, reference.Values);

            for (var operation = 0; operation < 1_500; operation++)
            {
                var value = NextDuplicateHeavyDouble(random);
                switch (random.Next(5))
                {
                    case 0:
                        ranker.GetRank(value).Should().Be(reference.GetRank(value), Because(seed, operation, "GetRank"));
                        break;
                    case 1:
                        ranker.Add(value);
                        reference.Add(value);
                        break;
                    case 2:
                        ranker.GetRankNoAdd(value).Should()
                            .Be(reference.GetRankNoAdd(value), Because(seed, operation, "GetRankNoAdd"));
                        break;
                    case 3:
                        if (reference.Count == 0)
                        {
                            var removeAct = () => ranker.RemoveLast();
                            removeAct.Should().Throw<SlidingWindowRankerException>();
                            break;
                        }
                        ranker.RemoveLast().Should().Be(reference.RemoveLast(), Because(seed, operation, "RemoveLast"));
                        break;
                    default:
                        if (reference.Count == 0)
                        {
                            var replaceAct = () => ranker.ReplaceLastAndGetRank(value);
                            replaceAct.Should().Throw<SlidingWindowRankerException>();
                            break;
                        }
                        ranker.ReplaceLastAndGetRank(value).Should()
                            .Be(reference.ReplaceLastAndGetRank(value), Because(seed, operation, "ReplaceLastAndGetRank"));
                        break;
                }

                RankerInvariantChecker.AssertInvariants(ranker, reference.Values);
            }
        }
    }

    [Fact]
    public void RemoveLast_UnboundedWindow_RemovesTheNewestValue()
    {
        var ranker = new SlidingWindowRanker<double>(int.MaxValue);
        for (var i = 0; i < 100; i++)
        {
            ranker.GetRank(i);
        }
        ranker.Count.Should().Be(100);

        ranker.RemoveLast().Should().Be(99.0);
        ranker.Count.Should().Be(99);
        ranker.GetRankNoAdd(99.0).Should().Be(1.0, "99 is no longer in the window, so every value is below it");

        // Replacement round-trips indefinitely in unbounded mode, once there is a value to replace
        ranker.GetRank(1000.0).Should().Be(99 / 100.0);
        for (var i = 0; i < 50; i++)
        {
            ranker.ReplaceLastAndGetRank(1000.0 + i).Should().Be(99 / 100.0);
            ranker.Count.Should().Be(100);
        }
    }

    [Fact]
    public void RemoveLast_UnboundedWindow_WithInitialValues_RemovesTheNewestInitialValue()
    {
        var ranker = new SlidingWindowRanker<double>([1.0, 2.0, 3.0], windowSize: int.MaxValue);

        ranker.RemoveLast().Should().Be(3.0, "initial values are ordered oldest to newest");
        ranker.Count.Should().Be(2);
    }

    [Fact]
    public void RemoveLast_UnboundedWindow_CalledTwiceWithoutAnAdd_ThrowsAndLeavesStateIntact()
    {
        var ranker = new SlidingWindowRanker<double>(int.MaxValue);
        ranker.Add(1.0);
        ranker.Add(2.0);

        ranker.RemoveLast().Should().Be(2.0);

        var act = () => ranker.RemoveLast();
        act.Should().Throw<SlidingWindowRankerException>().WithMessage("*int.MaxValue*");
        ranker.Count.Should().Be(1, "the failed removal must not change the window");
        ranker.GetRankNoAdd(1.0).Should().Be(0);
    }

    [Fact]
    public void RemoveLast_TransfersTheNewestObservationToAnotherRanker()
    {
        // The BruRankPlusMinus workflow: a realtime bar value changes sign, so the observation
        // must move from the negative ranker to the positive one.
        const int windowSize = 8;
        var negative = new SlidingWindowRanker<double>(windowSize);
        var positive = new SlidingWindowRanker<double>(windowSize);
        var negativeReference = new ReferenceSlidingWindowRanker<double>([], windowSize);
        var positiveReference = new ReferenceSlidingWindowRanker<double>([], windowSize);

        var random = new Random(4242);
        for (var i = 0; i < 400; i++)
        {
            var value = Math.Round(random.NextDouble() * 4 - 2, 1);
            var (ranker, reference) = value < 0 ? (negative, negativeReference) : (positive, positiveReference);
            ranker.GetRank(value).Should().Be(reference.GetRank(value));

            // The bar updates and flips sign
            var flipped = value == 0 ? 0.0 : -value;
            ranker.RemoveLast().Should().Be(reference.RemoveLast());

            var (otherRanker, otherReference) = flipped < 0 ? (negative, negativeReference) : (positive, positiveReference);
            otherRanker.GetRank(flipped).Should().Be(otherReference.GetRank(flipped));

            RankerInvariantChecker.AssertInvariants(negative, negativeReference.Values);
            RankerInvariantChecker.AssertInvariants(positive, positiveReference.Values);
        }
    }

    private static string Because(int seed, int operation, string op)
    {
        return $"seed={seed} operation={operation} op={op}";
    }

    private static double NextDuplicateHeavyDouble(Random random)
    {
        return random.Next(4) == 0 ? random.Next(3) : Math.Round(random.NextDouble() * 6, 1);
    }
}
