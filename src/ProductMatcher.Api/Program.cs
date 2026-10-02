using ProductMatcher.Api.Endpoints;
using ProductMatcher.Application;
using ProductMatcher.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<ProductMatcher.Api.ValidationExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await app.Services.MigrateDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapCatalogueEndpoints();
app.MapMatchEndpoints();
app.MapEmbeddingEndpoints();

await app.RunAsync();

public partial class Program;
