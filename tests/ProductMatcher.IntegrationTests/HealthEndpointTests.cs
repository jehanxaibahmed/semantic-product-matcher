using System.Net;
using ProductMatcher.IntegrationTests.Infrastructure;

namespace ProductMatcher.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class HealthEndpointTests(MatcherApiFactory factory)
{
    [Fact]
    public async Task Health_returns_ok()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
