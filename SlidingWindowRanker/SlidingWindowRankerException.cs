namespace SlidingWindowRanker;

[Serializable]
public class SlidingWindowRankerException : Exception
{
    public SlidingWindowRankerException()
    {
    }

    public SlidingWindowRankerException(string message)
        : base(message)
    {
    }

    public SlidingWindowRankerException(string message, Exception inner)
        : base(message, inner)
    {
    }
}