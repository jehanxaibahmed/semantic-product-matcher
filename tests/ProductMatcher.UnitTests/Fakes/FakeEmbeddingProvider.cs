using Microsoft.Extensions.AI;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.UnitTests.Fakes;

internal sealed class FakeEmbeddingProvider(string model = "fake-model") : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata { get; } = new(model);

    public List<IEnumerable<string>> Calls { get; } = [];

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls.Add(values.ToList());
        var embeddings = values.Select(_ => new Embedding<float>(new float[Product.EmbeddingDimensions])).ToList();
        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }
    
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return Metadata;
        }
        return this.GetType() == serviceType ? this : null;
    }
    
    public void Dispose() {}
}

internal sealed class FakeEmbeddingJobSignal : Application.Abstractions.IEmbeddingJobSignal
{
    public int Notifications { get; private set; }

    public void Notify() => Notifications++;
}
