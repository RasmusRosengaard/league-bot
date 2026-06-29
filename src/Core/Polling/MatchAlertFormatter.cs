using System.Globalization;

namespace LolMatchAlert.Core.Polling;

/// <summary>Bygger tekstrepræsentationer af en <see cref="MatchAlert"/> (testbart, Discord-uafhængigt).</summary>
public static class MatchAlertFormatter
{
    /// <summary>Én linje, f.eks.: "HereComesTheSun#2019 spillede Sion og tabte — 7/7/13 i Ranked Solo/Duo".</summary>
    public static string ShortLine(MatchAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);
        var result = alert.Win ? "vandt" : "tabte";
        return $"{alert.RiotId} spillede {alert.ChampionName} og {result} — {alert.Kda} i {alert.QueueName}";
    }

    /// <summary>Embed-titel, f.eks.: "Sejr som Sion".</summary>
    public static string Title(MatchAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);
        return $"{alert.ResultText} som {alert.ChampionName}";
    }

    /// <summary>KDA inkl. ratio, f.eks. "7/7/13 (2.86 KDA)".</summary>
    public static string KdaWithRatio(MatchAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);
        var ratio = alert.KdaRatio.ToString("0.00", CultureInfo.InvariantCulture);
        return $"{alert.Kda} ({ratio} KDA)";
    }
}
