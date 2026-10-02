using Microsoft.EntityFrameworkCore;
using ProductMatcher.Domain.Products;
using ProductMatcher.Infrastructure.Embeddings.Caching;

namespace ProductMatcher.Infrastructure.Persistence;

public sealed class MatcherDbContext(DbContextOptions<MatcherDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    internal DbSet<EmbeddingCacheEntry> EmbeddingCache => Set<EmbeddingCacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatcherDbContext).Assembly);
    }
}
