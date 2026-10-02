using Microsoft.Extensions.DependencyInjection;
using ProductMatcher.Application.Catalogue;
using ProductMatcher.Application.Matching;

namespace ProductMatcher.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CatalogueImportService>();
        services.AddScoped<CatalogueEmbeddingService>();
        services.AddScoped<MatchService>();
        services.AddScoped<MatchFeedbackService>();
        return services;
    }
}
