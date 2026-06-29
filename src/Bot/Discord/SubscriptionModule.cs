using Discord;
using Discord.Interactions;
using LolMatchAlert.Core.Riot;
using LolMatchAlert.Core.Subscriptions;
using Microsoft.Extensions.Logging;

namespace LolMatchAlert.Bot.Discord;

/// <summary>Slash-kommandoer: /subscribe, /unsubscribe, /list.</summary>
public sealed class SubscriptionModule(
    ISubscriptionRepository subscriptions,
    IRiotClient riotClient,
    TimeProvider timeProvider,
    ILogger<SubscriptionModule> logger) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("subscribe", "Abonnér denne kanal på en LoL-konto (post-match-alerts).")]
    public async Task SubscribeAsync(
        [Summary("riotid", "Riot-ID på formen gameName#tagLine, f.eks. Faker#KR1")] string riotid,
        [Summary("region", "Region/platform, f.eks. euw1, na1, kr")] string region)
    {
        if (Context.Guild is null)
        {
            await RespondAsync("Denne kommando kan kun bruges i en server.", ephemeral: true);
            return;
        }

        if (!RiotId.TryParse(riotid, out var parsedId))
        {
            await RespondAsync("Ugyldigt Riot-ID. Brug formen `gameName#tagLine`, f.eks. `Faker#KR1`.", ephemeral: true);
            return;
        }

        if (!Regions.TryParse(region, out var parsedRegion))
        {
            await RespondAsync(
                $"Ukendt region `{region}`. Understøttede: {string.Join(", ", Regions.SupportedPlatforms)}.",
                ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            var account = await riotClient.GetAccountByRiotIdAsync(parsedId, parsedRegion, CancellationToken.None);
            if (account is null)
            {
                await FollowupAsync($"Kontoen `{parsedId}` blev ikke fundet i regionen `{parsedRegion.Platform}`.", ephemeral: true);
                return;
            }

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                Puuid = account.Puuid,
                GameName = account.GameName,
                TagLine = account.TagLine,
                Region = parsedRegion.Platform,
                DiscordGuildId = Context.Guild.Id,
                DiscordChannelId = Context.Channel.Id,
                CreatedAt = timeProvider.GetUtcNow(),
            };

            var added = await subscriptions.AddAsync(subscription, CancellationToken.None);
            await FollowupAsync(
                added
                    ? $"✅ Abonneret på **{account.GameName}#{account.TagLine}** (`{parsedRegion.Platform}`) i denne kanal."
                    : $"Denne kanal abonnerer allerede på **{account.GameName}#{account.TagLine}**.",
                ephemeral: true);
        }
        catch (RiotAuthException ex)
        {
            logger.LogError(ex, "Riot-auth-fejl ved subscribe.");
            await FollowupAsync("Riot-API'et afviste kaldet — API-nøglen er muligvis udløbet. Prøv igen senere.", ephemeral: true);
        }
        catch (RiotRateLimitException)
        {
            await FollowupAsync("Riot-API'et er rate-limited lige nu. Prøv igen om lidt.", ephemeral: true);
        }
        catch (RiotApiException ex)
        {
            logger.LogError(ex, "Riot-API-fejl ved subscribe.");
            await FollowupAsync("Der opstod en fejl ved opslag mod Riot. Prøv igen senere.", ephemeral: true);
        }
    }

    [SlashCommand("unsubscribe", "Fjern et abonnement i denne kanal.")]
    public async Task UnsubscribeAsync(
        [Summary("riotid", "Riot-ID på formen gameName#tagLine")] string riotid)
    {
        if (!RiotId.TryParse(riotid, out var parsedId))
        {
            await RespondAsync("Ugyldigt Riot-ID. Brug formen `gameName#tagLine`.", ephemeral: true);
            return;
        }

        var channelSubs = await subscriptions.GetByChannelAsync(Context.Channel.Id, CancellationToken.None);
        var match = channelSubs.FirstOrDefault(s =>
            string.Equals(s.GameName, parsedId.GameName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.TagLine, parsedId.TagLine, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            await RespondAsync($"Denne kanal abonnerer ikke på **{parsedId}**.", ephemeral: true);
            return;
        }

        await subscriptions.RemoveAsync(match.Puuid, Context.Channel.Id, CancellationToken.None);
        await RespondAsync($"🗑️ Fjernede abonnementet på **{match.GameName}#{match.TagLine}** i denne kanal.", ephemeral: true);
    }

    [SlashCommand("list", "Vis hvilke konti denne kanal abonnerer på.")]
    public async Task ListAsync()
    {
        var channelSubs = await subscriptions.GetByChannelAsync(Context.Channel.Id, CancellationToken.None);
        if (channelSubs.Count == 0)
        {
            await RespondAsync("Denne kanal abonnerer ikke på nogen konti endnu. Brug `/subscribe`.", ephemeral: true);
            return;
        }

        var lines = channelSubs.Select(s => $"• **{s.GameName}#{s.TagLine}** (`{s.Region}`)");
        var embed = new EmbedBuilder()
            .WithTitle("Abonnementer i denne kanal")
            .WithDescription(string.Join("\n", lines))
            .WithColor(Color.Blue)
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}
