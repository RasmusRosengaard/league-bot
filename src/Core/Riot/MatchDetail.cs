namespace LolMatchAlert.Core.Riot;

/// <summary>
/// Domæne-projektion af et match-v5 match. Indeholder kun de felter botten bruger,
/// så Core er fri for det rå JSON-skema.
/// </summary>
public sealed record MatchDetail(
    string MatchId,
    int QueueId,
    string GameMode,
    TimeSpan GameDuration,
    IReadOnlyList<MatchParticipant> Participants)
{
    /// <summary>Find deltageren med den angivne PUUID, eller null hvis den ikke er med.</summary>
    public MatchParticipant? FindParticipant(string puuid) =>
        Participants.FirstOrDefault(p => string.Equals(p.Puuid, puuid, StringComparison.OrdinalIgnoreCase));
}

/// <summary>En enkelt spillers data i et match.</summary>
public sealed record MatchParticipant(
    string Puuid,
    string ChampionName,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    string? RiotIdGameName,
    string? RiotIdTagline)
{
    /// <summary>KDA formateret som "k/d/a".</summary>
    public string Kda => $"{Kills}/{Deaths}/{Assists}";

    /// <summary>KDA-ratio ((kills+assists)/deaths); deaths=0 behandles som 1 for at undgå division med nul.</summary>
    public double KdaRatio => (Kills + Assists) / (double)Math.Max(1, Deaths);
}
