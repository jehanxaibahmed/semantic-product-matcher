using Microsoft.Extensions.Logging.Abstractions;
using ProductMatcher.Application.Catalogue;
using ProductMatcher.Domain.Products;
using ProductMatcher.UnitTests.Fakes;

namespace ProductMatcher.UnitTests.Catalogue;

public class CatalogueImportServiceTests
{
    private readonly InMemoryProductRepository _repository = new();
    private readonly FakeEmbeddingJobSignal _signal = new();

    private CatalogueImportService CreateService() =>
        new(_repository, _signal, NullLogger<CatalogueImportService>.Instance);

    [Fact]
    public async Task Creates_new_products_and_wakes_the_embedding_job()
    {
        var result = await CreateService().ImportAsync(
            [new CatalogueRow(2, "FP-1001", "Red Peppers", "Fresh Produce", "5kg box", null)], default);

        Assert.Equal(1, result.Created);
        Assert.Single(_repository.Items);
        Assert.Equal(1, _signal.Notifications);
    }

    [Fact]
    public async Task Updates_changed_products_and_counts_unchanged_ones()
    {
        _repository.Add(new Product("FP-1001", "Red Peppers", "Fresh Produce", "5kg box", null));
        _repository.Add(new Product("FP-1002", "Yellow Peppers", "Fresh Produce", "5kg box", null));

        var result = await CreateService().ImportAsync(
        [
            new CatalogueRow(2, "fp-1001", "Red Peppers Large", "Fresh Produce", "5kg box", null),
            new CatalogueRow(3, "FP-1002", "Yellow Peppers", "Fresh Produce", "5kg box", null),
        ], default);

        Assert.Equal((0, 1, 1), (result.Created, result.Updated, result.Unchanged));
        Assert.Equal("Red Peppers Large", _repository.Items[0].Name);
    }

    [Fact]
    public async Task Rejects_invalid_and_duplicate_rows_with_line_numbers()
    {
        var result = await CreateService().ImportAsync(
        [
            new CatalogueRow(2, "", "No SKU", null, null, null),
            new CatalogueRow(3, "FP-1", " ", null, null, null),
            new CatalogueRow(4, "FP-2", "Ok", null, null, null),
            new CatalogueRow(5, "fp-2", "Duplicate", null, null, null),
        ], default);

        Assert.Equal(1, result.Created);
        Assert.Equal([2, 3, 5], result.Errors.Select(e => e.LineNumber));
    }

    [Fact]
    public async Task Does_not_wake_the_job_when_nothing_changed()
    {
        _repository.Add(new Product("FP-1001", "Red Peppers", "Fresh Produce", "5kg box", null));

        await CreateService().ImportAsync(
            [new CatalogueRow(2, "FP-1001", "Red Peppers", "Fresh Produce", "5kg box", null)], default);

        Assert.Equal(0, _signal.Notifications);
    }
}
