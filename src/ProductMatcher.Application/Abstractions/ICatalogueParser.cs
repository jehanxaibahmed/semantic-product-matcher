using ProductMatcher.Application.Catalogue;

namespace ProductMatcher.Application.Abstractions;

/// <summary>Reads catalogue rows from an uploaded file.</summary>
public interface ICatalogueParser
{
    Task<IReadOnlyList<CatalogueRow>> ParseAsync(Stream content, CancellationToken cancellationToken);
}
