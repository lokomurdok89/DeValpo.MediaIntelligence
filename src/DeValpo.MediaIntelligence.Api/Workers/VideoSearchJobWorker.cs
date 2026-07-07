using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Workers;

public sealed class VideoSearchJobWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AnalysisWorkerOptions> options,
    ILogger<VideoSearchJobWorker> logger) : BackgroundService
{
    /// <summary>
    /// Resets interrupted video jobs and continuously processes pending video search work.
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
                var service = scope.ServiceProvider.GetRequiredService<IVideoSearchApplicationService>();
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
                logger.LogError(exception, ApiResourceText.VideoSearchJobWorkerFailed);
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(options.Value.PollIntervalSeconds, 1)), stoppingToken);
            }
        }
    }

    private async Task ResetInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IVideoSearchApplicationService>();
        await service.ResetRunningJobsAsync(cancellationToken);
    }
}
