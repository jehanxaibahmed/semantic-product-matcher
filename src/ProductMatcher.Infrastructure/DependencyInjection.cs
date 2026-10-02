using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using OpenAI.Embeddings;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Matching;
using ProductMatcher.Infrastructure.Background;
using ProductMatcher.Infrastructure.Catalogue;
using ProductMatcher.Infrastructure.Embeddings;
using ProductMatcher.Infrastructure.Embeddings.Caching;
using ProductMatcher.Infrastructure.Persistence;

namespace ProductMatcher.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Matcher")
            ?? throw new InvalidOperationException("Connection string 'Matcher' is not configured.");

        // The factory also registers MatcherDbContext as scoped; singletons such as the cache store use the factory.
        services.AddDbContextFactory<MatcherDbContext>(o => o.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductSearch, ProductSearch>();
        services.AddScoped<IMatchHistoryRepository, MatchHistoryRepository>();
        services.Configure<RerankingOptions>(configuration.GetSection(RerankingOptions.SectionName));
        services.Configure<ConfidenceOptions>(configuration.GetSection(ConfidenceOptions.SectionName));
        services.AddSingleton<ICatalogueParser, CsvCatalogueParser>();

        var options = configuration.GetSection(EmbeddingOptions.SectionName).Get<EmbeddingOptions>() ?? new();
        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));
        AddEmbeddings(services, options);

        services.AddSingleton<EmbeddingJobSignal>();
        services.AddSingleton<IEmbeddingJobSignal>(sp => sp.GetRequiredService<EmbeddingJobSignal>());

        if (options.BackgroundJob)
        {
            services.AddHostedService<EmbeddingWorker>();
        }

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatcherDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    private static void AddEmbeddings(IServiceCollection services, EmbeddingOptions options)
    {
        services.AddSingleton<EmbeddingCacheMetrics>();

        if (string.Equals(options.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient(OllamaHttpClientName, client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });
        }

        if (!options.Cache.Enabled)
        {
            services.AddSingleton(sp => CreateEmbeddingProvider(options, sp));
            return;
        }

        services.AddSingleton<IEmbeddingCacheStore, PostgresEmbeddingCacheStore>();
        services.AddSingleton<IEmbeddingProvider>(sp => new CachedEmbeddingProvider(
            CreateEmbeddingProvider(options, sp),
            new MemoryCache(new MemoryCacheOptions { SizeLimit = options.Cache.MemoryEntries }),
            sp.GetRequiredService<IEmbeddingCacheStore>(),
            sp.GetRequiredService<EmbeddingCacheMetrics>(),
            sp.GetRequiredService<TimeProvider>()));
    }

    private const string OllamaHttpClientName = "ollama";

    private static IEmbeddingProvider CreateEmbeddingProvider(EmbeddingOptions options, IServiceProvider services)
    {
        if (string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return new LocalHashingEmbeddingProvider();
        }

        if (string.Equals(options.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Model) || options.Model.StartsWith("text-embedding-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Ollama provider selected but Embeddings:Model is not an Ollama model. Set it to e.g. 'nomic-embed-text' or 'bge-m3'.");
            }

            var http = services.GetRequiredService<IHttpClientFactory>().CreateClient(OllamaHttpClientName);
            return new OllamaEmbeddingProvider(http, options.Model);
        }

        if (!string.Equals(options.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unknown embedding provider '{options.Provider}'. Use 'OpenAI', 'Ollama' or 'Local'.");
        }

        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OpenAI provider selected but no API key: set Embeddings:ApiKey or OPENAI_API_KEY.");
        return new OpenAiEmbeddingProvider(new EmbeddingClient(options.Model, apiKey), options.Model);
    }
}
