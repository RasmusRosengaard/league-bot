using Discord;
using LolMatchAlert.Core.Polling;

namespace LolMatchAlert.Bot.Discord;

/// <summary>Bygger et Discord-embed ud fra en <see cref="MatchAlert"/>.</summary>
public static class MatchEmbedFactory
{
    private static readonly Color WinColor = new(0x2E, 0xCC, 0x71);  // grøn
    private static readonly Color LossColor = new(0xE7, 0x4C, 0x3C); // rød

    public static Embed Build(MatchAlert alert, string dataDragonVersion)
    {
        ArgumentNullException.ThrowIfNull(alert);

        return new EmbedBuilder()
            .WithAuthor($"{alert.RiotId} • {alert.Region}")
            .WithTitle(MatchAlertFormatter.Title(alert))
            .WithThumbnailUrl(DataDragon.ChampionIconUrl(dataDragonVersion, alert.ChampionName))
            .WithColor(alert.Win ? WinColor : LossColor)
            .AddField("Resultat", alert.ResultText, inline: true)
            .AddField("KDA", MatchAlertFormatter.KdaWithRatio(alert), inline: true)
            .AddField("Champion", alert.ChampionName, inline: true)
            .AddField("Kø", alert.QueueName, inline: true)
            .AddField("Varighed", alert.DurationText, inline: true)
            .WithFooter($"Match {alert.MatchId}")
            .Build();
    }
}
