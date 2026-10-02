using Microsoft.EntityFrameworkCore;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.Infrastructure.Persistence;

public sealed class MatcherDbContext(DbContextOptions<MatcherDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatcherDbContext).Assembly);
    }
}
