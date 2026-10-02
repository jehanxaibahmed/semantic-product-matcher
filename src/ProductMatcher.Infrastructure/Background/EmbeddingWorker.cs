using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductMatcher.Application.Catalogue;

namespace ProductMatcher.Infrastructure.Background;

/// <summary>Embeds pending products at start-up, after each import, and every few minutes as a safety net.</summary>
internal sealed partial class EmbeddingWorker(
    EmbeddingJobSignal signal,
    IServiceScopeFactory scopes,
    ILogger<EmbeddingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            signal.Reset();
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<CatalogueEmbeddingService>();
                await service.EmbedPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // The worker must survive transient failures and retry on the next wake-up.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogFailed(ex);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(SweepInterval);
            try
            {
                await signal.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Sweep interval elapsed.
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Embedding job failed; will retry")]
    private partial void LogFailed(Exception exception);
}
