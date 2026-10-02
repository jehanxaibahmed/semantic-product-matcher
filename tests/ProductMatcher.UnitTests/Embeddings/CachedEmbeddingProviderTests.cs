using Microsoft.Extensions.Caching.Memory;
using ProductMatcher.Infrastructure.Embeddings.Caching;
using ProductMatcher.UnitTests.Fakes;

namespace ProductMatcher.UnitTests.Embeddings;

public sealed class CachedEmbeddingProviderTests : IDisposable
{
    private readonly FakeEmbeddingProvider _inner = new();
    private readonly InMemoryEmbeddingCacheStore _store = new();
    private readonly EmbeddingCacheMetrics _metrics = new();
    private MemoryCache _memory = new(new MemoryCacheOptions { SizeLimit = 100 });

    private CachedEmbeddingProvider CreateProvider() =>
        new(_inner, _memory, _store, _metrics, TimeProvider.System);

    public void Dispose() => _memory.Dispose();

    [Fact]
    public async Task Second_call_for_the_same_text_is_served_from_memory()
    {
        var provider = CreateProvider();

        await provider.EmbedAsync(["red peppers"], default);
        await provider.EmbedAsync(["red peppers"], default);

        Assert.Single(_inner.Calls);
        Assert.Equal(1, _metrics.Snapshot().MemoryHits);
    }

    [Fact]
    public async Task Falls_back_to_the_durable_store_after_a_restart()
    {
        await CreateProvider().EmbedAsync(["red peppers"], default);
        _memory.Dispose();
        _memory = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });

        await CreateProvider().EmbedAsync(["red peppers"], default);

        Assert.Single(_inner.Calls);
        Assert.Equal(1, _metrics.Snapshot().StoreHits);
    }

    [Fact]
    public async Task Only_uncached_and_distinct_texts_reach_the_provider_and_order_is_preserved()
    {
        var provider = CreateProvider();
        await provider.EmbedAsync(["milk"], default);

        var vectors = await provider.EmbedAsync(["eggs", "milk", "eggs", "butter"], default);

        Assert.Equal(["eggs", "butter"], _inner.Calls[1]);
        Assert.Equal(4, vectors.Count);
        Assert.Same(vectors[0], vectors[2]);
    }

    [Fact]
    public void Cache_key_depends_on_model_and_text()
    {
        var a = CachedEmbeddingProvider.CacheKey("m1", "milk");

        Assert.Equal(64, a.Length);
        Assert.Equal(a, CachedEmbeddingProvider.CacheKey("m1", "milk"));
        Assert.NotEqual(a, CachedEmbeddingProvider.CacheKey("m2", "milk"));
        Assert.NotEqual(a, CachedEmbeddingProvider.CacheKey("m1", "Milk"));
    }

    private sealed class InMemoryEmbeddingCacheStore : IEmbeddingCacheStore
    {
        private readonly Dictionary<string, float[]> _entries = [];

        public Task<IReadOnlyDictionary<string, float[]>> GetManyAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, float[]>>(
                keys.Where(_entries.ContainsKey).ToDictionary(k => k, k => _entries[k]));

        public Task PutManyAsync(IReadOnlyList<EmbeddingCacheEntry> entries, CancellationToken cancellationToken)
        {
            foreach (var e in entries)
            {
                _entries.TryAdd(e.Key, e.Embedding);
            }

            return Task.CompletedTask;
        }
    }
}
