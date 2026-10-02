namespace ProductMatcher.Infrastructure.Embeddings.Caching;

/// <summary>Durable second-level cache for embeddings.</summary>
internal interface IEmbeddingCacheStore
{
    Task<IReadOnlyDictionary<string, float[]>> GetManyAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);

    Task PutManyAsync(IReadOnlyList<EmbeddingCacheEntry> entries, CancellationToken cancellationToken);
}
