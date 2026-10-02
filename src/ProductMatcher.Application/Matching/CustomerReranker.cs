namespace ProductMatcher.Application.Matching;

/// <summary>
/// Adds a boost to products the customer confirmed before, then sorts again. The boost grows with
/// the confirmation count up to <see cref="RerankingOptions.SaturationCount"/>, so a single
/// mistaken confirmation does not dominate.
/// </summary>
public static class CustomerReranker
{
    public static IReadOnlyList<MatchCandidate> Rerank(
        IReadOnlyList<MatchCandidate> candidates,
        string normalizedQuery,
        CustomerHistory history,
        RerankingOptions options)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(options);

        return candidates
            .Select(c => c with { Boost = BoostFor(c.ProductId, normalizedQuery, history, options) })
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.Similarity)
            .ToList();
    }

    private static double BoostFor(Guid productId, string normalizedQuery, CustomerHistory history, RerankingOptions options)
    {
        history.PhraseConfirmations.TryGetValue((normalizedQuery, productId), out var phraseCount);
        history.ProductConfirmations.TryGetValue(productId, out var productCount);

        return options.PhraseBoost * Saturate(phraseCount, options.SaturationCount)
             + options.ProductBoost * Saturate(productCount, options.SaturationCount);
    }

    private static double Saturate(int count, int saturation) =>
        saturation <= 0 ? (count > 0 ? 1 : 0) : Math.Min(count, saturation) / (double)saturation;
}
