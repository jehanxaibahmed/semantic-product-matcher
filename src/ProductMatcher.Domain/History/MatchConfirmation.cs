namespace ProductMatcher.Domain.History;

/// <summary>A customer confirmed that their wording referred to a specific product. Append-only.</summary>
public sealed class MatchConfirmation
{
    public const int MaxCustomerIdLength = 64;

    private MatchConfirmation()
    {
        CustomerId = Query = NormalizedQuery = string.Empty;
    }

    public MatchConfirmation(string customerId, string query, string normalizedQuery, Guid productId, DateTimeOffset confirmedAt)
        : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedQuery);
        if (customerId.Trim().Length > MaxCustomerIdLength)
        {
            throw new ArgumentException($"Customer id must be at most {MaxCustomerIdLength} characters.", nameof(customerId));
        }

        Id = Guid.CreateVersion7();
        CustomerId = customerId.Trim();
        Query = query.Trim();
        NormalizedQuery = normalizedQuery;
        ProductId = productId;
        ConfirmedAt = confirmedAt;
    }

    public Guid Id { get; private set; }

    public string CustomerId { get; private set; }

    public string Query { get; private set; }

    public string NormalizedQuery { get; private set; }

    public Guid ProductId { get; private set; }

    public DateTimeOffset ConfirmedAt { get; private set; }
}
