using System.Net;
using System.Net.Http.Json;
using System.Text;
using ProductMatcher.IntegrationTests.Infrastructure;

namespace ProductMatcher.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class CatalogueEndpointTests(MatcherApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Import_then_embed_populates_the_catalogue()
    {
        using var client = factory.CreateClient();
        using var file = new MultipartFormDataContent();
        using var csv = new StreamContent(File.OpenRead(SampleData.CataloguePath));
        file.Add(csv, "file", "sample-catalogue.csv");

        var import = await client.PostAsync(new Uri("/api/catalogue/import", UriKind.Relative), file);
        var result = await import.Content.ReadFromJsonAsync<ImportResponse>();

        Assert.Equal(HttpStatusCode.OK, import.StatusCode);
        Assert.Equal(151, result!.Created);
        Assert.Empty(result.Errors);

        var embed = await client.PostAsync(new Uri("/api/catalogue/embed", UriKind.Relative), null);
        var embedded = await embed.Content.ReadFromJsonAsync<EmbedResponse>();
        Assert.Equal(151, embedded!.Embedded);

        var stats = await client.GetFromJsonAsync<StatsResponse>(new Uri("/api/catalogue/stats", UriKind.Relative));
        Assert.Equal(new StatsResponse(151, 151, 0), stats);
    }

    [Fact]
    public async Task Reimporting_a_changed_row_marks_only_that_product_for_re_embedding()
    {
        using var client = factory.CreateClient();
        await PostCsvAsync(client, "sku,name,category\nA-1,Red Peppers,Produce\nA-2,Onions,Produce\n");
        await client.PostAsync(new Uri("/api/catalogue/embed", UriKind.Relative), null);

        var second = await PostCsvAsync(client, "sku,name,category\nA-1,Red Peppers Large,Produce\nA-2,Onions,Produce\n");

        Assert.Equal((0, 1, 1), (second.Created, second.Updated, second.Unchanged));
        var stats = await client.GetFromJsonAsync<StatsResponse>(new Uri("/api/catalogue/stats", UriKind.Relative));
        Assert.Equal(1, stats!.Pending);

        var product = await client.GetFromJsonAsync<ProductResponse>(new Uri("/api/products/a-1", UriKind.Relative));
        Assert.Equal("Red Peppers Large", product!.Name);
        Assert.False(product.Embedded);
    }

    [Fact]
    public async Task Import_with_a_bad_header_returns_400()
    {
        using var client = factory.CreateClient();
        using var body = new StringContent("code,title\nA,B\n", Encoding.UTF8, "text/csv");

        var response = await client.PostAsync(new Uri("/api/catalogue/import", UriKind.Relative), body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<ImportResponse> PostCsvAsync(HttpClient client, string csv)
    {
        using var body = new StringContent(csv, Encoding.UTF8, "text/csv");
        var response = await client.PostAsync(new Uri("/api/catalogue/import", UriKind.Relative), body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportResponse>())!;
    }

    private sealed record ImportResponse(int Created, int Updated, int Unchanged, IReadOnlyList<object> Errors);

    private sealed record EmbedResponse(int Embedded);

    private sealed record StatsResponse(int Total, int Embedded, int Pending);

    private sealed record ProductResponse(string Sku, string Name, bool Embedded);
}
