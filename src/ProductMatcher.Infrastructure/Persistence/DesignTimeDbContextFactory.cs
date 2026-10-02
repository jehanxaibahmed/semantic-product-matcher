using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductMatcher.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MatcherDbContext>
{
    public MatcherDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MatcherDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5434;Database=product_matcher;Username=matcher;Password=matcher",
                o => o.UseVector())
            .Options;
        return new MatcherDbContext(options);
    }
}
