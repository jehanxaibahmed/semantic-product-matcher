using System.Net;
using System.Net.Http.Json;
using ProductMatcher.IntegrationTests.Infrastructure;

namespace ProductMatcher.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class MatchHistoryTests(MatcherApiFactory factory) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();
        await client.SeedSampleCatalogueAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Confirmations_teach_the_matcher_a_customers_own_wording()
    {
        using var client = factory.CreateClient();
        const string query = "the usual reds";

        // "reds" alone is ambiguous: peppers, onions, chillies. This customer always means red onions.
        var before = await MatchAsync(client, query, "bistro-42");
        Assert.NotEqual("FP-1009", before.Candidates[0].Sku);

        for (var i = 0; i < 3; i++)
        {
            var confirm = await client.PostAsJsonAsync(
                new Uri("/api/match/confirm", UriKind.Relative),
                new { customerId = "bistro-42", query = "2 boxes of reds", sku = "FP-1009" });
            Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        }

        var after = await MatchAsync(client, query, "bistro-42");
        Assert.Equal("FP-1009", after.Candidates[0].Sku);
        Assert.True(after.Candidates[0].Boost > 0);

        var otherCustomer = await MatchAsync(client, query, "cafe-7");
        Assert.Equal(before.Candidates[0].Sku, otherCustomer.Candidates[0].Sku);

        var history = await client.GetFromJsonAsync<List<HistoryItem>>(
            new Uri("/api/customers/bistro-42/history", UriKind.Relative));
        Assert.Equal(3, history!.Count);
        Assert.All(history, h => Assert.Equal("FP-1009", h.Sku));
    }

    [Fact]
    public async Task Confirming_an_unknown_sku_returns_404()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/api/match/confirm", UriKind.Relative),
            new { customerId = "bistro-42", query = "reds", sku = "NOPE-1" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<MatchResponse> MatchAsync(HttpClient client, string query, string customerId)
    {
        var response = await client.PostAsJsonAsync(
            new Uri("/api/match", UriKind.Relative), new { query, topK = 3, customerId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MatchResponse>())!;
    }

    private sealed record MatchResponse(string NormalizedQuery, List<Candidate> Candidates);

    private sealed record Candidate(string Sku, double Similarity, double Boost, double Score);

    private sealed record HistoryItem(string Query, string Sku, string ProductName);
}
