namespace ProductMatcher.Application.Matching;

public sealed class ConfidenceOptions
{
    public const string SectionName = "Confidence";

    /// <summary>Used when the active embedding model has no entry in <see cref="Models"/>.</summary>
    public ConfidenceThresholds Default { get; set; } = new();

    /// <summary>
    /// Thresholds per embedding model. Similarity scales differ between models, so each one is
    /// calibrated on its own. See docs/benchmark.md.
    /// </summary>
#pragma warning disable CA2227 // Bound from configuration.
    public Dictionary<string, ConfidenceThresholds> Models { get; set; } = new(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227

    public ConfidenceThresholds For(string model) =>
        Models.TryGetValue(model, out var thresholds) ? thresholds : Default;
}

public sealed class ConfidenceThresholds
{
    /// <summary>Minimum top score for <c>AutoAccept</c>.</summary>
    public double AutoAcceptScore { get; set; } = 0.60;

    /// <summary>Minimum lead of the top score over the runner-up for <c>AutoAccept</c>.</summary>
    public double MinMargin { get; set; } = 0.05;

    /// <summary>Below this top score the result is <c>NoMatch</c>.</summary>
    public double ReviewScore { get; set; } = 0.30;
}
