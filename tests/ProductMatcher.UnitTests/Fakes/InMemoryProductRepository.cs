using ProductMatcher.Application.Abstractions;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.UnitTests.Fakes;

internal sealed class InMemoryProductRepository : IProductRepository
{
    public List<Product> Items { get; } = [];

    public int SaveCount { get; private set; }

    public Task<Dictionary<string, Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Where(p => skus.Contains(p.Sku)).ToDictionary(p => p.Sku));

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(p => p.Sku == Product.NormalizeSku(sku)));

    public Task<IReadOnlyList<Product>> GetPendingEmbeddingAsync(string currentModel, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>(
            Items.Where(p => !p.HasEmbedding || p.EmbeddingModel != currentModel).Take(take).ToList());

    public Task<CatalogueStats> GetStatsAsync(string currentModel, CancellationToken cancellationToken)
    {
        var embedded = Items.Count(p => p.HasEmbedding && p.EmbeddingModel == currentModel);
        return Task.FromResult(new CatalogueStats(Items.Count, embedded, Items.Count - embedded));
    }

    public void Add(Product product) => Items.Add(product);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
