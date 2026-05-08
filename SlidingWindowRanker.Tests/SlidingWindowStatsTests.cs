using FluentAssertions;
using Xunit.Abstractions;

namespace SlidingWindowRanker.Tests;

public class SlidingWindowStatsTests
{
    private readonly ITestOutputHelper _output;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SlidingWindowStatsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GetValueAtRank_ReturnsCorrectValue_ForKnownSortedWindow()
    {
        var initialValues = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var stats = new SlidingWindowStats<double>(initialValues);

        stats.GetValueAtRank(0.0).Should().Be(1);
        stats.GetValueAtRank(0.5).Should().Be(6); // index = 5 → 6
        stats.GetValueAtRank(0.9).Should().Be(10);
    }

    [Fact]
    public void GetMedian_ReturnsMiddleValue_ForOddSizedWindow()
    {
        var initialValues = new List<double> { 5, 1, 4, 2, 3 };
        var stats = new SlidingWindowStats<double>(initialValues);

        // Sorted: [1, 2, 3, 4, 5]; median index = (int)(0.5 * 5) = 2 → 3
        stats.GetMedian().Should().Be(3);
    }

    [Fact]
    public void GetIQR_ReturnsCorrectRange()
    {
        var initialValues = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var stats = new SlidingWindowStats<double>(initialValues);

        // Q25 index = (int)(0.25 * 10) = 2 → value 3
        // Q75 index = (int)(0.75 * 10) = 7 → value 8
        stats.GetQ25().Should().Be(3);
        stats.GetQ75().Should().Be(8);
        stats.GetIQR().Should().Be(5);
    }

    [Fact]
    public void GetZScore_ReturnsApproximatelyZero_ForMedianValue()
    {
        var initialValues = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var stats = new SlidingWindowStats<double>(initialValues);
        var medianBefore = stats.GetMedian();

        var z = stats.GetZScore(medianBefore);

        z.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void GetZScore_IsPositive_ForValueAboveMedian()
    {
        var initialValues = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var stats = new SlidingWindowStats<double>(initialValues);

        var z = stats.GetZScore(20.0);

        z.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetZScore_IsNegative_ForValueBelowMedian()
    {
        var initialValues = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var stats = new SlidingWindowStats<double>(initialValues);

        var z = stats.GetZScore(-10.0);

        z.Should().BeLessThan(0);
    }

    [Fact]
    public void GetZScore_ReturnsZero_WhenIqrIsZero()
    {
        var initialValues = new List<double> { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };
        var stats = new SlidingWindowStats<double>(initialValues);

        var z = stats.GetZScore(7.0);

        z.Should().Be(0);
    }

    [Fact]
    public void GetZScoreNoAdd_DoesNotChangeWindow()
    {
        var initialValues = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var stats = new SlidingWindowStats<double>(initialValues);
        var countBefore = stats.Count;
        var medianBefore = stats.GetMedian();

        stats.GetZScoreNoAdd(100.0);

        stats.Count.Should().Be(countBefore);
        stats.GetMedian().Should().Be(medianBefore);
    }

    [Fact]
    public void GetZScore_WindowSlides_CorrectlyAcrossManyBars()
    {
        const int windowSize = 100;
        var rng = new Random(42);
        var initialValues = Enumerable.Range(0, windowSize).Select(_ => rng.NextDouble() * 100).ToList();
        var stats = new SlidingWindowStats<double>(initialValues);

        for (var i = 0; i < 10000; i++)
        {
            var nextValue = rng.NextDouble() * 100;
            stats.GetZScore(nextValue);
        }

        stats.Count.Should().Be(windowSize);
        var median = stats.GetMedian();
        median.Should().BeInRange(0, 100);
        var iqr = stats.GetIQR();
        iqr.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetZScore_AgreesWithMadFormula_OnNormalData_WithinTolerance()
    {
        const int windowSize = 1000;
        var rng = new Random(42);
        var values = new List<double>(windowSize);
        for (var i = 0; i < windowSize; i++)
        {
            // Box-Muller for approximate normal
            var u1 = 1.0 - rng.NextDouble();
            var u2 = 1.0 - rng.NextDouble();
            values.Add(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }

        var stats = new SlidingWindowStats<double>(new List<double>(values));
        var iqr = stats.GetIQR();
        var median = stats.GetMedian();

        // MAD-based equivalent
        var madValues = values.Select(v => Math.Abs(v - median)).OrderBy(v => v).ToList();
        var mad = madValues[(int)(0.5 * windowSize)];

        var iqrSigma = SlidingWindowStats<double>.IQRScale * iqr;
        var madSigma = 1.4826 * mad;

        // For ~1000 normal samples the two should agree to within ~10%
        (Math.Abs(iqrSigma - madSigma) / madSigma).Should().BeLessThan(0.10);
    }
}
