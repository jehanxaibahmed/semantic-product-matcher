using ProductMatcher.Domain.Products;
using ProductMatcher.Infrastructure.Embeddings;

namespace ProductMatcher.UnitTests.Embeddings;

public class LocalHashingEmbeddingProviderTests
{
    private static float Cosine(float[] a, float[] b) => a.Zip(b, (x, y) => x * y).Sum();

    [Fact]
    public void Produces_unit_length_vectors_of_the_stored_dimension()
    {
        var vector = LocalHashingEmbeddingProvider.Embed("Red Peppers Large");

        Assert.Equal(Product.EmbeddingDimensions, vector.Length);
        Assert.Equal(1f, MathF.Sqrt(vector.Sum(v => v * v)), 3);
    }

    [Fact]
    public void Is_deterministic()
    {
        Assert.Equal(
            LocalHashingEmbeddingProvider.Embed("cherry tomatoes"),
            LocalHashingEmbeddingProvider.Embed("cherry tomatoes"));
    }

    [Fact]
    public void Similar_text_scores_higher_than_unrelated_text()
    {
        var target = LocalHashingEmbeddingProvider.Embed("Red Peppers Large. Fresh Produce. 5kg box");
        var misspelt = LocalHashingEmbeddingProvider.Embed("large red pepers");
        var unrelated = LocalHashingEmbeddingProvider.Embed("nitrile gloves");

        Assert.True(Cosine(target, misspelt) > Cosine(target, unrelated));
    }
}
