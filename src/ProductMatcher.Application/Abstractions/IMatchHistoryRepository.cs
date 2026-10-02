using ProductMatcher.Application.Matching;
using ProductMatcher.Domain.History;

namespace ProductMatcher.Application.Abstractions;

public interface IMatchHistoryRepository
{
    void Add(MatchConfirmation confirmation);

    /// <summary>Confirmation counts for one customer: per product, and per (phrase, product) for the given phrases.</summary>
    Task<CustomerHistory> GetHistoryAsync(
        string customerId, IReadOnlyCollection<string> normalizedQueries, CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoryItem>> GetRecentAsync(string customerId, int take, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record HistoryItem(string Query, string NormalizedQuery, string Sku, string ProductName, DateTimeOffset ConfirmedAt);
