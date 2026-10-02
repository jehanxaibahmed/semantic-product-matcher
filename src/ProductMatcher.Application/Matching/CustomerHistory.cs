namespace ProductMatcher.Application.Matching;

/// <summary>What a customer has confirmed before, used to re-rank their results.</summary>
public sealed class CustomerHistory
{
    public static readonly CustomerHistory Empty = new(new Dictionary<Guid, int>(), new Dictionary<(string, Guid), int>());

    public CustomerHistory(
        IReadOnlyDictionary<Guid, int> productConfirmations,
        IReadOnlyDictionary<(string NormalizedQuery, Guid ProductId), int> phraseConfirmations)
    {
        ProductConfirmations = productConfirmations;
        PhraseConfirmations = phraseConfirmations;
    }

    /// <summary>How often each product was confirmed for any wording.</summary>
    public IReadOnlyDictionary<Guid, int> ProductConfirmations { get; }

    /// <summary>How often each product was confirmed for one exact normalised phrase.</summary>
    public IReadOnlyDictionary<(string NormalizedQuery, Guid ProductId), int> PhraseConfirmations { get; }

    public IReadOnlyList<Guid> ProductsConfirmedFor(string normalizedQuery) =>
        PhraseConfirmations.Keys.Where(k => k.NormalizedQuery == normalizedQuery).Select(k => k.ProductId).ToList();
}
