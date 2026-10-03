using Microsoft.Extensions.Options;
using ProductMatcher.Application.Matching;
using ProductMatcher.Domain.History;
using ProductMatcher.UnitTests.Fakes;

namespace ProductMatcher.UnitTests.Matching;

public sealed class MatchServiceTests : IDisposable
{
    private static readonly MatchCandidate RedLarge = new(Guid.NewGuid(), "FP-1001", "Red Peppers Large", "Fresh Produce", "5kg box", 0.90);
    private static readonly MatchCandidate RedSmall = new(Guid.NewGuid(), "FP-1002", "Red Peppers Small", "Fresh Produce", "5kg box", 0.85);
    private static readonly MatchCandidate Chillies = new(Guid.NewGuid(), "FP-1006", "Red Chillies", "Fresh Produce", "1kg bag", 0.62);

    private readonly FakeEmbeddingProvider _embeddings = new();
    private readonly InMemoryMatchHistoryRepository _history = new();

    private MatchService CreateService(FakeProductSearch search) =>
        new(_embeddings, search, _history, Options.Create(new RerankingOptions()), Options.Create(new ConfidenceOptions()));

    [Fact]
    public async Task Embeds_the_normalised_query()
    {
        var result = await CreateService(new FakeProductSearch(RedLarge)).MatchAsync("2 boxes of the large red peppers", 5, null, default);

        Assert.Equal("large red peppers", result.NormalizedQuery);
        Assert.Equal(["large red peppers"], _embeddings.Calls.Single());
        Assert.Equal("FP-1001", result.Candidates.Single().Sku);
    }

    [Fact]
    public async Task Batch_uses_a_single_embedding_call()
    {
        var search = new FakeProductSearch(RedLarge);

        var results = await CreateService(search).MatchManyAsync(["red peppers", "milk", "eggs"], 3, null, default);

        Assert.Equal(3, results.Count);
        Assert.Single(_embeddings.Calls);
        Assert.Equal(3, search.Calls);
    }

    [Fact]
    public async Task Customer_history_promotes_their_usual_product()
    {
        _history.Add(new MatchConfirmation("acme", "red peppers", "red peppers", RedSmall.ProductId, DateTimeOffset.UnixEpoch));

        var result = await CreateService(new FakeProductSearch(RedLarge, RedSmall)).MatchAsync("red peppers", 2, "acme", default);

        Assert.Equal(["FP-1002", "FP-1001"], result.Candidates.Select(c => c.Sku));
        Assert.True(result.Candidates[0].Boost > 0);
    }

    [Fact]
    public async Task Confirmed_product_outside_the_nearest_results_is_still_considered()
    {
        _history.Add(new MatchConfirmation("acme", "reds", "reds", Chillies.ProductId, DateTimeOffset.UnixEpoch));
        _history.Add(new MatchConfirmation("acme", "reds", "reds", Chillies.ProductId, DateTimeOffset.UnixEpoch));
        _history.Add(new MatchConfirmation("acme", "reds", "reds", Chillies.ProductId, DateTimeOffset.UnixEpoch));

        // topK 1 gives a pool of 4; five fillers rank above the chillies, so only pinning brings them in.
        var fillers = Enumerable.Range(0, 5)
            .Select(i => new MatchCandidate(Guid.NewGuid(), $"F-{i}", "Filler", "c", "u", 0.69 - (i * 0.01)));
        var search = new FakeProductSearch([RedLarge, RedSmall, Chillies, .. fillers]);

        var result = await CreateService(search).MatchAsync("reds", 1, "acme", default);

        Assert.Equal("FP-1006", result.Candidates.Single().Sku);
    }

    [Fact]
    public async Task Other_customers_history_has_no_effect()
    {
        _history.Add(new MatchConfirmation("acme", "red peppers", "red peppers", RedSmall.ProductId, DateTimeOffset.UnixEpoch));
        var search = new FakeProductSearch(RedLarge, RedSmall);

        var result = await CreateService(search).MatchAsync("red peppers", 2, "globex", default);

        Assert.Equal("FP-1001", result.Candidates[0].Sku);
        Assert.Equal([2], search.RequestedTopK);
    }

    [Theory]
    [InlineData("", 5)]
    [InlineData("peppers", 0)]
    [InlineData("peppers", 51)]
    public async Task Rejects_invalid_requests(string query, int topK)
    {
        await Assert.ThrowsAsync<MatchValidationException>(
            () => CreateService(new FakeProductSearch()).MatchAsync(query, topK, null, default));
    }

    [Fact]
    public async Task Rejects_overlong_queries()
    {
        await Assert.ThrowsAsync<MatchValidationException>(
            () => CreateService(new FakeProductSearch()).MatchAsync(new string('a', MatchService.MaxQueryLength + 1), 5, null, default));
    }
    
    public void Dispose()
    {
        _embeddings.Dispose();
    }
}
