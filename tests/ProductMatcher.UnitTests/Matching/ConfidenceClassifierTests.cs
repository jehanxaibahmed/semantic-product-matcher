using ProductMatcher.Application.Matching;
using ProductMatcher.Domain.Matching;

namespace ProductMatcher.UnitTests.Matching;

public class ConfidenceClassifierTests
{
    private static readonly ConfidenceThresholds Thresholds = new() { AutoAcceptScore = 0.6, MinMargin = 0.05, ReviewScore = 0.3 };

    private static MatchCandidate[] Candidates(params double[] scores) =>
        scores.Select((s, i) => new MatchCandidate(Guid.NewGuid(), $"SKU-{i}", "n", "c", "u", s)).ToArray();

    [Theory]
    [InlineData(0.85, 0.60, ConfidenceBand.AutoAccept)]
    [InlineData(0.85, 0.82, ConfidenceBand.NeedsReview)] // ambiguous: margin too small
    [InlineData(0.55, 0.20, ConfidenceBand.NeedsReview)] // clear lead but weak score
    [InlineData(0.25, 0.10, ConfidenceBand.NoMatch)]
    public void Classifies_by_score_and_margin(double top, double second, ConfidenceBand expected)
    {
        var decision = ConfidenceClassifier.Classify(Candidates(top, second), Thresholds);

        Assert.Equal(expected, decision.Band);
    }

    [Fact]
    public void No_match_has_no_sku()
    {
        var decision = ConfidenceClassifier.Classify(Candidates(0.1, 0.05), Thresholds);

        Assert.Null(decision.Sku);
        Assert.Contains("below the review threshold", decision.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Single_strong_candidate_is_auto_accepted()
    {
        var decision = ConfidenceClassifier.Classify(Candidates(0.9), Thresholds);

        Assert.Equal(ConfidenceBand.AutoAccept, decision.Band);
        Assert.Null(decision.Margin);
    }

    [Fact]
    public void Empty_candidate_list_is_no_match()
    {
        Assert.Equal(ConfidenceBand.NoMatch, ConfidenceClassifier.Classify([], Thresholds).Band);
    }

    [Fact]
    public void Thresholds_are_resolved_per_model_with_a_default()
    {
        var options = new ConfidenceOptions();
        options.Models["model-a"] = new ConfidenceThresholds { AutoAcceptScore = 0.9 };

        Assert.Equal(0.9, options.For("MODEL-A").AutoAcceptScore);
        Assert.Same(options.Default, options.For("unknown"));
    }

    [Fact]
    public void Batch_summary_counts_each_band()
    {
        MatchResult Result(ConfidenceBand band) =>
            new("q", "q", new MatchDecision(band, null, 0, null, ""), []);

        var batch = BatchMatchResult.From(
            [Result(ConfidenceBand.AutoAccept), Result(ConfidenceBand.AutoAccept), Result(ConfidenceBand.NoMatch)]);

        Assert.Equal(new BatchSummary(2, 0, 1), batch.Summary);
    }
}
