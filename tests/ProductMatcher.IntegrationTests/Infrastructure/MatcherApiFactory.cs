using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductMatcher.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ProductMatcher.IntegrationTests.Infrastructure;

/// <summary>Runs the API against a throwaway pgvector container with the offline embedder.</summary>
public sealed class MatcherApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("pgvector/pgvector:pg17").Build();

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Matcher", _db.GetConnectionString());
        builder.UseSetting("Embeddings:Provider", "Local");
        builder.UseSetting("Embeddings:BackgroundJob", "false");
    }

    /// <summary>Clears all data so each test starts from an empty catalogue.</summary>
    public async Task ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatcherDbContext>();
        var tables = db.Model.GetEntityTypes().Select(t => $"\"{t.GetTableName()}\"");
#pragma warning disable EF1002 // Table names come from the EF model, not user input.
        await db.Database.ExecuteSqlRawAsync($"TRUNCATE {string.Join(", ", tables)} CASCADE");
#pragma warning restore EF1002
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<MatcherApiFactory>
{
    public const string Name = "api";
}
