using Microsoft.Extensions.AI;

namespace ProductMatcher.Infrastructure.Embeddings;

public sealed class ZeroPaddingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> inner, int targetDimensions) : IEmbeddingGenerator<string, Embedding<float>>
{
    public void Dispose() => inner.Dispose();

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var result = await inner.GenerateAsync(values, options, cancellationToken);
        var paddedList = new List<Embedding<float>>(result.Count);
        
        foreach (var original in result)
        {
            var vector = original.Vector.ToArray();
            if (vector.Length < targetDimensions)
            {
                var padded = new float[targetDimensions];
                Array.Copy(vector, padded, vector.Length);
                var newEmbedding = new Embedding<float>(padded)
                {
                    CreatedAt = original.CreatedAt,
                    ModelId = original.ModelId
                };
                paddedList.Add(newEmbedding);
            }
            else if (vector.Length > targetDimensions)
            {
                throw new ArgumentException($"Embedding has {vector.Length} dimensions, which exceeds the target {targetDimensions}.");
            }
            else
            {
                paddedList.Add(original);
            }
        }
        
        return new GeneratedEmbeddings<Embedding<float>>(paddedList)
        {
            Usage = result.Usage
        };
    }
    
    public object? GetService(Type serviceType, object? serviceKey = null) => inner.GetService(serviceType, serviceKey);
}
