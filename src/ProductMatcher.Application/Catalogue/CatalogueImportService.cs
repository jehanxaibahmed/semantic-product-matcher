using Microsoft.Extensions.Logging;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.Application.Catalogue;

/// <summary>Upserts catalogue rows by SKU. Rows whose text changes lose their embedding and are re-embedded by the job.</summary>
public sealed partial class CatalogueImportService(
    IProductRepository products,
    IEmbeddingJobSignal embeddingJob,
    ILogger<CatalogueImportService> logger)
{
    public async Task<ImportResult> ImportAsync(IReadOnlyList<CatalogueRow> rows, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var errors = new List<ImportError>();
        var valid = new List<CatalogueRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Sku))
            {
                errors.Add(new ImportError(row.LineNumber, row.Sku, "SKU is required."));
            }
            else if (string.IsNullOrWhiteSpace(row.Name))
            {
                errors.Add(new ImportError(row.LineNumber, row.Sku, "Name is required."));
            }
            else if (!seen.Add(Product.NormalizeSku(row.Sku)))
            {
                errors.Add(new ImportError(row.LineNumber, row.Sku, "Duplicate SKU in file."));
            }
            else
            {
                valid.Add(row);
            }
        }

        var existing = await products.GetBySkusAsync(
            valid.Select(r => Product.NormalizeSku(r.Sku!)).ToList(), cancellationToken).ConfigureAwait(false);

        int created = 0, updated = 0, unchanged = 0;
        foreach (var row in valid)
        {
            if (existing.TryGetValue(Product.NormalizeSku(row.Sku!), out var product))
            {
                if (product.UpdateDetails(row.Name!, row.Category ?? string.Empty, row.Unit ?? string.Empty, row.Description))
                {
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }
            else
            {
                products.Add(new Product(row.Sku!, row.Name!, row.Category ?? string.Empty, row.Unit ?? string.Empty, row.Description));
                created++;
            }
        }

        await products.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogImported(created, updated, unchanged, errors.Count);

        if (created + updated > 0)
        {
            embeddingJob.Notify();
        }

        return new ImportResult(created, updated, unchanged, errors);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Catalogue import: {Created} created, {Updated} updated, {Unchanged} unchanged, {Errors} rejected")]
    private partial void LogImported(int created, int updated, int unchanged, int errors);
}
