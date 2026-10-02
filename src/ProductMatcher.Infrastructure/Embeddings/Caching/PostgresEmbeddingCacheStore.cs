using Microsoft.EntityFrameworkCore;
using Pgvector;
using ProductMatcher.Infrastructure.Persistence;

namespace ProductMatcher.Infrastructure.Embeddings.Caching;

internal sealed class PostgresEmbeddingCacheStore(IDbContextFactory<MatcherDbContext> contexts) : IEmbeddingCacheStore
{
    public async Task<IReadOnlyDictionary<string, float[]>> GetManyAsync(
        IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return new Dictionary<string, float[]>();
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.EmbeddingCache
            .AsNoTracking()
            .Where(e => keys.Contains(e.Key))
            .ToDictionaryAsync(e => e.Key, e => e.Embedding, cancellationToken);
    }

    public async Task PutManyAsync(IReadOnlyList<EmbeddingCacheEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return;
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var entry in entries)
        {
            // Concurrent requests may embed the same text; the first writer wins.
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO embedding_cache ("Key", "Model", "Text", "Embedding", "CreatedAt")
                VALUES ({entry.Key}, {entry.Model}, {entry.Text}, {new Vector(entry.Embedding)}, {entry.CreatedAt})
                ON CONFLICT ("Key") DO NOTHING
                """, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
