using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductMatcher.Application;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Catalogue;
using ProductMatcher.Application.Matching;
using ProductMatcher.Infrastructure;
using ProductMatcher.Infrastructure.Embeddings.Caching;
using ProductMatcher.Infrastructure.Persistence;

namespace ProductMatcher.Benchmark;

/// <summary>Runs the full pipeline in-process against a dedicated database for one embedding provider.</summary>
internal sealed class BenchmarkRunner(BenchmarkSettings settings)
{
    private const int TopK = 5;
    private const int ConfirmationsPerCase = 3;

    public async Task<ProviderResult> RunAsync(string provider, CancellationToken ct)
    {
        using var host = BuildHost(provider);
        var services = host.Services;
        await services.MigrateDatabaseAsync(ct);
        await ResetAsync(services, ct);

        Console.WriteLine($"[{provider}] importing and embedding catalogue...");
        var (catalogueSize, embedMs) = await LoadCatalogueAsync(services, ct);

        var queries = BenchmarkData.LoadQueries(settings.QueriesPath);
        var metrics = services.GetRequiredService<EmbeddingCacheMetrics>();
        var model = services.GetRequiredService<IEmbeddingProvider>().Model;
        var thresholds = services.GetRequiredService<IOptions<ConfidenceOptions>>().Value.For(model);

        var cacheBeforeCold = metrics.Snapshot();
        Console.WriteLine($"[{provider}] cold pass over {queries.Count} queries...");
        var cold = new List<QueryOutcome>(queries.Count);
        foreach (var query in queries)
        {
            cold.Add(await MatchOneAsync(services, query, ct));
        }

        var cacheAfterCold = metrics.Snapshot();

        Console.WriteLine($"[{provider}] warm pass (cache)...");
        var warm = new List<double>(queries.Count);
        foreach (var query in queries)
        {
            warm.Add((await MatchOneAsync(services, query, ct)).LatencyMs);
        }

        var cacheAfterWarm = metrics.Snapshot();

        Console.WriteLine($"[{provider}] personalisation...");
        var history = await RunHistoryAsync(services, ct);

        return new ProviderResult(
            provider, model, catalogueSize, embedMs, cold, warm, cacheBeforeCold, cacheAfterCold, cacheAfterWarm, history,
            new ConfiguredThresholds(thresholds.AutoAcceptScore, thresholds.MinMargin, thresholds.ReviewScore));
    }

    /// <summary>A provider spec is <c>Local</c>, <c>OpenAI</c> or <c>Ollama:&lt;model&gt;</c> (for example <c>Ollama:nomic-embed-text</c>).</summary>
    private IHost BuildHost(string spec)
    {
        var separator = spec.IndexOf(':');
        var provider = separator < 0 ? spec : spec[..separator];
        var model = separator < 0 ? null : spec[(separator + 1)..];
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration
            .AddJsonFile(settings.ApiSettingsPath, optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Matcher"] = settings.ConnectionString,
                ["Embeddings:Provider"] = provider,
                ["Embeddings:BackgroundJob"] = "false",
            });
        if (model is not null)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Embeddings:Model"] = model });
        }

        builder.Logging.ClearProviders();
        builder.Logging.AddFilter(_ => false);

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        return builder.Build();
    }

    private static async Task ResetAsync(IServiceProvider services, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatcherDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE match_history, embedding_cache, products CASCADE", ct);
    }

    private async Task<(int Size, double EmbedMs)> LoadCatalogueAsync(IServiceProvider services, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var parser = scope.ServiceProvider.GetRequiredService<ICatalogueParser>();
        await using var file = File.OpenRead(settings.CataloguePath);
        var rows = await parser.ParseAsync(file, ct);
        await scope.ServiceProvider.GetRequiredService<CatalogueImportService>().ImportAsync(rows, ct);

        var stopwatch = Stopwatch.StartNew();
        var embedded = await scope.ServiceProvider.GetRequiredService<CatalogueEmbeddingService>().EmbedPendingAsync(ct);
        return (embedded, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static async Task<QueryOutcome> MatchOneAsync(IServiceProvider services, BenchmarkQuery query, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var matcher = scope.ServiceProvider.GetRequiredService<MatchService>();

        var stopwatch = Stopwatch.StartNew();
        var result = await matcher.MatchAsync(query.Query, TopK, null, ct);
        var latency = stopwatch.Elapsed.TotalMilliseconds;

        var index = result.Candidates.ToList().FindIndex(c => c.Sku == query.ExpectedSku);
        return new QueryOutcome(
            query,
            result.NormalizedQuery,
            index >= 0 ? index + 1 : null,
            (result.Candidates.Count > 0 ? result.Candidates[0].Sku : null),
            result.Decision.TopScore,
            result.Decision.Margin,
            result.Decision.Band,
            latency);
    }

    /// <summary>
    /// Top-1 for each customer phrase before and after the customer confirms it, tested on the
    /// exact phrase and on an order-style rewording of it ("2 boxes of ...").
    /// </summary>
    private async Task<IReadOnlyList<HistoryOutcome>> RunHistoryAsync(IServiceProvider services, CancellationToken ct)
    {
        var cases = BenchmarkData.LoadHistory(settings.HistoryPath);
        var phrasings = cases
            .SelectMany(c => new[] { (Case: c, Phrasing: c.Query), (Case: c, Phrasing: $"2 boxes of {c.Query} please") })
            .ToList();

        var before = new List<string?>();
        foreach (var (c, phrasing) in phrasings)
        {
            before.Add(await TopSkuAsync(services, phrasing, c.CustomerId, ct));
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var feedback = scope.ServiceProvider.GetRequiredService<MatchFeedbackService>();
            foreach (var c in cases)
            {
                for (var i = 0; i < ConfirmationsPerCase; i++)
                {
                    await feedback.ConfirmAsync(c.CustomerId, c.Query, c.ExpectedSku, ct);
                }
            }
        }

        var outcomes = new List<HistoryOutcome>(phrasings.Count);
        for (var i = 0; i < phrasings.Count; i++)
        {
            var (c, phrasing) = phrasings[i];
            outcomes.Add(new HistoryOutcome(c, phrasing, before[i], await TopSkuAsync(services, phrasing, c.CustomerId, ct)));
        }

        return outcomes;
    }

    private static async Task<string?> TopSkuAsync(IServiceProvider services, string query, string customerId, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<MatchService>().MatchAsync(query, TopK, customerId, ct);
        return (result.Candidates.Count > 0 ? result.Candidates[0].Sku : null);
    }
}

internal sealed record BenchmarkSettings(
    string ConnectionString,
    string CataloguePath,
    string QueriesPath,
    string HistoryPath,
    string ApiSettingsPath,
    string OutputPath);
