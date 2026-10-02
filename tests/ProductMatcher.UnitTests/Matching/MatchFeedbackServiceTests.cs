using ProductMatcher.Application.Matching;
using ProductMatcher.Domain.Products;
using ProductMatcher.UnitTests.Fakes;

namespace ProductMatcher.UnitTests.Matching;

public class MatchFeedbackServiceTests
{
    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryMatchHistoryRepository _history = new();

    private MatchFeedbackService CreateService() => new(_products, _history, TimeProvider.System);

    [Fact]
    public async Task Records_the_normalised_query_against_the_product()
    {
        var product = new Product("FP-1001", "Red Peppers Large", "Fresh Produce", "5kg box", null);
        _products.Add(product);

        var ok = await CreateService().ConfirmAsync(" acme ", "2 boxes of reds", "fp-1001", default);

        Assert.True(ok);
        var entry = Assert.Single(_history.Items);
        Assert.Equal(("acme", "reds", product.Id), (entry.CustomerId, entry.NormalizedQuery, entry.ProductId));
    }

    [Fact]
    public async Task Unknown_sku_returns_false()
    {
        Assert.False(await CreateService().ConfirmAsync("acme", "reds", "NOPE", default));
        Assert.Empty(_history.Items);
    }

    [Theory]
    [InlineData("", "reds", "FP-1")]
    [InlineData("acme", "", "FP-1")]
    [InlineData("acme", "reds", "")]
    public async Task Rejects_missing_fields(string customerId, string query, string sku)
    {
        await Assert.ThrowsAsync<MatchValidationException>(() => CreateService().ConfirmAsync(customerId, query, sku, default));
    }
}
