using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api.Services;

public class TelegramPollingHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<TelegramOptions> options,
    ILogger<TelegramPollingHostedService> logger) : BackgroundService
{
    private readonly TelegramOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnablePolling || string.IsNullOrWhiteSpace(_options.BotToken))
        {
            logger.LogInformation("Telegram polling is disabled.");
            return;
        }

        long offset = 0;
        var pollingDelay = TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds));
        var failureDelay = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var handler = scope.ServiceProvider.GetRequiredService<ITelegramUpdateHandler>();
                var updates = await botClient.GetUpdatesAsync(offset, 25, stoppingToken);

                foreach (var update in updates.OrderBy(item => item.UpdateId))
                {
                    await handler.HandleAsync(update, stoppingToken);
                    offset = Math.Max(offset, update.UpdateId + 1);
                }

                await Task.Delay(pollingDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Telegram polling iteration failed.");
                await Task.Delay(failureDelay, stoppingToken);
            }
        }
    }
}
