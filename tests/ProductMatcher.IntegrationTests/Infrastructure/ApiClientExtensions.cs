namespace ProductMatcher.IntegrationTests.Infrastructure;

internal static class ApiClientExtensions
{
    /// <summary>Imports the sample catalogue and embeds it synchronously.</summary>
    public static async Task SeedSampleCatalogueAsync(this HttpClient client)
    {
        using var file = new MultipartFormDataContent();
        using var csv = new StreamContent(File.OpenRead(SampleData.CataloguePath));
        file.Add(csv, "file", "sample-catalogue.csv");

        (await client.PostAsync(new Uri("/api/catalogue/import", UriKind.Relative), file)).EnsureSuccessStatusCode();
        (await client.PostAsync(new Uri("/api/catalogue/embed", UriKind.Relative), null)).EnsureSuccessStatusCode();
    }
}
