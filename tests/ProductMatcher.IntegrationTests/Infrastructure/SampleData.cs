namespace ProductMatcher.IntegrationTests.Infrastructure;

internal static class SampleData
{
    public static string CataloguePath => Path.Combine(AppContext.BaseDirectory, "data", "sample-catalogue.csv");
}
