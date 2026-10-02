using ProductMatcher.Domain.Matching;

namespace ProductMatcher.Application.Matching;

/// <summary>Maps a ranked candidate list to a decision band, giving the reason in plain words.</summary>
public static class ConfidenceClassifier
{
    public static MatchDecision Classify(IReadOnlyList<MatchCandidate> candidates, ConfidenceThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(thresholds);

        if (candidates.Count == 0)
        {
            return new MatchDecision(ConfidenceBand.NoMatch, null, 0, null, "No embedded products to compare against.");
        }

        var top = candidates[0];
        double? margin = candidates.Count > 1 ? Math.Round(top.Score - candidates[1].Score, 4) : null;

        if (top.Score < thresholds.ReviewScore)
        {
            return new MatchDecision(ConfidenceBand.NoMatch, null, top.Score, margin,
                $"Best score {top.Score:F2} is below the review threshold {thresholds.ReviewScore:F2}.");
        }

        if (top.Score < thresholds.AutoAcceptScore)
        {
            return new MatchDecision(ConfidenceBand.NeedsReview, top.Sku, top.Score, margin,
                $"Best score {top.Score:F2} is below the auto-accept threshold {thresholds.AutoAcceptScore:F2}.");
        }

        if (margin is { } m && m < thresholds.MinMargin)
        {
            return new MatchDecision(ConfidenceBand.NeedsReview, top.Sku, top.Score, margin,
                $"Top two candidates are within {m:F2} of each other ({candidates[0].Sku}, {candidates[1].Sku}).");
        }

        return new MatchDecision(ConfidenceBand.AutoAccept, top.Sku, top.Score, margin, "Strong, unambiguous match.");
    }
}

/// <summary>The suggested outcome for one query. <see cref="Sku"/> is null for <c>NoMatch</c>.</summary>
public sealed record MatchDecision(ConfidenceBand Band, string? Sku, double TopScore, double? Margin, string Reason);
