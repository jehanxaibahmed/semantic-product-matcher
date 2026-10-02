using Microsoft.EntityFrameworkCore;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.Infrastructure.Persistence;

internal sealed class ProductRepository(MatcherDbContext db) : IProductRepository
{
    public async Task<Dictionary<string, Product>> GetBySkusAsync(
        IReadOnlyCollection<string> skus, CancellationToken cancellationToken)
    {
        if (skus.Count == 0)
        {
            return new Dictionary<string, Product>(StringComparer.Ordinal);
        }

        return await db.Products
            .Where(p => skus.Contains(p.Sku))
            .ToDictionaryAsync(p => p.Sku, StringComparer.Ordinal, cancellationToken);
    }

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        var normalized = Product.NormalizeSku(sku);
        return db.Products.SingleOrDefaultAsync(p => p.Sku == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetPendingEmbeddingAsync(
        string currentModel, int take, CancellationToken cancellationToken) =>
        await db.Products
            .Where(p => p.Embedding == null || p.EmbeddingModel != currentModel)
            .OrderBy(p => p.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<CatalogueStats> GetStatsAsync(string currentModel, CancellationToken cancellationToken)
    {
        var total = await db.Products.CountAsync(cancellationToken);
        var embedded = await db.Products
            .CountAsync(p => p.Embedding != null && p.EmbeddingModel == currentModel, cancellationToken);
        return new CatalogueStats(total, embedded, total - embedded);
    }

    public void Add(Product product) => db.Products.Add(product);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
