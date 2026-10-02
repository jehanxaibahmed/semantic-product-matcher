using OpenAI.Embeddings;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.Infrastructure.Embeddings;

internal sealed class OpenAiEmbeddingProvider(EmbeddingClient client, string model) : IEmbeddingProvider
{
    private static readonly EmbeddingGenerationOptions Options = new() { Dimensions = Product.EmbeddingDimensions };

    public string Model { get; } = model;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var result = await client.GenerateEmbeddingsAsync(texts, Options, cancellationToken);
        return result.Value
            .OrderBy(e => e.Index)
            .Select(e => e.ToFloats().ToArray())
            .ToList();
    }
}
