using Microsoft.EntityFrameworkCore;
using Pgvector;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Matching;

namespace ProductMatcher.Infrastructure.Persistence;

/// <summary>Cosine nearest-neighbour search using pgvector's <c>&lt;=&gt;</c> operator and the HNSW index.</summary>
internal sealed class ProductSearch(MatcherDbContext db) : IProductSearch
{
    public async Task<IReadOnlyList<MatchCandidate>> SearchAsync(
        float[] queryVector, int topK, IReadOnlyCollection<Guid> alwaysInclude, CancellationToken cancellationToken)
    {
        var vector = new Vector(queryVector);
        var pinned = alwaysInclude.ToArray();

        // The first branch uses the HNSW index. The second adds products the customer confirmed for
        // this phrase, so history can promote them even when they fall outside the nearest k.
        var rows = await db.Database.SqlQuery<SearchRow>($"""
            SELECT "Id", "Sku", "Name", "Category", "Unit", "Distance" FROM (
                (SELECT "Id", "Sku", "Name", "Category", "Unit", "Embedding" <=> {vector} AS "Distance"
                 FROM products
                 WHERE "Embedding" IS NOT NULL
                 ORDER BY "Embedding" <=> {vector}
                 LIMIT {topK})
                UNION
                (SELECT "Id", "Sku", "Name", "Category", "Unit", "Embedding" <=> {vector} AS "Distance"
                 FROM products
                 WHERE "Embedding" IS NOT NULL AND "Id" = ANY({pinned}))
            ) AS candidates
            ORDER BY "Distance"
            """).ToListAsync(cancellationToken);

        return rows
            .Select(r => new MatchCandidate(r.Id, r.Sku, r.Name, r.Category, r.Unit, 1 - r.Distance))
            .ToList();
    }

    private sealed record SearchRow(Guid Id, string Sku, string Name, string Category, string Unit, double Distance);
}
