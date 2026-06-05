namespace SlidingWindowRanker.Tests;

internal static class BruZScoreReplayHelper
{
    internal static (List<double> Warmup, List<double> Stream) BuildWarmupAndStream(int windowSize, int seed, int streamLength)
    {
        var rng = new Random(seed);
        var warmup = new List<double>(windowSize);
        while (warmup.Count < windowSize)
        {
            var delta = NextDelta(rng);
            if (delta > 0)
            {
                warmup.Add(Math.Log(delta));
            }
        }

        var stream = new List<double>(streamLength);
        while (stream.Count < streamLength)
        {
            var delta = NextDelta(rng);
            if (delta > 0)
            {
                stream.Add(Math.Log(delta));
            }
        }

        return (warmup, stream);
    }

    private static double NextDelta(Random rng)
    {
        return rng.NextDouble() switch
        {
            < 0.1 => rng.Next(1, 5),
            < 0.2 => rng.Next(1000, 5000),
            _ => rng.Next(5, 500)
        };
    }
}
