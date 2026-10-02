using System.Text;
using ProductMatcher.Infrastructure.Catalogue;

namespace ProductMatcher.UnitTests.Catalogue;

public class CsvCatalogueParserTests
{
    private static MemoryStream Csv(string text) => new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task Parses_rows_with_quoted_fields_and_optional_columns()
    {
        using var csv = Csv("SKU,Name,Unit\nFP-1,\"Peppers, red\",5kg\nFP-2,Onions,\n");

        var rows = await new CsvCatalogueParser().ParseAsync(csv, default);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Peppers, red", rows[0].Name);
        Assert.Null(rows[0].Category);
        Assert.Equal(3, rows[1].LineNumber);
    }

    [Fact]
    public async Task Rejects_a_header_without_required_columns()
    {
        using var csv = Csv("code,title\nA,B\n");

        await Assert.ThrowsAsync<FormatException>(() => new CsvCatalogueParser().ParseAsync(csv, default));
    }
}
