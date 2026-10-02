namespace ProductMatcher.Infrastructure.Embeddings.Caching;

/// <summary>A stored embedding keyed by a hash of model and text.</summary>
internal sealed class EmbeddingCacheEntry
{
    public required string Key { get; init; }

    public required string Model { get; init; }

    public required string Text { get; init; }

#pragma warning disable CA1819 // Vector payload.
    public required float[] Embedding { get; init; }
#pragma warning restore CA1819

    public required DateTimeOffset CreatedAt { get; init; }
}
