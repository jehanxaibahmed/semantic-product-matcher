using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Catalogue;

namespace ProductMatcher.Api.Endpoints;

internal static class CatalogueEndpoints
{
    public static IEndpointRouteBuilder MapCatalogueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalogue").WithTags("Catalogue");

        group.MapPost("/import", ImportAsync)
            .Accepts<IFormFile>("multipart/form-data", "text/csv")
            .DisableAntiforgery()
            .WithSummary("Upsert products from a CSV file (multipart field 'file' or a raw text/csv body).");

        group.MapPost("/embed", async (CatalogueEmbeddingService service, CancellationToken ct) =>
                Results.Ok(new { embedded = await service.EmbedPendingAsync(ct) }))
            .WithSummary("Embed all pending products now instead of waiting for the background job.");

        group.MapGet("/stats", async (IProductRepository products, IEmbeddingProvider embeddings, CancellationToken ct) =>
                Results.Ok(await products.GetStatsAsync(embeddings.Model, ct)))
            .WithSummary("Product counts and embedding progress.");

        app.MapGet("/api/products/{sku}", async (string sku, IProductRepository products, CancellationToken ct) =>
                await products.GetBySkuAsync(sku, ct) is { } p
                    ? Results.Ok(new ProductResponse(p.Sku, p.Name, p.Category, p.Unit, p.Description, p.HasEmbedding))
                    : Results.NotFound())
            .WithTags("Catalogue");

        return app;
    }

    private static async Task<IResult> ImportAsync(
        HttpRequest request,
        ICatalogueParser parser,
        CatalogueImportService importer,
        CancellationToken ct)
    {
        Stream content;
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null)
            {
                return Results.Problem("Multipart upload must include a 'file' field.", statusCode: 400);
            }

            content = file.OpenReadStream();
        }
        else
        {
            content = request.Body;
        }

        IReadOnlyList<CatalogueRow> rows;
        await using (content)
        {
            try
            {
                rows = await parser.ParseAsync(content, ct);
            }
            catch (FormatException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        }

        return Results.Ok(await importer.ImportAsync(rows, ct));
    }

    private sealed record ProductResponse(
        string Sku, string Name, string Category, string Unit, string? Description, bool Embedded);
}
