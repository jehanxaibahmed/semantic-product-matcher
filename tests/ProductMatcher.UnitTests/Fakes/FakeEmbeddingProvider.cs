using ProductMatcher.Application.Abstractions;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.UnitTests.Fakes;

internal sealed class FakeEmbeddingProvider(string model = "fake-model") : IEmbeddingProvider
{
    public string Model { get; } = model;

    public List<IReadOnlyList<string>> Calls { get; } = [];

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        Calls.Add(texts);
        IReadOnlyList<float[]> vectors = texts.Select(_ => new float[Product.EmbeddingDimensions]).ToList();
        return Task.FromResult(vectors);
    }
}

internal sealed class FakeEmbeddingJobSignal : Application.Abstractions.IEmbeddingJobSignal
{
    public int Notifications { get; private set; }

    public void Notify() => Notifications++;
}
