using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace ProductMatcher.Benchmark;

/// <summary>A labelled query. <see cref="ExpectedSku"/> is null when nothing in the catalogue should match.</summary>
internal sealed record BenchmarkQuery(string Query, string? ExpectedSku, string Kind);

/// <summary>A customer-specific phrase and the product that customer means by it.</summary>
internal sealed record HistoryCase(string CustomerId, string Query, string ExpectedSku);

internal static class BenchmarkData
{
    private static readonly CsvConfiguration Csv = new(CultureInfo.InvariantCulture) { TrimOptions = TrimOptions.Trim };

    public static IReadOnlyList<BenchmarkQuery> LoadQueries(string path) =>
        Read(path, r => new BenchmarkQuery(
            r.GetField("query")!,
            NullIfEmpty(r.GetField("expected_sku")),
            r.GetField("kind")!));

    public static IReadOnlyList<HistoryCase> LoadHistory(string path) =>
        Read(path, r => new HistoryCase(r.GetField("customer_id")!, r.GetField("query")!, r.GetField("expected_sku")!));

    private static List<T> Read<T>(string path, Func<CsvReader, T> map)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, Csv);
        csv.Read();
        csv.ReadHeader();
        var rows = new List<T>();
        while (csv.Read())
        {
            rows.Add(map(csv));
        }

        return rows;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
