namespace LolMatchAlert.Bot;

/// <summary>
/// Midlertidig liveness-service for trin 1: bekræfter at hosten kører og at
/// graceful shutdown respekterer <see cref="CancellationToken"/>.
/// Erstattes senere af den rigtige poll-løkke (MatchPollingService).
/// </summary>
public sealed class HeartbeatService(ILogger<HeartbeatService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LoL Match-Alert bot host startet — alive.");

        try
        {
            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                logger.LogInformation("alive");
            }
        }
        catch (OperationCanceledException)
        {
            // Forventet ved graceful shutdown.
        }

        logger.LogInformation("LoL Match-Alert bot host stopper.");
    }
}
