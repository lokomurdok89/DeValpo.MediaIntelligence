using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Application.FaceSearch;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Workers;

public sealed class FaceSearchJobWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AnalysisWorkerOptions> options,
    ILogger<FaceSearchJobWorker> logger) : BackgroundService
{
    /// <summary>
    /// Resets interrupted face jobs and continuously processes pending AWS face search work.
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
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IFaceSearchApplicationService>();
                var processed = await service.ProcessNextPendingAsync(stoppingToken);

                if (!processed)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(options.Value.PollIntervalSeconds, 1)), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, ApiResourceText.FaceSearchJobWorkerFailed);
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(options.Value.PollIntervalSeconds, 1)), stoppingToken);
            }
        }
    }

    private async Task ResetInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFaceSearchApplicationService>();
        await service.ResetRunningJobsAsync(cancellationToken);
    }
}
