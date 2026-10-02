namespace ProductMatcher.Application.Matching;

public sealed class RerankingOptions
{
    public const string SectionName = "Reranking";

    /// <summary>Maximum boost for a product the customer confirmed for this exact phrase.</summary>
    public double PhraseBoost { get; set; } = 0.25;

    /// <summary>Maximum boost for a product the customer confirmed for any phrase.</summary>
    public double ProductBoost { get; set; } = 0.05;

    /// <summary>Confirmations needed for the full boost; fewer give a proportional share.</summary>
    public int SaturationCount { get; set; } = 3;

    /// <summary>How many vector candidates to fetch per requested result before re-ranking.</summary>
    public int CandidatePoolMultiplier { get; set; } = 4;
}
