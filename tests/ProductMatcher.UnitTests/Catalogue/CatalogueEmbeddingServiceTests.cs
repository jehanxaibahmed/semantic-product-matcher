using Microsoft.Extensions.Logging.Abstractions;
using ProductMatcher.Application.Catalogue;
using ProductMatcher.Domain.Products;
using ProductMatcher.UnitTests.Fakes;

namespace ProductMatcher.UnitTests.Catalogue;

public class CatalogueEmbeddingServiceTests
{
    [Fact]
    public async Task Embeds_pending_products_in_batches()
    {
        var repository = new InMemoryProductRepository();
        for (var i = 0; i < 5; i++)
        {
            repository.Add(new Product($"SKU-{i}", $"Product {i}", "Cat", "each", null));
        }

        var provider = new FakeEmbeddingProvider();
        var service = new CatalogueEmbeddingService(
            repository, provider, TimeProvider.System, NullLogger<CatalogueEmbeddingService>.Instance);

        var embedded = await service.EmbedPendingAsync(default, batchSize: 2);

        Assert.Equal(5, embedded);
        Assert.Equal([2, 2, 1], provider.Calls.Select(c => c.Count()));
        Assert.All(repository.Items, p => Assert.Equal("fake-model", p.EmbeddingModel));
    }

    [Fact]
    public async Task Re_embeds_products_from_a_different_model()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product("SKU-1", "Product", "Cat", "each", null);
        product.SetEmbedding(new float[Product.EmbeddingDimensions], "old-model", DateTimeOffset.UnixEpoch);
        repository.Add(product);

        var service = new CatalogueEmbeddingService(
            repository, new FakeEmbeddingProvider("new-model"), TimeProvider.System,
            NullLogger<CatalogueEmbeddingService>.Instance);

        Assert.Equal(1, await service.EmbedPendingAsync(default));
        Assert.Equal("new-model", product.EmbeddingModel);
    }
}
