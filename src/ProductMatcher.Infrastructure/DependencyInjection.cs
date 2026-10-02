using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Infrastructure.Background;
using ProductMatcher.Infrastructure.Catalogue;
using ProductMatcher.Infrastructure.Embeddings;
using ProductMatcher.Infrastructure.Persistence;

namespace ProductMatcher.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Matcher")
            ?? throw new InvalidOperationException("Connection string 'Matcher' is not configured.");

        services.AddDbContext<MatcherDbContext>(o => o.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddSingleton<ICatalogueParser, CsvCatalogueParser>();

        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));
        services.AddSingleton<IEmbeddingProvider>(sp => CreateEmbeddingProvider(
            sp.GetRequiredService<IOptions<EmbeddingOptions>>().Value));

        services.AddSingleton<EmbeddingJobSignal>();
        services.AddSingleton<IEmbeddingJobSignal>(sp => sp.GetRequiredService<EmbeddingJobSignal>());

        var options = configuration.GetSection(EmbeddingOptions.SectionName).Get<EmbeddingOptions>() ?? new();
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

    private static IEmbeddingProvider CreateEmbeddingProvider(EmbeddingOptions options)
    {
        if (string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return new LocalHashingEmbeddingProvider();
        }

        if (!string.Equals(options.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unknown embedding provider '{options.Provider}'. Use 'OpenAI' or 'Local'.");
        }

        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OpenAI provider selected but no API key: set Embeddings:ApiKey or OPENAI_API_KEY.");
        return new OpenAiEmbeddingProvider(new EmbeddingClient(options.Model, apiKey), options.Model);
    }
}
