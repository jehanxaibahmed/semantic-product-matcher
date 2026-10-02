using Microsoft.EntityFrameworkCore;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Matching;
using ProductMatcher.Domain.History;

namespace ProductMatcher.Infrastructure.Persistence;

internal sealed class MatchHistoryRepository(MatcherDbContext db) : IMatchHistoryRepository
{
    public void Add(MatchConfirmation confirmation) => db.MatchHistory.Add(confirmation);

    public async Task<CustomerHistory> GetHistoryAsync(
        string customerId, IReadOnlyCollection<string> normalizedQueries, CancellationToken cancellationToken)
    {
        var products = await db.MatchHistory
            .Where(m => m.CustomerId == customerId)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count, cancellationToken);

        if (products.Count == 0)
        {
            return CustomerHistory.Empty;
        }

        var phrases = await db.MatchHistory
            .Where(m => m.CustomerId == customerId && normalizedQueries.Contains(m.NormalizedQuery))
            .GroupBy(m => new { m.NormalizedQuery, m.ProductId })
            .Select(g => new { g.Key.NormalizedQuery, g.Key.ProductId, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new CustomerHistory(
            products,
            phrases.ToDictionary(p => (p.NormalizedQuery, p.ProductId), p => p.Count));
    }

    public async Task<IReadOnlyList<HistoryItem>> GetRecentAsync(string customerId, int take, CancellationToken cancellationToken) =>
        await db.MatchHistory
            .Where(m => m.CustomerId == customerId)
            .OrderByDescending(m => m.ConfirmedAt)
            .Take(take)
            .Join(db.Products, m => m.ProductId, p => p.Id,
                (m, p) => new HistoryItem(m.Query, m.NormalizedQuery, p.Sku, p.Name, m.ConfirmedAt))
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
