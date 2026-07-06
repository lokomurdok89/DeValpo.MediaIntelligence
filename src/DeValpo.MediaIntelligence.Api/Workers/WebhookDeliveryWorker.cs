using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Workers;

public sealed class WebhookDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WebhookDeliveryOptions> options,
    ILogger<WebhookDeliveryWorker> logger) : BackgroundService
{
    /// <summary>
    /// Polls due webhook deliveries and sends them in scoped processors.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await ProcessNextAsync(stoppingToken);
            if (!processed)
            {
                await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryProcessor>();
            return await processor.ProcessNextDueAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, ApiResourceText.WebhookDeliveryWorkerFailed);
            await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken);
            return false;
        }
    }
}
