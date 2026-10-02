using ProductMatcher.Application.Matching;

namespace ProductMatcher.UnitTests.Matching;

public class CustomerRerankerTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly RerankingOptions Options = new() { PhraseBoost = 0.3, ProductBoost = 0.06, SaturationCount = 3 };

    private static MatchCandidate Candidate(Guid id, string sku, double similarity) =>
        new(id, sku, sku, "c", "u", similarity);

    private static CustomerHistory History(int phraseCountForB = 0, int productCountForB = 0) => new(
        new Dictionary<Guid, int> { [B] = productCountForB },
        new Dictionary<(string, Guid), int> { [("peppers", B)] = phraseCountForB });

    [Fact]
    public void Boost_grows_with_confirmations_and_saturates()
    {
        var candidates = new[] { Candidate(A, "A", 0.8), Candidate(B, "B", 0.7) };

        var once = CustomerReranker.Rerank(candidates, "peppers", History(1, 1), Options).Single(c => c.Sku == "B");
        var many = CustomerReranker.Rerank(candidates, "peppers", History(10, 10), Options).Single(c => c.Sku == "B");

        Assert.Equal(0.1 + 0.02, once.Boost, 6);
        Assert.Equal(0.3 + 0.06, many.Boost, 6);
    }

    [Fact]
    public void A_single_confirmation_does_not_override_a_much_better_match()
    {
        var candidates = new[] { Candidate(A, "A", 0.95), Candidate(B, "B", 0.5) };

        var result = CustomerReranker.Rerank(candidates, "peppers", History(1, 1), Options);

        Assert.Equal("A", result[0].Sku);
    }

    [Fact]
    public void Phrase_boost_applies_only_to_the_same_phrase()
    {
        var candidates = new[] { Candidate(B, "B", 0.7) };

        var result = CustomerReranker.Rerank(candidates, "something else", History(3, 3), Options);

        Assert.Equal(0.06, result[0].Boost, 6);
    }

    [Fact]
    public void Score_is_capped_at_one()
    {
        var result = CustomerReranker.Rerank([Candidate(B, "B", 0.9)], "peppers", History(3, 3), Options);

        Assert.Equal(1.0, result[0].Score);
        Assert.Equal(0.9, result[0].Similarity);
    }
}
