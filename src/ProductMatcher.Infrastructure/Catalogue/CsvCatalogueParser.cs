using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Catalogue;

namespace ProductMatcher.Infrastructure.Catalogue;

/// <summary>Parses CSV with a header row. Required columns: <c>sku</c>, <c>name</c>. Optional: <c>category</c>, <c>unit</c>, <c>description</c>.</summary>
internal sealed class CsvCatalogueParser : ICatalogueParser
{
    private static readonly CsvConfiguration Configuration = new(CultureInfo.InvariantCulture)
    {
        PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
        MissingFieldFound = null,
        HeaderValidated = null,
        TrimOptions = TrimOptions.Trim,
    };

    public async Task<IReadOnlyList<CatalogueRow>> ParseAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        using var csv = new CsvReader(reader, Configuration);

        var rows = new List<CatalogueRow>();
        if (!await csv.ReadAsync() || !csv.ReadHeader())
        {
            return rows;
        }

        var header = (csv.HeaderRecord ?? []).Select(h => h.Trim().ToLowerInvariant()).ToHashSet();
        if (!header.Contains("sku") || !header.Contains("name"))
        {
            throw new FormatException("CSV header must contain 'sku' and 'name' columns.");
        }

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(new CatalogueRow(
                csv.Parser.RawRow,
                csv.GetField("sku"),
                csv.GetField("name"),
                csv.GetField("category"),
                csv.GetField("unit"),
                csv.GetField("description")));
        }

        return rows;
    }
}
