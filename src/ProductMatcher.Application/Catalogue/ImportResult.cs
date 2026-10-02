namespace ProductMatcher.Application.Catalogue;

public sealed record ImportResult(int Created, int Updated, int Unchanged, IReadOnlyList<ImportError> Errors);

public sealed record ImportError(int LineNumber, string? Sku, string Message);
