using CathLab.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CathLab.Infrastructure.BackgroundServices;

public class PacsIngestionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PacsIngestionWorker> _logger;
    private readonly IngestionSettings _settings;

    public PacsIngestionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PacsIngestionWorker> logger,
        IngestionSettings settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PACS ingestion worker started (every {Min} min)", _settings.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunIngestionCycleAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error during PACS ingestion cycle"); }

            _settings.MarkRun(DateTime.UtcNow);

            // Wait until lastRun + interval, re-evaluating whenever the admin changes the period.
            while (!stoppingToken.IsCancellationRequested)
            {
                var changeToken = _settings.GetChangeToken();           // fetch BEFORE computing the deadline
                var remaining = _settings.LastRunUtc!.Value + _settings.Interval - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, changeToken);
                try { await Task.Delay(remaining, linked.Token); }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested) { /* period changed */ }
            }
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

        var newCount      = results.Count(r => r.Succeeded && !r.WasAlreadyIngested && !r.WasArchived);
        var alreadyCount  = results.Count(r => r.WasAlreadyIngested);
        var archivedCount = results.Count(r => r.WasArchived);
        var failedCount   = results.Count(r => !r.Succeeded);

        _logger.LogInformation(
            "Ingestion cycle done — New: {New}, Already ingested: {Already}, Archived (skipped): {Archived}, Failed: {Failed}",
            newCount, alreadyCount, archivedCount, failedCount);
        
        var scheduling = scope.ServiceProvider.GetRequiredService<StudySchedulingService>();
        var started = await scheduling.StartDueStudiesAsync();
        if (started > 0) _logger.LogInformation("{Count} study(ies) moved to InProgress", started);
    }
}