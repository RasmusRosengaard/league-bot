using LolMatchAlert.Core.Riot;
using LolMatchAlert.Core.Subscriptions;

namespace LolMatchAlert.Core.Polling;

/// <summary>
/// Den display-klare information for én notifikation om en spillet kamp.
/// Discord-laget bygger et embed ud fra denne — Core kender ikke til Discord.
/// </summary>
public sealed record MatchAlert(
    string MatchId,
    RiotId RiotId,
    string Region,
    string ChampionName,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    int QueueId,
    string GameMode,
    TimeSpan GameDuration)
{
    public string Kda => $"{Kills}/{Deaths}/{Assists}";

    public double KdaRatio => (Kills + Assists) / (double)Math.Max(1, Deaths);

    public string ResultText => Win ? "Sejr" : "Nederlag";

    public string QueueName => QueueNames.Get(QueueId, GameMode);

    /// <summary>Kamplængde som "mm:ss".</summary>
    public string DurationText => $"{(int)GameDuration.TotalMinutes}:{GameDuration.Seconds:D2}";

    public static MatchAlert From(AccountToPoll account, MatchDetail match, MatchParticipant participant) => new(
        match.MatchId,
        account.RiotId,
        account.Region.Platform,
        participant.ChampionName,
        participant.Win,
        participant.Kills,
        participant.Deaths,
        participant.Assists,
        match.QueueId,
        match.GameMode,
        match.GameDuration);
}
