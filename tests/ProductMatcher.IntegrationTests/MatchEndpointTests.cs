using System.Net;
using System.Net.Http.Json;
using ProductMatcher.IntegrationTests.Infrastructure;

namespace ProductMatcher.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class MatchEndpointTests(MatcherApiFactory factory) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();
        await client.SeedSampleCatalogueAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("2 boxes of the large red peppers", "FP-1001")]
    [InlineData("nitrile gloves large", "CL-9503")]
    [InlineData("semi skimmed milk x6", "DA-2002")]
    public async Task Returns_the_expected_product_first(string query, string expectedSku)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(new Uri("/api/match", UriKind.Relative), new { query, topK = 3 });
        var result = await response.Content.ReadFromJsonAsync<MatchResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, result!.Candidates.Count);
        Assert.Equal(expectedSku, result.Candidates[0].Sku);
        Assert.True(result.Candidates[0].Score >= result.Candidates[1].Score);
    }

    [Fact]
    public async Task Batch_returns_one_result_per_line_in_order()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/match/batch", UriKind.Relative),
            new { queries = new[] { "red onions", "basmati rice" }, topK = 1 });
        var results = await response.Content.ReadFromJsonAsync<List<MatchResponse>>();

        Assert.Equal(["FP-1009", "DG-6001"], results!.Select(r => r.Candidates[0].Sku));
    }

    [Fact]
    public async Task Invalid_top_k_returns_400()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/match", UriKind.Relative), new { query = "peppers", topK = 500 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record MatchResponse(string Query, string NormalizedQuery, List<Candidate> Candidates);

    private sealed record Candidate(string Sku, string Name, double Score);
}
