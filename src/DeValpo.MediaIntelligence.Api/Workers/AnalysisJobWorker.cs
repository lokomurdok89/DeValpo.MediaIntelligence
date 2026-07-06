using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Application.MediaAnalysis;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Workers;

public sealed class AnalysisJobWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AnalysisWorkerOptions> options,
    ILogger<AnalysisJobWorker> logger) : BackgroundService
{
    /// <summary>
    /// Polls persisted pending jobs and processes them in scoped application services.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        await ResetInterruptedJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await ProcessNextAsync(stoppingToken);
            if (!processed)
            {
                await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken);
            }
        }
    }

    private async Task ResetInterruptedJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMediaAnalysisApplicationService>();
        await service.ResetRunningJobsAsync(stoppingToken);
    }

    private async Task<bool> ProcessNextAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IMediaAnalysisApplicationService>();
            return await service.ProcessNextPendingAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, ApiResourceText.AnalysisJobWorkerFailed);
            await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken);
            return false;
        }
    }
}
