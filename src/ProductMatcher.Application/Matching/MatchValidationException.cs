namespace ProductMatcher.Application.Matching;

/// <summary>The match request was invalid; the API maps this to HTTP 400.</summary>
public sealed class MatchValidationException : Exception
{
    public MatchValidationException()
    {
    }

    public MatchValidationException(string message)
        : base(message)
    {
    }

    public MatchValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
