using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LolMatchAlert.Bot.Discord;

/// <summary>
/// Forbinder til Discord-gatewayen, registrerer slash-kommandoer og router
/// interaktioner til InteractionService (hver i sin egen DI-scope).
/// </summary>
public sealed class DiscordBotService(
    DiscordSocketClient client,
    InteractionService interactions,
    IServiceProvider services,
    IOptions<DiscordOptions> options,
    ILogger<DiscordBotService> logger) : IHostedService
{
    private readonly DiscordOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Log += OnLog;
        interactions.Log += OnLog;
        client.Ready += OnReadyAsync;
        client.InteractionCreated += OnInteractionCreatedAsync;

        await interactions.AddModuleAsync<SubscriptionModule>(services);

        await client.LoginAsync(TokenType.Bot, _options.Token);
        await client.StartAsync();
        logger.LogInformation("Discord-klient startet, forbinder til gatewayen...");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        client.Ready -= OnReadyAsync;
        client.InteractionCreated -= OnInteractionCreatedAsync;
        await client.LogoutAsync();
        await client.StopAsync();
        logger.LogInformation("Discord-klient stoppet.");
    }

    private async Task OnReadyAsync()
    {
        try
        {
            if (_options.TestGuildId is { } guildId)
            {
                await interactions.RegisterCommandsToGuildAsync(guildId);
                logger.LogInformation("Slash-kommandoer registreret i test-guild {GuildId}.", guildId);
            }
            else
            {
                await interactions.RegisterCommandsGloballyAsync();
                logger.LogInformation("Slash-kommandoer registreret globalt (kan tage op til ~1 time at propagere).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kunne ikke registrere slash-kommandoer.");
        }
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        using var scope = services.CreateScope();
        var context = new SocketInteractionContext(client, interaction);
        var result = await interactions.ExecuteCommandAsync(context, scope.ServiceProvider);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Interaktion fejlede: {Error} — {Reason}", result.Error, result.ErrorReason);
        }
    }

    private Task OnLog(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information,
        };

#pragma warning disable CA2254 // Discord leverer allerede formateret besked
        logger.Log(level, message.Exception, "[Discord:{Source}] {Message}", message.Source, message.Message);
#pragma warning restore CA2254
        return Task.CompletedTask;
    }
}
