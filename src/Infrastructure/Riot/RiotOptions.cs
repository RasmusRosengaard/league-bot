using System.ComponentModel.DataAnnotations;

namespace LolMatchAlert.Infrastructure.Riot;

/// <summary>Konfiguration for Riot-API-klienten. Bindes fra sektionen "Riot".</summary>
public sealed class RiotOptions
{
    public const string SectionName = "Riot";

    /// <summary>
    /// Riot-API-nøgle. Development-nøgler udløber hver 24. time — i produktion
    /// bruges en personal/production-nøgle. Læses fra konfiguration, aldrig hardcodet.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Timeout pr. enkelt HTTP-forsøg.</summary>
    [Range(1, 120)]
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>Antal match-ids der hentes pr. konto ved hver poll.</summary>
    [Range(1, 100)]
    public int MatchIdsPerPoll { get; set; } = 5;
}
