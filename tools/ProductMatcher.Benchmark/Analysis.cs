using ProductMatcher.Domain.Matching;

namespace ProductMatcher.Benchmark;

internal sealed record AccuracySummary(
    int InCatalogue, double Top1, double HitAt3, double HitAt5, double Mrr,
    IReadOnlyDictionary<string, (int Count, double Top1)> ByKind);

internal sealed record BandSummary(
    int AutoAccepted, double AutoAcceptPrecision, double AutoAcceptCoverage, int WrongAutoAccepts,
    int NeedsReview, int NoMatchInCatalogue, int OutOfCatalogue, int OutOfCatalogueNoMatch);

internal sealed record SweepRow(double AutoAcceptScore, double MinMargin, double Precision, double Coverage, int OutOfCatalogueAutoAccepted);

internal sealed record Calibration(SweepRow? AutoAccept, double ReviewScore, double OutOfCatalogueNoMatchRecall, double CorrectMatchesLost);

/// <summary>Turns raw outcomes into the numbers in docs/benchmark.md.</summary>
internal static class Analysis
{
    // Stricter than a production target because thresholds are fitted on the same queries they are scored on.
    public const double TargetPrecision = 0.98;
    private const double MaxCorrectLostToNoMatch = 0.02;

    public static AccuracySummary Accuracy(IReadOnlyList<QueryOutcome> outcomes)
    {
        var inCat = outcomes.Where(o => o.InCatalogue).ToList();
        var byKind = inCat.GroupBy(o => o.Query.Kind)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (g.Count(), Share(g, o => o.Top1Correct)));

        return new AccuracySummary(
            inCat.Count,
            Share(inCat, o => o.Top1Correct),
            Share(inCat, o => o.Rank <= 3),
            Share(inCat, o => o.Rank <= 5),
            inCat.Count == 0 ? 0 : inCat.Average(o => o.Rank is { } r ? 1.0 / r : 0),
            byKind);
    }

    public static BandSummary Bands(IReadOnlyList<QueryOutcome> outcomes)
    {
        var inCat = outcomes.Where(o => o.InCatalogue).ToList();
        var ooc = outcomes.Where(o => !o.InCatalogue).ToList();
        var auto = outcomes.Where(o => o.Band == ConfidenceBand.AutoAccept).ToList();
        var autoCorrect = auto.Count(o => o.Top1Correct);

        return new BandSummary(
            auto.Count,
            auto.Count == 0 ? 0 : (double)autoCorrect / auto.Count,
            inCat.Count == 0 ? 0 : (double)autoCorrect / inCat.Count,
            auto.Count - autoCorrect,
            outcomes.Count(o => o.Band == ConfidenceBand.NeedsReview),
            inCat.Count(o => o.Band == ConfidenceBand.NoMatch),
            ooc.Count,
            ooc.Count(o => o.Band == ConfidenceBand.NoMatch));
    }

    /// <summary>Auto-accept precision and coverage for a grid of thresholds, judged by top score and margin alone.</summary>
    public static IReadOnlyList<SweepRow> Sweep(IReadOnlyList<QueryOutcome> outcomes)
    {
        var inCatalogue = outcomes.Count(o => o.InCatalogue);
        var rows = new List<SweepRow>();
        foreach (var score in Grid(0.20, 0.90, 0.05))
        {
            foreach (var margin in new[] { 0.0, 0.02, 0.05, 0.10 })
            {
                var auto = outcomes.Where(o => o.TopScore >= score && (o.Margin is null || o.Margin >= margin)).ToList();
                var correct = auto.Count(o => o.Top1Correct);
                rows.Add(new SweepRow(
                    score, margin,
                    auto.Count == 0 ? 1 : (double)correct / auto.Count,
                    inCatalogue == 0 ? 0 : (double)correct / inCatalogue,
                    auto.Count(o => !o.InCatalogue)));
            }
        }

        return rows;
    }

    /// <summary>
    /// Auto-accept: the widest-coverage setting with precision at or above <see cref="TargetPrecision"/> and no
    /// out-of-catalogue query accepted (ties go to the stricter setting). Review: the highest floor that drops at
    /// most 2% of correct matches into NoMatch.
    /// </summary>
    public static Calibration Calibrate(IReadOnlyList<QueryOutcome> outcomes)
    {
        var best = Sweep(outcomes)
            .Where(r => r.Precision >= TargetPrecision && r.OutOfCatalogueAutoAccepted == 0 && r.Coverage > 0)
            .OrderByDescending(r => r.Coverage)
            .ThenByDescending(r => r.AutoAcceptScore)
            .ThenByDescending(r => r.MinMargin)
            .FirstOrDefault();

        var correct = outcomes.Where(o => o.Top1Correct).ToList();
        var ooc = outcomes.Where(o => !o.InCatalogue).ToList();
        var ceiling = best?.AutoAcceptScore ?? 0.9;
        var review = Grid(0.05, ceiling, 0.025)
            .Where(r => Share(correct, o => o.TopScore < r) <= MaxCorrectLostToNoMatch)
            .DefaultIfEmpty(0.05)
            .Max();

        return new Calibration(best, review, Share(ooc, o => o.TopScore < review), Share(correct, o => o.TopScore < review));
    }

    public static double Percentile(IReadOnlyList<double> values, double p)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.Order().ToArray();
        var rank = (int)Math.Ceiling(p / 100 * sorted.Length) - 1;
        return sorted[Math.Clamp(rank, 0, sorted.Length - 1)];
    }

    private static double Share<T>(IEnumerable<T> items, Func<T, bool> predicate)
    {
        var list = items as IReadOnlyCollection<T> ?? items.ToList();
        return list.Count == 0 ? 0 : (double)list.Count(predicate) / list.Count;
    }

    private static IEnumerable<double> Grid(double from, double to, double step)
    {
        for (var v = from; v <= to + 1e-9; v += step)
        {
            yield return Math.Round(v, 3);
        }
    }
}
