namespace LolMatchAlert.Core.Riot;

/// <summary>Resultat fra account-v1: en konto identificeret ved sin PUUID.</summary>
public sealed record RiotAccount(string Puuid, string GameName, string TagLine)
{
    public RiotId RiotId => new(GameName, TagLine);
}
