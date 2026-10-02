using Microsoft.Extensions.DependencyInjection;
using ProductMatcher.Application.Catalogue;

namespace ProductMatcher.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CatalogueImportService>();
        services.AddScoped<CatalogueEmbeddingService>();
        return services;
    }
}
