using Microsoft.Extensions.AI;
using ProductMatcher.Application.Abstractions;

namespace ProductMatcher.Api.Endpoints;

internal static class EmbeddingEndpoints
{
    public static IEndpointRouteBuilder MapEmbeddingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/embeddings/info", (IEmbeddingGenerator<string, Embedding<float>> provider) =>
                Results.Ok(new { model = provider.GetService<EmbeddingGeneratorMetadata>()?.ProviderName ?? "unknown" }))
            .WithTags("Embeddings")
            .WithSummary("Embedding model info.");

        return app;
    }
}
