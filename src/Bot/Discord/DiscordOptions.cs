using System.ComponentModel.DataAnnotations;

namespace LolMatchAlert.Bot.Discord;

/// <summary>Konfiguration for Discord-botten. Bindes fra sektionen "Discord".</summary>
public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    /// <summary>Bot-token fra Discord Developer Portal. Læses fra konfiguration, aldrig hardcodet.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Valgfri test-guild. Hvis sat registreres slash-kommandoer kun i denne guild.
    /// Ellers registreres de instant i alle guilds botten er medlem af (og når den
    /// joiner nye); er der ingen guilds i cache, registreres globalt (~1 time at propagere).
    /// </summary>
    public ulong? TestGuildId { get; set; }
}
