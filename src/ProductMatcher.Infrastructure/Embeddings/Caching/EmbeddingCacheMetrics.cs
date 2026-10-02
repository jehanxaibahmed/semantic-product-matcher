namespace ProductMatcher.Infrastructure.Embeddings.Caching;

/// <summary>Process-wide cache counters, reported by <c>GET /api/embeddings/cache</c>.</summary>
public sealed class EmbeddingCacheMetrics
{
    private long _memoryHits;
    private long _storeHits;
    private long _misses;
    private long _providerCalls;

    public void RecordMemoryHits(int count) => Interlocked.Add(ref _memoryHits, count);

    public void RecordStoreHits(int count) => Interlocked.Add(ref _storeHits, count);

    public void RecordMisses(int count) => Interlocked.Add(ref _misses, count);

    public void RecordProviderCall() => Interlocked.Increment(ref _providerCalls);

    public EmbeddingCacheSnapshot Snapshot()
    {
        var memory = Interlocked.Read(ref _memoryHits);
        var store = Interlocked.Read(ref _storeHits);
        var misses = Interlocked.Read(ref _misses);
        var lookups = memory + store + misses;
        return new EmbeddingCacheSnapshot(
            memory, store, misses, Interlocked.Read(ref _providerCalls),
            lookups == 0 ? 0 : Math.Round((double)(memory + store) / lookups, 4));
    }
}

public sealed record EmbeddingCacheSnapshot(long MemoryHits, long StoreHits, long Misses, long ProviderCalls, double HitRate);
