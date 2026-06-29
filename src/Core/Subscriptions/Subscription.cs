namespace LolMatchAlert.Core.Subscriptions;

/// <summary>
/// Et abonnement: én Discord-kanal følger én LoL-konto. Samme konto kan følges
/// i flere kanaler (hver med sin egen række).
/// </summary>
public class Subscription
{
    public Guid Id { get; set; }

    /// <summary>Riot PUUID for den fulgte konto (globalt unik).</summary>
    public string Puuid { get; set; } = string.Empty;

    public string GameName { get; set; } = string.Empty;

    public string TagLine { get; set; } = string.Empty;

    /// <summary>Kanonisk platform-id, f.eks. "euw1".</summary>
    public string Region { get; set; } = string.Empty;

    public ulong DiscordGuildId { get; set; }

    public ulong DiscordChannelId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string RiotIdString => $"{GameName}#{TagLine}";
}
