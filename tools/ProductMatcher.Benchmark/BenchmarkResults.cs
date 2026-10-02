using ProductMatcher.Domain.Matching;
using ProductMatcher.Infrastructure.Embeddings.Caching;

namespace ProductMatcher.Benchmark;

/// <summary>What happened to one query. <see cref="Rank"/> is the 1-based position of the expected SKU, if found.</summary>
internal sealed record QueryOutcome(
    BenchmarkQuery Query,
    string NormalizedQuery,
    int? Rank,
    string? TopSku,
    double TopScore,
    double? Margin,
    ConfidenceBand Band,
    double LatencyMs)
{
    public bool InCatalogue => Query.ExpectedSku is not null;

    public bool Top1Correct => Rank == 1;
}

internal sealed record HistoryOutcome(HistoryCase Case, string Phrasing, string? TopSkuBefore, string? TopSkuAfter)
{
    public bool CorrectBefore => TopSkuBefore == Case.ExpectedSku;

    public bool CorrectAfter => TopSkuAfter == Case.ExpectedSku;
}

internal sealed record ProviderResult(
    string Provider,
    string Model,
    int CatalogueSize,
    double CatalogueEmbedMs,
    IReadOnlyList<QueryOutcome> Cold,
    IReadOnlyList<double> WarmLatenciesMs,
    EmbeddingCacheSnapshot CacheBeforeCold,
    EmbeddingCacheSnapshot CacheAfterCold,
    EmbeddingCacheSnapshot CacheAfterWarm,
    IReadOnlyList<HistoryOutcome> History,
    ConfiguredThresholds Thresholds);

internal sealed record ConfiguredThresholds(double AutoAcceptScore, double MinMargin, double ReviewScore);
