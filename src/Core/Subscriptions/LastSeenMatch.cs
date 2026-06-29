namespace LolMatchAlert.Core.Subscriptions;

/// <summary>
/// Det senest behandlede match for en konto (nøglet på PUUID), så samme kamp
/// aldrig postes to gange — uanset hvor mange kanaler der følger kontoen.
/// </summary>
public class LastSeenMatch
{
    public string Puuid { get; set; } = string.Empty;

    public string LastMatchId { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
