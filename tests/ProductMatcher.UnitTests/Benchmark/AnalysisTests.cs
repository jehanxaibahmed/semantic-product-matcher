using ProductMatcher.Benchmark;
using ProductMatcher.Domain.Matching;

namespace ProductMatcher.UnitTests.Benchmark;

public class AnalysisTests
{
    private static QueryOutcome Outcome(string? expected, int? rank, double score, double? margin = 0.2, string kind = "typo") =>
        new(new BenchmarkQuery("q", expected, kind), "q", rank, "X", score, margin, ConfidenceBand.NeedsReview, 1);

    [Fact]
    public void Accuracy_counts_only_in_catalogue_queries()
    {
        var outcomes = new[] { Outcome("A", 1, 0.9), Outcome("B", 2, 0.8), Outcome("C", null, 0.5), Outcome(null, null, 0.1) };

        var accuracy = Analysis.Accuracy(outcomes);

        Assert.Equal(3, accuracy.InCatalogue);
        Assert.Equal(1 / 3.0, accuracy.Top1, 6);
        Assert.Equal(2 / 3.0, accuracy.HitAt3, 6);
        Assert.Equal((1 + 0.5) / 3, accuracy.Mrr, 6);
    }

    [Fact]
    public void Calibration_picks_the_widest_threshold_that_keeps_precision_and_rejects_out_of_catalogue()
    {
        var outcomes = new[]
        {
            Outcome("A", 1, 0.90), Outcome("B", 1, 0.80), Outcome("C", 1, 0.62),
            Outcome("D", 2, 0.55), // wrong top-1: must stay below the auto-accept threshold
            Outcome(null, null, 0.30), // out-of-catalogue
        };

        var calibration = Analysis.Calibrate(outcomes);

        Assert.NotNull(calibration.AutoAccept);
        Assert.Equal(0.60, calibration.AutoAccept.AutoAcceptScore, 6);
        Assert.Equal(1.0, calibration.AutoAccept.Precision);
        Assert.Equal(0.75, calibration.AutoAccept.Coverage, 6);
    }

    [Fact]
    public void Review_floor_never_discards_more_than_two_percent_of_correct_matches()
    {
        var outcomes = Enumerable.Range(0, 50).Select(i => Outcome($"S{i}", 1, 0.30 + (i * 0.01)))
            .Append(Outcome(null, null, 0.10))
            .ToList();

        var calibration = Analysis.Calibrate(outcomes);

        Assert.True(calibration.CorrectMatchesLost <= 0.02);
        Assert.Equal(1.0, calibration.OutOfCatalogueNoMatchRecall);
    }

    [Theory]
    [InlineData(50, 3)]
    [InlineData(95, 5)]
    [InlineData(100, 5)]
    public void Percentile_uses_nearest_rank(double p, double expected)
    {
        Assert.Equal(expected, Analysis.Percentile([5, 1, 4, 2, 3], p));
    }
}
