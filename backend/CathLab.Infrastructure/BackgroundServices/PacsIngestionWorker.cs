using CathLab.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CathLab.Infrastructure.BackgroundServices;

public class PacsIngestionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PacsIngestionWorker> _logger;

    // Poll every 15 minutes - can be adjusted as needed
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(900);

    public PacsIngestionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PacsIngestionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PACS ingestion worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunIngestionCycleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PACS ingestion cycle");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunIngestionCycleAsync()
    {
        // BackgroundService is singleton so we need a scope for scoped services
        using var scope = _scopeFactory.CreateScope();
        var ingestionService = scope.ServiceProvider
            .GetRequiredService<StudyIngestionService>();

        _logger.LogInformation("Running PACS ingestion cycle at {Time}", DateTime.UtcNow);

        var results = await ingestionService.IngestAllPendingAsync();

        var newCount      = results.Count(r => r.Succeeded && !r.WasAlreadyIngested);
        var alreadyCount  = results.Count(r => r.WasAlreadyIngested);
        var failedCount   = results.Count(r => !r.Succeeded);

        _logger.LogInformation(
            "Ingestion cycle done — New: {New}, Already ingested: {Already}, Failed: {Failed}",
            newCount, alreadyCount, failedCount);
    }
}