namespace ProductMatcher.Domain.Matching;

/// <summary>What should happen to a match result.</summary>
public enum ConfidenceBand
{
    /// <summary>Strong, unambiguous match; safe to apply without a person checking it.</summary>
    AutoAccept,

    /// <summary>Plausible match, or several close candidates; a person should pick.</summary>
    NeedsReview,

    /// <summary>Nothing in the catalogue is close enough.</summary>
    NoMatch,
}
