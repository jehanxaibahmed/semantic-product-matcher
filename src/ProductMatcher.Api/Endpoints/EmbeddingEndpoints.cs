using ProductMatcher.Application.Abstractions;
using ProductMatcher.Infrastructure.Embeddings.Caching;

namespace ProductMatcher.Api.Endpoints;

internal static class EmbeddingEndpoints
{
    public static IEndpointRouteBuilder MapEmbeddingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/embeddings/cache", (EmbeddingCacheMetrics metrics, IEmbeddingProvider provider) =>
                Results.Ok(new { model = provider.Model, cache = metrics.Snapshot() }))
            .WithTags("Embeddings")
            .WithSummary("Embedding cache hit and miss counters since start-up.");

        return app;
    }
}
