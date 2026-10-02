using ProductMatcher.Application.Abstractions;

namespace ProductMatcher.Application.Matching;

/// <summary>Finds the catalogue products closest to customer-written text.</summary>
public sealed class MatchService(IEmbeddingProvider embeddings, IProductSearch search)
{
    public const int MaxTopK = 50;
    public const int MaxQueryLength = 500;
    public const int MaxBatchSize = 100;

    public async Task<MatchResult> MatchAsync(string query, int topK, CancellationToken cancellationToken)
    {
        var results = await MatchManyAsync([query], topK, cancellationToken).ConfigureAwait(false);
        return results[0];
    }

    /// <summary>Matches several order lines with one embedding call.</summary>
    public async Task<IReadOnlyList<MatchResult>> MatchManyAsync(
        IReadOnlyList<string> queries, int topK, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queries);
        Validate(queries, topK);

        var normalized = queries.Select(QueryNormalizer.Normalize).ToList();
        var vectors = await embeddings.EmbedAsync(normalized, cancellationToken).ConfigureAwait(false);

        var results = new List<MatchResult>(queries.Count);
        for (var i = 0; i < queries.Count; i++)
        {
            var candidates = await search.SearchAsync(vectors[i], topK, cancellationToken).ConfigureAwait(false);
            results.Add(new MatchResult(queries[i], normalized[i], candidates));
        }

        return results;
    }

    private static void Validate(IReadOnlyList<string> queries, int topK)
    {
        if (queries.Count == 0 || queries.Count > MaxBatchSize)
        {
            throw new MatchValidationException($"Provide between 1 and {MaxBatchSize} queries.");
        }

        if (topK is < 1 or > MaxTopK)
        {
            throw new MatchValidationException($"topK must be between 1 and {MaxTopK}.");
        }

        foreach (var query in queries)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new MatchValidationException("Query must not be empty.");
            }

            if (query.Length > MaxQueryLength)
            {
                throw new MatchValidationException($"Query must be at most {MaxQueryLength} characters.");
            }
        }
    }
}
