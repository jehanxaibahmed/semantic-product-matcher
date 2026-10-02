using Microsoft.Extensions.Options;
using ProductMatcher.Application.Abstractions;

namespace ProductMatcher.Application.Matching;

/// <summary>Finds the catalogue products closest to customer-written text, personalised by match history.</summary>
public sealed class MatchService(
    IEmbeddingProvider embeddings,
    IProductSearch search,
    IMatchHistoryRepository history,
    IOptions<RerankingOptions> reranking,
    IOptions<ConfidenceOptions> confidence)
{
    public const int MaxTopK = 50;
    public const int MaxQueryLength = 500;
    public const int MaxBatchSize = 100;
    private const int MaxCandidatePool = 100;

    public async Task<MatchResult> MatchAsync(string query, int topK, string? customerId, CancellationToken cancellationToken)
    {
        var results = await MatchManyAsync([query], topK, customerId, cancellationToken).ConfigureAwait(false);
        return results[0];
    }

    /// <summary>Matches several order lines with one embedding call and one history lookup.</summary>
    public async Task<IReadOnlyList<MatchResult>> MatchManyAsync(
        IReadOnlyList<string> queries, int topK, string? customerId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queries);
        Validate(queries, topK, customerId);

        var normalized = queries.Select(QueryNormalizer.Normalize).ToList();
        var vectors = await embeddings.EmbedAsync(normalized, cancellationToken).ConfigureAwait(false);

        var options = reranking.Value;
        var thresholds = confidence.Value.For(embeddings.Model);
        var customerHistory = string.IsNullOrWhiteSpace(customerId)
            ? CustomerHistory.Empty
            : await history.GetHistoryAsync(customerId.Trim(), normalized.Distinct().ToList(), cancellationToken)
                .ConfigureAwait(false);
        var personalised = !ReferenceEquals(customerHistory, CustomerHistory.Empty);
        var pool = personalised ? Math.Min(MaxCandidatePool, topK * Math.Max(1, options.CandidatePoolMultiplier)) : topK;

        var results = new List<MatchResult>(queries.Count);
        for (var i = 0; i < queries.Count; i++)
        {
            var pinned = customerHistory.ProductsConfirmedFor(normalized[i]);
            var candidates = await search.SearchAsync(vectors[i], pool, pinned, cancellationToken).ConfigureAwait(false);
            if (personalised)
            {
                candidates = CustomerReranker.Rerank(candidates, normalized[i], customerHistory, options);
            }

            var top = candidates.Take(topK).ToList();
            results.Add(new MatchResult(queries[i], normalized[i], ConfidenceClassifier.Classify(top, thresholds), top));
        }

        return results;
    }

    private static void Validate(IReadOnlyList<string> queries, int topK, string? customerId)
    {
        if (queries.Count == 0 || queries.Count > MaxBatchSize)
        {
            throw new MatchValidationException($"Provide between 1 and {MaxBatchSize} queries.");
        }

        if (topK is < 1 or > MaxTopK)
        {
            throw new MatchValidationException($"topK must be between 1 and {MaxTopK}.");
        }

        if (customerId is { Length: > Domain.History.MatchConfirmation.MaxCustomerIdLength })
        {
            throw new MatchValidationException(
                $"customerId must be at most {Domain.History.MatchConfirmation.MaxCustomerIdLength} characters.");
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
