using Discord;
using Discord.WebSocket;
using LolMatchAlert.Bot.Discord;
using LolMatchAlert.Core.Polling;
using LolMatchAlert.Core.Subscriptions;
using LolMatchAlert.Infrastructure.Riot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LolMatchAlert.Bot.Polling;

/// <summary>
/// Poller abonnerede konti på et fast interval og poster nye kampe som embeds.
/// Fejl på én konto stopper ikke løkken (logges og fortsætter). Respekterer
/// CancellationToken for graceful shutdown.
/// </summary>
public sealed class MatchPollingService(
    IServiceProvider services,
    DiscordSocketClient discordClient,
    IOptions<PollingOptions> pollingOptions,
    IOptions<RiotOptions> riotOptions,
    ILogger<MatchPollingService> logger) : BackgroundService
{
    private readonly PollingOptions _polling = pollingOptions.Value;
    private readonly RiotOptions _riot = riotOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Poll-løkke startet (interval: {Interval}).", _polling.Interval);

        using var timer = new PeriodicTimer(_polling.Interval);
        do
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Uventet fejl i poll-cyklus — fortsætter.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));

        logger.LogInformation("Poll-løkke stoppet.");
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        // Vent til gatewayen er forbundet, ellers kan vi ikke poste.
        if (discordClient.ConnectionState != ConnectionState.Connected)
        {
            logger.LogDebug("Discord ikke forbundet endnu — springer denne cyklus over.");
            return;
        }

        using var scope = services.CreateScope();
        var subscriptions = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var coordinator = scope.ServiceProvider.GetRequiredService<MatchPollingCoordinator>();

        var accounts = await subscriptions.GetDistinctAccountsToPollAsync(cancellationToken);
        if (accounts.Count == 0)
        {
            return;
        }

        logger.LogDebug("Poller {Count} konti.", accounts.Count);

        foreach (var account in accounts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var alerts = await coordinator.PollAccountAsync(account, _riot.MatchIdsPerPoll, cancellationToken);
                if (alerts.Count == 0)
                {
                    continue;
                }

                var targets = await subscriptions.GetByPuuidAsync(account.Puuid, cancellationToken);
                foreach (var alert in alerts)
                {
                    await FanOutAsync(alert, targets, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Isolér fejl pr. konto.
                logger.LogError(ex, "Fejl ved polling af {RiotId} — springer over.", account.RiotId);
            }
        }
    }

    private async Task FanOutAsync(MatchAlert alert, IReadOnlyList<Subscription> targets, CancellationToken cancellationToken)
    {
        var embed = MatchEmbedFactory.Build(alert, _polling.DataDragonVersion);

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (discordClient.GetChannel(target.DiscordChannelId) is not IMessageChannel channel)
            {
                logger.LogWarning("Kanal {ChannelId} ikke tilgængelig — opslag springes over.", target.DiscordChannelId);
                continue;
            }

            try
            {
                await channel.SendMessageAsync(embed: embed);
                logger.LogInformation("Postede {Summary} i kanal {ChannelId}.",
                    MatchAlertFormatter.ShortLine(alert), target.DiscordChannelId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kunne ikke poste i kanal {ChannelId}.", target.DiscordChannelId);
            }
        }
    }
}
