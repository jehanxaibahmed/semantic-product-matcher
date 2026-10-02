namespace ProductMatcher.Application.Catalogue;

/// <summary>One product as supplied in an import file. <see cref="LineNumber"/> is used in error messages.</summary>
public sealed record CatalogueRow(
    int LineNumber,
    string? Sku,
    string? Name,
    string? Category,
    string? Unit,
    string? Description);
