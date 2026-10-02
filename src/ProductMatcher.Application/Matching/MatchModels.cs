using ProductMatcher.Domain.Matching;

namespace ProductMatcher.Application.Matching;

/// <summary>
/// A catalogue product proposed for a query. <see cref="Similarity"/> is cosine similarity from the
/// vector search; <see cref="Boost"/> comes from the customer's history; <see cref="Score"/> ranks the results.
/// </summary>
public sealed record MatchCandidate(
    Guid ProductId, string Sku, string Name, string Category, string Unit, double Similarity, double Boost = 0)
{
    public double Score => Math.Min(1, Similarity + Boost);
}

public sealed record MatchResult(
    string Query, string NormalizedQuery, MatchDecision Decision, IReadOnlyList<MatchCandidate> Candidates);

public sealed record BatchMatchResult(IReadOnlyList<MatchResult> Results, BatchSummary Summary)
{
    public static BatchMatchResult From(IReadOnlyList<MatchResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return new BatchMatchResult(results, new BatchSummary(
            results.Count(r => r.Decision.Band == ConfidenceBand.AutoAccept),
            results.Count(r => r.Decision.Band == ConfidenceBand.NeedsReview),
            results.Count(r => r.Decision.Band == ConfidenceBand.NoMatch)));
    }
}

public sealed record BatchSummary(int AutoAccepted, int NeedsReview, int NoMatch);
