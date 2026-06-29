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
        client.JoinedGuild += OnJoinedGuildAsync;
        interactions.InteractionExecuted += OnInteractionExecutedAsync;

        await interactions.AddModuleAsync<SubscriptionModule>(services);

        await client.LoginAsync(TokenType.Bot, _options.Token);
        await client.StartAsync();
        logger.LogInformation("Discord-klient startet, forbinder til gatewayen...");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        client.Ready -= OnReadyAsync;
        client.InteractionCreated -= OnInteractionCreatedAsync;
        client.JoinedGuild -= OnJoinedGuildAsync;
        interactions.InteractionExecuted -= OnInteractionExecutedAsync;
        await client.LogoutAsync();
        await client.StopAsync();
        logger.LogInformation("Discord-klient stoppet.");
    }

    private async Task OnReadyAsync()
    {
        try
        {
            // En specifik test-guild vinder, hvis sat.
            if (_options.TestGuildId is { } guildId)
            {
                await interactions.RegisterCommandsToGuildAsync(guildId);
                logger.LogInformation("Slash-kommandoer registreret i guild {GuildId}.", guildId);
                return;
            }

            // Ellers: registrér instant i alle guilds botten er medlem af.
            // (Guild-kommandoer er instant; globale kan tage op til ~1 time.)
            var guilds = client.Guilds;
            if (guilds.Count > 0)
            {
                foreach (var guild in guilds)
                {
                    await interactions.RegisterCommandsToGuildAsync(guild.Id);
                    logger.LogInformation("Slash-kommandoer registreret i {GuildName} ({GuildId}).", guild.Name, guild.Id);
                }
            }
            else
            {
                await interactions.RegisterCommandsGloballyAsync();
                logger.LogInformation("Ingen guilds i cache — registrerede globalt (kan tage op til ~1 time).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kunne ikke registrere slash-kommandoer.");
        }
    }

    private async Task OnJoinedGuildAsync(SocketGuild guild)
    {
        if (_options.TestGuildId is not null)
        {
            return;
        }

        try
        {
            await interactions.RegisterCommandsToGuildAsync(guild.Id);
            logger.LogInformation("Slash-kommandoer registreret i ny guild {GuildName} ({GuildId}).", guild.Name, guild.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kunne ikke registrere kommandoer i guild {GuildId}.", guild.Id);
        }
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        logger.LogInformation("Interaktion modtaget: {Type} (id {Id}).", interaction.Type, interaction.Id);
        try
        {
            using var scope = services.CreateScope();
            var context = new SocketInteractionContext(client, interaction);
            var result = await interactions.ExecuteCommandAsync(context, scope.ServiceProvider);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Interaktion fejlede: {Error} — {Reason}", result.Error, result.ErrorReason);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet fejl ved håndtering af interaktion.");
        }
    }

    // Med RunMode.Async returneres eksekveringsresultatet her i stedet for fra ExecuteCommandAsync.
    private Task OnInteractionExecutedAsync(ICommandInfo? command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            logger.LogWarning("Kommando '{Command}' fejlede: {Error} — {Reason}",
                command?.Name, result.Error, result.ErrorReason);
        }

        return Task.CompletedTask;
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
