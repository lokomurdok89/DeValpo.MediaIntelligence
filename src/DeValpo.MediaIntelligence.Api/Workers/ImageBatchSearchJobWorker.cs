using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Workers;

public sealed class ImageBatchSearchJobWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AnalysisWorkerOptions> options,
    ILogger<ImageBatchSearchJobWorker> logger) : BackgroundService
{
    /// <summary>
    /// Resets interrupted image batch jobs and continuously processes pending image search work.
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
                var service = scope.ServiceProvider.GetRequiredService<IImageBatchSearchApplicationService>();
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
                logger.LogError(exception, ApiResourceText.ImageBatchSearchJobWorkerFailed);
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(options.Value.PollIntervalSeconds, 1)), stoppingToken);
            }
        }
    }

    private async Task ResetInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IImageBatchSearchApplicationService>();
        await service.ResetRunningJobsAsync(cancellationToken);
    }
}
