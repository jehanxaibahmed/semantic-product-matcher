using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Matching;
using ProductMatcher.Domain.History;

namespace ProductMatcher.UnitTests.Fakes;

internal sealed class InMemoryMatchHistoryRepository : IMatchHistoryRepository
{
    public List<MatchConfirmation> Items { get; } = [];

    public void Add(MatchConfirmation confirmation) => Items.Add(confirmation);

    public Task<CustomerHistory> GetHistoryAsync(
        string customerId, IReadOnlyCollection<string> normalizedQueries, CancellationToken cancellationToken)
    {
        var mine = Items.Where(i => i.CustomerId == customerId).ToList();
        if (mine.Count == 0)
        {
            return Task.FromResult(CustomerHistory.Empty);
        }

        return Task.FromResult(new CustomerHistory(
            mine.GroupBy(i => i.ProductId).ToDictionary(g => g.Key, g => g.Count()),
            mine.Where(i => normalizedQueries.Contains(i.NormalizedQuery))
                .GroupBy(i => (i.NormalizedQuery, i.ProductId))
                .ToDictionary(g => g.Key, g => g.Count())));
    }

    public Task<IReadOnlyList<HistoryItem>> GetRecentAsync(string customerId, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HistoryItem>>([]);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
