using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Matching;

namespace ProductMatcher.Api.Endpoints;

internal static class MatchEndpoints
{
    private const int DefaultTopK = 5;
    private const int HistoryPageSize = 50;

    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/match").WithTags("Matching");

        group.MapPost("/", async (MatchRequest request, MatchService matcher, CancellationToken ct) =>
                Results.Ok(await matcher.MatchAsync(request.Query, request.TopK ?? DefaultTopK, request.CustomerId, ct)))
            .WithSummary("Return the top-k catalogue products for one customer-written product name.");

        group.MapPost("/batch", async (BatchMatchRequest request, MatchService matcher, CancellationToken ct) =>
                Results.Ok(await matcher.MatchManyAsync(request.Queries, request.TopK ?? DefaultTopK, request.CustomerId, ct)))
            .WithSummary("Match several order lines in one call.");

        group.MapPost("/confirm", async (ConfirmRequest request, MatchFeedbackService feedback, CancellationToken ct) =>
                await feedback.ConfirmAsync(request.CustomerId, request.Query, request.Sku, ct)
                    ? Results.NoContent()
                    : Results.Problem($"Unknown SKU '{request.Sku}'.", statusCode: 404))
            .WithSummary("Record that a customer's wording referred to this product.");

        app.MapGet("/api/customers/{customerId}/history",
                async (string customerId, IMatchHistoryRepository history, CancellationToken ct) =>
                    Results.Ok(await history.GetRecentAsync(customerId, HistoryPageSize, ct)))
            .WithTags("Matching")
            .WithSummary("A customer's most recent confirmed matches.");

        return app;
    }

    internal sealed record MatchRequest(string Query, int? TopK, string? CustomerId);

    internal sealed record BatchMatchRequest(IReadOnlyList<string> Queries, int? TopK, string? CustomerId);

    internal sealed record ConfirmRequest(string CustomerId, string Query, string Sku);
}
