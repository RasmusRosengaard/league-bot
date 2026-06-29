using LolMatchAlert.Core.Riot;

namespace LolMatchAlert.Core.Subscriptions;

/// <summary>
/// En unik konto der skal polles (distinkt på tværs af alle abonnementer),
/// med regionen allerede oversat til routing.
/// </summary>
public sealed record AccountToPoll(string Puuid, string GameName, string TagLine, Region Region)
{
    public RiotId RiotId => new(GameName, TagLine);
}
