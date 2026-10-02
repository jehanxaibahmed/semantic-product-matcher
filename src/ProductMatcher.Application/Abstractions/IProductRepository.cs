using ProductMatcher.Domain.Products;

namespace ProductMatcher.Application.Abstractions;

public interface IProductRepository
{
    Task<Dictionary<string, Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken);

    /// <summary>Products with no embedding, or one produced by a different model.</summary>
    Task<IReadOnlyList<Product>> GetPendingEmbeddingAsync(string currentModel, int take, CancellationToken cancellationToken);

    Task<CatalogueStats> GetStatsAsync(string currentModel, CancellationToken cancellationToken);

    void Add(Product product);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record CatalogueStats(int Total, int Embedded, int Pending);
