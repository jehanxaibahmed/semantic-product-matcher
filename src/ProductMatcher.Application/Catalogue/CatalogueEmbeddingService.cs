using Microsoft.Extensions.Logging;
using ProductMatcher.Application.Abstractions;

namespace ProductMatcher.Application.Catalogue;

/// <summary>Embeds products that have no vector, or one from an older model, in batches.</summary>
public sealed partial class CatalogueEmbeddingService(
    IProductRepository products,
    IEmbeddingProvider embeddings,
    TimeProvider clock,
    ILogger<CatalogueEmbeddingService> logger)
{
    public const int DefaultBatchSize = 100;

    /// <summary>Embeds every pending product. Returns how many were embedded.</summary>
    public async Task<int> EmbedPendingAsync(CancellationToken cancellationToken, int batchSize = DefaultBatchSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var total = 0;
        while (true)
        {
            var batch = await products.GetPendingEmbeddingAsync(embeddings.Model, batchSize, cancellationToken)
                .ConfigureAwait(false);
            if (batch.Count == 0)
            {
                break;
            }

            var vectors = await embeddings.EmbedAsync(batch.Select(p => p.SearchText).ToList(), cancellationToken)
                .ConfigureAwait(false);
            if (vectors.Count != batch.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding provider returned {vectors.Count} vectors for {batch.Count} inputs.");
            }

            var now = clock.GetUtcNow();
            for (var i = 0; i < batch.Count; i++)
            {
                batch[i].SetEmbedding(vectors[i], embeddings.Model, now);
            }

            await products.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            total += batch.Count;
            LogBatch(batch.Count, total);
        }

        return total;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Embedded {BatchCount} products ({Total} so far)")]
    private partial void LogBatch(int batchCount, int total);
}
