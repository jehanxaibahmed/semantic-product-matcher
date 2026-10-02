using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using ProductMatcher.Application.Abstractions;

namespace ProductMatcher.Infrastructure.Embeddings.Caching;

/// <summary>
/// Decorator that checks an in-memory LRU, then the durable store, and only sends the
/// remaining texts to the real provider. Duplicate texts in a batch are embedded once.
/// </summary>
internal sealed class CachedEmbeddingProvider(
    IEmbeddingProvider inner,
    IMemoryCache memory,
    IEmbeddingCacheStore store,
    EmbeddingCacheMetrics metrics,
    TimeProvider clock) : IEmbeddingProvider
{
    public string Model => inner.Model;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        var keys = texts.Select(t => CacheKey(inner.Model, t)).ToArray();
        var found = new Dictionary<string, float[]>(StringComparer.Ordinal);

        foreach (var key in keys.Distinct())
        {
            if (memory.TryGetValue(key, out float[]? cached) && cached is not null)
            {
                found[key] = cached;
            }
        }

        metrics.RecordMemoryHits(found.Count);

        var notInMemory = keys.Distinct().Where(k => !found.ContainsKey(k)).ToList();
        if (notInMemory.Count > 0)
        {
            var stored = await store.GetManyAsync(notInMemory, cancellationToken);
            metrics.RecordStoreHits(stored.Count);
            foreach (var (key, vector) in stored)
            {
                found[key] = vector;
                Remember(key, vector);
            }
        }

        var missing = keys
            .Select((key, i) => (key, text: texts[i]))
            .Where(x => !found.ContainsKey(x.key))
            .DistinctBy(x => x.key)
            .ToList();

        if (missing.Count > 0)
        {
            metrics.RecordMisses(missing.Count);
            metrics.RecordProviderCall();

            var vectors = await inner.EmbedAsync(missing.Select(m => m.text).ToList(), cancellationToken);
            var now = clock.GetUtcNow();
            var entries = new List<EmbeddingCacheEntry>(missing.Count);
            for (var i = 0; i < missing.Count; i++)
            {
                found[missing[i].key] = vectors[i];
                Remember(missing[i].key, vectors[i]);
                entries.Add(new EmbeddingCacheEntry
                {
                    Key = missing[i].key,
                    Model = inner.Model,
                    Text = missing[i].text,
                    Embedding = vectors[i],
                    CreatedAt = now,
                });
            }

            await store.PutManyAsync(entries, cancellationToken);
        }

        return keys.Select(k => found[k]).ToList();
    }

    internal static string CacheKey(string model, string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(model + "\n" + text));
        return Convert.ToHexStringLower(bytes);
    }

    private void Remember(string key, float[] vector) =>
        memory.Set(key, vector, new MemoryCacheEntryOptions { Size = 1 });
}
