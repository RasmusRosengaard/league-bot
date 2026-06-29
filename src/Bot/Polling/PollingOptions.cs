using System.ComponentModel.DataAnnotations;
using LolMatchAlert.Core.Polling;

namespace LolMatchAlert.Bot.Polling;

/// <summary>Konfiguration for baggrunds-poll-løkken. Bindes fra sektionen "Polling".</summary>
public sealed class PollingOptions
{
    public const string SectionName = "Polling";

    /// <summary>Hvor ofte der polles for nye kampe.</summary>
    [Range(typeof(TimeSpan), "00:00:30", "06:00:00")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>Data Dragon-version brugt til champion-ikoner.</summary>
    public string DataDragonVersion { get; set; } = DataDragon.DefaultVersion;
}
