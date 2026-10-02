using ProductMatcher.Application.Matching;
using ProductMatcher.UnitTests.Fakes;

namespace ProductMatcher.UnitTests.Matching;

public class MatchServiceTests
{
    private static readonly MatchCandidate Peppers = new(Guid.NewGuid(), "FP-1001", "Red Peppers Large", "Fresh Produce", "5kg box", 0.9);

    [Fact]
    public async Task Embeds_the_normalised_query()
    {
        var embeddings = new FakeEmbeddingProvider();
        var service = new MatchService(embeddings, new FakeProductSearch(Peppers));

        var result = await service.MatchAsync("2 boxes of the large red peppers", 5, default);

        Assert.Equal("large red peppers", result.NormalizedQuery);
        Assert.Equal(["large red peppers"], embeddings.Calls.Single());
        Assert.Equal("FP-1001", result.Candidates.Single().Sku);
    }

    [Fact]
    public async Task Batch_uses_a_single_embedding_call()
    {
        var embeddings = new FakeEmbeddingProvider();
        var search = new FakeProductSearch(Peppers);
        var service = new MatchService(embeddings, search);

        var results = await service.MatchManyAsync(["red peppers", "milk", "eggs"], 3, default);

        Assert.Equal(3, results.Count);
        Assert.Single(embeddings.Calls);
        Assert.Equal(3, search.Calls);
    }

    [Theory]
    [InlineData("", 5)]
    [InlineData("peppers", 0)]
    [InlineData("peppers", 51)]
    public async Task Rejects_invalid_requests(string query, int topK)
    {
        var service = new MatchService(new FakeEmbeddingProvider(), new FakeProductSearch());

        await Assert.ThrowsAsync<MatchValidationException>(() => service.MatchAsync(query, topK, default));
    }

    [Fact]
    public async Task Rejects_overlong_queries()
    {
        var service = new MatchService(new FakeEmbeddingProvider(), new FakeProductSearch());

        await Assert.ThrowsAsync<MatchValidationException>(
            () => service.MatchAsync(new string('a', MatchService.MaxQueryLength + 1), 5, default));
    }
}
