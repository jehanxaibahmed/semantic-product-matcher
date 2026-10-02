using ProductMatcher.Application.Matching;

namespace ProductMatcher.Api.Endpoints;

internal static class MatchEndpoints
{
    private const int DefaultTopK = 5;

    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/match").WithTags("Matching");

        group.MapPost("/", async (MatchRequest request, MatchService matcher, CancellationToken ct) =>
                Results.Ok(await matcher.MatchAsync(request.Query, request.TopK ?? DefaultTopK, ct)))
            .WithSummary("Return the top-k catalogue products for one customer-written product name.");

        group.MapPost("/batch", async (BatchMatchRequest request, MatchService matcher, CancellationToken ct) =>
                Results.Ok(await matcher.MatchManyAsync(request.Queries, request.TopK ?? DefaultTopK, ct)))
            .WithSummary("Match several order lines in one call.");

        return app;
    }

    internal sealed record MatchRequest(string Query, int? TopK);

    internal sealed record BatchMatchRequest(IReadOnlyList<string> Queries, int? TopK);
}
