namespace ProductMatcher.Application.Matching;

/// <summary>A catalogue product proposed for a query. <see cref="Score"/> is cosine similarity in [-1, 1].</summary>
public sealed record MatchCandidate(Guid ProductId, string Sku, string Name, string Category, string Unit, double Score);

public sealed record MatchResult(string Query, string NormalizedQuery, IReadOnlyList<MatchCandidate> Candidates);
