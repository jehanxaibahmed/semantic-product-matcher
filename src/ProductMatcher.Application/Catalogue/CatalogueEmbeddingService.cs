using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ProductMatcher.Application.Abstractions;

namespace ProductMatcher.Application.Catalogue;

/// <summary>Embeds products that have no vector, or one from an older model, in batches.</summary>
public sealed partial class CatalogueEmbeddingService(
    IProductRepository products,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    TimeProvider clock,
    ILogger<CatalogueEmbeddingService> logger)
{
    public const int DefaultBatchSize = 100;

    /// <summary>Embeds every pending product. Returns how many were embedded.</summary>
    public async Task<int> EmbedPendingAsync(CancellationToken cancellationToken, int batchSize = DefaultBatchSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var total = 0;
        var modelId = embeddings.GetService<EmbeddingGeneratorMetadata>()?.ProviderName ?? "unknown";
        while (true)
        {
            var batch = await products.GetPendingEmbeddingAsync(modelId, batchSize, cancellationToken)
                .ConfigureAwait(false);
            if (batch.Count == 0)
            {
                break;
            }

            var generated = await embeddings.GenerateAsync(batch.Select(p => p.SearchText).ToList(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var vectors = generated.Select(e => e.Vector.ToArray()).ToList();
            if (vectors.Count != batch.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding provider returned {vectors.Count} vectors for {batch.Count} inputs.");
            }

            var now = clock.GetUtcNow();
            for (var i = 0; i < batch.Count; i++)
            {
                batch[i].SetEmbedding(vectors[i], modelId, now);
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
