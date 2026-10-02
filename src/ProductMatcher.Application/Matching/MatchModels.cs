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

public sealed record MatchResult(string Query, string NormalizedQuery, IReadOnlyList<MatchCandidate> Candidates);
