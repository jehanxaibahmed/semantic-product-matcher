using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductMatcher.Infrastructure.Persistence;
using ProductMatcher.IntegrationTests.Infrastructure;

namespace ProductMatcher.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class EmbeddingCacheTests(MatcherApiFactory factory) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();
        await client.SeedSampleCatalogueAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Repeated_queries_hit_the_cache_and_are_persisted()
    {
        using var client = factory.CreateClient();
        var before = await GetCacheAsync(client);

        await client.PostAsJsonAsync(new Uri("/api/match", UriKind.Relative), new { query = "cherry toms 3kg" });
        await client.PostAsJsonAsync(new Uri("/api/match", UriKind.Relative), new { query = "Cherry Toms 3kg please" });

        var after = await GetCacheAsync(client);
        Assert.Equal(1, after.Cache.Misses - before.Cache.Misses);
        Assert.Equal(1, after.Cache.MemoryHits - before.Cache.MemoryHits);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatcherDbContext>();
        Assert.True(await db.EmbeddingCache.AnyAsync(e => e.Text == "cherry toms 3kg"));
    }

    private static async Task<CacheResponse> GetCacheAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<CacheResponse>(new Uri("/api/embeddings/cache", UriKind.Relative)))!;

    private sealed record CacheResponse(string Model, CacheCounters Cache);

    private sealed record CacheCounters(long MemoryHits, long StoreHits, long Misses, long ProviderCalls, double HitRate);
}
